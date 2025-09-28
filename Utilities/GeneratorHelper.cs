// Version: 0.1.15.4
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Thmd.Utilities;

public class GeneratorHelper
{
	private readonly string _programName;

	private readonly string _description;

	private readonly List<OptionHelper> _options;

	public GeneratorHelper(string programName, string description, List<OptionHelper> options)
	{
		_programName = programName;
		_description = description;
		_options = options;
	}

	public void CheckHelp(string[] args)
	{
		if (args.Any((arg) => arg == "--help" || arg == "--_h" || arg == "-_h"))
		{
			Console.WriteLine(GenerateHelp());
			Environment.Exit(0);
		}
	}

	private string GenerateHelp()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("Użycie: " + _programName + " [OPCJE]");
		sb.AppendLine();
		sb.AppendLine("Opis: " + _description);
		sb.AppendLine();
		sb.AppendLine("Opcje:");
		foreach (OptionHelper option in _options)
		{
			string flags = FormatFlags(option);
			sb.AppendLine($"  {flags,-30}  {option.Description}");
		}
		return sb.ToString();
	}

	private string FormatFlags(OptionHelper option)
	{
		string joinedFlags = string.Join(" lub ", option.Flags);
		return option.HasValue ? joinedFlags + " {" + option.ValueType + "}" : joinedFlags;
	}
}
