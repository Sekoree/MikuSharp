using DisCatSharp.Lavalink.Entities;

using System;

namespace MikuSharp.Entities;

public class Entry
{
	public LavalinkTrack Track { get; protected set; }
	public DateTimeOffset AdditionDate { get; protected set; }

	public Entry(LavalinkTrack t, DateTimeOffset addtime)
	{
		this.Track = t;
		this.AdditionDate = addtime;
	}
}