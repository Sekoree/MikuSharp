using System;

using DisCatSharp.Lavalink.Entities;

namespace MikuSharp.Entities;

public class PlaylistEntry : Entry
{
    public PlaylistEntry(LavalinkTrack t, DateTimeOffset addDate, DateTimeOffset moddate, int pos) : base(t, addDate)
    {
        this.ModifyDate = moddate;
        this.Position = pos;
    }

    public DateTimeOffset ModifyDate { get; set; }
    public int Position { get; set; }
}
