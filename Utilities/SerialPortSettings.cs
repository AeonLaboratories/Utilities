using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Xml.Serialization;

namespace Utilities
{
    public class SerialPortSettings
    {
        public static string[] PortNameValues;
        public static int[] BaudRateValues = { 115200, 57600, 38400, 28800, 19200, 14400, 9600, 4800, 2400, 1200, 600, 300 };
		public static int[] DataBitsValues = { 8, 7 };
        public static Parity[] ParityValues = (Parity[])System.Enum.GetValues(typeof(Parity));
        public static StopBits[] StopBitValues = (StopBits[])System.Enum.GetValues(typeof(StopBits));
        public static Handshake[] HandShakeValues = (Handshake[])System.Enum.GetValues(typeof(Handshake));
        public static string[] PhysicalPorts;

        static SerialPortSettings()
        {
            string[] defaultPortNames = { "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM10" };
            PhysicalPorts = SerialPort.GetPortNames();
            PortNameValues = (PhysicalPorts == null || PhysicalPorts.Length == 0) ?
                defaultPortNames : PhysicalPorts;
        }

        string _PortName;
		[XmlAttribute]
		public string PortName
        {
            get { return _PortName; }
            set
            {
                for (int i = 0; i < PortNameValues.Length; i++)
                {
                    if (value == PortNameValues[i])
                    {
                        _PortName = value;
                        break;
                    }
                }
            }
        }

        int _BaudRate;
		public int BaudRate
        {
            get { return _BaudRate; }
            set
            {
                for (int i = 0; i < BaudRateValues.Length; i++)
                {
                    if (value == BaudRateValues[i])
                    {
                        _BaudRate = value;
                        break;
                    }
                }
            }
        }

		public Parity Parity { get; set; }

        int _DataBits;
		public int DataBits
        {
            get { return _DataBits; }
            set
            {
                for (int i = 0; i < DataBitsValues.Length; i++)
                {
                    if (value == DataBitsValues[i])
                    {
                        _DataBits = value;
                        break;
                    }
                }
            }
        }

		public StopBits StopBits { get; set; }
		public Handshake Handshake { get; set; }
        
        public SerialPortSettings(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, Handshake handshake)
        {
            PortName = portName;
            BaudRate = baudRate;
            Parity = parity;
            DataBits = dataBits;
            StopBits = stopBits;
            Handshake = handshake;
        }

        public SerialPortSettings(string portName, int baudRate) : 
			this(portName, baudRate, 
				System.IO.Ports.Parity.None, 
				8, System.IO.Ports.StopBits.One, 
				System.IO.Ports.Handshake.None)
        {}

        public SerialPortSettings(string portName) : 
			this(portName, 19200)
        {}

        public SerialPortSettings() :
			this(PortNameValues[0])
		{}

		public SerialPortSettings Clone()
		{
			return new SerialPortSettings(PortName, BaudRate, Parity, DataBits, StopBits, Handshake);
		}
    }
}
