using DisCatSharp.Lavalink.Entities;
using DisCatSharp.Lavalink.Enums;

using MikuSharp.Attributes;
using MikuSharp.Entities;
using MikuSharp.Enums;
using MikuSharp.Utilities;

namespace MikuSharp.Commands;

[SlashCommandGroup("playlists", "Manage your playlists", dmPermission: false)]
public class Playlist : ApplicationCommandsModule
{
	[SlashCommandGroup("new", "Playlist creation")]
	public class PlaylistCreation : ApplicationCommandsModule
	{
		[SlashCommand("copy_queue", "Copy the current queue to a playlist!")]
		[RequireUserAndBotVoicechatConnection]
		public static async Task CopyQueueToNewPlaylistAsync(InteractionContext ctx,
			[Option("name", "Name of new playlist")] string name
		)
		{
			await ctx.DeferAsync(true);
			if (!name.TryNormalize(out var playlistName))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Queue Copy").WithDescription("**Error**\n\nUnable to create a playlist with this name.\n\nAllowed chars:\n- `0-9`\n- `a-z`\n- `A-Z`\n- `-`\n- `_`\n- ` `!").Build()));
				return;
			}

			var q = await Database.GetQueueAsync(ctx.Guild);
			if (q.Count == 0)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Nothing in queue"));
				return;
			}
			var pls = await PlaylistDb.GetPlaylistsSimple(ctx.Member.Id);
			if (pls.Any(x => x == playlistName))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Queue Copy").WithDescription("**Error**\n\nYou already have a playlist with that playlist!").Build()));
				return;
			}
			await PlaylistDb.AddPlaylist(playlistName, ctx.Member.Id);
			foreach (var e in q)
				await PlaylistDb.AddEntry(playlistName, ctx.Member.Id, e.Track.Encoded);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Queue Copy").WithDescription("Queue was saved to new playlist -> " + playlistName).Build()));
		}

		[SlashCommand("create", "Create a playlist")]
		public static async Task CreatePlaylistAsync(InteractionContext ctx,
			[Option("name", "Name of new playlist")] string name
		)
		{
			await ctx.DeferAsync(true);
			if (!name.TryNormalize(out var playlistName))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Create Playlist").WithDescription("**Error**\n\nUnable to create a playlist with this name.\n\nAllowed chars:\n- `0-9`\n- `a-z`\n- `A-Z`\n- `-`\n- `_`\n- ` `!").Build()));
				return;
			}
			var pls = await PlaylistDb.GetPlaylistsSimple(ctx.Member.Id);
			if (pls.Any(x => x == playlistName))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Create Playlist").WithDescription("**Error** You already have a playlist with that playlist!").Build()));
				return;
			}
			await PlaylistDb.AddPlaylist(playlistName, ctx.Member.Id);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Create Playlist").WithDescription("New Playlist was created -> " + playlistName).Build()));
		}

		[SlashCommand("create_fixed", "Create a fixed playlist (linked to a Youtube or SoundCloud playlist)")]
		public static async Task CreateFixedPlaylistAsync(InteractionContext ctx,
			[Option("name", "Name of new playlist")] string name,
			[Option("link", "Link to playlist")] string link
		)
		{
			await ctx.DeferAsync(true);
			if (!name.TryNormalize(out var playlistName))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Create Fixed Playlist").WithDescription("**Error**\n\nUnable to create a playlist with this name.\n\nAllowed chars:\n- `0-9`\n- `a-z`\n- `A-Z`\n- `-`\n- `_`\n- ` `!").Build()));
				return;
			}
			var pls = await PlaylistDb.GetPlaylistsSimple(ctx.Member.Id);
			if (pls.Any(x => x == name))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Create Fixed Playlist").WithDescription("**Error** You already have a playlist with that playlist!").Build()));
				return;
			}
			LavalinkTrackLoadingResult s = null;
			try
			{
				s = await MikuBot.LavalinkSessions[ctx.Client.ShardId].LoadTracksAsync(link);
			}
			catch
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Create Fixed Playlist").WithDescription("**Error** Reasons could be:\n> The provided link was not a playlist\n> The playlist is unavailable (for example set to private)").Build()));
				return;
			}
			if (s.LoadType != LavalinkLoadResultType.Playlist)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Create Fixed Playlist").WithDescription("**Error** Reasons could be:\n> The provided link was not a playlist\n> The playlist is unavailable (for example set to private)").Build()));
				return;
			}
			if (link.Contains("youtu") && !link.Contains("soundcloud"))
				await PlaylistDb.AddPlaylist(playlistName, ctx.Member.Id, ExtService.Youtube, link);
			else
				await PlaylistDb.AddPlaylist(playlistName, ctx.Member.Id, ExtService.Soundcloud, link);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Create Fixed Playlist").WithDescription($"Fixed playlist created with playlist -> {playlistName} and {((LavalinkPlaylist)s.Result).Tracks.Count} Songs!").Build()));
		}
	}

	[SlashCommandGroup("manage", "Playlist management")]
	public class PlaylistManagement : ApplicationCommandsModule
	{
		[SlashCommand("list", "List all your playlists")]
		public static async Task ListPlaylistsAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync(true);
			try
			{
				var pls = await ctx.Guild.GetPlaylists(ctx.Member.Id);
				ctx.Client.Logger.LogDebug("{plCount}", pls.Count.ToString());
				if (pls.Count == 0)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You don't have any playlist"));
					return;
				}
				var inter = ctx.Client.GetInteractivity();
				var songsPerPage = 0;
				var currentPage = 1;
				var songAmount = 0;
				var totalP = pls.Count / 5;
				if (pls.Count % 5 != 0)
					totalP++;
				var emb = new DiscordEmbedBuilder();
				List<Page> pages = new();
				foreach (var track in pls)
				{
					//ctx.Client.Logger.LogDebug(Track.Value == null);
					//ctx.Client.Logger.LogDebug(Track.Key);
					var songCount = 0;
					var ent = await track.Value.GetEntriesAsync();
					songCount = ent.Count;
					var sub = track.Value.ExternalService == ExtService.None
						? $"SendAt on: {track.Value.Creation}\n" +
							$"Last modified on: {track.Value.Modify}"
						: $"SendAt on: {track.Value.Creation}\n" +
							$"{track.Value.ExternalService} [Link]({track.Value.Url})";
					emb.AddField(new DiscordEmbedField($"**{songAmount + 1}.{track.Key}** ({songCount} Songs)", sub));
					songsPerPage++;
					songAmount++;
					emb.WithTitle($"List Playlists");
					if (songsPerPage == 5)
					{
						songsPerPage = 0;
						emb.WithFooter($"Page {currentPage}/{totalP}");
						pages.Add(new Page(embed: emb));
						emb.ClearFields();
						currentPage++;
					}
					if (songAmount == pls.Count)
					{
						emb.WithFooter($"Page {currentPage}/{totalP}");
						pages.Add(new Page(embed: emb));
						emb.ClearFields();
					}
				}
				if (currentPage == 1)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(pages.First().Embed));
					return;
				}
				else if (currentPage == 2 && songsPerPage == 0)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(pages.First().Embed));
					return;
				}
				foreach (var eP in pages.Where(x => x.Embed.Fields.Count == 0).ToList())
					pages.Remove(eP);
				await inter.SendPaginatedResponseAsync(ctx.Interaction, true, false, ctx.User, pages, token: MikuBot.CanellationTokenSource.Token);
			}
			catch (Exception ex)
			{
				ctx.Client.Logger.LogError("{msg}", ex.Message);
				ctx.Client.Logger.LogError("{stack}", ex.StackTrace);
			}
		}

		[SlashCommand("show", "Show the contents of a playlist")]
		public static async Task ShowPlaylistAsync(InteractionContext ctx,
			[Option("playlist", "Name of playlist to show", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var pls = await PlaylistDb.GetPlaylistsSimple(ctx.Member.Id);
			if (pls.All(x => x != playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Show Playlist").WithDescription("**Error** You don't have a playlist with that playlist!").Build()));
				return;
			}
			var q = await PlaylistDb.GetPlaylistAsync(ctx.Guild, ctx.Member.Id, playlist);
			var queue = await q.GetEntriesAsync();
			if (queue.Count == 0)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Playlist empty!"));
				return;
			}
			var inter = ctx.Client.GetInteractivity();
			var songsPerPage = 0;
			var currentPage = 1;
			var songAmount = 0;
			var totalP = queue.Count / 5;
			if (queue.Count % 5 != 0)
				totalP++;
			var emb = new DiscordEmbedBuilder();
			List<Page> pages = new();
			foreach (var track in queue)
			{
				var time = track.Track.Info.Length.Hours < 1 ? track.Track.Info.Length.ToString(@"mm\:ss") : track.Track.Info.Length.ToString(@"hh\:mm\:ss");
				emb.AddField(new DiscordEmbedField($"**{songAmount + 1}.{track.Track.Info.Title.Replace("*", "").Replace("|", "")}** by {track.Track.Info.Author.Replace("*", "").Replace("|", "")} [{time}]",
					$"Added on {track.AdditionDate} [Link]({track.Track.Info.Uri.AbsoluteUri})"));
				songsPerPage++;
				songAmount++;
				emb.WithTitle($"Songs in {playlist}");
				if (songsPerPage == 5)
				{
					songsPerPage = 0;
					emb.WithFooter($"Page {currentPage}/{totalP}");
					pages.Add(new Page(embed: emb));
					emb.ClearFields();
					currentPage++;
				}
				if (songAmount == queue.Count)
				{
					emb.WithFooter($"Page {currentPage}/{totalP}");
					pages.Add(new Page(embed: emb));
					emb.ClearFields();
				}
			}
			if (currentPage == 1)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(pages.First().Embed));
				return;
			}
			else if (currentPage == 2 && songsPerPage == 0)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(pages.First().Embed));
				return;
			}
			foreach (var eP in pages.Where(x => x.Embed.Fields.Count == 0).ToList())
				pages.Remove(eP);
			await inter.SendPaginatedResponseAsync(ctx.Interaction, true, false, ctx.User, pages, token: MikuBot.CanellationTokenSource.Token);
		}

		[SlashCommand("delete", "Delete a playlist")]
		public static async Task DeletePlaylistAsync(InteractionContext ctx,
			[Option("playlist", "Name of playlist to delete", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var pls = await PlaylistDb.GetPlaylistsSimple(ctx.Member.Id);
			if (pls.All(x => x != playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Delete Playlist").WithDescription("**Error** You don't have a playlist with that playlist!").Build()));
				return;
			}
			await PlaylistDb.RemovePlaylist(playlist, ctx.Member.Id);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Delete Playlist").WithDescription("Deleted playlist -> " + playlist).Build()));
		}

		[SlashCommand("rename", "Rename a playlist")]
		public static async Task RenamePlaylistAsync(InteractionContext ctx,
			[Option("playlist", "Name of playlist to rename", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist,
			[Option("name", "New name for playlist")] string name
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var p = await PlaylistDb.GetPlaylistsSimple(ctx.Member.Id);
			if (p.All(x => x != playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Rename Playlist").WithDescription("**Error** You don't have a playlist with that name!").Build()));
				return;
			}
			var pls = await ctx.Guild.GetPlaylistAsync(ctx.Member.Id, playlist);
			await pls.GetEntriesAsync();

			await PlaylistDb.RenameList(playlist, ctx.Member.Id, name);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Rename Playlist").WithDescription($"Renamed Playlist to {playlist} -> {name}!").Build()));
		}

		[SlashCommand("clear", "Clear all entries from a playlist")]
		public static async Task ClearPlaylistAsync(InteractionContext ctx,
			[Option("playlist", "Name of playlist to clear", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var p = await PlaylistDb.GetPlaylistsSimple(ctx.Member.Id);
			if (p.All(x => x != playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Clear Playlist").WithDescription("**Error** You don't have a playlist with that playlist!").Build()));
				return;
			}
			await PlaylistDb.ClearListAsync(playlist, ctx.Member.Id);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Clear Playlist").WithDescription($"Cleared all songs from playlist -> {playlist}!").Build()));
		}

		[SlashCommand("play", "Play a playlist/Add the songs to the queue")]
		[RequireUserVoicechatConnection]
		public static async Task PlayPlaylistAsync(InteractionContext ctx,
			[Option("playlist", "Name of playlist to play", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist
		)
		{
			await ctx.DeferAsync(true);
			var ps = await ctx.Guild.GetPlaylists(ctx.Member.Id);
			if (ps.All(x => x.Key != playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Play Playlist").WithDescription("**Error** You don't have a playlist with that playlist!").Build()));
				return;
			}
			var pls = await ctx.Guild.GetPlaylistAsync(ctx.Member.Id, playlist);
			var p = await pls.GetEntriesAsync();
			if (MikuBot.Guilds.All(x => x.Key != ctx.Guild.Id))
				MikuBot.Guilds.TryAdd(ctx.Guild.Id, new Guild(ctx.Client.ShardId));
			var g = MikuBot.Guilds[ctx.Guild.Id];
			g.MusicInstance ??= new MusicInstance(MikuBot.LavalinkSessions[ctx.Client.ShardId], ctx.Client.ShardId);
			await g.ConditionalConnect(ctx);
			g.MusicInstance.CommandChannel = ctx.Channel;
			await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, p);
			if (g.MusicInstance.GuildPlayer.IsConnected && g.MusicInstance.PlayState is PlayState.NotPlaying or PlayState.Stopped)
				await g.MusicInstance.PlaySong();
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Play Playlist").WithDescription($"Playing playlist/Added to queue!").Build()));
		}
	}

	[SlashCommandGroup("song", "Song management")]
	public class SongOperations : ApplicationCommandsModule
	{
		[SlashCommand("add", "Add a song to a playlist")]
		public static async Task AddSongAsync(InteractionContext ctx,
			[Option("playlist", "Name of playlist to add song to", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist,
			[Option("url_or_search", "Url or name of song to add")] string song
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var p = await PlaylistDb.GetPlaylistsSimple(ctx.Member.Id);
			if (p.All(x => x != playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Add Song").WithDescription("**Error** You don't have a playlist with that playlist!").Build()));
				return;
			}
			var pls = await ctx.Guild.GetPlaylistAsync(ctx.Member.Id, playlist);
			if (pls.ExternalService != ExtService.None)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Add Song").WithDescription("**Error** This playlist is a fixed one, you cant add songs to this!").Build()));
				return;
			}
			var got = await PlaylistDb.GetSong(song, ctx);
			if (got == null)
				return;
			await PlaylistDb.AddEntry(playlist, ctx.Member.Id, got.Tracks);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Add Song").WithDescription($"Added entry -> {got.Tracks[0].Info.Title}!").Build()));
		}

		[SlashCommand("insert_at", "Insert a song into a playlist at a choosen position")]
		public static async Task InsertAtAsync(InteractionContext ctx,
			[Option("playlist", "Name of playlist to add song to", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist,
			[Option("position", "Position to insert song at", true), Autocomplete(typeof(AutocompleteProviders.SongProvider))] string posi,
			[Option("url_or_search", "Url or name of song to add")] string song
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var pos = Convert.ToInt32(posi);
			var p = await PlaylistDb.GetPlaylistsSimple(ctx.Member.Id);
			if (p.All(x => x != playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription("**Error** You don't have a playlist with that playlist!").Build()));
				return;
			}
			var pls = await ctx.Guild.GetPlaylistAsync(ctx.Member.Id, playlist);
			if (pls.ExternalService != ExtService.None)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription("**Error** This playlist is a fixed one, you cant add songs to this!").Build()));
				return;
			}
			var got = await PlaylistDb.GetSong(song, ctx);
			if (got == null)
				return;
			got.Tracks.Reverse();
			await ctx.Guild.InsertEntry(playlist, ctx.Member.Id, got.Tracks, pos);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription($"Inserted entry -> {got.Tracks[0].Info.Title} at {pos}!").Build()));
		}

		[SlashCommand("move", "Move a song to a specific position in your playlist")]
		public static async Task MoveSongAsync(InteractionContext ctx,
			[Option("playlist", "Name of playlist to move the song within", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist,
			[Option("old_position", "Position to move the song from", true), Autocomplete(typeof(AutocompleteProviders.SongProvider))] string oldposi,
			[Option("new_position", "Position to move song to", true), Autocomplete(typeof(AutocompleteProviders.SongProvider))] string newposi
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var oldpos = Convert.ToInt32(oldposi);
			var newpos = Convert.ToInt32(newposi);
			var p = await PlaylistDb.GetPlaylistsSimple(ctx.Member.Id);
			if (p.All(x => x != playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription("**Error** You don't have a playlist with that playlist!").Build()));
				return;
			}
			var pls = await ctx.Guild.GetPlaylistAsync(ctx.Member.Id, playlist);
			if (pls.ExternalService != ExtService.None)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription("**Error** This playlist is a fixed one, you cant move songs!").Build()));
				return;
			}
			var e = await pls.GetEntriesAsync();
			if (e[newpos] == null | e[oldpos] == null)
				return;
			await ctx.Guild.MoveListItemsAsync(playlist, ctx.Member.Id, oldpos, newpos);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Move Song").WithDescription($"Moved entry -> {e[oldpos].Track.Info.Title} to position {newpos}!").Build()));
		}

		[SlashCommand("remove", "Remove a song from a playlist")]
		public static async Task RemoveSongAsync(InteractionContext ctx,
			[Option("playlist", "Name of playlist to remove the song from", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist,
			[Option("song", "Song to remove", true), Autocomplete(typeof(AutocompleteProviders.SongProvider))] string posi
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var pos = Convert.ToInt32(posi);
			var p = await PlaylistDb.GetPlaylistsSimple(ctx.Member.Id);
			if (p.All(x => x != playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Remove Song").WithDescription("**Error** You don't have a playlist with that playlist!").Build()));
				return;
			}
			var ents = await ctx.Guild.GetPlaylistAsync(ctx.Member.Id, playlist);
			var en = await ents.GetEntriesAsync();
			await ctx.Guild.RemoveFromList(pos, playlist, ctx.Member.Id);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Remove Song").WithDescription($"Entry removed! -> {en[pos].Track.Info.Title}").Build()));
		}
	}

	public static bool CheckError(string playlist)
		=> playlist == "error";
}
