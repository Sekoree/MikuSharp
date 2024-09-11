using System;

using DisCatSharp.Lavalink.Entities;

namespace MikuSharp.Entities;

public class QueueEntry : Entry
{
    public QueueEntry(LavalinkTrack t, ulong m, DateTimeOffset adddate, int pos) : base(t, adddate)
    {
        this.Position = pos;
        this.AddedBy = m;
    }

    public int Position { get; set; }
    public ulong AddedBy { set; get; }
}
