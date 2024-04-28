using Flicker.Library;

namespace Flicker.Tests;

public class SelfTest
{
    [Fact]
    public async void SimpleMeasurementTest()
    {
        var flicker = new Library.Flicker(data: "COM10",cfg: "COM11");
        flicker.Start();
        flicker.Inicialize();
        var data = await flicker.Measure(16000, Gain.X16, Time.Ms4);
        flicker.Stop();
        flicker.Dispose();
        Assert.True(data.Valid);
        Assert.Equal(16000,data.DataPoints.Count);
    }

    [Fact]
    public void DetectPortType()
    {
        Assert.Equal(PortTypeFinder.PortType.Data,PortTypeFinder.Find("COM10"));
        Assert.Equal(PortTypeFinder.PortType.Config,PortTypeFinder.Find("COM11"));
    }
}