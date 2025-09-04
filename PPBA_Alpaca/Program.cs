// File: Program.cs
//using PPBA_Alpaca.Alpaca;
using PPBA_Alpaca.AppConfig;
using PPBA_Alpaca.Devices;
using PPBA_Alpaca.Logging;
using PPBA_Alpaca.PPBA;
using PPBA_Alpaca.Properties;
using PPBA_Alpaca.Servers;
using PPBA_Alpaca.Utils;
using System;
using System.IO.Ports;
using System.Threading;
using Settings = PPBA_Alpaca.AppConfig.Settings;

namespace PPBA_Alpaca
{
    static class Program
    {
        // A simple wait handle to block until Ctrl+C is pressed
        private static readonly ManualResetEventSlim ExitEvent = new ManualResetEventSlim(false);

        static void Main(string[] args)
        {
            Logger logger = null;
            SerialPort serialPort = null;
            AlpacaServer server = null;

            try
            {
                // 1. Load settings from App.config
                var cfg = Settings.Default;
                logger = new Logger(nameof(Program));
                logger.LogInfo("Starting PPBA Alpaca Server…");

                // 2. Read connection parameters
                string ipAddress = cfg.ServerIPAddress;
                int port = cfg.ServerPort;
                string comPortName = cfg.ComPortName;
                int baudRate = cfg.BaudRate;
                int deviceNum = cfg.DeviceNumber;

                logger.LogInfo(
                    $"Configuration – IP: {ipAddress}, Port: {port}, " +
                    $"COM: {comPortName}@{baudRate}, Device#: {deviceNum}"
                );

                // 3. Open and configure the serial port
                serialPort = new SerialPort(comPortName, baudRate)
                {
                    ReadTimeout = cfg.ReadTimeoutMs,
                    WriteTimeout = cfg.WriteTimeoutMs
                };
                serialPort.Open();
                logger.LogInfo($"Serial port {comPortName} opened.");

                // 4. Wire up the PPBA controller and Alpaca switch device
                var controller = new PPBAController(serialPort, logger);
                var switchDevice = new SwitchDevice(controller, deviceNum, logger);

                // Perform the P# → PPBA_OK handshake via SwitchDevice.Initialize()
                if (!switchDevice.Initialize(timeoutMs: cfg.ReadTimeoutMs))
                {
                    logger.LogError(
                        $"Initialization failed for SwitchDevice #{deviceNum}. Aborting startup.",
                        null
                    );
                    return;
                }

                // 5. Instantiate and start the Alpaca server, register our device
                server = new AlpacaServer(ipAddress, port, logger);
                server.RegisterDevice(switchDevice);
                server.Start();
                logger.LogInfo($"Alpaca server listening on {ipAddress}:{port}.");

                // 6. Handle graceful shutdown on Ctrl+C
                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;   // prevent immediate termination
                    ExitEvent.Set();   // signal shutdown
                };

                logger.LogInfo("Press Ctrl+C to exit.");
                ExitEvent.Wait();     // block until CancelKeyPress

                // 7. Stop server
                logger.LogInfo("Shutdown requested. Stopping server…");
                server.Stop();
            }
            catch (Exception ex)
            {
                // Catch-all to ensure we log any unhandled exception
                logger?.LogError("Fatal exception in Main()", ex);
            }
            finally
            {
                // 8. Clean up resources
                serialPort?.Dispose();
                server?.Dispose();
                logger?.Dispose();
            }
        }
    }
}

////using PPBA_Alpaca.Alpaca;
//using PPBA_Alpaca.Servers;
//using PPBA_Alpaca.AppConfig;
//using PPBA_Alpaca.Devices;
//using PPBA_Alpaca.Logging;
//using PPBA_Alpaca.PPBA;
//using PPBA_Alpaca.Properties;
//using PPBA_Alpaca.Utils;
//using System;
//using System.IO.Ports;
//using System.Threading;
//using Settings = PPBA_Alpaca.AppConfig.Settings;

//namespace PPBA_Alpaca
//{
//    static class Program
//    {
//        // A simple wait handle to block until Ctrl+C is pressed
//        private static readonly ManualResetEventSlim ExitEvent = new ManualResetEventSlim(false);

//        static void Main(string[] args)
//        {
//            Logger logger = null;
//            SerialPort serialPort = null;
//            AlpacaServer server = null;

//            try
//            {
//                // 1. Load settings from App.config
//                var cfg = Settings.Default;
//                logger = new Logger(nameof(Program));
//                logger.LogInfo("Starting PPBA Alpaca Server…");

//                // 2. Read connection parameters
//                string ipAddress = cfg.ServerIPAddress;
//                int port = cfg.ServerPort;
//                string comPortName = cfg.ComPortName;
//                int baudRate = cfg.BaudRate;
//                int deviceNum = cfg.DeviceNumber;

//                logger.LogInfo(
//                    $"Configuration – IP: {ipAddress}, Port: {port}, " +
//                    $"COM: {comPortName}@{baudRate}, Device#: {deviceNum}"
//                );

//                // 3. Open and configure the serial port
//                serialPort = new SerialPort(comPortName, baudRate)
//                {
//                    ReadTimeout = cfg.ReadTimeoutMs,
//                    WriteTimeout = cfg.WriteTimeoutMs
//                };
//                serialPort.Open();
//                logger.LogInfo($"Serial port {comPortName} opened.");

//                // 4. Wire up the PPBA controller and Alpaca device
//                var controller = new PPBAController(serialPort, logger);
//                if (!controller.SendInitialPing())
//                {
//                    logger.LogError("Failed to receive PPBA_OK. Aborting startup.", null);
//                    return;
//                }

//                var switchDevice = new SwitchDevice(controller, deviceNum, logger);

//                // 5. Start the Alpaca server
//                server = new AlpacaServer(ipAddress, port, logger);
//                server.RegisterDevice(switchDevice);
//                server.Start();
//                logger.LogInfo($"Alpaca server listening on {ipAddress}:{port}.");

//                // 6. Handle graceful shutdown on Ctrl+C
//                Console.CancelKeyPress += (s, e) =>
//                {
//                    e.Cancel = true;        // Prevent the process from terminating immediately
//                    ExitEvent.Set();        // Signal shutdown
//                };

//                logger.LogInfo("Press Ctrl+C to exit.");
//                ExitEvent.Wait();          // Block here until CancelKeyPress

//                // 7. Stop server
//                logger.LogInfo("Shutdown requested. Stopping server…");
//                server.Stop();
//            }
//            catch (Exception ex)
//            {
//                // Catch-all to ensure we log any unhandled exception
//                logger?.LogError("Fatal exception in Main()", ex);
//            }
//            finally
//            {
//                // 8. Clean up resources
//                serialPort?.Dispose();
//                server?.Dispose();
//                logger?.Dispose();
//            }
//        }
//    }
//}


