using System.Text.RegularExpressions;

using MikuSharp.Entities;
using MikuSharp.Enums;

namespace MikuSharp.Utilities;

public static partial class PlaylistDB
{
	[GeneratedRegex("[^a-zA-Z0-9_\\- ]")]
	private static partial Regex PlaylistNameRegex();

	public static async Task<Dictionary<string, Playlist>> GetPlaylists(this DiscordGuild guild, ulong userId)
	{
		try
		{

			var connString = MikuBot.Config.DbConnectString;
			using var conn = new NpgsqlConnection(connString);
			await conn.OpenAsync(MikuBot._canellationTokenSource.Token);
			var cmd = new NpgsqlCommand("SELECT * FROM playlists WHERE userid = @userId ORDER BY playlistname ASC;", conn);
			cmd.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
			var reader = await cmd.ExecuteReaderAsync(MikuBot._canellationTokenSource.Token);
			var playlists = new Dictionary<string, Playlist>();
			while (await reader.ReadAsync(MikuBot._canellationTokenSource.Token))
			{
				var playlistName = Convert.ToString(reader["playlistname"]);
				var playlist = await GetPlaylist(guild, userId, playlistName);
				playlists.Add(playlistName, playlist);
			}
			reader.Close();
			cmd.Dispose();
			conn.Close();
			conn.Dispose();
			MikuBot.ShardedClient.Logger.LogDebug("User {name} has {count} playlists", userId, playlists.Count);
			return playlists;
		}
		catch (Exception ex)
		{
			MikuBot.ShardedClient.Logger.LogError(ex.Message);
			MikuBot.ShardedClient.Logger.LogError(ex.StackTrace);
			return null;
		}
	}

