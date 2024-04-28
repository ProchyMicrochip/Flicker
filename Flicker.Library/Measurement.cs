namespace Flicker.Library;

public class Measurement
{
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public TimeSpan Duration { get; set; }
    public int CalculatedRate => (int)(DataPoints.Count / Duration.TotalSeconds);
    public List<DataPoint> DataPoints { get; set; } = [];
    public bool Valid { get; set; }
    public uint Samples { get; set; }
    public Gain Gain { get; set; }
    public Time Time { get; set; }
}
public readonly record struct DataPoint(uint Index, ushort X, ushort Y, ushort Z, ushort Checksum);