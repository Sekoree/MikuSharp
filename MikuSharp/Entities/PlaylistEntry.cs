using DisCatSharp.Lavalink.Entities;

using System;

namespace MikuSharp.Entities;

public class PlaylistEntry : Entry
{
	public DateTimeOffset ModifyDate { get; set; }
	public int Position { get; set; }

	public PlaylistEntry(LavalinkTrack t, DateTimeOffset addDate, DateTimeOffset moddate, int pos) : base(t, addDate)
	{
		this.ModifyDate = moddate;
		this.Position = pos;
	}
}