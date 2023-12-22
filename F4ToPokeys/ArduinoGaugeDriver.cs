using CommandMessenger;
using CommandMessenger.Transport.Serial;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows.Documents;

namespace F4ToPokeys
{
    public class ArduinoGaugeDevice
    {
        public string SerialNumber { get; set; }
        public string PortName { get; set; }
    }

    public class ArduinoGaugeDriver : IDisposable
    {
        enum Command
        {
            HandshakeRequest,
            HandshakeResponse,
            SetTarget,
            Status
        }

        SerialTransport serialTransport;
        CmdMessenger cmdMessenger;

        public static int BAUD_RATE = 57600;

        public static List<ArduinoGaugeDevice> GetConnectedDevices()
        {
            List<ArduinoGaugeDevice> devices = new List<ArduinoGaugeDevice>();

            string[] ports = SerialPort.GetPortNames();
            foreach (var portname in ports)
            {
                Debug.WriteLine("Attempting to connect to " + portname);
                var device = DetectArduinoGaugeDevice(portname);
                if (device != null)
                    devices.Add(device);
            }

            return devices;
        }

        private static ArduinoGaugeDevice DetectArduinoGaugeDevice(string portname)
        {
            SerialTransport serialTransport = new SerialTransport()
            {
                CurrentSerialSettings = { PortName = portname, BaudRate = BAUD_RATE, DtrEnable = false }
            };
            CmdMessenger cmdMessenger = new CmdMessenger(serialTransport);

            try
            {
                cmdMessenger.Connect();

                var command = new SendCommand((int)Command.HandshakeRequest, (int)Command.HandshakeResponse, 1000);
                var handshakeResultCommand = cmdMessenger.SendCommand(command);

                if (handshakeResultCommand.Ok)
                {
                    // read response
                    var software = handshakeResultCommand.ReadStringArg();
                    var serialNumber = handshakeResultCommand.ReadStringArg();

                    if (software.Contains("GaugeDriver"))
                    {
                        // create device
                        ArduinoGaugeDevice device = new ArduinoGaugeDevice()
                        {
                            PortName = portname,
                            SerialNumber = serialNumber
                        };

                        return device;
                    }
                    else
                    {
                        Debug.WriteLine("Connected to Arduino, but not an ArduinoGauge device.");
                        return null;
                    }
                }
                else
                {
                    Debug.WriteLine("Handshake FAILED");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
            finally
            {
                cmdMessenger.Disconnect();
                cmdMessenger.Dispose();
                serialTransport.Dispose();
            }
        }

        public byte StepperMotorCount => 12;
        public string SerialNumber { get; private set; }

        public void SetTarget(byte stepperMotorIndex, ushort targetSteps)
        {
            var command = new SendCommand((int)Command.SetTarget);
            command.AddArgument(stepperMotorIndex);
            command.AddArgument(targetSteps);

            cmdMessenger.SendCommand(command);
        }

        #region Construction
        public ArduinoGaugeDriver(ArduinoGaugeDevice device)
        {
            SerialNumber = device.SerialNumber;

            serialTransport = new SerialTransport()
            {
                CurrentSerialSettings = { PortName = device.PortName, BaudRate = BAUD_RATE, DtrEnable = false }
            };

            cmdMessenger = new CmdMessenger(serialTransport);

            cmdMessenger.Connect();
        }

        public void Dispose()
        {
            cmdMessenger.Disconnect();
            cmdMessenger.Dispose();
            serialTransport.Dispose();
        }
        #endregion
    }
}