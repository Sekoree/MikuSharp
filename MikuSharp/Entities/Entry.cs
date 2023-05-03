using DisCatSharp.Lavalink;

namespace MikuSharp.Entities;

public class Entry
{
	public LavalinkTrack track { get; protected set; }
	public DateTimeOffset additionDate { get; protected set; }
	public Entry(LavalinkTrack t, DateTimeOffset addtime)
	{
		track = t;
		additionDate = addtime;
	}
}
