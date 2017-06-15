using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Utilities
{
    public class SerialDevice
    {
        public delegate void ResponseHandlerType(string s);
        static long instanceCount = 0;


        #region Fields

        USBSerialPort port;

        Stopwatch txSw = new Stopwatch();
        Stopwatch rxSw = new Stopwatch();

		Queue<string> CommandQ = new Queue<string>();
        [XmlIgnore] public string LatestCommand;

        Thread txThread;
        ManualResetEvent txThreadSignal = new ManualResetEvent(false);
        Thread prxThread;
        ManualResetEvent prxThreadSignal = new ManualResetEvent(false);

		const int RXBUF_SIZE = 256;
		char[] rx;							// serial receive data buffer
        Crc rxCrc;

        [XmlIgnore] public LogFile DebugLog;
		[XmlIgnore] public bool Logging = false;
		[XmlIgnore] public bool Disconnected = true;
		[XmlIgnore] public ResponseHandlerType ResponseReceived;
		[XmlIgnore] public bool ErrorCrc = false;
		[XmlIgnore] public bool ErrorBufferOverflow = false;

		[XmlIgnore] public uint ReceiveEvents = 0;
		[XmlIgnore] public uint TotalBytesRead = 0;
		[XmlIgnore] public uint ETXCount = 0;
		[XmlIgnore] public uint ResponseCount = 0;

		const int MAX_CRC_ERRORS = 3;       // triggers a reset
		[XmlIgnore] public uint CRCErrors = 0;
		[XmlIgnore] public uint BufferOverflows = 0;
		[XmlIgnore] public uint Resets = 0;
		[XmlIgnore] public UInt16 rxCrcCode = 0;

		#endregion Fields

		#region Properties

		public SerialPortSettings PortSettings { get; set; }
        public int MillisecondsBetweenBytes { get; set; }		// time delay between transmitted characters
        public int MillisecondsBetweenMessages { get; set; }	// time delay between transmitted codewords (data+CRC+termchar)
        public CrcOptions CrcConfig { get; set; }

		[XmlIgnore] public long MillisecondsSinceLastTx { get { return txSw.ElapsedMilliseconds; } }
		[XmlIgnore] public long MillisecondsSinceLastRx { get { return rxSw.ElapsedMilliseconds; } }
		[XmlIgnore] public long LongestSilence { get { return rxSw.Longest; } }

		public bool Idle { get { return CommandQ.Count == 0; } }

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

            if (CrcConfig == null) CrcConfig = new CrcOptions();
			rx = new char[RXBUF_SIZE];
            rxCrc = new Crc(CrcConfig);
			if (CrcConfig.OmitTermChar)
				prxThread = new Thread(process_rx2);
			else
	            prxThread = new Thread(process_rx);
			prxThread.Name = "SD prxThread " + instanceCount.ToString();
			prxThread.IsBackground = true;
			prxThread.Start();

			txThread = new Thread(transmit);
			txThread.Name = "SD txThread " + instanceCount.ToString();
			txThread.IsBackground = true;
			txThread.Start();

			Connect();
            SerialPortMonitor.PortArrived += OnPortConnected;
            SerialPortMonitor.PortRemoved += OnPortRemoved;
		}

		void Connect()
		{
			port = new USBSerialPort();
			port.RtsEnable = true;   // may supply power to hardware connected to the port
			port.PortName = PortSettings.PortName;
			port.BaudRate = PortSettings.BaudRate;
			port.Parity = PortSettings.Parity;
			port.DataBits = PortSettings.DataBits;
			port.StopBits = PortSettings.StopBits;
			port.Handshake = PortSettings.Handshake;
			port.ReadTimeout = 500;
			port.WriteTimeout = 500;
			port.Encoding = Encoding.GetEncoding("iso-8859-1");
			port.DataReceived += new
				SerialDataReceivedEventHandler(receive);

			try { port.Open(); if (port.IsOpen) Disconnected = false; }
			catch { }
		}

		// Disposing the USBSerialPort makes sure it is registered by Windows 
		// in HKEY_LOCAL_MACHINE\HARDWARE\DEVICEMAP\SERIALCOMM, so that 
		// SerialPort.GetPortNames() can re-detect the port.
		void Disconnect()
		{
			//try { port.Write("!"); /* any value */ }
			//catch (IOException ex)
			//{
				port.Close();
				port.Dispose();
				port = null;
				Disconnected = true;
			//}
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
			if (Disconnected || string.IsNullOrEmpty(command)) return false;
			lock (CommandQ) CommandQ.Enqueue(command);
            txThreadSignal.Set();   // release txThread block
			return true;
		}

        public void Reset()
        {
            //if (port.IsOpen) port.Close();
			//port.Open();
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
				Crc txCrc = new Crc(CrcConfig);
                string command = "";

				while (true)
				{
					if (txbOut < tx.Length)
					{
						try
						{
							if (MillisecondsBetweenBytes == -1)		// do not pace characters
							{
								port.Write(tx, 0, tx.Length);
								txbOut = tx.Length;
							}
							else
								port.Write(tx, txbOut++, 1);
							if (MillisecondsBetweenBytes > 0) Thread.Sleep(MillisecondsBetweenBytes);
						}
						catch {}
					}
					else
					{
                        LatestCommand = command;
						txSw.Restart();

						if (CommandQ.Count > 0)
						{
							lock (CommandQ) command = CommandQ.Dequeue();
							txbOut = 0;
							txCrc.Init();
							tx = txCrc.Append(command);

                            long delay = Math.Max(0, MillisecondsBetweenMessages - MillisecondsSinceLastTx);
                            Thread.Sleep((int)delay);
							if (Logging) DebugLog.Record("SerialDevice: " + command);
						}
						else
						{
							txThreadSignal.Reset();         // reset to block
							txThreadSignal.WaitOne();       // wait for a command
						}
					}
				}
			}
			catch (Exception e) { MessageBox.Show(e.ToString()); }
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
                return new string(rx, Rxb_head, tail - Rxb_head);
            else
            {
                return new string(rx, Rxb_head, RXBUF_SIZE - Rxb_head) +
                    new string(rx, 0, tail);
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
        void process_rx()
        {
			try
			{
				int rxb_read = ClearRxb();
                rxCrc.Init();
				ErrorCrc = false;

				int bwuTermChar = 0;  // bytes with unexpected TermChar
				while (true)
				{
					while (rxb_read != Rxb_write)
					{
						if (bwuTermChar > 0) bwuTermChar++;
						char c = rx[rxb_read];
						if (c == CrcConfig.TermChar)
						{
							ETXCount++;
							if (rxCrc.Good())
							{
								string s = rxbSequence(retreat2(rxb_read));
								if (Logging) DebugLog.Record("SerialDevice: " + s.TrimEnd());
								if (ResponseReceived != null) ResponseReceived(s);
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
							if (Logging) DebugLog.WriteLine(rxbSequence(rxb_read) + "[CRC Error]");
						}
					}
					if (ErrorBufferOverflow)
					{
						handleCrcError();
						rxb_read = ClearRxb();
					}
					// wait here until something more comes in
					prxThreadSignal.Reset();         // reset the signal to the blocking state
					prxThreadSignal.WaitOne();       // wait for signal to be set (by receive())
				}
			}
			catch (Exception e)
			{ MessageBox.Show(e.ToString()); }
        }

		// No TermChar; this version detects end of message by a period of silence.
		void process_rx2()
		{
			try
			{
				bool startFresh = true;
				int rxb_read = 0;

				while (true)
				{
					bool timedOut = false;

					prxThreadSignal.Reset();
					if (startFresh)
					{
						rxb_read = ClearRxb();
						rxCrc.Init();
						ErrorCrc = false;
						prxThreadSignal.WaitOne();
						startFresh = false;
					}
					else
						timedOut = !prxThreadSignal.WaitOne(Math.Max(5, MillisecondsBetweenMessages));						

					if (ErrorBufferOverflow)
					{
						// silently discard the (possibly partial) message
						startFresh = true;
						continue;
					}

					while (rxb_read != Rxb_write)
					{
						rxCrcCode = rxCrc.Update(rx[rxb_read]);
						rxb_read = advance(rxb_read);
					}

					if (timedOut)
					{
						if (rxCrc.Good())
						{
							string s = rxbSequence(retreat2(rxb_read));
							if (Logging) DebugLog.Record("SerialDevice: " + s.TrimEnd());
							if (ResponseReceived != null) ResponseReceived(s);
							ResponseCount++;
						}
						else if (!RxbEmpty())
						{
							handleCrcError();
							if (Logging) DebugLog.Record("SerialDevice: " + rxbSequence(rxb_read) + "[CRC Error]");
						}
						startFresh = true;
					}
				}
			}
			catch (Exception e)
			{ MessageBox.Show(e.ToString()); }
		}
		
		#endregion process received bytes

        // This is an event handler invoked by port, a SerialPort.
        // Runs in a ThreadPool thread started by the SerialPort.
        void receive(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                ReceiveEvents++;
                int n = port.BytesToRead;
                for (int i = 0; i < n; i++)
                {
                    rxSw.Restart();

                    if (ErrorBufferOverflow = RxbFull())    // assignment, not comparison
                    {
                        BufferOverflows++;
                        port.ReadByte();    // discard the byte
                    }
                    else
                    {
                        rx[Rxb_write] = (char)port.ReadByte();
                        Rxb_write = advance(Rxb_write);
                        TotalBytesRead++;
                    }
                    prxThreadSignal.Set();   // release prxThread block
                }
            }
            catch { }
        }
    }
}