	public static async Task<List<string>> GetPlaylistsSimple(ulong userId)
	{
		var connString = MikuBot.Config.DbConnectString;
		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);
		var cmd = new NpgsqlCommand("SELECT playlistname FROM playlists WHERE userid = @userId ORDER BY playlistname ASC;", conn);
		cmd.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
		var reader = await cmd.ExecuteReaderAsync(MikuBot._canellationTokenSource.Token);
		var playlists = new List<string>();
		while (await reader.ReadAsync(MikuBot._canellationTokenSource.Token))
			playlists.Add(Convert.ToString(reader["playlistname"]));
		reader.Close();
		cmd.Dispose();
		conn.Close();
		conn.Dispose();
		return playlists;
	}

	public static async Task<Playlist> GetPlaylist(this DiscordGuild guild, ulong userId, string playlistName)
	{
		try
		{
			var connString = MikuBot.Config.DbConnectString;
			using var conn = new NpgsqlConnection(connString);
			await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

			var entryCount = 0;
			var countCmd = new NpgsqlCommand("SELECT COUNT(*) FROM playlistentries WHERE userid = @userId AND playlistname = @playlistName;", conn);
			countCmd.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
			countCmd.Parameters.AddWithValue("playlistName", playlistName);
			var countReader = await countCmd.ExecuteReaderAsync(MikuBot._canellationTokenSource.Token);
			while (await countReader.ReadAsync(MikuBot._canellationTokenSource.Token))
				entryCount = Convert.ToInt32(countReader["count"]);
			countReader.Close();
			countCmd.Dispose();

			var playlistCmd = new NpgsqlCommand("SELECT * FROM playlists WHERE userid = @userId AND playlistname = @playlistName;", conn);
			playlistCmd.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
			playlistCmd.Parameters.AddWithValue("playlistName", playlistName);
			var playlistReader = await playlistCmd.ExecuteReaderAsync(MikuBot._canellationTokenSource.Token);

			Playlist playlist = null;
			while (await playlistReader.ReadAsync(MikuBot._canellationTokenSource.Token))
			{
				var extService = Convert.ToString(playlistReader["extservice"]);
				if (Music.GetExtService(extService) != ExtService.None)
					try
					{
						var url = new Uri(Convert.ToString(playlistReader["url"]));
						var ss = await MikuBot.LavalinkNodeConnections.First().Value.Rest.GetTracksAsync(url);
						entryCount = ss.Tracks.Count;
					}
					catch { }
				playlist = new Playlist(
					Music.GetExtService(extService),
					Convert.ToString(playlistReader["url"]),
					Convert.ToString(playlistReader["playlistname"]),
					Convert.ToUInt64(playlistReader["userid"]),
					entryCount,
					DateTimeOffset.Parse(Convert.ToString(playlistReader["creation"])),
					DateTimeOffset.Parse(Convert.ToString(playlistReader["changed"]))
				);
			}

			playlistReader.Close();
			playlistCmd.Dispose();
			conn.Close();
			conn.Dispose();
			MikuBot.ShardedClient.Logger.LogDebug("Playlist {name} has {count} entries", playlistName, entryCount);
			return playlist ?? throw new Exception("The playlist " + playlistName + " could not be found.");

		}
		catch (Exception ex)
		{
			MikuBot.ShardedClient.Logger.LogError(ex.Message);
			MikuBot.ShardedClient.Logger.LogError(ex.StackTrace);
			return null;
		}
	}

	public static async Task ReorderList(this DiscordGuild guild, string playlistName, ulong userId)
	{
		var connString = MikuBot.Config.DbConnectString;
		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		var playlist = await GetPlaylist(guild, userId, playlistName);
		var entries = await playlist.GetEntries();

		var deleteCmd = new NpgsqlCommand("DELETE FROM playlistentries WHERE userid = @userId AND playlistname = @playlistName;", conn);
		deleteCmd.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
		deleteCmd.Parameters.AddWithValue("playlistName", playlistName);
		await deleteCmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);
		deleteCmd.Dispose();

		var position = 0;
		var insertCmdText = "";
		foreach (var entry in entries)
		{
			var additionDate = entry.AdditionDate.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss");
			var modifyDate = entry.ModifyDate.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss");
			insertCmdText += $"INSERT INTO playlistentries VALUES ({position}, @playlistName, @userId, '{entry.Track.TrackString}', '{additionDate}', '{modifyDate}');";
			position++;
		}

		var insertCmd = new NpgsqlCommand(insertCmdText, conn);
		insertCmd.Parameters.AddWithValue("playlistName", playlistName);
		insertCmd.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
		await insertCmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);
		insertCmd.Dispose();

		conn.Close();
		conn.Dispose();
	}

	public static async Task RebuildList(ulong userId, string playlistName, List<PlaylistEntry> entries)
	{
		var connString = MikuBot.Config.DbConnectString;
		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		var cmdDelete = new NpgsqlCommand("DELETE FROM playlistentries WHERE userid = @userId AND playlistname = @playlistName;", conn);
		cmdDelete.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
		cmdDelete.Parameters.AddWithValue("playlistName", playlistName);
		await cmdDelete.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);
		cmdDelete.Dispose();

		var insertCmdText = "";
		foreach (var entry in entries)
		{
			var additionDate = entry.AdditionDate.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss");
			var modifyDate = entry.ModifyDate.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss");
			insertCmdText += $"INSERT INTO playlistentries VALUES ({entry.Position}, @playlistName, @userId, '{entry.Track.TrackString}', '{additionDate}', '{modifyDate}');";
		}

		var cmdInsert = new NpgsqlCommand(insertCmdText, conn);
		cmdInsert.Parameters.AddWithValue("playlistName", playlistName);
		cmdInsert.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
		await cmdInsert.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);
		cmdInsert.Dispose();

		conn.Close();
		conn.Dispose();
	}

	/// <summary>
	/// Tries to normalize a playlist name.
	/// </summary>
	/// <param name="desiredPlaylistName">The desired name of the playlist the user wants to create or update.</param>
	/// <param name="normalizedPlaylistName">Our normalized database compatible playlist name.</param>
	/// <returns>Whether the playlist name could be normlized.</returns>
	public static bool TryNormalize(this string desiredPlaylistName, out string normalizedPlaylistName)
	{
		normalizedPlaylistName = PlaylistNameRegex().Replace(desiredPlaylistName, "");

		return !string.IsNullOrEmpty(normalizedPlaylistName) && !string.IsNullOrWhiteSpace(normalizedPlaylistName);
	}

	public static async Task AddPlaylist(string playlistName, ulong userId, ExtService extService = ExtService.None, string url = "")
	{
		var connString = MikuBot.Config.DbConnectString;
		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		var cmd = new NpgsqlCommand("INSERT INTO playlists VALUES (@userId, @playlistName, @url, @extService, @creationDate, @modifiedDate)", conn);
		cmd.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
		cmd.Parameters.AddWithValue("playlistName", playlistName);
		cmd.Parameters.AddWithValue("url", url);
		cmd.Parameters.AddWithValue("extService", extService.ToString());
		cmd.Parameters.AddWithValue("creationDate", DateTime.UtcNow);
		cmd.Parameters.AddWithValue("modifiedDate", DateTime.UtcNow);
		await cmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);
		cmd.Dispose();

		conn.Close();
		conn.Dispose();
	}

	public static async Task RemovePlaylist(string playlistName, ulong userId)
	{
		var connString = MikuBot.Config.DbConnectString;
		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		var cmd = new NpgsqlCommand("DELETE FROM playlists WHERE playlistname = @playlistName AND userid = @userId;", conn);
		cmd.Parameters.AddWithValue("playlistName", playlistName);
		cmd.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
		await cmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);
		cmd.Dispose();

		await ClearList(playlistName, userId);

		conn.Close();
		conn.Dispose();
	}

	public static async Task AddEntry(string playlistName, ulong userId, string trackString)
	{
		var position = 0;
		var connString = MikuBot.Config.DbConnectString;
		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		var cmd2 = new NpgsqlCommand($"SELECT COUNT(*) FROM playlistentries WHERE userid = @userId AND playlistname = @playlistName;", conn);
		cmd2.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
		cmd2.Parameters.AddWithValue("playlistName", playlistName);
		var reader = await cmd2.ExecuteReaderAsync(MikuBot._canellationTokenSource.Token);
		while (await reader.ReadAsync(MikuBot._canellationTokenSource.Token))
			position = reader.GetInt32(0);
		reader.Close();
		cmd2.Dispose();

		var cmd = new NpgsqlCommand("INSERT INTO playlistentries VALUES (@position, @playlistName, @userId, @trackString, @additionDate, @modifyDate);" +
									"UPDATE playlists SET changed = @modifyDate WHERE userid = @userId AND playlistname = @playlistName;", conn);
		cmd.Parameters.AddWithValue("position", position);
		cmd.Parameters.AddWithValue("playlistName", playlistName);
		cmd.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
		cmd.Parameters.AddWithValue("trackString", trackString);
		cmd.Parameters.AddWithValue("additionDate", DateTime.UtcNow);
		cmd.Parameters.AddWithValue("modifyDate", DateTime.UtcNow);
		await cmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);
		cmd.Dispose();

		conn.Close();
		conn.Dispose();
	}

	public static async Task AddEntry(string playlistName, ulong userId, List<LavalinkTrack> tracks)
	{
		var position = 0;
		var connString = MikuBot.Config.DbConnectString;
		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		var cmd2 = new NpgsqlCommand("SELECT COUNT(*) FROM playlistentries WHERE userid = @userId AND playlistname = @playlistName;", conn);
		cmd2.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
		cmd2.Parameters.AddWithValue("playlistName", playlistName);
		var reader = await cmd2.ExecuteReaderAsync(MikuBot._canellationTokenSource.Token);
		while (await reader.ReadAsync(MikuBot._canellationTokenSource.Token))
			position = reader.GetInt32(0);
		reader.Close();
		cmd2.Dispose();

		var longcmd = "UPDATE playlists SET changed = @modifyDate WHERE userid = @userId AND playlistname = @playlistName;";
		foreach (var track in tracks)
		{
			longcmd += $"INSERT INTO playlistentries VALUES (@position, @playlistName, @userId, '{track.TrackString}', @additionDate, @modifyDate);";
			position++;
		}

		var cmd = new NpgsqlCommand(longcmd, conn);
		cmd.Parameters.AddWithValue("position", position);
		cmd.Parameters.AddWithValue("playlistName", playlistName);
		cmd.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
		cmd.Parameters.AddWithValue("additionDate", DateTime.UtcNow);
		cmd.Parameters.AddWithValue("modifyDate", DateTime.UtcNow);
		await cmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);
		cmd.Dispose();

		conn.Close();
		conn.Dispose();
	}

	public static async Task InsertEntry(this DiscordGuild guild, string playlistName, ulong userId, string trackString, int position)
	{
		var playlist = await GetPlaylist(guild, userId, playlistName);
		var entries = await playlist.GetEntries();
		entries.Insert(position, new PlaylistEntry(LavalinkUtilities.DecodeTrack(trackString), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, position));
		await RebuildList(userId, playlistName, entries);
	}

	public static async Task InsertEntry(this DiscordGuild guild, string playlistName, ulong userId, List<LavalinkTrack> tracks, int position)
	{
		var playlist = await GetPlaylist(guild, userId, playlistName);
		var entries = await playlist.GetEntries();
		foreach (var track in tracks)
			entries.Insert(position, new PlaylistEntry(LavalinkUtilities.DecodeTrack(track.TrackString), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, position));
		await RebuildList(userId, playlistName, entries);
	}

	public static async Task ClearList(string playlistName, ulong userId)
	{
		var connString = MikuBot.Config.DbConnectString;
		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		var cmd = new NpgsqlCommand("DELETE FROM playlistentries WHERE userid = @userId AND playlistname = @playlistName;", conn);
		cmd.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
		cmd.Parameters.AddWithValue("playlistName", playlistName);
		await cmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);
		cmd.Dispose();

		conn.Close();
		conn.Dispose();
	}

	public static async Task MoveListItems(this DiscordGuild guild, string playlistName, ulong userId, int oldPosition, int newPosition)
	{
		var playlist = await GetPlaylist(guild, userId, playlistName);
		var entries = await playlist.GetEntries();

		var tempEntries = new List<PlaylistEntry>(entries.Count);
		var newEntries = new List<PlaylistEntry>(entries.Count);

		foreach (var entry in entries)
		{
			if (entry.Position == oldPosition)
				entry.Position = newPosition;
			else if (entry.Position == newPosition)
				entry.Position = oldPosition;

			tempEntries.Add(entry);
		}

		newEntries.AddRange(tempEntries.OrderBy(x => x.Position));

		await RebuildList(userId, playlistName, newEntries);
	}

	public static async Task RemoveFromList(this DiscordGuild guild, int position, string playlistName, ulong userId)
	{
		var connString = MikuBot.Config.DbConnectString;
		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		var cmd = new NpgsqlCommand("DELETE FROM playlistentries WHERE pos = @position AND userid = @userId AND playlistname = @playlistName; UPDATE playlists SET changed = @modifyDate WHERE userid = @userId AND playlistname = @playlistName;", conn);
		cmd.Parameters.AddWithValue("position", position);
		cmd.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
		cmd.Parameters.AddWithValue("playlistName", playlistName);
		cmd.Parameters.AddWithValue("modifyDate", DateTime.UtcNow);

		await cmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);
		cmd.Dispose();

		await ReorderList(guild, playlistName, userId);

		conn.Close();
		conn.Dispose();
	}

	public static async Task RenameList(string playlistName, ulong userId, string newName)
	{
		var connString = MikuBot.Config.DbConnectString;
		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		var cmd = new NpgsqlCommand("UPDATE playlists SET playlistname = @newName WHERE userid = @userId AND playlistname = @playlistName;", conn);
		cmd.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
		cmd.Parameters.AddWithValue("playlistName", playlistName);
		cmd.Parameters.AddWithValue("newName", newName);

		await cmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);
		cmd.Dispose();

		var cmd2 = new NpgsqlCommand("UPDATE playlistentries SET playlistname = @newName WHERE userid = @userId AND playlistname = @playlistName; UPDATE playlists SET changed = @modifyDate WHERE userid = @userId AND playlistname = @newName;", conn);
		cmd2.Parameters.AddWithValue("userId", Convert.ToInt64(userId));
		cmd2.Parameters.AddWithValue("playlistName", playlistName);
		cmd2.Parameters.AddWithValue("newName", newName);
		cmd2.Parameters.AddWithValue("modifyDate", DateTime.UtcNow);

		await cmd2.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);
		cmd2.Dispose();

		conn.Close();
		conn.Dispose();
	}

	public static async Task<TrackResult> GetSong(string url_or_name, InteractionContext ctx)
	{
		var nodeConnection = MikuBot.LavalinkNodeConnections.First().Value;
		var inter = ctx.Client.GetInteractivity();
		if (url_or_name.IsNndUrl())
		{
			var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Processing NND Video...").AsEphemeral());
			var split = url_or_name.Split("/".ToCharArray());
			var nico_nico_id = split.First(x => x.StartsWith("sm") || x.StartsWith("nm")).Split("?")[0];
			FtpClient ftpClient = new(MikuBot.Config.NndConfig.FtpConfig.Hostname, new NetworkCredential(MikuBot.Config.NndConfig.FtpConfig.User, MikuBot.Config.NndConfig.FtpConfig.Password));
			ftpClient.Connect();
			if (!ftpClient.FileExists($"{nico_nico_id}.mp3"))
			{
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Preparing download..."));
				var ex = await ctx.GetNNDAsync(url_or_name, nico_nico_id, msg.Id);
				if (ex == null)
				{
					await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Please try again or verify the link"));
					return null;
				}
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Uploading..."));
				ftpClient.UploadStream(ex, $"{nico_nico_id}.mp3", FtpRemoteExists.Skip, true);
			}
			var Track = await nodeConnection.Rest.GetTracksAsync(new Uri($"https://nnd.meek.moe/new/{nico_nico_id}.mp3"));
			return new TrackResult(Track.PlaylistInfo, Track.Tracks.First());
		}
		// Bilibili
		else if (url_or_name.ToLower().StartsWith("https://www.bilibili.com")
			|| url_or_name.ToLower().StartsWith("http://www.bilibili.com"))
		{
			var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Processing Bilibili Video...").AsEphemeral());
			url_or_name = url_or_name.Replace("https://www.bilibili.com/", "");
			url_or_name = url_or_name.Replace("http://www.bilibili.com/", "");
			var split = url_or_name.Split("/".ToCharArray());
			if (!split.Contains("video"))
			{
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Failure"));
				return null;
			}
			var nndID = split[1].Split("?")[0];
			FtpClient client = new(MikuBot.Config.NndConfig.FtpConfig.Hostname, new NetworkCredential(MikuBot.Config.NndConfig.FtpConfig.User, MikuBot.Config.NndConfig.FtpConfig.Password));
			client.Connect();
			if (!client.FileExists($"{nndID}.mp3"))
			{
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Preparing download..."));
				var ex = await ctx.GetBilibiliAsync(nndID, msg.Id);
				if (ex == null)
				{
					await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Please try again or verify the link"));
					return null;
				}
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Uploading..."));
				client.UploadStream(ex, $"{nndID}.mp3", FtpRemoteExists.Skip, true);
			}
			var Track = await nodeConnection.Rest.GetTracksAsync(new Uri($"https://nnd.meek.moe/new/{nndID}.mp3"));
			return new TrackResult(Track.PlaylistInfo, Track.Tracks.First());
		}
		else if (url_or_name.StartsWith("http://") | url_or_name.StartsWith("https://"))
		{
			var s = await nodeConnection.Rest.GetTracksAsync(new Uri(url_or_name));
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
								.WithAuthor($"Requested by {ctx.Member.UsernameWithDiscriminator} || Timeout 30 seconds", iconUrl: ctx.Member.AvatarUrl)
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
								.WithAuthor($"Requested by {ctx.Member.UsernameWithDiscriminator} || Timeout 30 seconds", iconUrl: ctx.Member.AvatarUrl)
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
			var s = await nodeConnection.Rest.GetTracksAsync(url_or_name);
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
					var leng = s.Tracks.Count;
					if (leng > 5)
						leng = 5;
					List<DiscordStringSelectComponentOption> selectOptions = new(leng)
					{

					};
					DiscordStringSelectComponent select = new("Select song to play", selectOptions, minOptions: 1, maxOptions: 1);
					var em = new DiscordEmbedBuilder()
							.WithTitle("Results!")
							.WithDescription("Please select a track:\n")
							.WithAuthor($"Requested by {ctx.Member.UsernameWithDiscriminator} || Timeout 30 seconds", iconUrl: ctx.Member.AvatarUrl);
					for (var i = 0; i < leng; i++)
						em.AddField(new DiscordEmbedField($"{i + 1}.{s.Tracks.ElementAt(i).Title} [{s.Tracks.ElementAt(i).Length}]", $"by {s.Tracks.ElementAt(i).Author} [Link]({s.Tracks.ElementAt(i).Uri})"));
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
