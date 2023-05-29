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

	public async Task<List<PlaylistEntry>> GetEntries()
	{
		var Entries = new List<PlaylistEntry>(this.SongCount);
		if (this.ExternalService == ExtService.None)
		{
			var connString = MikuBot.Config.DbConnectString;
			var conn = new NpgsqlConnection(connString);
			await conn.OpenAsync();
			var cmd2 = new NpgsqlCommand($"SELECT * FROM playlistentries WHERE userid = {this.UserID} AND playlistname = @pl ORDER BY pos ASC;", conn);
			cmd2.Parameters.AddWithValue("pl", this.Name);
			var reader = await cmd2.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				Entries.Add(new PlaylistEntry(LavalinkUtilities.DecodeTrack(Convert.ToString(reader["trackstring"])), DateTimeOffset.Parse(Convert.ToString(reader["addition"])), DateTimeOffset.Parse(Convert.ToString(reader["changed"])), Convert.ToInt32(reader["pos"])));
			}
			reader.Close();
			cmd2.Dispose();
			conn.Close();
			conn.Dispose();
		}
		else
		{
			var trs = await MikuBot.LavalinkNodeConnections.First().Value.ConnectedGuilds.First().Value.GetTracksAsync(new Uri(this.Url));
			int i = 0;
			foreach (var t in trs.Tracks)
			{
				Entries.Add(new PlaylistEntry(t, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, i));
				i++;
			}
		}
		return Entries;
	}
}
