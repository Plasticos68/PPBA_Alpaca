// File: AlpacaServer/IAlpacaDevice.cs
namespace PPBA_Alpaca.Servers
{
    /// <summary>
    /// Contract for any device exposed via our AlpacaServer.
    /// DeviceType and DeviceNumber form the unique path for JSON‐RPC calls.
    /// </summary>
    public interface IAlpacaDevice
    {
        /// <summary>
        /// Numeric identifier within a device category (e.g. relay #1, relay #2).
        /// </summary>
        int DeviceNumber { get; }

        /// <summary>
        /// Alpaca “device type” name (e.g. "Switch", "Dome", "CCD").
        /// </summary>
        string DeviceType { get; }

        /// <summary>
        /// Perform any handshakes or setup before the server begins routing calls.
        /// Called once at startup; return false on failure.
        /// </summary>
        bool Initialize(int timeoutMs = 1000);
    }
}


