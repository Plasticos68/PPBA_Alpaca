using System;
using System.Configuration;
using PPBA_Alpaca.Logging;

namespace PPBA_Alpaca.AppConfig
{
    public static class Settings
    {
        // Exposes an instance‐style API under the familiar "Default" property
        public static SettingsWrapper Default { get; } = new SettingsWrapper();

        // Internal wrapper that forwards to the static helpers
        public sealed class SettingsWrapper
        {
            public string ServerIPAddress => Settings.ServerIPAddress;
            public int ServerPort => Settings.ServerPort;
            public string ComPortName => Settings.ComPortName;
            public int BaudRate => Settings.BaudRate;
            public int DeviceNumber => Settings.DeviceNumber;
            public int ReadTimeoutMs => Settings.ReadTimeoutMs;
            public int WriteTimeoutMs => Settings.WriteTimeoutMs;
            public LogLevel MinLogLevel => Settings.MinLogLevel;
        }

        // Your original static properties with defaults
        public static string ServerIPAddress => GetString("ServerIPAddress", "127.0.0.1");
        public static int ServerPort => GetInt("ServerPort", 11111);
        public static string ComPortName => GetString("ComPortName", "COM1");
        public static int BaudRate => GetInt("BaudRate", 9600);
        public static int DeviceNumber => GetInt("DeviceNumber", 0);
        public static int ReadTimeoutMs => GetInt("ReadTimeoutMs", 500);
        public static int WriteTimeoutMs => GetInt("WriteTimeoutMs", 500);
        public static LogLevel MinLogLevel => GetEnum("MinLogLevel", LogLevel.Info);

        // Helper readers
        private static string GetString(string key, string defaultVal)
        {
            var val = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(val) ? defaultVal : val;
        }

        private static int GetInt(string key, int defaultVal)
        {
            var val = ConfigurationManager.AppSettings[key];
            return int.TryParse(val, out var result) ? result : defaultVal;
        }

        private static TEnum GetEnum<TEnum>(string key, TEnum defaultVal)
            where TEnum : struct
        {
            var val = ConfigurationManager.AppSettings[key];
            return Enum.TryParse(val, true, out TEnum result) ? result : defaultVal;
        }
    }
}
