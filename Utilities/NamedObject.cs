using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Utilities
{
	public class NamedObject
	{
		[XmlAttribute] public virtual string Name { get; set; }
	}
}
