// Version: 0.1.0.18
using System.Windows.Media;

namespace Thmd.Configuration;

public class SubtitleConfig
{
	public double FontSize { get; set; }

	public FontFamily FontFamily { get; set; }

	public Brush FontColor { get; set; }

	public Shadow Shadow { get; set; }

	public SubtitleConfig(double size, string fontfamily, Brush color, bool show_shadow, Shadow shadow = null)
	{
		FontSize = size;
		FontFamily = new FontFamily(fontfamily);
		FontColor = color;
		Shadow = shadow ?? new Shadow
		{
			Color = Colors.Black,
			ShadowDepth = 0.0,
			Opacity = 0.5,
			BlurRadius = 10.0,
			Visible = show_shadow
		};
	}
}
