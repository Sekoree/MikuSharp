using DisCatSharp.Lavalink.Entities;

using System.Collections.Generic;

namespace MikuSharp.Entities;

public class TrackResult
{
	public LavalinkPlaylistInfo PlaylistInfo { get; set; }
	public List<LavalinkTrack> Tracks { get; set; }

	public TrackResult(LavalinkPlaylistInfo pl, IEnumerable<LavalinkTrack> tr)
	{
		this.PlaylistInfo = pl;
		this.Tracks = new();
		this.Tracks.AddRange(tr);
	}

	public TrackResult(LavalinkPlaylistInfo pl, LavalinkTrack tr)
	{
		this.PlaylistInfo = pl;
		this.Tracks = new()
		{
			tr
		};
	}
}