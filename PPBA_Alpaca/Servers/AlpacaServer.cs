// File: Servers/AlpacaServer.cs
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PPBA_Alpaca.Logging;

namespace PPBA_Alpaca.Servers
//namespace PPBA_Alpaca.Alpaca
{
    /// <summary>
    /// Simple HTTP‐based JSON endpoint router for Alpaca devices.
    /// Maps /{deviceType}/{deviceNumber}/{action} → IAlpacaDevice method calls.
    /// </summary>
    public class AlpacaServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Logger _logger;
        private readonly ConcurrentDictionary<string, IAlpacaDevice> _devices
            = new ConcurrentDictionary<string, IAlpacaDevice>(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource _cts;
        private Task _listenerTask;

        /// <summary>
        /// Create and bind the HTTP listener.
        /// </summary>
        public AlpacaServer(string ipAddress, int port, Logger logger)
        {
            if (string.IsNullOrWhiteSpace(ipAddress)) throw new ArgumentNullException(nameof(ipAddress));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{ipAddress}:{port}/");
        }

        /// <summary>
        /// Register one device for routing. Must implement IAlpacaDevice.
        /// </summary>
        public void RegisterDevice(IAlpacaDevice device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            string key = MakeKey(device.DeviceType, device.DeviceNumber);
            if (!_devices.TryAdd(key, device))
                throw new InvalidOperationException(
                    $"Device {device.DeviceType} #{device.DeviceNumber} is already registered.");

            _logger.LogInfo($"Registered {device.DeviceType} #{device.DeviceNumber}");
        }

        /// <summary>
        /// Start listening for HTTP requests. Non‐blocking.
        /// </summary>
        public void Start()
        {
            if (_listener.IsListening) return;

            _cts = new CancellationTokenSource();
            _listener.Start();
            _listenerTask = Task.Run(() => ListenLoopAsync(_cts.Token));
            _logger.LogInfo("Alpaca HTTP listener started.");
        }

        /// <summary>
        /// Stop listening and clean up.
        /// </summary>
        public void Stop()
        {
            if (!_listener.IsListening) return;

            _cts.Cancel();
            _listener.Stop();
            _listener.Close();
            _logger.LogInfo("Alpaca HTTP listener stopped.");
        }

        private async Task ListenLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                HttpListenerContext context = null;
                try
                {
                    context = await _listener.GetContextAsync().ConfigureAwait(false);
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
                // URL format: /{deviceType}/{deviceNumber}/{action}
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

                // Map action → method name
                string methodName;
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



                //// Map action → method name
                //string methodName = action.ToLowerInvariant() switch
                //{
                //    "on" => "TurnOn",
                //    "off" => "TurnOff",
                //    "init" => "Initialize",
                //    _ => null
                //};

                if (methodName == null)
                {
                    WriteJson(response, 404, new { Error = "Unknown action" });
                    return;
                }

                var method = device.GetType().GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.Instance);

                if (method == null)
                {
                    WriteJson(response, 500, new { Error = "Action not implemented" });
                    return;
                }

                // Prepare parameters: optional timeout query
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

        //private void ProcessRequest(HttpListenerContext context)
        //{
        //    var request = context.Request;
        //    var response = context.Response;
        //    try
        //    {
        //        // URL format: /{deviceType}/{deviceNumber}/{action}
        //        var segments = request.Url.AbsolutePath
        //            .Trim('/')
        //            .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        //        if (segments.Length != 3)
        //            return WriteJson(response, 400, new { Error = "Invalid path format" });

        //        string deviceType = segments[0];
        //        if (!int.TryParse(segments[1], out int deviceNumber))
        //            return WriteJson(response, 400, new { Error = "Invalid device number" });

        //        string action = segments[2];
        //        string key = MakeKey(deviceType, deviceNumber);

        //        if (!_devices.TryGetValue(key, out var device))
        //            return WriteJson(response, 404, new { Error = "Device not found" });

        //        // Map action → method name (case‐insensitive)
        //        string methodName = action.ToLowerInvariant() switch
        //        {
        //            "on" => "TurnOn",
        //            "off" => "TurnOff",
        //            "init" => "Initialize",
        //            _ => null
        //        };

        //        if (methodName == null)
        //            return WriteJson(response, 404, new { Error = "Unknown action" });

        //        MethodInfo method = device.GetType().GetMethod(
        //            methodName,
        //            BindingFlags.Public | BindingFlags.Instance);

        //        if (method == null)
        //            return WriteJson(response, 500, new { Error = "Action not implemented" });

        //        // Invoke method (assumes signature: bool Method(int? timeoutMs = null))
        //        object[] parameters = method.GetParameters().Length == 1
        //            ? new object[] { request.QueryString["timeout"] != null
        //                ? int.Parse(request.QueryString["timeout"])
        //                : 1000 }
        //            : Array.Empty<object>();

        //        bool result = (bool)method.Invoke(device, parameters);

        //        WriteJson(response, 200, new { Value = result });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Exception routing request", ex);
        //        WriteJson(response, 500, new { Error = ex.Message });
        //    }
        //}

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
