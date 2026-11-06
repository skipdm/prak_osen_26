namespace PackingApp;

public class ServiceInfo
{
    public string Address;
    public bool IsWorking = true;
    public string Error = "";

    public ServiceInfo(string address)
    {
        Address = address;
    }
}
