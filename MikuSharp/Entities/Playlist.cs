using DisCatSharp.Lavalink.Entities;

using MikuSharp.Enums;

namespace MikuSharp.Entities;

public class Playlist
{
	public string Name { get; set; }
	public ulong UserId { get; set; }
	public ExtService ExternalService { get; set; }
	public string Url { get; set; }
	public int SongCount { get; set; }
	public DateTimeOffset Creation { get; set; }
	public DateTimeOffset Modify { get; set; }

	public Playlist(ExtService e, string u, string n, ulong usr, int c, DateTimeOffset crea, DateTimeOffset mody)
	{
		this.ExternalService = e;
		this.Url = u;
		this.Name = n;
		this.UserId = usr;
		this.SongCount = c;
		this.Creation = crea;
		this.Modify = mody;
	}

	public async Task<List<PlaylistEntry>> GetEntriesAsync()
	{
		var entries = new List<PlaylistEntry>();
		if (this.SongCount > 0)
		{
			if (this.ExternalService == ExtService.None)
			{
				var connString = MikuBot.Config.DbConnectString;
				var conn = new NpgsqlConnection(connString);
				await conn.OpenAsync(MikuBot.CanellationTokenSource.Token);
				var cmd = new NpgsqlCommand("SELECT * FROM playlistentries WHERE userid = @userId AND playlistname = @playlistName ORDER BY pos ASC;", conn);
				cmd.Parameters.AddWithValue("userId", Convert.ToInt64(this.UserId));
				cmd.Parameters.AddWithValue("playlistName", this.Name);
				await cmd.PrepareAsync();
				var ll = MikuBot.ShardedClient.GetShard(0).GetLavalink();
				var reader = await cmd.ExecuteReaderAsync(MikuBot.CanellationTokenSource.Token);
				while (await reader.ReadAsync(MikuBot.CanellationTokenSource.Token))
					entries.Add(new PlaylistEntry(await ll.ConnectedSessions.First().Value.DecodeTrackAsync(Convert.ToString(reader["trackstring"])), DateTimeOffset.Parse(Convert.ToString(reader["addition"])), DateTimeOffset.Parse(Convert.ToString(reader["changed"])), Convert.ToInt32(reader["pos"])));
				await reader.CloseAsync();
				await cmd.DisposeAsync();
				await conn.CloseAsync();
				await conn.DisposeAsync();
			}
			else
			{
				var conn = MikuBot.LavalinkSessions?.First().Value ?? null;
				LavalinkGuildPlayer? client = null;
				if (conn.ConnectedPlayers.Any())
					client = conn.ConnectedPlayers.First().Value;
				if (client == null)
					return entries;
				var trs = await client.LoadTracksAsync(this.Url);
				var i = 0;
				foreach (var t in ((LavalinkPlaylist)trs.Result).Tracks)
				{
					entries.Add(new PlaylistEntry(t, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, i));
					i++;
				}
			}
		}
		return entries;
	}
}
