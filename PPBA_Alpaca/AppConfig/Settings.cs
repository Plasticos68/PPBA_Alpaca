namespace PPBA_Alpaca.AppConfig
{
    public class Settings
    {
        public string SerialPort { get; set; } = "COM3";

        public static Settings Load()
        {
            // Load from file or registry
            return new Settings { SerialPort = "COM3" };
        }
    }
}
