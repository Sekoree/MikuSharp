using DisCatSharp.Lavalink.Entities;

using System;

namespace MikuSharp.Entities;

public class QueueEntry : Entry
{
	public int Position { get; set; }
	public ulong AddedBy { set; get; }

	public QueueEntry(LavalinkTrack t, ulong m, DateTimeOffset adddate, int pos) : base(t, adddate)
	{
		this.Position = pos;
		this.AddedBy = m;
	}
}