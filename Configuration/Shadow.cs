// Version: 0.1.0.17
using System.Windows.Media;

namespace Thmd.Configuration;

public class Shadow
{
	public Color Color { get; set; } = Colors.Black;

	public double ShadowDepth { get; set; } = 0.0;

	public double Opacity { get; set; } = 0.5;

	public double BlurRadius { get; set; } = 10.0;

	public bool Visible { get; set; } = true;
}
