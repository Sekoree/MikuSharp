using MikuSharp.Enums;

namespace MikuSharp.Entities;

public class Playlist
{
	public string Name { get; set; }
	public ulong UserID { get; set; }
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
		this.UserID = usr;
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
				await conn.OpenAsync(MikuBot._canellationTokenSource.Token);
				var cmd = new NpgsqlCommand("SELECT * FROM playlistentries WHERE userid = @userId AND playlistname = @playlistName ORDER BY pos ASC;", conn);
				cmd.Parameters.AddWithValue("userId", Convert.ToInt64(this.UserID));
				cmd.Parameters.AddWithValue("playlistName", this.Name);
				cmd.Prepare();
				var reader = await cmd.ExecuteReaderAsync(MikuBot._canellationTokenSource.Token);
				while (await reader.ReadAsync(MikuBot._canellationTokenSource.Token))
					entries.Add(new PlaylistEntry(LavalinkUtilities.DecodeTrack(Convert.ToString(reader["trackstring"])), DateTimeOffset.Parse(Convert.ToString(reader["addition"])), DateTimeOffset.Parse(Convert.ToString(reader["changed"])), Convert.ToInt32(reader["pos"])));
				reader.Close();
				cmd.Dispose();
				conn.Close();
				conn.Dispose();
			}
			else
			{
				var conn = MikuBot.LavalinkNodeConnections?.First().Value ?? null;
				LavalinkGuildConnection? client = null;
				if (conn.ConnectedGuilds.Any())
					client = conn.ConnectedGuilds.First().Value;
				if (client != null)
				{
					var trs = await client.GetTracksAsync(new Uri(this.Url));
					var i = 0;
					foreach (var t in trs.Tracks)
					{
						entries.Add(new PlaylistEntry(t, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, i));
						i++;
					}
				}
			}
		}
		return entries;
	}
}
