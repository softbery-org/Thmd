// Version: 0.1.0.16
using Thmd.Media;

namespace Thmd.Repeats;

public class RepeatManager
{
	private IPlayer _player;

	public RepeatManager(IPlayer player)
	{
		_player = player;
	}

	public void Repeat(Video media, RepeatType repeat)
	{
	}

	private Video RepeatOne()
	{
		return _player.Playlist.Current;
	}
}
