﻿using Newtonsoft.Json;
using System;
using System.Xml.Serialization;

namespace Utilities
{
	// Computes a filtered (smoothed) rate of change.
	// Follows http://www.holoborodko.com/pavel
	// Longer filters provide increased noise suppression.
	[JsonObject(MemberSerialization.OptIn)]
	public class RateOfChange
	{
		public static implicit operator double(RateOfChange roc)
		{ return roc?.Value ?? 0; }

		#region variables

		Stopwatch sw = new Stopwatch();
		double[] v;				// history of values
		double[] t;				// times since prior values
		double[] c;				// filter polynomial coefficients for v[]
		int _FilterLength = 11;	// filter length == c.Length
		double fDivisor = 512;	// a common divisor taken out of the c[] elements
		double _RateOfChange;

		#endregion variables

		#region Properties
		public virtual string Name { get; set; }
		[JsonProperty]
		public int FilterLength
		{
			get { return _FilterLength; }
			set
			{
				if (value < 5) _FilterLength = 5;
				else if (value > 63) _FilterLength = 63;
				else if (value % 2 != 0) _FilterLength = value; // make sure length is odd
				else _FilterLength = value - 1;
			}
		}
		[JsonProperty]
		public double SamplingIntervalMilliseconds { get; set; }
		public double Value => _RateOfChange;
		public RateOfChange RoC { get; set; }

		#endregion Properties

		public RateOfChange() { sw.Start(); }

		public RateOfChange(int filterLength) : this()
		{ FilterLength = filterLength; }

		public void Initialize()
		{
			RoC?.Initialize();
			createFilter(FilterLength);
		}
		
		// See http://www.holoborodko.com/pavel for theoretical basis.
		// See "holobordko filter.ods" for the analysis that led to this
		// algorithm and implementation.
		//
		// Symmetric filters find the rate of change at the time of the middle value.
		//
		// According to Holoborodko,
		// "One-sided filters have several disadvantages comparing to centered versions.
		// For example, to achieve the same noise suppression one-sided filter
		// should be much longer than centered one. Also they strongly amplify noise
		// in the mid-frequency range."
		//
		void createFilter(int length)
		{
			c = new double[_FilterLength];

			int n = _FilterLength - 3;			// 2m in Holoborodko
			int M = (_FilterLength - 1) / 2;	// middle position
			double bL, bR, b0;
			bL = b0 = 0; 
			for (int i = 0; i < M; i++)
			{
				bR = Utility.binomCoeff(n, i);
				c[i] = bL - bR;
				c[length - 1 - i] = -c[i];
				bL = b0;
				b0 = bR;
			}

			fDivisor = Math.Pow(2, _FilterLength - 2);

			v = new double[_FilterLength + 1];		// extra element at end for new value to be shifted in
			t = new double[_FilterLength + 1];		// extra element at end for new value to be shifted in
		}

		public double Update(double newValue)
		{
			double elapsed = sw.Elapsed.TotalMilliseconds;
			if (elapsed >= SamplingIntervalMilliseconds)
			{
				// store the latest datapoint and dT
				v[_FilterLength] = newValue;
				t[_FilterLength] = elapsed; sw.Restart();
				double sumPT = 0;			// sum of polynomial terms
				double sumT = 0;			// total time in milliseconds;
				for (int i = 0; i < _FilterLength; i++)
				{
					v[i] = v[i + 1];
					sumPT += c[i] * v[i];

					t[i] = t[i + 1];
					sumT += t[i];
				}
				double averageSamplesPerSecond = 1000 * _FilterLength / sumT;
				_RateOfChange = averageSamplesPerSecond * sumPT / fDivisor;
				if (RoC != null) RoC.Update(_RateOfChange);
			}
			return _RateOfChange;
		}
	}
}