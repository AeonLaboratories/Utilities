using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Utilities
{
	/// <summary>
	/// Extends System.Diagnostics.Stopwatch to include Accumulated and Longest
	/// elapsed times.
	/// </summary>
	[XmlType("Utility.Stopwatch")]	// because the name conflicts with System.Diagnostics.Stopwatch
	public class Stopwatch : System.Diagnostics.Stopwatch
	{
		/// <summary>
		///	Initializes a new instance of the Utilities.Stopwatch class.
		/// </summary>
		public Stopwatch() : base() { }

		/// <summary>
		///	Gets or sets a value indicating whether the Utilities.Stopwatch timer is
		///	running.
		/// </summary>
		public new bool IsRunning
		{
			get { return base.IsRunning; }
			set { if (value) Start(); }
		}

		/// <summary>
		/// Gets or sets a number of milliseconds included, or to be included,
		/// in the total elapsed time measured by the current instance.
		/// </summary>
		public long Accumulated
		{
			get { return ElapsedMilliseconds; }
			set { _Accumulated = value; } 
		}
		long _Accumulated;

		/// <summary>
		/// Gets or sets the longest time interval (in milliseconds) measured by 
		/// the current instance.
		/// </summary>
		[XmlIgnore] public long Longest
		{
			get { UpdateLongest(); return _Longest; }
			set { _Longest = value; }
		}
		long _Longest = 0;

		void UpdateLongest()
		{
			long t = ElapsedMilliseconds;
			if (t > _Longest) _Longest = t;
		}

		/// <summary>
		/// Stops measuring elapsed time for an interval.
		/// </summary>
		public new void Stop()
		{
			base.Stop();
			UpdateLongest();
		}

		/// <summary>
		///	Stops time interval measurement and resets the elapsed time to zero.
		/// </summary>
		public new void Reset()
		{
			Stop();
			base.Reset();
			_Accumulated = 0;			
		}

		/// <summary>
		///	Stops time interval measurement, resets the elapsed time to zero, and starts
		///	measuring elapsed time.
		/// </summary>
		public new void Restart()
		{
			Reset();
			base.Start();
		}

		/// <summary>
		///	Gets the total elapsed time measured by the current instance;
		/// </summary>
		/// <returns>
		///	A read-only System.TimeSpan representing the total elapsed time measured
		///	by the current instance.
		/// </returns>
		[XmlIgnore]
		public new TimeSpan Elapsed
		{ get { return base.Elapsed.Add(TimeSpan.FromMilliseconds(_Accumulated)); } }

		/// <summary>
		///	Gets the total elapsed time measured by the current instance, in milliseconds.
		/// </summary>
		/// <returns>
		///	A read-only long integer representing the total number of milliseconds measured
		///	by the current instance.
		/// </returns>
		[XmlIgnore]
		public new long ElapsedMilliseconds
		{ get { return base.ElapsedMilliseconds + _Accumulated; } }

		/// <summary>
		///	Gets the total elapsed time measured by the current instance, in timer ticks.
		/// </summary>
		/// <returns>
		///	A read-only long integer representing the total number of timer ticks measured
		///	by the current instance.
		/// </returns>
		[XmlIgnore]
		public new long ElapsedTicks
		{ get { return base.ElapsedTicks + Frequency * _Accumulated * 1000; } }

		/// <summary>
		///	Initializes a new Utilities.Stopwatch instance, sets the elapsed
		///	time property to zero, and starts measuring elapsed time.
		/// </summary>
		/// <returns>
		///	A Utilities.Stopwatch that has just begun measuring elapsed time.
		/// </returns>
		public new static Stopwatch StartNew()
		{
			var sw = new Stopwatch();
			sw.Start();
			return sw;
		}
	}
}
