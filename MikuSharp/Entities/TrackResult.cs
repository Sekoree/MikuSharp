namespace MikuSharp.Entities;

public class TrackResult
{
	public LavalinkPlaylistInfo PlaylistInfo { get; set; }

	public List<LavalinkTrack> Tracks { get; set; } = new();

	public TrackResult(LavalinkPlaylistInfo playlistInfo, IEnumerable<LavalinkTrack> tracks)
	{
		this.PlaylistInfo = playlistInfo;
		this.Tracks.AddRange(tracks);
	}
	public TrackResult(LavalinkPlaylistInfo playlistInfo, LavalinkTrack track)
	{
		this.PlaylistInfo = playlistInfo;
		this.Tracks.Add(track);
	}
}
