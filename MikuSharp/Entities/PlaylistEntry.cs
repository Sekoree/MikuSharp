using DisCatSharp.Lavalink;

namespace MikuSharp.Entities;

public class PlaylistEntry : Entry
{
	public DateTimeOffset modifyDate { get; set; }
	public int Position { get; set; }
	public PlaylistEntry(LavalinkTrack t, DateTimeOffset addDate, DateTimeOffset moddate, int pos) : base(t, addDate)
	{
		modifyDate = moddate;
		Position = pos;
	}
}
