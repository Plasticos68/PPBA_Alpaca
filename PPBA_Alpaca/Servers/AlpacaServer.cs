using System;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PPBA_Alpaca.Logging;

namespace PPBA_Alpaca.Servers

{
    public class AlpacaServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Logger _logger;
        private readonly ConcurrentDictionary<string, IAlpacaDevice> _devices
            = new ConcurrentDictionary<string, IAlpacaDevice>(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource _cts;
        private Task _listenerTask;
        private readonly MdnsAdvertiser _mdns;
        private readonly string _ipAddress;
        private readonly int _port;

        public AlpacaServer(string ipAddress, int port, Logger logger)
        {
            if (string.IsNullOrWhiteSpace(ipAddress)) throw new ArgumentNullException(nameof(ipAddress));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _ipAddress = ipAddress;
            _port = port;

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{ipAddress}:{port}/");

            _mdns = new MdnsAdvertiser();
        }

        public void RegisterDevice(IAlpacaDevice device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            string key = MakeKey(device.DeviceType, device.DeviceNumber);
            if (!_devices.TryAdd(key, device))
                throw new InvalidOperationException(
                    $"Device {device.DeviceType} #{device.DeviceNumber} is already registered.");

            _logger.LogInfo($"Registered {device.DeviceType} #{device.DeviceNumber}");
        }

        public void Start()
        {
            if (_listener.IsListening) return;

            _cts = new CancellationTokenSource();
            _listener.Start();
            _listenerTask = Task.Run(() => ListenLoopAsync(_cts.Token));
            _logger.LogInfo("Alpaca HTTP listener started.");

            try
            {
                _logger.LogInfo("Starting mDNS advertisement...");
                _mdns.Start("PPBA_Server", _port);
                _logger.LogInfo("mDNS advertisement started.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to start mDNS advertisement", ex);
            }
        }

        public void Stop()
        {
            if (!_listener.IsListening) return;

            _cts.Cancel();
            _listener.Stop();
            _listener.Close();
            _logger.LogInfo("Alpaca HTTP listener stopped.");

            try
            {
                _mdns.Stop();
                _logger.LogInfo("mDNS advertisement stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to stop mDNS advertisement", ex);
            }
        }

        private async Task ListenLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync().ConfigureAwait(false);
                    _ = Task.Run(() => ProcessRequest(context), ct);
                }
                catch (HttpListenerException) when (ct.IsCancellationRequested)
                {
                    // listener was stopped, exit loop
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error accepting HTTP request", ex);
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                var segments = request.Url.AbsolutePath
                    .Trim('/')
                    .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length != 3)
                {
                    WriteJson(response, 400, new { Error = "Invalid path format" });
                    return;
                }

                string deviceType = segments[0];
                if (!int.TryParse(segments[1], out int deviceNumber))
                {
                    WriteJson(response, 400, new { Error = "Invalid device number" });
                    return;
                }

                string action = segments[2];
                string key = MakeKey(deviceType, deviceNumber);

                if (!_devices.TryGetValue(key, out var device))
                {
                    WriteJson(response, 404, new { Error = "Device not found" });
                    return;
                }

                //string methodName = action.ToLowerInvariant() switch
                //{
                //    "on" => "TurnOn",
                //    "off" => "TurnOff",
                //    "init" => "Initialize",
                //    _ => null
                //};

                // Map action → method name
                string methodName = null;
                switch (action.ToLowerInvariant())
                {
                    case "on":
                        methodName = "TurnOn";
                        break;
                    case "off":
                        methodName = "TurnOff";
                        break;
                    case "init":
                        methodName = "Initialize";
                        break;
                    default:
                        methodName = null;
                        break;
                }



                if (methodName == null)
                {
                    WriteJson(response, 404, new { Error = "Unknown action" });
                    return;
                }

                var method = device.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                if (method == null)
                {
                    WriteJson(response, 500, new { Error = "Action not implemented" });
                    return;
                }

                object[] parameters = method.GetParameters().Length == 1
                    ? new object[]
                    {
                    request.QueryString["timeout"] != null
                        ? int.Parse(request.QueryString["timeout"])
                        : 1000
                    }
                    : Array.Empty<object>();

                bool result = (bool)method.Invoke(device, parameters);
                WriteJson(response, 200, new { Value = result });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception routing request", ex);
                WriteJson(response, 500, new { Error = ex.Message });
            }
        }

        private void WriteJson(HttpListenerResponse response, int statusCode, object payload)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(payload);
            byte[] data = Encoding.UTF8.GetBytes(json);

            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = data.Length;
            response.OutputStream.Write(data, 0, data.Length);
            response.OutputStream.Close();
        }

        private static string MakeKey(string deviceType, int deviceNumber) =>
            $"{deviceType.ToLowerInvariant()}:{deviceNumber}";

        public void Dispose()
        {
            Stop();
        }
    }
}
