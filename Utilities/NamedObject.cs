using System.Xml.Serialization;

namespace Utilities
{
	public class NamedObject
	{
		[XmlAttribute] public virtual string Name { get; set; }
	}
}
