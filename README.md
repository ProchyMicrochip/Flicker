# Flickermeter

> Library for communicating with Flickermeter, developed by Jakub Procházka at Technical university of Liberec

## Usage
`PortFinder.Find(string portName)` - Returns type of port

`Flickermeter.Start()` - Starts serial ports

`Flickermeter.Inicialize()` - puts device in measuring mode

`Flickermeter.Measure(uint samples, Gain gain, Time time)` - Starts measurement with selected parameters, note that samples should be multiple of 1000

For examples see, `Flicker.Tests`
