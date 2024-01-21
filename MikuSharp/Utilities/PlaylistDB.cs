/*using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity.Extensions;
using DisCatSharp.Lavalink;

using FluentFTP;

using MikuSharp.Entities;
using MikuSharp.Enums;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MikuSharp.Utilities;

public class PlaylistDB
{
	public static async Task<Dictionary<string, Playlist>> GetPlaylists(DiscordGuild guild, ulong u)
	{
		var connString = MikuBot.Config.DbConnectString;
		var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();
		var cmd2 = new NpgsqlCommand($"SELECT * FROM playlists WHERE userid = {u} ORDER BY playlistname ASC;", conn);
		var reader = await cmd2.ExecuteReaderAsync();
		Dictionary<string, Playlist> lists = new();
		while (await reader.ReadAsync())
		{
			lists.Add(Convert.ToString(reader["playlistname"]), await GetPlaylist(guild, u, Convert.ToString(reader["playlistname"])));
		}
		reader.Close();
		cmd2.Dispose();
		conn.Close();
		conn.Dispose();
		return lists;
	}

	public static async Task<List<string>> GetPlaylistsSimple(ulong u)
	{
		var connString = MikuBot.Config.DbConnectString;
		var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();
		var cmd2 = new NpgsqlCommand($"SELECT * FROM playlists WHERE userid = {u} ORDER BY playlistname ASC;", conn);
		var reader = await cmd2.ExecuteReaderAsync();
		List<string> lists = new();
		while (await reader.ReadAsync())
		{
			lists.Add(Convert.ToString(reader["playlistname"]));
		}
		reader.Close();
		cmd2.Dispose();
		conn.Close();
		conn.Dispose();
		return lists;
	}

	public static async Task<Playlist> GetPlaylist(DiscordGuild guild, ulong u, string p)
	{
		var connString = MikuBot.Config.DbConnectString;
		var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();
		int am = 0;
		var cmd = new NpgsqlCommand($"SELECT COUNT(*)" +
			$"FROM playlistentries " +
			$"WHERE userid = {u} " +
			$"AND playlistname = @pl;", conn);
		cmd.Parameters.AddWithValue("pl", p);
		var reader = await cmd.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			am = Convert.ToInt32(reader["count"]);
		}
		reader.Close();
		cmd.Dispose();
		var cmd2 = new NpgsqlCommand($"SELECT *" +
			$"FROM playlists " +
			$"WHERE userid = {u} " +
			$"AND playlistname = @pl;", conn);
		var para = cmd2.CreateParameter();
		para.ParameterName = "pl";
		para.Value = p;
		cmd2.Parameters.Add(para);
		var reader2 = await cmd2.ExecuteReaderAsync();
		Playlist pl = null;
		while (await reader2.ReadAsync())
		{
			if (Music.GetExtService(Convert.ToString(reader2["extservice"])) != ExtService.None)
			{
				try
				{
					var ss = await MikuBot.LavalinkNodeConnections.First().Value.Rest.GetTracksAsync(new Uri(Convert.ToString(reader2["url"])));
					am = ss.Tracks.Count;
				}
				catch { }
			}
			pl = new Playlist(Music.GetExtService(Convert.ToString(reader2["extservice"])), Convert.ToString(reader2["url"]), Convert.ToString(reader2["playlistname"]), Convert.ToUInt64(reader2["userid"]), am, DateTimeOffset.Parse(Convert.ToString(reader2["creation"])), DateTimeOffset.Parse(Convert.ToString(reader2["changed"])));
		}
		reader2.Close();
		cmd2.Dispose();
		conn.Close();
		conn.Dispose();
		if (pl == null) throw new Exception("Tf is up? " + p);
		return pl;
	}

	public static async Task ReorderList(DiscordGuild guild, string p, ulong u)
	{
		var connString = MikuBot.Config.DbConnectString;
		var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();
		var listNow = await GetPlaylist(guild, u, p);
		var ln = await listNow.GetEntries();
		var cmd = new NpgsqlCommand($"DELETE FROM playlistentries WHERE userid = {u} AND playlistname = @pl;", conn);
		var para = cmd.CreateParameter();
		para.ParameterName = "pl";
		para.Value = p;
		cmd.Parameters.Add(para);
		await cmd.ExecuteNonQueryAsync();
		cmd.Dispose();
		int i = 0;
		string longcmd = "";
		foreach (var qi in ln)
		{
			string adddate = $"{qi.additionDate.UtcDateTime.Year}-{qi.additionDate.UtcDateTime.Month}-{qi.additionDate.UtcDateTime.Day} {qi.additionDate.UtcDateTime.Hour}:{qi.additionDate.UtcDateTime.Minute}:{qi.additionDate.UtcDateTime.Second}";
			string moddate = $"{qi.modifyDate.UtcDateTime.Year}-{qi.modifyDate.UtcDateTime.Month}-{qi.modifyDate.UtcDateTime.Day} {qi.modifyDate.UtcDateTime.Hour}:{qi.modifyDate.UtcDateTime.Minute}:{qi.modifyDate.UtcDateTime.Second}";
			longcmd += $"INSERT INTO playlistentries VALUES ({i},@p,@u,'{qi.track.TrackString}','{adddate}','{moddate}');";
			i++;
		}
		var cmd2 = new NpgsqlCommand(longcmd, conn);
		var para1 = cmd2.CreateParameter();
		para1.ParameterName = "p";
		para1.Value = p;
		cmd2.Parameters.Add(para1);
		var para2 = cmd2.CreateParameter();
		para2.ParameterName = "u";
		para2.Value = Convert.ToInt64(u);
		cmd2.Parameters.Add(para2);
		await cmd2.ExecuteNonQueryAsync();
		cmd2.Dispose();
		conn.Close();
		conn.Dispose();
	}

	public static async Task RebuildList(ulong u, string p, List<PlaylistEntry> q)
	{
		var connString = MikuBot.Config.DbConnectString;
		var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();
		var queueNow = q;
		var cmd = new NpgsqlCommand($"DELETE FROM playlistentries WHERE userid = {u} AND playlistname = @pl;", conn);
		var para = cmd.CreateParameter();
		para.ParameterName = "pl";
		para.Value = p;
		cmd.Parameters.Add(para);
		await cmd.ExecuteNonQueryAsync();
		cmd.Dispose();
		string longcmd = "";
		foreach (var qi in queueNow)
		{
			string adddate = $"{qi.additionDate.UtcDateTime.Year}-{qi.additionDate.UtcDateTime.Month}-{qi.additionDate.UtcDateTime.Day} {qi.additionDate.UtcDateTime.Hour}:{qi.additionDate.UtcDateTime.Minute}:{qi.additionDate.UtcDateTime.Second}";
			string moddate = $"{qi.modifyDate.UtcDateTime.Year}-{qi.modifyDate.UtcDateTime.Month}-{qi.modifyDate.UtcDateTime.Day} {qi.modifyDate.UtcDateTime.Hour}:{qi.modifyDate.UtcDateTime.Minute}:{qi.modifyDate.UtcDateTime.Second}";
			longcmd += $"INSERT INTO playlistentries VALUES ({qi.Position},@p,@u,'{qi.track.TrackString}','{adddate}','{moddate}');";
		}
		var cmd2 = new NpgsqlCommand(longcmd, conn);
		var para1 = cmd2.CreateParameter();
		para1.ParameterName = "p";
		para1.Value = p;
		cmd2.Parameters.Add(para1);
		var para2 = cmd2.CreateParameter();
		para2.ParameterName = "u";
		para2.Value = Convert.ToInt64(u);
		cmd2.Parameters.Add(para2);
		await cmd2.ExecuteNonQueryAsync();
		cmd2.Dispose();
		conn.Close();
		conn.Dispose();
	}

	public static async Task AddPlaylist(string p, ulong u, ExtService e = ExtService.None, string url = "")
	{
		var connString = MikuBot.Config.DbConnectString;
		var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();
		var cmd = new NpgsqlCommand("INSERT INTO playlists VALUES (@u,@p,@url,@ext,@cre,@mody)", conn);
		var para = cmd.CreateParameter();
		para.ParameterName = "u";
		para.Value = Convert.ToInt64(u);
		cmd.Parameters.Add(para);
		var para2 = cmd.CreateParameter();
		para2.ParameterName = "p";
		para2.Value = p;
		cmd.Parameters.Add(para2);
		var para3 = cmd.CreateParameter();
		para3.ParameterName = "url";
		para3.Value = url;
		cmd.Parameters.Add(para3);
		var para4 = cmd.CreateParameter();
		para4.ParameterName = "ext";
		para4.Value = e.ToString();
		cmd.Parameters.Add(para4);
		var para5 = cmd.CreateParameter();
		para5.ParameterName = "cre";
		para5.Value = DateTime.UtcNow;
		cmd.Parameters.Add(para5);
		var para6 = cmd.CreateParameter();
		para6.ParameterName = "mody";
		para6.Value = DateTime.UtcNow;
		cmd.Parameters.Add(para6);
		await cmd.ExecuteNonQueryAsync();
		cmd.Dispose();
		conn.Close();
		conn.Dispose();
	}

	public static async Task RemovePlaylist(string p, ulong u)
	{
		var connString = MikuBot.Config.DbConnectString;
		var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();
		var cmd = new NpgsqlCommand($"DELETE FROM playlists WHERE playlistname = @pl AND userid = {u};", conn);
		var para = cmd.CreateParameter();
		para.ParameterName = "pl";
		para.Value = p;
		cmd.Parameters.Add(para);
		await cmd.ExecuteNonQueryAsync();
		await ClearList(p, u);
		cmd.Dispose();
		conn.Close();
		conn.Dispose();
	}

	public static async Task AddEntry(string p, ulong u, string ts)
	{
		int position = 0;
		var connString = MikuBot.Config.DbConnectString;
		var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();
		var cmd2 = new NpgsqlCommand($"SELECT Count(*) FROM playlistentries WHERE userid = {u} AND playlistname = @pl;", conn);
		cmd2.Parameters.AddWithValue("pl", p);
		var reader = await cmd2.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			position = reader.GetInt32(0);
		}
		reader.Close();
		cmd2.Dispose();
		var cmd = new NpgsqlCommand("INSERT INTO playlistentries VALUES (@pos,@p,@u,@ts,@add,@mody);UPDATE playlists SET changed=@mody WHERE userid=@u AND playlistname=@p;", conn);
		var para = cmd.CreateParameter();
		para.ParameterName = "pos";
		para.Value = position;
		cmd.Parameters.Add(para);
		var para2 = cmd.CreateParameter();
		para2.ParameterName = "p";
		para2.Value = p;
		cmd.Parameters.Add(para2);
		var para3 = cmd.CreateParameter();
		para3.ParameterName = "u";
		para3.Value = Convert.ToInt64(u);
		cmd.Parameters.Add(para3);
		var para4 = cmd.CreateParameter();
		para4.ParameterName = "ts";
		para4.Value = ts;
		cmd.Parameters.Add(para4);
		var para5 = cmd.CreateParameter();
		para5.ParameterName = "add";
		para5.Value = DateTime.UtcNow;
		cmd.Parameters.Add(para5);
		var para6 = cmd.CreateParameter();
		para6.ParameterName = "mody";
		para6.Value = DateTime.UtcNow;
		cmd.Parameters.Add(para6);
		await cmd.ExecuteNonQueryAsync();
		cmd.Dispose();
		conn.Close();
		conn.Dispose();
	}

	public static async Task AddEntry(string p, ulong u, List<LavalinkTrack> ts)
	{
		int position = 0;
		var connString = MikuBot.Config.DbConnectString;
		var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();
		var cmd2 = new NpgsqlCommand($"SELECT Count(*) FROM playlistentries WHERE userid = {u} AND playlistname = @pl;", conn);
		var para = cmd2.CreateParameter();
		para.ParameterName = "pl";
		para.Value = p;
		cmd2.Parameters.Add(para);
		var reader = await cmd2.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			position = reader.GetInt32(0);
		}
		reader.Close();
		cmd2.Dispose();
		string longcmd = "UPDATE playlists SET changed=@mody WHERE userid=@u AND playlistname=@p;";
		foreach (var tt in ts)
		{
			longcmd += $"INSERT INTO playlistentries VALUES ({position},@p,@u,'{tt.TrackString}',@add,@mody);";
			position++;
		}
		var cmd = new NpgsqlCommand(longcmd, conn);
		var para2 = cmd.CreateParameter();
		para2.ParameterName = "p";
		para2.Value = p;
		cmd.Parameters.Add(para2);
		var para3 = cmd.CreateParameter();
		para3.ParameterName = "u";
		para3.Value = Convert.ToInt64(u);
		cmd.Parameters.Add(para3);
		var para4 = cmd.CreateParameter();
		para4.ParameterName = "add";
		para4.Value = DateTime.UtcNow;
		cmd.Parameters.Add(para4);
		var para5 = cmd.CreateParameter();
		para5.ParameterName = "mody";
		para5.Value = DateTime.UtcNow;
		cmd.Parameters.Add(para5);
		await cmd.ExecuteNonQueryAsync();
		cmd.Dispose();
		conn.Close();
		conn.Dispose();
	}

	public static async Task InsertEntry(DiscordGuild guild, string p, ulong u, string ts, int pos)
	{
		var qnow = await GetPlaylist(guild, u, p);
		var q = await qnow.GetEntries();
		q.Insert(pos, new PlaylistEntry(LavalinkUtilities.DecodeTrack(ts), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, pos));
		await RebuildList(u, p, q);
	}

	public static async Task InsertEntry(DiscordGuild guild, string p, ulong u, List<LavalinkTrack> ts, int pos)
	{
		var qnow = await GetPlaylist(guild, u, p);
		var q = await qnow.GetEntries();
		foreach (var tt in ts)
		{
			q.Insert(pos, new PlaylistEntry(LavalinkUtilities.DecodeTrack(tt.TrackString), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, pos));
		}
		await RebuildList(u, p, q);
	}

	public static async Task ClearList(string p, ulong u)
	{
		var connString = MikuBot.Config.DbConnectString;
		var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();
		var cmd = new NpgsqlCommand($"DELETE FROM playlistentries WHERE userid = {u} AND playlistname = @pl;", conn);
		var para = cmd.CreateParameter();
		para.ParameterName = "pl";
		para.Value = p;
		cmd.Parameters.Add(para);
		await cmd.ExecuteNonQueryAsync();
		cmd.Dispose();
		conn.Close();
		conn.Dispose();
	}

	public static async Task MoveListItems(DiscordGuild guild, string p, ulong u, int oldpos, int newpos)
	{
		var qnow = await GetPlaylist(guild, u, p);
		var q = await qnow.GetEntries();
		//(q[newpos], q[oldpos]) = (q[oldpos], q[newpos]);
		List<PlaylistEntry> tempQ = new(q.Count);
		List<PlaylistEntry> newQ = new(q.Count);
		foreach(var entry in q)
		{
			if (entry.Position == oldpos)
				entry.Position = newpos;
			else if (entry.Position == newpos)
				entry.Position = oldpos;
			else
				entry.Position = entry.Position;
			tempQ.Add(entry);
		}
		newQ.AddRange(tempQ.OrderBy(x => x.Position));
		await RebuildList(u, p, newQ);
	}

	public static async Task RemoveFromList(DiscordGuild guild, int position, string p, ulong u)
	{
		var connString = MikuBot.Config.DbConnectString;
		var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();
		var cmd = new NpgsqlCommand($"DELETE FROM playlistentries WHERE pos = {position} AND userid = {u} AND playlistname = @pl;UPDATE playlists SET changed=@mody WHERE userid= {u} AND playlistname=@pl;", conn);
		var para = cmd.CreateParameter();
		para.ParameterName = "pl";
		para.Value = p;
		cmd.Parameters.Add(para);
		var para2 = cmd.CreateParameter();
		para2.ParameterName = "mody";
		para2.Value = DateTime.UtcNow;
		cmd.Parameters.Add(para2);
		await cmd.ExecuteNonQueryAsync();
		cmd.Dispose();
		await ReorderList(guild, p, u);
		conn.Close();
		conn.Dispose();
	}

	public static async Task RenameList(string p, ulong u, string newname)
	{
		var connString = MikuBot.Config.DbConnectString;
		var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();
		var cmd = new NpgsqlCommand($"UPDATE playlists SET playlistname = @newn WHERE userid = {u} AND playlistname = @pl;", conn);
		var para = cmd.CreateParameter();
		para.ParameterName = "pl";
		para.Value = p;
		cmd.Parameters.Add(para);
		var para2 = cmd.CreateParameter();
		para2.ParameterName = "newn";
		para2.Value = newname;
		cmd.Parameters.Add(para2);
		await cmd.ExecuteNonQueryAsync();
		cmd.Dispose();
		var cmd2 = new NpgsqlCommand($"UPDATE playlistentries SET playlistname = @newn WHERE userid = {u} AND playlistname = @pl;UPDATE playlists SET changed = @mody WHERE userid= {u} AND playlistname = @newn;", conn);
		var para3 = cmd2.CreateParameter();
		para3.ParameterName = "pl";
		para3.Value = p;
		cmd2.Parameters.Add(para3);
		var para4 = cmd2.CreateParameter();
		para4.ParameterName = "newn";
		para4.Value = newname;
		cmd2.Parameters.Add(para4);
		var para5 = cmd2.CreateParameter();
		para5.ParameterName = "mody";
		para5.Value = DateTime.UtcNow;
		cmd2.Parameters.Add(para5);
		await cmd2.ExecuteNonQueryAsync();
		cmd2.Dispose();
		conn.Close();
		conn.Dispose();
	}

	public static async Task<TrackResult> GetSong(string n, InteractionContext ctx)
	{
		var nodeConnection = MikuBot.LavalinkNodeConnections.First().Value;
		var inter = ctx.Client.GetInteractivity();
		if (n.ToLower().StartsWith("http://nicovideo.jp")
			|| n.ToLower().StartsWith("http://sp.nicovideo.jp")
			|| n.ToLower().StartsWith("https://nicovideo.jp")
			|| n.ToLower().StartsWith("https://sp.nicovideo.jp")
			|| n.ToLower().StartsWith("http://www.nicovideo.jp")
			|| n.ToLower().StartsWith("https://www.nicovideo.jp"))
		{
			var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Processing NND Video...").AsEphemeral());
			var split = n.Split("/".ToCharArray());
			var nndID = split.First(x => x.StartsWith("sm") || x.StartsWith("nm")).Split("?")[0];
			FtpClient ftpClient = new(MikuBot.Config.NndConfig.FtpConfig.Hostname, new NetworkCredential(MikuBot.Config.NndConfig.FtpConfig.User, MikuBot.Config.NndConfig.FtpConfig.Password));
			ftpClient.Connect();
			if (!ftpClient.FileExists($"{nndID}.mp3"))
			{
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Preparing download..."));
				var ex = await ctx.GetNNDAsync(n, nndID, msg.Id);
				if (ex == null)
				{
					await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Please try again or verify the link"));
					return null;
				}
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Uploading..."));
				ftpClient.UploadStream(ex, $"{nndID}.mp3", FtpRemoteExists.Skip, true);
			}
			var Track = await nodeConnection.Rest.GetTracksAsync(new Uri($"https://nnd.meek.moe/new/{nndID}.mp3"));
			return new TrackResult(Track.PlaylistInfo, Track.Tracks.First());
		}
		else if (n.StartsWith("http://") | n.StartsWith("https://"))
		{
			var s = await nodeConnection.Rest.GetTracksAsync(new Uri(n));
			switch (s.LoadResultType)
			{
				case LavalinkLoadResultType.LoadFailed:
					{
						await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("Loading this song/playlist failed, please try again, reasons could be:\n" +
							"> Playlist is set to private or unlisted\n" +
							"> The song is unavailable/deleted").Build()));
						return null;
					};
				case LavalinkLoadResultType.NoMatches:
					{
						await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("No song/playlist was found with this URL, please try again/a different one").Build()));
						return null;
					};
				case LavalinkLoadResultType.PlaylistLoaded:
					{
						if (s.PlaylistInfo.SelectedTrack == -1)
						{
							List<DiscordButtonComponent> buttons = new(2)
							{
								new DiscordButtonComponent(ButtonStyle.Success, "yes", "Add entire playlist"),
								new DiscordButtonComponent(ButtonStyle.Primary, "no", "Don't add")
							};
							var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder()
								.WithTitle("Playlist link detected!")
								.WithDescription("Choose how to handle the playlist link")
								.WithAuthor($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator} || Timeout 30 seconds", iconUrl: ctx.Member.AvatarUrl)
								.Build()).AddComponents(buttons));
							var resp = await inter.WaitForButtonAsync(msg, ctx.User, TimeSpan.FromSeconds(30));
							if (resp.TimedOut)
							{
								buttons.ForEach(x => x.Disable());
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent("Timed out!"));
								return null;
							}
							else if (resp.Result.Id == "yes")
							{
								buttons.ForEach(x => x.Disable());
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent("Adding entire playlist"));
								return new TrackResult(s.PlaylistInfo, s.Tracks);
							}
							else
							{
								buttons.ForEach(x => x.Disable());
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent("Canceled!"));
								return null;
							}
						}
						else
						{
							List<DiscordButtonComponent> buttons = new(3)
							{
								new DiscordButtonComponent(ButtonStyle.Primary, "yes", "Add only referred song"),
								new DiscordButtonComponent(ButtonStyle.Success, "yes", "Add the entire playlist"),
								new DiscordButtonComponent(ButtonStyle.Danger, "no", "Cancel")
							};
							var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder()
								.WithTitle("Link with Playlist detected!")
								.WithDescription("Please choose how to handle the playlist link")
								.WithAuthor($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator} || Timeout 30 seconds", iconUrl: ctx.Member.AvatarUrl)
								.Build()).AddComponents(buttons));
							var resp = await inter.WaitForButtonAsync(msg, ctx.User, TimeSpan.FromSeconds(30));
							if (resp.TimedOut)
							{
								buttons.ForEach(x => x.Disable());
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent("Timed out!"));
								return null;
							}
							else if (resp.Result.Id == "yes")
							{
								buttons.ForEach(x => x.Disable());
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent($"Adding single song: {s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack).Title}"));
								return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack));
							}
							else if (resp.Result.Id == "all")
							{
								buttons.ForEach(x => x.Disable());
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent($"Adding entire playlist: {s.PlaylistInfo.Name}"));
								return new TrackResult(s.PlaylistInfo, s.Tracks);
							}
							else
							{
								buttons.ForEach(x => x.Disable());
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent("Canceled!"));
								return null;
							}
						}
					};
				default:
					{
						await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent($"Playing single song: {s.Tracks.First().Title}"));
						return new TrackResult(s.PlaylistInfo, s.Tracks.First());
					};
			}
		}
		else
		{
			var s = await nodeConnection.Rest.GetTracksAsync(n);
			switch (s.LoadResultType)
			{
				case LavalinkLoadResultType.LoadFailed:
					{
						await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("Loading this song/playlist failed, please try again, reasons could be:\n" +
							"> The song is unavailable/deleted").Build()));
						return null;
					};
				case LavalinkLoadResultType.NoMatches:
					{
						await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("No song was found, please try again").Build()));
						return null;
					};
				default:
					{
						int leng = s.Tracks.Count;
						if (leng > 5) leng = 5;
						List<DiscordStringSelectComponentOption> selectOptions = new(leng)
						{

						};
						DiscordStringSelectComponent select = new("Select song to play", selectOptions, minOptions: 1, maxOptions: 1);
						var em = new DiscordEmbedBuilder()
							.WithTitle("Results!")
							.WithDescription("Please select a track:\n")
							.WithAuthor($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator} || Timeout 30 seconds", iconUrl: ctx.Member.AvatarUrl);
						for (int i = 0; i < leng; i++)
						{
							em.AddField(new DiscordEmbedField($"{i + 1}.{s.Tracks.ElementAt(i).Title} [{s.Tracks.ElementAt(i).Length}]", $"by {s.Tracks.ElementAt(i).Author} [Link]({s.Tracks.ElementAt(i).Uri})"));
						}
						var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(em.Build()).AddComponents(select));
						var resp = await inter.WaitForSelectAsync(msg, ctx.User, select.CustomId, ComponentType.StringSelect, TimeSpan.FromSeconds(30));
						if (resp.TimedOut)
						{
							select.Disable();
							await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(select).WithContent("Timed out!"));
							return null;
						}
						var trackSelect = Convert.ToInt32(resp.Result.Values.First());
						var track = s.Tracks.ElementAt(trackSelect);
						select.Disable();
						await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(select).WithContent($"Choosed {track.Title}"));

						return new TrackResult(s.PlaylistInfo, track);
					};
			}
		}
	}
}
*/

