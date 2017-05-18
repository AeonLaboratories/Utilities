using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Utilities
{
    public static class PropertyInfoExtensions
    {
		public static bool XmlIgnored(this PropertyInfo pi)
		{
			return (pi.GetCustomAttributes(typeof(XmlIgnoreAttribute), false).Length > 0);
		}

		public static bool IsSettable(this PropertyInfo pi)
        {
            if (pi == null || !pi.CanRead || !pi.CanWrite)
                return false;

            Type pType = pi.PropertyType;
            if (!(pType.IsPublic || pType.IsNestedPublic))
                return false;

            object[] CustomAttributes = pi.GetCustomAttributes(false);
            foreach (object attribute in CustomAttributes)
            {
                if (attribute is XmlIgnoreAttribute ||
                    attribute is XmlAttributeAttribute && pi.Name.Equals("Name"))
                    return false;
            }

            return true;
        }
    }

    public static class TextBoxExtensions
    {
        private const uint ECM_FIRST = 0x1500;
        private const uint EM_SETCUEBANNER = ECM_FIRST + 1;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, uint wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        public static void SetWatermark(this TextBox textBox, string watermarkText)
        {
            SendMessage(textBox.Handle, EM_SETCUEBANNER, 0, watermarkText);
        }
    }

    public static class TypeExtensions
    {
        public static bool IsAtomic(this Type type)
        {
            return type.IsValueType || type == typeof(string);
        }
    }

	public static class ColorExtensions
	{
		public static Color Blend(this Color color1, Color color2, double percent)
		{
			if (percent <= 0)
				return color1;
			else if (percent >= 1)
				return color2;

			double p1 = 1 - percent;
			
			int r1 = (int)(color1.R * p1);
			int g1 = (int)(color1.G * p1);
			int b1 = (int)(color1.B * p1);
			int r2 = (int)(color2.R * percent);
			int g2 = (int)(color2.G * percent);
			int b2 = (int)(color2.B * percent);

			return Color.FromArgb(r1 + r2, g1 + g2, b1 + b2);
		}
	}
}
