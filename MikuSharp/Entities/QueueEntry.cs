using DisCatSharp.Lavalink;

using System;

namespace MikuSharp.Entities
{
    public class QueueEntry : Entry
    {
        public int position { get; set; }
        public ulong addedBy { set; get; }
        public QueueEntry(LavalinkTrack t, ulong m, DateTimeOffset adddate, int pos) : base(t, adddate)
        {
            position = pos;
            addedBy = m;
        }
    }
}
