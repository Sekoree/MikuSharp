using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;
using System.Text;

namespace MikuSharp.Entities
{
    public class TrackResult
    {
        public LavalinkPlaylistInfo PlaylistInfo { get; set; }
        public List<LavalinkTrack> Tracks { get; set; }
        public TrackResult(LavalinkPlaylistInfo pl, IEnumerable<LavalinkTrack> tr)
        {
            PlaylistInfo = pl;
            Tracks = new List<LavalinkTrack>();
            Tracks.AddRange(tr);
        }
        public TrackResult(LavalinkPlaylistInfo pl, LavalinkTrack tr)
        {
            PlaylistInfo = pl;
            Tracks = new List<LavalinkTrack>();
            Tracks.Add(tr);
        }
    }
}
