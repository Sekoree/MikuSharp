using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;
using System.Text;

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
