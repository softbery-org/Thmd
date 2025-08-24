// Version: 0.1.0.35
using System.Collections.Generic;
using Thmd.Media;

namespace Thmd.Controls;

public class VideoExampleList
{
	public static List<Video> VideosList { get; set; } = GetVideos();

	public static List<Video> GetVideos()
	{
		List<Video> list = new List<Video>();
		list.Add(new Video("F:\\Filmy\\A Minecraft Movie\\Minecraft Film A Minecraft Movie 2025 - Filman cc - Filmy i Seri.mp4"));
		list.Add(new Video("F:\\Filmy\\Arcane\\Sezon 2\\Arcane - Sezon 2 Odcinek 1 - Filman cc - Filmy i Seriale Online .mp4"));
		list.Add(new Video("F:\\Filmy\\Arcane\\Sezon 2\\Arcane - Sezon 2 Odcinek 2 - Filman cc - Filmy i Seriale Online .mp4"));
		list.Add(new Video("F:\\Filmy\\Arcane\\Sezon 2\\Arcane - Sezon 2 Odcinek 3 - Filman cc - Filmy i Seriale Online .mp4"));
		list.Add(new Video("F:\\Filmy\\Futurama\\Futurama S07E03 PL 720p.mp4"));
		return list;
	}
}
