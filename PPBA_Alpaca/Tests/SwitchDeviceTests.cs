using ASCOM.DeviceInterface;
using PPBA_Alpaca.Properties;

public class SwitchDevice : ISwitchV2
{
    private readonly PPBAController _controller;
    private readonly Settings _config;

    public SwitchDevice(PPBAController controller, Settings config)
    {
        _controller = controller;
        _config = config;
    }

    public bool GetSwitch(int id) => _controller.GetPortState(id);
    public void SetSwitch(int id, bool state) => _controller.SetPortState(id, state);
    public string SwitchName(int id) => $"Power Port {id}";
    public short MaxSwitch => 4; // Or however many ports PPBA exposes

    // Implement other ISwitchV2 members as needed
}
