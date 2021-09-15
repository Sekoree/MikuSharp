using DisCatSharp.Lavalink;

using System;

namespace MikuSharp.Entities
{
    public class PlaylistEntry : Entry
    {
        public DateTimeOffset modifyDate { get; set; }
        public PlaylistEntry(LavalinkTrack t, DateTimeOffset addDate, DateTimeOffset moddate) : base(t, addDate)
        {
            modifyDate = moddate;
        }
    }
}
