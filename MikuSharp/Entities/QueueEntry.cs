using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;
using System.Text;

namespace MikuSharp.Entities
{
    public class QueueEntry : Entry
    {
        public DiscordMember addedBy { set; get; }
        public QueueEntry(LavalinkTrack t, DiscordMember m) : base(t)
        {
            addedBy = m;
        }
    }
}
