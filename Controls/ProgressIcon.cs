// Version: 0.1.1.60
using System.Windows;

namespace Thmd.Controls;

internal class ProgressIcon
{
	public string Name { get; set; }

	public string Path { get; set; }

	public Size Size { get; set; }

	public ProgressIcon(string name, string path, Size size)
	{
		Name = name;
		Path = path;
		Size = size;
	}
}
