// File: Devices/SwitchDevice.cs
using System;
using PPBA_Alpaca.Logging;
using PPBA_Alpaca.Servers;
using PPBA_Alpaca.Utils;

//
namespace PPBA_Alpaca.Devices
{
    /// <summary>
    /// Represents one relay‐switch channel on the PPBA box.
    /// Implements IAlpacaDevice so AlpacaServer can discover and route to it.
    /// </summary>
    public class SwitchDevice : IAlpacaDevice, IDisposable
    {
        private readonly PPBAController _controller;
        private readonly Logger _logger;
        private readonly object _sync = new object();
        private bool _disposed;

        public int DeviceNumber { get; }
        public string DeviceType => "Switch";

        // PPBA protocol commands
        private string InitialPingCommand => "P#";
        private string ExpectedPingResponse => "PPBA_OK";
        private string RelayOnCommand => $"O{DeviceNumber}#";
        private string RelayOffCommand => $"F{DeviceNumber}#";

        public SwitchDevice(PPBAController controller, int deviceNumber, Logger logger)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            DeviceNumber = deviceNumber;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handshake with the box: send "P#" and expect "PPBA_OK".
        /// Should be called once immediately after opening the port.
        /// </summary>
        public bool Initialize(int timeoutMs = 1000)
        {
            _logger.LogInfo($"[Switch #{DeviceNumber}] Initializing (ping)…");
            bool ok = _controller.SendInitialPing(timeoutMs);
            if (!ok)
                _logger.LogError($"[Switch #{DeviceNumber}] Ping failed – no PPBA_OK.", null);
            return ok;
        }

        /// <summary>
        /// Turn this relay on.
        /// </summary>
        public bool TurnOn(int timeoutMs = 1000)
        {
            return SendRelayCommand(RelayOnCommand, "ON", timeoutMs);
        }

        /// <summary>
        /// Turn this relay off.
        /// </summary>
        public bool TurnOff(int timeoutMs = 1000)
        {
            return SendRelayCommand(RelayOffCommand, "OFF", timeoutMs);
        }

        private bool SendRelayCommand(string command, string action, int timeoutMs)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SwitchDevice));

            lock (_sync)
            {
                _logger.LogInfo($"[Switch #{DeviceNumber}] Sending {action} command: {command}");

                // Use a generic SendCommand on the controller (you can implement this in PPBAController)
                var response = _controller.SendCommand(command, timeoutMs)?.Trim();

                if (string.Equals(response, ExpectedPingResponse, StringComparison.Ordinal))
                {
                    _logger.LogInfo($"[Switch #{DeviceNumber}] {action} succeeded.");
                    return true;
                }

                _logger.LogWarn(
                    $"[Switch #{DeviceNumber}] Unexpected response to {action}: “{response ?? "<null>"}”"
                );
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            // no unmanaged resources owned here
            _disposed = true;
        }
    }
}


