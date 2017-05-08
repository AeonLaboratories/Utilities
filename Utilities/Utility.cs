using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Numerics;
using System.Collections;

namespace Utilities
{
	public class Utility
    {
		public static string[] lineTerminators = new string[] { "\r\n", "\n" };

		public static string[] SplitIntoLines(string s)
		{ return s.Split(lineTerminators, StringSplitOptions.None); }
		
		public static string byte_string(string s)
		{
			StringBuilder sb = new StringBuilder();
			foreach (byte b in s)
			{
				sb.Append(b.ToString("X2"));
				sb.Append(" ");
			}
			return sb.ToString().TrimEnd();
		}

		// assumes the processor architecture is little-endian
		// assumes exactly 2 bytes are to be converted
		// assumes s contains bytes at MSBIndex and MSBIndex + 1
		public static Int16 getMSBFirstInt16(string s, int MSBindex)
		{
			byte[] ba = new byte[2];
			ba[0] = (byte)s[MSBindex + 1];
			ba[1] = (byte)s[MSBindex];

			return BitConverter.ToInt16(ba, 0);
		}

		public static string IndentLines(string text, string indent)
		{
			string indented = "";
			string[] lines = SplitIntoLines(text);
			int n = lines.Length;
			if (n < 1) return "";
			for (int i = 0; i < n - 1; i++)
				indented += indent + lines[i] + "\r\n";
			return indented + indent + lines[n-1];
		}

		public static string IndentLines(string text)
		{ return IndentLines(text, "   "); }
		
		public static string ToStringLine(string name, object value)
		{
			StringBuilder sb = new StringBuilder(name);
			sb.Append(": ");
			sb.Append(value.ToString());
			sb.Append("\r\n");
			return sb.ToString();
		}

		public static string ToStringLine(string name, bool value)
		{ return ToStringLine(name, value ? "Yes" : "No"); }


        // Returns a format string for representing a number n rounded to s significant digits.
		public static string sigDigitsString(double n, int s)
        {
            string pformat = "0." + new string('0', s - 1);
            string n1 = n.ToString(pformat + "e+00");
            int e = int.Parse(n1.Substring(n1.Length - 3)); // get the exponent

            if (e >= Math.Max(s, 3) || e <= -2)
                return n.ToString(pformat + "e-0");
            else
            {
                pformat += "0"; // extra zero in case needed left of decimal point
                int len = 1 + s - e; if (len == 2) len = 1;
                return n.ToString(pformat.Substring(0, len));
            }
        }

        //calculates the binomial coefficient "n choose k"
		public static double binomCoeff(int n, int k)
        {
            if (n < 0 || k < 0) return 0;
            if (k > n) return 0;
            if (k > n / 2) k = n - k;
            double r = 1;
            for (int d = 1; d <= k; d++)
            {
                r *= n--;
                r /= d;
            }
            return r;
        }

		public static string xmlAttribute(string attr, string value)
		{
			var sb = new StringBuilder(" ");
			sb.Append(attr);
			sb.Append("=\"");
			sb.Append(value);
			sb.Append("\"");
			return sb.ToString();
		}

		public static string xmlTag(string element, string value, List<string> attributes)
		{
			var sb = new StringBuilder();
			sb.Append("<"); sb.Append(element);
			if (attributes != null)
				foreach (string s in attributes)
					sb.Append(s);
			if (string.IsNullOrWhiteSpace(value))
				sb.Append(" />");
			else
			{
				sb.Append(">");

				// *hack* *cough*
				// Microsoft's implementation of ToString() for booleans
				// outputs "True" or "False", but XmlSerialization
				// is case-sensitive and requires either "true" or "false"
				if (value == "False") value = "false";
				else if (value == "True") value = "true";

				sb.Append(value);
				sb.Append("</"); sb.Append(element); sb.Append(">");
			}
			sb.Append("\r\n");
			return sb.ToString();
		}

		public static string xmlTag(string element, string value)
		{
			return xmlTag(element, value, "");
		}

		public static string xmlTag(string element, string value, string attribute)
		{
			var attributes = new List<string>();
			if (!string.IsNullOrWhiteSpace(attribute))
				attributes.Add(attribute);
			return xmlTag(element, value, attributes);
		}

		public static double[] Multiply(double[] a, double k)
		{ for (int i = 0; i < a.Length; ++i) a[i] = k * a[i]; return a; }

		public static double[] Negate(double[] a) { Multiply(a, -1); return a; }

		// evaluate polynomial in x
		public static double EvaluatePolynomial(double[] coeffs, double x)
		{
			int i = coeffs.Length;
			double sum = coeffs[--i];
			while (i > 0)
				sum = x * sum + coeffs[--i];
			return sum;
		}

		// evaluate polynomial in z
		public static Complex EvaluatePolynomial(Complex[] coeffs, Complex z)
		{
			int i = coeffs.Length;
			Complex sum = coeffs[--i];
			while (i > 0)
				sum = z * sum + coeffs[--i];
			return sum;
		}

		public static bool IsList(object o)
		{
			return typeof(IList).IsAssignableFrom(o.GetType());
		}
    }

	public struct ObjectPair
	{
		public object x;
		public object y;

		public ObjectPair(object x, object y)
		{
			this.x = x;
			this.y = y;
		}
	}

	public enum Services { MessageBox, TitledMessageBox, PlaySound }

	public class Request : EventArgs
	{
		public Services Service;
		public object Args;

		public Request(Services service, object args)
		{
			Service = service;
			Args = args;
		}
	}


    public class LookupTable
    {
		public string filename;

        public double[] key;
        public double[] value;
		public int count { get; set; }
		public bool outOfRange { get; set; }

        public LookupTable(string filename)
        { load(filename); }

        void load(string filename)
        {
            StreamReader fin;
            try { fin = new StreamReader(filename); }
            catch
            {
                throw new Exception("Couldn't open '" + filename + "'");
            }
			this.filename = filename;

            int nLines = 0;
            string line = fin.ReadLine();
            while (line != null)
            {
                ++nLines;
                line = fin.ReadLine();
            }

            count = nLines;
            key = new double[count];
            value = new double[count];

            fin.DiscardBufferedData();
            fin.BaseStream.Seek(0, SeekOrigin.Begin);

            char[] delimiters = { '\t' };
            string[] token;
            int i = 0;
            line = fin.ReadLine();
            while (line != null)
            {
                token = line.Split(delimiters);
                key[i] = double.Parse(token[0]);
                value[i] = double.Parse(token[1]);
                ++i;
                line = fin.ReadLine();
            }
            fin.Close();
        }

        public double Interpolate(double d)
		{
			if (d < key[0] || d > key[count - 1])
            {
                outOfRange = true; 
                return d;
            }
			outOfRange = false;
			int i = Array.BinarySearch(key, d);
			if (i >= 0) return value[i];
			i = ~i;	// index of lowest key that is greater than d					
			return value[i - 1] + (d - key[i - 1]) * (value[i] - value[i - 1]) / (key[i] - key[i - 1]);				
		}

    }
}