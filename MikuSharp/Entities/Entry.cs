using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;
using System.Text;

namespace MikuSharp.Entities
{
    public class Entry
    {
        public LavalinkTrack track { get; protected set; }
        public DateTimeOffset additionDate { get; protected set; }
        public Entry(LavalinkTrack t)
        {
            track = t;
            additionDate = DateTimeOffset.UtcNow;
        }
    }
}
