using DisCatSharp.Lavalink.Entities;

namespace MikuSharp.Entities;

public class TrackResult
{
	public string Name { get; set; }

	public List<LavalinkTrack> Tracks { get; set; } = new();

	public TrackResult(string name, IEnumerable<LavalinkTrack> tracks)
	{
		this.Name = name;
		this.Tracks.AddRange(tracks);
	}
	public TrackResult(string name, LavalinkTrack track)
	{
		this.Name = name;
		this.Tracks.Add(track);
	}
}
