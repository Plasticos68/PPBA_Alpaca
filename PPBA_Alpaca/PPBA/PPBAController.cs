using System.IO.Ports;
public class PPBAController
{
    private readonly SerialPort _port;

    public PPBAController(string portName)
    {
        _port = new SerialPort(portName, 9600);
        _port.Open();
    }

    public bool GetPortState(int portId)
    {
        _port.WriteLine($"GET PORT{portId}");
        var response = _port.ReadLine();
        return response.Contains("ON");
    }

    public void SetPortState(int portId, bool state)
    {
        var command = state ? $"ON PORT{portId}" : $"OFF PORT{portId}";
        _port.WriteLine(command);
        var ack = _port.ReadLine();
        if (!ack.Contains("OK"))
            throw new InvalidOperationException($"Failed to set port {portId}");
    }

    public void Dispose() => _port?.Close();
}
