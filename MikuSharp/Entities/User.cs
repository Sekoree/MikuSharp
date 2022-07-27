using System.Collections.Generic;
using System.Threading.Tasks;

namespace MikuSharp.Entities;

public class User
{
	public Dictionary<ulong, List<string>> Prefixes { get; set; }
	public Dictionary<string, Playlist> Playlists { get; set; }
	public Task UpdateExtPlaylists { get; set; }
}
