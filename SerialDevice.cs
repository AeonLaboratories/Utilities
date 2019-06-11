using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Utilities
{
	[JsonObject(MemberSerialization.OptIn)]
	public class SerialDevice
	{
        public enum RtsModes { Enabled, Disabled, Toggle }

		static long instanceCount = 0;

		#region Fields

		USBSerialPort port;

        bool Transmitting;              // or waiting MillisecondsBetweenMessages or Bytes
		Stopwatch txSw = new Stopwatch();
		Stopwatch rxSw = new Stopwatch();

		Queue<string> CommandQ = new Queue<string>();
		[XmlIgnore] public string LatestCommand;

		Thread txThread;
		AutoResetEvent txThreadSignal = new AutoResetEvent(false);
		Thread prxThread;
		AutoResetEvent prxThreadSignal = new AutoResetEvent(false);

		const int RXBUF_SIZE = 1024;
		byte[] rx;							// serial receive data buffer
		Crc rxCrc = null;

        [XmlIgnore] public LogFile Log;
		[JsonProperty]//, DefaultValue(false)]
		public bool EscapeLoggedData { get; set; } = false;
        string Escape(string s)
        {
            if (EscapeLoggedData)
                return Regex.Escape(s);
            else
                return s;
        }

        [XmlIgnore] public bool Disconnected = true;
		[XmlIgnore] public Action<string> ResponseReceived;
		[XmlIgnore] public bool ErrorCrc = false;
		[XmlIgnore] public bool ErrorBufferOverflow = false;

		[XmlIgnore] public uint ReceiveEvents = 0;
		[XmlIgnore] public uint TotalBytesRead = 0;
		[XmlIgnore] public uint ETXCount = 0;
		[XmlIgnore] public uint ResponseCount = 0;

		const int MAX_CRC_ERRORS = 3;	// triggers a reset
		[XmlIgnore] public uint CRCErrors = 0;
		[XmlIgnore] public uint BufferOverflows = 0;
		[XmlIgnore] public uint Resets = 0;
		[XmlIgnore] public UInt16 rxCrcCode = 0;

		#endregion Fields

		#region Properties

		[JsonProperty]
		public SerialPortSettings PortSettings { get; set; }
		[JsonProperty]
		public int MillisecondsBetweenBytes { get; set; }       // time delay between transmitted characters
		[JsonProperty]
		public int MillisecondsBetweenMessages { get; set; }    // time delay between transmitted codewords (data+CRC+termchar)
		[JsonProperty]
		public CrcOptions CrcConfig { get; set; }               // if CRC is not to be used, leave or set CrcOptions = null

		[JsonProperty]
		public RtsModes RtsMode { get; set; } = RtsModes.Enabled;

        public long MillisecondsSinceLastTx => txSw.ElapsedMilliseconds;
		public long MillisecondsSinceLastRx => rxSw.ElapsedMilliseconds;
		public long LongestSilence => rxSw.Longest;

		public bool Idle => Disconnected || (CommandQ.Count == 0 && !Transmitting);

		public void WaitForIdle() { while (!Idle) Thread.Sleep(5); }

		#endregion Properties

		public SerialDevice(SerialPortSettings portSettings)
		{
			Configure(portSettings);
		}

		public void Configure(SerialPortSettings portSettings)
		{
			PortSettings = portSettings;
		}

		public SerialDevice(string portName, int baudRate) :
			this(new SerialPortSettings(portName, baudRate)) {}

		public SerialDevice(string portName) :
			this(new SerialPortSettings(portName)) {}

		// when empty constructor is used, Configure() and then Initialize() are required before use
		public SerialDevice() { }

		public void Initialize()
		{
			instanceCount++;
			Connect();
			SerialPortMonitor.PortArrived += OnPortConnected;
			SerialPortMonitor.PortRemoved += OnPortRemoved;
		}

		void Connect()
		{
            port = new USBSerialPort
            {
                DiscardNull = false,
                PortName = PortSettings.PortName,
                BaudRate = PortSettings.BaudRate,
                Parity = PortSettings.Parity,
                DataBits = PortSettings.DataBits,
                StopBits = PortSettings.StopBits,
                Handshake = PortSettings.Handshake,
                ReceivedBytesThreshold = 1,     // redundant; the default is 1
                ReadTimeout = 20,
                WriteTimeout = 20,
                Encoding = Utility.ASCII8Encoding
			};

            port.DataReceived += new
				SerialDataReceivedEventHandler(rxDetected);
            port.PinChanged += new
                SerialPinChangedEventHandler(PinChanged);

            try
            {
                port.Open();
                if (port.IsOpen)
                {
					Disconnected = false;

					if (RtsMode == RtsModes.Enabled)
                        port.RtsEnable = true;
                    else if (RtsMode == RtsModes.Disabled)
                        port.RtsEnable = false;
                    else if (RtsMode == RtsModes.Toggle)
                        port.SetRtsControlToggle();
                    port.DtrEnable = true;

					// enable this thread to retrieve SerialPort data asynchronously
					rxThread = new Thread(receive)
					{
						Name = $"SerialDevice {instanceCount} receive",
						IsBackground = true
					};
					rxThread.Start();

					rx = new byte[RXBUF_SIZE];
					if (CrcConfig != null)
						rxCrc = new Crc(CrcConfig);
					if (CrcConfig == null || CrcConfig.OmitTermChar)
						prxThread = new Thread(process_rx2) { Name = $"SerialDevice {instanceCount} process_rx2" };
					else
						prxThread = new Thread(process_rx) { Name = $"SerialDevice {instanceCount} process_rx" };
					prxThread.IsBackground = true;
					prxThread.Start();

					txThread = new Thread(transmit)
					{
						Name = $"SerialDevice {instanceCount} transmit",
						IsBackground = true
					};
					txThread.Start();
				}
			}
			catch { }
		}

		// Disposing the USBSerialPort makes sure it is registered by Windows 
		// in HKEY_LOCAL_MACHINE\HARDWARE\DEVICEMAP\SERIALCOMM, so that 
		// SerialPort.GetPortNames() can re-detect the port.
		void Disconnect()
		{
            Disconnected = true;
            if (RtsMode == RtsModes.Toggle)
                port?.ClearRtsControlToggle();

            port?.Close();
			port?.Dispose();
			port = null;
        }

		void OnPortConnected(string portName)
		{
			if (portName == PortSettings.PortName)
				Connect();
		}

		void OnPortRemoved(string portName)
		{
			if (portName == PortSettings.PortName)
				Disconnect();
		}

		public bool Command(string command)
		{
			if (string.IsNullOrEmpty(command))
                return false;
            lock (CommandQ) CommandQ.Enqueue(command);
			txThreadSignal.Set();   // release txThread block
			return !Disconnected;
		}

		public void Reset()
		{
			Disconnect();
			Connect();
			CRCErrors = 0;
			Resets++;
		}

		public void Close()
		{
			Disconnect();	// closes and disposes the port
		}

		// run transmit() in non-GUI thread
		// Paces the character transmission rate so a slower
		// embedded processor can complete its byte-received ISR
		// before another character arrives. (This is independent
		// of baud rate.)
		void transmit()
		{
			try
			{
				byte[] tx = { };						// transmit data buffer
				int txbOut = 0;
                Crc txCrc = null;
                if (CrcConfig != null)
                    txCrc = new Crc(CrcConfig);
				string command = "";
                txSw.Restart();

                while (port != null && !Disconnected)
				{
					if (txbOut < tx.Length)
					{
						try
						{
                            if (MillisecondsBetweenBytes == -1)     // do not pace characters
                            {
                                if (MillisecondsSinceLastTx >= MillisecondsBetweenMessages)
                                {
                                    port.Write(tx, 0, tx.Length);
                                    txbOut = tx.Length;
                                    txSw.Restart();
                                }
                                else
                                    Thread.Sleep(0);
                            }
                            else
                            {
                                if (MillisecondsSinceLastTx >= MillisecondsBetweenBytes)
                                {
                                    port.Write(tx, txbOut++, 1);
                                    txSw.Restart();
                                }
                                else
                                    Thread.Sleep(0);
                            }
                        }
                        catch(Exception e)
                        {
                            // ignore port write errors?
                            Log?.Record("Exception in SerialDevice transmit(): " + e.ToString());
                            // but take a breather
                            Thread.Sleep(Math.Max(20, MillisecondsBetweenMessages));
                        }
                    }
					else
					{
                        Transmitting = false;
                        LatestCommand = command;

                        if (CommandQ.Count > 0)
                        {
                            lock (CommandQ) command = CommandQ.Dequeue();
                            txbOut = 0;
                            if (CrcConfig == null)
                            {
                                tx = Utility.ToByteArray(command);
                            }
                            else
                            {
                                txCrc.Init();
                                tx = txCrc.Append(command);
                            }

                            Log?.Record("SerialDevice (transmit): " + Escape(command));
                            Transmitting = true;
                        }
                        else
                        {
                            txThreadSignal.WaitOne(1000);   // wait for a command
                        }
                    }
				}
			}
			catch (Exception e) { Notice.Send(e.ToString()); }
		}

		#region process received bytes

		///////////////////////////////////////////////////////
		volatile int Rxb_write = 0;		// place for next byte from receiver
		int Rxb_head = 0;

		///////////////////////////////////////////////////////
		// ring buffer pointer movement and state
		int advance(int p) { return ((p + 1) % RXBUF_SIZE); }
		int retreat2(int p) { return ((p + RXBUF_SIZE - 2) % RXBUF_SIZE); }

		// whether there's room for another byte from the receiver
		bool RxbFull() { return advance(Rxb_write) == Rxb_head; }

		bool RxbEmpty() { return Rxb_head == Rxb_write; }

		int ClearRxb() { Rxb_head = Rxb_write; return Rxb_head; }

		string rxbSequence(int tail)
		{
            if (tail > Rxb_head)
            {
                return Utility.ByteArrayToString(rx, Rxb_head, tail - Rxb_head);
            }
            else
            {
                return Utility.ByteArrayToString(rx, Rxb_head, RXBUF_SIZE - Rxb_head) +
                    Utility.ByteArrayToString(rx, 0, tail);
            }
		}

		void handleCrcError()
		{
			ErrorCrc = true;
			CRCErrors++;
			if (CRCErrors > MAX_CRC_ERRORS) Reset();
		}

		// Scans through the rx buffer, extracts CRC-validated messages,
		// and sends them to the ResponseReceived delegate.
		// Runs in its own thread, prxThread.
        // Requires a valid (non-null) CrcConfig
		void process_rx()
		{
			try
			{
				int rxb_read = ClearRxb();
    		    rxCrc.Init();
				ErrorCrc = false;

				int bwuTermChar = 0;  // bytes with unexpected TermChar
				while (port != null && !Disconnected)
				{
					while (rxb_read != Rxb_write)
					{
						if (bwuTermChar > 0) bwuTermChar++;
						byte c = rx[rxb_read];
						if (c == CrcConfig.TermChar)
						{
							ETXCount++;
							if (rxCrc.Good())
							{
								string s = rxbSequence(retreat2(rxb_read));
								Log?.Record("SerialDevice (process_rx): " + Escape(s.TrimEnd()));
								ResponseReceived?.Invoke(s);
								ResponseCount++;
							}
							else bwuTermChar = 1;

							if (rxCrc.Good() || ErrorCrc)
							{	// start fresh on the next byte
								rxb_read = Rxb_head = advance(rxb_read);
								bwuTermChar = 0;
								rxCrc.Init();
								ErrorCrc = false;
								continue;
							}
						}
						rxCrcCode = rxCrc.Update(c);
						rxb_read = advance(rxb_read);

						if (!ErrorCrc && bwuTermChar > 2)
						{
							handleCrcError();
							Log?.Record(Escape(rxbSequence(rxb_read)) + " [CRC Error]");
						}
					}
					if (ErrorBufferOverflow)
					{
						handleCrcError();
						rxb_read = ClearRxb();
					}
					// wait here until something more comes in
					prxThreadSignal.WaitOne();      // wait for signal to be set (by receive())
				}
			}
			catch (Exception e) { Notice.Send(e.ToString()); }
		}

		// No TermChar; this version detects end of message by a period of silence.
		void process_rx2()
		{
			try
			{
				bool startFresh = true;
				int rxb_read = 0;

				while (port != null && !Disconnected)
				{
					bool timedOut = false;

                    if (startFresh)
                    {
                        rxb_read = ClearRxb();
                        rxCrc?.Init();
                        ErrorCrc = false;
                        prxThreadSignal.WaitOne();
                        startFresh = false;
                    }
                    else
                    {
                        // Once the beginning of a message has arrived, if more than 5 milliseconds 
                        // of silence occurs, the message is treated as complete. (If 
                        // MillisecondsBetweenMessages is less than 5, then that timeout value is
                        // used instead.)
                        timedOut = !prxThreadSignal.WaitOne(Math.Min(5, MillisecondsBetweenMessages));
                    }

					if (ErrorBufferOverflow)
					{
						// silently discard the (possibly partial) message
						startFresh = true;
						continue;
					}

					while (rxb_read != Rxb_write)
					{
						if (rxCrc != null)
                            rxCrcCode = rxCrc.Update(rx[rxb_read]);
						rxb_read = advance(rxb_read);
					}

					if (timedOut)
					{
						if (rxCrc == null || rxCrc.Good())
						{
                            string s;
                            if (rxCrc == null)
                                s = rxbSequence(rxb_read);
                            else
                                s = rxbSequence(retreat2(rxb_read));
							Log?.Record("SerialDevice (process_rx2): " + Escape(s.TrimEnd()));
							ResponseReceived?.Invoke(s);
							ResponseCount++;
						}
						else if (!RxbEmpty())
						{
							if (rxCrc != null) handleCrcError();
							Log?.Record("SerialDevice (process_rx2): " + Escape(rxbSequence(rxb_read)) + " [CRC Error]");
						}
						startFresh = true;
					}
				}
			}
			catch (Exception e) { Notice.Send(e.ToString()); }
		}

		#endregion process received bytes

		// Transfer received bytes from the SerialPort buffer into this 
		// SerialDevice's rx buffer for processing
		byte[] readBuffer = new byte[4096];
		void getRxData()
		{
			int n;
			try { n = port.Read(readBuffer, 0, 4096); }
			catch { n = 0; }
			for (int i = 0; i < n; i++)
			{
				rxSw.Restart();

				if (ErrorBufferOverflow = RxbFull())    // assignment, not comparison
					BufferOverflows++;
				else
				{
					rx[Rxb_write] = readBuffer[i];
					Rxb_write = advance(Rxb_write);
					TotalBytesRead++;
				}
				prxThreadSignal.Set();   // process what was received
			}
			//Log?.Record("SerialDevice: TotalBytesRead = " + TotalBytesRead);
		}

		// option to retrieve SerialPort data asynchronously
		Thread rxThread;
		AutoResetEvent rxThreadSignal = new AutoResetEvent(false);
		void receive()
		{
			while (port != null && !Disconnected)
			{
				try
				{
					rxThreadSignal.WaitOne(100);   // wait for a message
					getRxData();
				}
				catch (Exception e) { Log?.Record($"SerialDevice: {e}"); }
			}
		}


		// This is an event handler invoked by port, a SerialPort.
		// Runs in a ThreadPool thread started by the SerialPort.
		void rxDetected(object sender, SerialDataReceivedEventArgs e)
		{
			ReceiveEvents++;
			//getRxData();			// for synchronous data processing
			rxThreadSignal.Set();       // grab the data asynchronously
		}

        void PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            Log?.Record($"A pin changed: {e.ToString()}");
        }
    }
}
