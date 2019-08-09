using System;
using System.Collections.Generic;
using System.Text;

namespace MikuSharp.Entities
{
    public class Playlist
    {
        public bool External { get; set; }
        public string Url { get; set; }
        public List<PlaylistEntry> Entries { get; set; }
    }
}
