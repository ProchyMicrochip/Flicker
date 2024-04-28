using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Management;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Management.Infrastructure;
using static System.Int32;

namespace Flicker.Library;

using System.IO.Ports;

public class Flicker : IDisposable
{
    private readonly SerialPort _data;
    private readonly SerialPort _cfg;
    public bool Inicialized { get; private set; } = false;
    public bool Measuring { get; private set; } = false;
    private byte[]? _bytes;
    private int _index;

    public Flicker(string data, string cfg)
    {
        _data = new SerialPort(data)
        {
            BaudRate = 921600, ReadBufferSize = MaxValue -1
        };
        _cfg = new SerialPort(cfg) { ReadTimeout = -1 };
    }

    private void DataOnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        var serial = sender as SerialPort;
        var bytes = serial?.BytesToRead;
        if (bytes is null or 0) return;
        if (serial != null) _index += serial.Read(_bytes, _index, bytes.Value);
    }

    public void Inicialize()
    {
        Check();
        _cfg.DiscardInBuffer();
        _cfg.WriteLine("self");
        if (_cfg.ReadLine() == "Self test OK")
        {
            Inicialized = true;
            return;
        }
        _cfg.WriteLine("init");
        if (_cfg.ReadLine() != "Device Inicialization") throw new Exception("Unable to Inicialize device");
        _cfg.WriteLine("start");
        if (_cfg.ReadLine() != "Sensor started") throw new Exception("Unable to start As73211");
        _cfg.WriteLine("self");
        if (_cfg.ReadLine() != "Self test OK") throw new Exception("Unknown Error on I2C bus");
        Inicialized = true;
    }

    public async Task<Measurement> Measure(uint samples, Gain gain = Gain.X8, Time time = Time.Ms2)
    {
        if (Measuring) throw new Exception("Already measuring");
        var measurement = new Measurement() { Samples = samples, Gain = gain, Time = time };
        try
        {
            if (!ReadyToMeasure()) throw new Exception("Device not Started or Inicialized");
            _index = 0;
            _bytes = new byte[(samples + 100) * 12];
            _data.DiscardInBuffer();
            _data.DataReceived += DataOnDataReceived;
            _cfg.DiscardInBuffer();
            SetSamples(samples);
            if (_cfg.ReadLine() != $"Sample Updated 0x{samples:X8}") throw new Exception("Unable to update Samples");
            SetTime(time);
            if (_cfg.ReadLine() != $"Time Updated 0x{time:X}") throw new Exception("Unable to update time");
            SetGain(gain);
            if (_cfg.ReadLine() != $"Gain Updated 0x{gain:X}") throw new Exception("Unable to update gain");
            var stopWatch = Stopwatch.StartNew();
            Measuring = true;
            _cfg.WriteLine("run");
            if (_cfg.ReadLine() != "Measurement started") throw new Exception("Error when starting Measurement");
            Debug.WriteLine("Measurement started");
            while (_cfg.ReadLine() != "Measurement ended")
            {
            }
            stopWatch.Stop();
            await Task.Delay(1000);
            Debug.WriteLine("Measurement ended");
            Measuring = false;
            _data.DataReceived -= DataOnDataReceived;
            measurement.Duration = stopWatch.Elapsed;
            DecodeData(ref measurement);
            
        }
        catch (Exception e)
        {
            DecodeData(ref measurement);
            Console.WriteLine(e);
            throw;
        }
        return measurement;
    }

    private void DecodeData(ref Measurement measurement)
    {
        if (_bytes == null) throw new NullReferenceException("Read buffer is missing");
        measurement.Valid = true;
        for (var readInx = 0; readInx < _index; readInx += 12)
        {
            var datapoint = new DataPoint(
                ((uint)_bytes[readInx + 3] << 24) | ((uint)_bytes[readInx + 2] << 16) |
                ((uint)_bytes[readInx + 1] << 8) | _bytes[readInx],
                (ushort)((_bytes[readInx + 5] << 8) | _bytes[readInx + 4]),
                (ushort)((_bytes[readInx + 7] << 8) | _bytes[readInx + 6]),
                (ushort)((_bytes[readInx + 9] << 8) | _bytes[readInx + 8]),
                (ushort)((_bytes[readInx + 11] << 8) | _bytes[readInx + 10]));
            measurement.DataPoints.Add(datapoint);
            if (((datapoint.Index >> 16) ^ (datapoint.Index & 0xFFFF) ^ datapoint.X ^ datapoint.Y ^ datapoint.Z) !=
                datapoint.Checksum)
            {
                measurement.Valid = false;
            }
        }
        //measurement.Valid = measurement.DataPoints.Count > measurement.Samples+20;
        //if(measurement.Valid == false) return;
        //var samples = measurement.Samples;
    }
    public void Start()
    {
        if (_data.IsOpen == false) _data.Open();
        if (_cfg.IsOpen == false) _cfg.Open();
    }

    private void SetTime(Time time)
    {
        Check();
        var text = (char)('0' + (byte)time);
        _cfg.WriteLine($"Time 0x0{text}");
    }

    private void SetGain(Gain gain)
    {
        Check();
        var text = (char)('0' + (byte)gain);
        _cfg.WriteLine($"Gain 0x0{text}");
    }

    private void SetSamples(uint samples)
    {
        Check();
        var text = new StringBuilder(samples.ToString("X8"));
        for (var i = 0; i < 8; i++)
        {
            text[i] = text[i] > '9' ? (char)(text[i] - 7) : text[i] ;
        }
        _cfg.WriteLine($"Sample 0x{text}");
    }

    public void Stop()
    {
        if (_cfg.IsOpen) _cfg.Close();
        if (_data.IsOpen) _data.Close();
    }

    public void Dispose()
    {
        Stop();
        _data.Dispose();
        _cfg.Dispose();
    }

    private void Check()
    {
        if (_cfg.IsOpen == false) throw new PortClosedExcepion();
    }
    private bool ReadyToMeasure()
    {
        if (_cfg.IsOpen == false) return false;
        if (Inicialized == false) return false;
        return !Measuring;
    }
}

/// <summary>
/// Sensitivity Multiplier
/// </summary>
public enum Gain : byte
{
    X2048 = 0,
    X1024 = 1,
    X512 = 2,
    X256 = 3,
    X128 = 4,
    X64 = 5,
    X32 = 6,
    X16 = 7,
    X8 = 8,
    X4 = 9,
    X2 = 10,
    X1 = 16
}

/// <summary>
/// Single point measurement time
/// </summary>
public enum Time : byte
{
    /**8 000Hz*/
    //Ms1 = 0,

    /**4 000Hz*/
    Ms2 = 1,

    /**2 000Hz*/
    Ms4 = 2,

    /**1 000Hz*/
    Ms8 = 3,

    /**500 Hz*/
    Ms16 = 4,

    /**250 Hz*/
    Ms32 = 5,

    /**125 Hz*/
    Ms64 = 6,

    /**62,5 Hz*/
    Ms128 = 7,

    /**31,25 Hz*/
    Ms256 = 8,

    /**15,625 Hz*/
    Ms512 = 9,

    /**7,813 Hz*/
    Ms1024 = 10,

    /**3,906 Hz*/
    Ms2048 = 11,

    /**1,953 Hz*/
    Ms4096 = 12,

    /**0,976 Hz*/
    Ms8192 = 13,

    /**0.488 Hz*/
    Ms16384 = 14,
}

public class PortClosedExcepion : Exception;