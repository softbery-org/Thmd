// Version: 0.1.1.20
using System.Collections.Generic;

namespace Thmd.Helpers;

public class HelpOption
{
	public List<string> Flags { get; }

	public string Description { get; }

	public bool HasValue { get; }

	public string ValueType { get; }

	public HelpOption(List<string> flags, string description, bool hasValue = false, string valueType = "bool")
	{
		Flags = flags;
		Description = description;
		HasValue = hasValue;
		ValueType = valueType;
	}
}
