using ASCOM.Alpaca;
using PPBA_Alpaca.Alpaca;
using PPBA_Alpaca.AppConfig;
using PPBA_Alpaca.PPBA;
using PPBA_Alpaca.Properties;
using System;

class Program
{
    static void Main(string[] args)
    {
        var config = Settings.Load(); // Load from file or registry
        var controller = new PPBAController(config.SerialPort);
        var switchDevice = new SwitchDevice(controller, config);

        var server = new AlpacaServer();
        server.AddDevice(switchDevice);
        server.Start();

        Console.WriteLine("PPBA Alpaca driver running. Press Enter to exit.");
        Console.ReadLine();
    }
}
