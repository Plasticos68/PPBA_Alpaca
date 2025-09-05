using Mono.Zeroconf;

public class MdnsAdvertiser
{
    private RegisterService _service;

    public void Start(string name, int port)
    {
        _service = new RegisterService
        {
            Name = name,
            RegType = "_alpaca._tcp",
            ReplyDomain = "local.",
            Port = (short)port,
            TxtRecord = new TxtRecord
            {
                new TxtRecordItem("DeviceType", "Switch"),
                new TxtRecordItem("DeviceNumber", "0"),
                new TxtRecordItem("Manufacturer", "PPBA"),
                new TxtRecordItem("Version", "1.0")
            }
        };

        _service.Register();
    }

    public void Stop()
    {
        _service?.Dispose();
    }
}


