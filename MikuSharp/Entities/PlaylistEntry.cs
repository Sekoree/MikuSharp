using DisCatSharp.Lavalink.Entities;

namespace MikuSharp.Entities;

public class PlaylistEntry : Entry
{
	public DateTimeOffset ModifyDate { get; set; }

	public int Position { get; set; }

	public PlaylistEntry(LavalinkTrack track, DateTimeOffset additionDate, DateTimeOffset modifyDate, int position)
		: base(track, additionDate)
	{
		this.ModifyDate = modifyDate;
		this.Position = position;
	}
}
