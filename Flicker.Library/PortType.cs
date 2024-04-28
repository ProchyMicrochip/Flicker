using System.IO.Ports;

namespace Flicker.Library;

public static class PortTypeFinder
{
    public static PortType Find(string portName)
    {
        try
        {
            var serialPort = new SerialPort(portName){ReadTimeout = 1000};
            serialPort.Open();
            serialPort.WriteLine("info");
            var response = serialPort.ReadLine();
            serialPort.Close();
            serialPort.Dispose();
            return response switch
            {
                "Config interface of Flickermeter" => PortType.Config,
                "Data interface of Flickermeter" => PortType.Data,
                _ => PortType.None
            };
        }
        catch (Exception)
        {
            return PortType.None;
        }
    }
    public enum PortType
    {
        None,
        Config,
        Data
    }
    
}

