using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;

namespace Utilities
{
	// This class provides a way to handle an exception that can occur
	// when a USB-Serial adapter is disconnected. The problem stems
	// from the way .NET handles the underlying stream in the case 
	// of a serial port disappearing: it doesn't allow the stream 
	// to be closed after the serial port is disconnected.
	//
	// The exception occurs during BaseStream's Finalize() method, 
	// which is called by the garbage collector. The way this class
	// gets around the problem is to inherit the .NET SerialPort 
	// class and override the Open() and Close() methods. During Open(), 
	// GC.SuppressFinalize(BaseStream) prevents BaseStream from
	// being finalized prematurely. When Close() is called,
	// BaseStream's Finalize() is re-enabled only if the SerialPort
	// is still open. (An alternative, and perhaps more robust
	// technique, would be to use try/catch to trap the exception
	// that would be raised by GC.ReRegisterForFinalize(BaseStream)
	// if the USB-Serial adapter were pulled out.)
	public class USBSerialPort : SerialPort
	{
		public new void Open()
		{
			if (!base.IsOpen)
			{
				base.Open();
				GC.SuppressFinalize(BaseStream);
			}
		}

		public new void Close()
		{
			//if (base.IsOpen)
			//{
			//	GC.ReRegisterForFinalize(BaseStream);
			//	base.Close();
			//}
			// Perhaps this alternative is superior:
			try { GC.ReRegisterForFinalize(BaseStream); } catch {}
			if (base.IsOpen) base.Close();
		}

		protected override void Dispose(bool disposing)
		{
			try { base.Dispose(disposing); }
			catch { /* ignore exception - bug with USB-serial adapters. */ }
		}
	}
}
