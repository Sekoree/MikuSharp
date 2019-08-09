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
        public PlaylistEntry(LavalinkTrack t) : base(t)
        {}
    }
}
