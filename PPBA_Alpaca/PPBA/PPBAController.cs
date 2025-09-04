using System;
using System.IO.Ports;
using PPBA_Alpaca.Logging;

namespace PPBA_Alpaca.Utils
{
    public class PPBAController : IDisposable
    {
        private readonly SerialPort _serialPort;
        private readonly Logger _logger;

        // Constants for the initial handshake
        private readonly string _initialPing = "P#";
        private readonly string _initialResponse = "PPBA_OK";

        private bool _disposed;

        public PPBAController(SerialPort serialPort, Logger logger)
        {
            _serialPort = serialPort
                ?? throw new ArgumentNullException(nameof(serialPort));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sends the initial ping command ("P#") and checks for "PPBA_OK".
        /// Returns true if the expected response is received within the timeout.
        /// </summary>
        public bool SendInitialPing(int timeoutMs = 1000)
        {
            _serialPort.ReadTimeout = timeoutMs;
            _serialPort.WriteTimeout = timeoutMs;

            _logger.LogInfo($"Sending initial ping: {_initialPing}");
            _serialPort.WriteLine(_initialPing);

            try
            {
                var response = _serialPort.ReadLine()?.Trim();
                if (response == _initialResponse)
                {
                    _logger.LogInfo("Received expected initial response.");
                    return true;
                }

                _logger.LogWarn($"Unexpected initial response: {response}");
                return false;
            }
            catch (TimeoutException tex)
            {
                _logger.LogError("Initial ping timed out.", tex);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during initial ping.", ex);
                return false;
            }
        }

        public string SendCommand(string command, int timeoutMs)
        {
            string _response;
            _response = "PPBA_OK";
            return _response;
        }

        /// <summary>
        /// Expose the constants if needed elsewhere.
        /// </summary>
        public string InitialPing => _initialPing;
        public string InitialResponse => _initialResponse;

        public void Dispose()
        {
            if (_disposed)
                return;

            // Controller doesn't own the port—don't dispose it here.
            _disposed = true;
        }
    }
}
