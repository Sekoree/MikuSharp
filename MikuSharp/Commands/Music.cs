using MikuSharp.Attributes;
using MikuSharp.Entities;
using MikuSharp.Enums;
using MikuSharp.Events;
using MikuSharp.Utilities;

namespace MikuSharp.Commands;

/// <summary>
/// The music commands
/// </summary>
[SlashCommandGroup("music", "Music commands", dmPermission: false)]
public class Music : ApplicationCommandsModule
{
	private static readonly string[] s_units = new[] { "", "ki", "Mi", "Gi" };
	private static string SizeToString(long l)
	{
		double d = l;
		var u = 0;
		while (d >= 900 && u < s_units.Length - 2)
		{
			u++;
			d /= 1024;
		}

		return $"{d:#,##0.00} {s_units[u]}B";
	}

	[SlashCommandGroup("base", "Base commands")]
	public class Base : ApplicationCommandsModule
	{
		[SlashCommand("join", "Joins the voice channel you're in")]
		[RequireUserVoicechatConnection]
		public static async Task JoinAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync(true);
			if (!MikuBot.Guilds.Any(x => x.Key == ctx.Guild.Id))
				MikuBot.Guilds.TryAdd(ctx.Guild.Id, new Guild(ctx.Client.ShardId));
			var g = MikuBot.Guilds[ctx.Guild.Id];
			g.musicInstance ??= new MusicInstance(MikuBot.LavalinkNodeConnections[ctx.Client.ShardId], ctx.Client.ShardId);
			await g.ConditionalConnect(ctx);
			g.musicInstance.usedChannel = ctx.Channel;
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Heya {ctx.Member.Mention}!"));
		}

		[SlashCommand("leave", "Leaves the channel")]
		public static async Task LeaveAsync(InteractionContext ctx, [Option("keep", "Whether to keep the queue")] bool keep = false)
		{
			await ctx.DeferAsync(true);
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (g.musicInstance == null)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I'm not in a voice channel"));
				return;
			}
			g.musicInstance.playstate = Playstate.NotPlaying;
			try
			{
				if (keep)
					await g.musicInstance.guildConnection.StopAsync();
				await g.musicInstance.guildConnection.DisconnectAsync();
				if (!keep)
					await Database.ClearQueueAsync(ctx.Guild);
				g.musicInstance = null;
			}
			catch (Exception)
			{ }
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cya! 💙"));
		}


		[SlashCommand("lstats", "Displays Lavalink statistics")]
		[ApplicationCommandRequireOwner]
		public static async Task GetLavalinkStatsAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync(true);
			var stats = MikuBot.LavalinkNodeConnections[ctx.Client.ShardId].Statistics;
			var sb = new StringBuilder();
			sb.Append("Lavalink resources usage statistics: ```")
				.Append("Uptime:                    ").Append(stats.Uptime).AppendLine()
				.Append("Players:                   ").AppendFormat("{0} active / {1} total", stats.ActivePlayers, stats.TotalPlayers).AppendLine()
				.Append("CPU Cores:                 ").Append(stats.CpuCoreCount).AppendLine()
				.Append("CPU Usage:                 ").AppendFormat("{0:#,##0.0%} lavalink / {1:#,##0.0%} system", stats.CpuLavalinkLoad, stats.CpuSystemLoad).AppendLine()
				.Append("RAM Usage:                 ").AppendFormat("{0} allocated / {1} used / {2} free / {3} reservable", SizeToString(stats.RamAllocated), SizeToString(stats.RamUsed), SizeToString(stats.RamFree), SizeToString(stats.RamReservable)).AppendLine()
				.Append("Audio frames (per minute): ").AppendFormat("{0:#,##0} sent / {1:#,##0} nulled / {2:#,##0} deficit", stats.AverageSentFramesPerMinute, stats.AverageNulledFramesPerMinute, stats.AverageDeficitFramesPerMinute).AppendLine()
				.Append("```");
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(sb.ToString()));
		}
	}

	[SlashCommandGroup("playback", "Playback controls")]
	public class Playback : ApplicationCommandsModule
	{
		[SlashCommand("seek", "Seek a song")]
		[RequireUserAndBotVoicechatConnection]
		public static async Task SeekAsync(InteractionContext ctx, [Option("position", "Position to seek to")] double position)
		{
			await ctx.DeferAsync(true);
			if (!MikuBot.Guilds.Any(x => x.Key == ctx.Guild.Id))
				MikuBot.Guilds.TryAdd(ctx.Guild.Id, new Guild(ctx.Client.ShardId));
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;
			if (g.musicInstance.playstate != Playstate.Playing && g.musicInstance.playstate != Playstate.Paused)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I don't play anything right now"));
				return;
			}
			g.musicInstance.usedChannel = ctx.Channel;
			var ts = TimeSpan.FromSeconds(position);
			await g.musicInstance.guildConnection.SeekAsync(ts);
			var pos = ts.Hours < 1 ? ts.ToString(@"mm\:ss") : ts.ToString(@"hh\:mm\:ss");
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Seeked {g.musicInstance.currentSong.track.Title} to {pos}"));
		}

		[SlashCommand("play", "Play or queue a song")]
		[RequireUserVoicechatConnection]
		public static async Task PlayAsync(InteractionContext ctx,
			[Option("song", "Song name or url to play")] string name_or_url = null,
			[Option("music_file", "Music file to play")] DiscordAttachment music_file = null
		)
		{
			await ctx.DeferAsync(true);
			if (!MikuBot.Guilds.Any(x => x.Key == ctx.Guild.Id))
				MikuBot.Guilds.TryAdd(ctx.Guild.Id, new Guild(ctx.Client.ShardId));
			var g = MikuBot.Guilds[ctx.Guild.Id];
			g.musicInstance ??= new MusicInstance(MikuBot.LavalinkNodeConnections[ctx.Client.ShardId], ctx.Client.ShardId);
			var curq = await Database.GetQueueAsync(ctx.Guild);
			if (curq.Count != 0 && g.musicInstance.playstate == Playstate.NotPlaying)
			{
				var inter = ctx.Client.GetInteractivity();
				List<DiscordButtonComponent> buttons = new(2)
			{
				new DiscordButtonComponent(ButtonStyle.Success, "restore", "Restore old queue"),
				new DiscordButtonComponent(ButtonStyle.Danger, "clear", "Clear old queue")
			};
				var msg = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Recover Queue").WithDescription("The last time the bot disconnected the queue wasnt cleared, do you want to restore and play that old one?").Build()).AddComponents(buttons));
				var hmm = await inter.WaitForButtonAsync(msg, ctx.User, TimeSpan.FromSeconds(30));
				if (hmm.TimedOut)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Timed out!"));
					return;
				}
				else if (hmm.Result.Id == "restore")
				{
					await hmm.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
					buttons.ForEach(x => x.Disable());
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Restored").AddComponents(buttons));
					await g.musicInstance.ConnectToChannel(ctx.Member.VoiceState.Channel);
					await g.musicInstance.PlaySong();
					return;
				}
				else
				{
					await hmm.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
					await Database.ClearQueueAsync(ctx.Guild);
					buttons.ForEach(x => x.Disable());
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cleared").AddComponents(buttons));
				}
			}

			await g.ConditionalConnect(ctx);

			if (music_file == null && name_or_url == null)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error: No song or file choosen"));
				return;
			}
			g.musicInstance.usedChannel = ctx.Channel;
			name_or_url = music_file.SearchUrlOrAttachment(name_or_url);
			var oldState = g.musicInstance.playstate;
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Trying to play/search {name_or_url}..."));
			var q = await g.musicInstance.QueueSong(name_or_url, ctx);
			if (q == null)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error: Song not found"));
				return;
			}
			var emb = new DiscordEmbedBuilder();
			if (oldState == Playstate.Playing)
			{
				emb.AddField(new DiscordEmbedField(q.Tracks.First().Title + "[" + (q.Tracks.First().Length.Hours != 0 ? q.Tracks.First().Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Author}\n" +
					$"Requested by {ctx.Member.Mention}"));
				if (q.Tracks.Count != 1)
					emb.AddField(new DiscordEmbedField("Playlist added:", $"added {q.Tracks.Count - 1} more"));
				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(emb.WithTitle("Added").Build()).AsEphemeral());
			}
			else
			{
				if (q.PlaylistInfo.SelectedTrack == -1 || q.PlaylistInfo.Name == null)
					emb.AddField(new DiscordEmbedField(q.Tracks.First().Title + "[" + (q.Tracks.First().Length.Hours != 0 ? q.Tracks.First().Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Author}\nRequested by {ctx.Member.Mention}"));
				else
					emb.AddField(new DiscordEmbedField(q.Tracks[q.PlaylistInfo.SelectedTrack].Title + "[" + (q.Tracks[q.PlaylistInfo.SelectedTrack].Length.Hours != 0 ? q.Tracks[q.PlaylistInfo.SelectedTrack].Length.ToString(@"hh\:mm\:ss") : q.Tracks[q.PlaylistInfo.SelectedTrack].Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks[q.PlaylistInfo.SelectedTrack].Author}\nRequested by {ctx.Member.Mention}"));
				if (q.Tracks.Count != 1)
					emb.AddField(new DiscordEmbedField("Playlist added:", $"added {q.Tracks.Count - 1} more"));
				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(emb.WithTitle("Playing").Build()).AsEphemeral());
			}
		}

		[SlashCommand("insert", "Queue a song at a specific position!")]
		[RequireUserVoicechatConnection]
		public static async Task InsertToQueueAsync(InteractionContext ctx,
			[Option("position", "Position to move song to", true), Autocomplete(typeof(AutocompleteProviders.QueueProvider))] string posi,
			[Option("song", "Song name or url to play")] string name_or_url = null,
			[Option("music_file", "Music file to play")] DiscordAttachment music_file = null
		)
		{
			await ctx.DeferAsync(true);
			var pos = Convert.ToInt32(posi);
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (pos < 1)
				return;
			g.musicInstance ??= new MusicInstance(MikuBot.LavalinkNodeConnections[ctx.Client.ShardId], ctx.Client.ShardId);

			await g.ConditionalConnect(ctx);

			if (music_file == null && name_or_url == null)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error: No song or file choosen"));
				return;
			}
			g.musicInstance.usedChannel = ctx.Channel;
			name_or_url = music_file.SearchUrlOrAttachment(name_or_url);
			var oldState = g.musicInstance.playstate;
			var q = await g.musicInstance.QueueSong(name_or_url, ctx, pos);
			if (q == null)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error: Song not found"));
				return;
			}
			var emb = new DiscordEmbedBuilder();
			if (oldState == Playstate.Playing)
			{
				emb.AddField(new DiscordEmbedField(q.Tracks.First().Title + "[" + (q.Tracks.First().Length.Hours != 0 ? q.Tracks.First().Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Author}\n" +
					$"Requested by {ctx.Member.Mention}\nAt position: {pos}"));
				if (q.Tracks.Count != 1)
					emb.AddField(new DiscordEmbedField("Playlist added:", $"added {q.Tracks.Count - 1} more"));
				emb.WithTitle("Playing");
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(emb.Build()));
			}
			else
			{
				if (q.PlaylistInfo.SelectedTrack == -1 || q.PlaylistInfo.Name == null)
					emb.AddField(new DiscordEmbedField(q.Tracks.First().Title + "[" + (q.Tracks.First().Length.Hours != 0 ? q.Tracks.First().Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Author}\nRequested by {ctx.Member.Mention}"));
				else
					emb.AddField(new DiscordEmbedField(q.Tracks[q.PlaylistInfo.SelectedTrack].Title + "[" + (q.Tracks[q.PlaylistInfo.SelectedTrack].Length.Hours != 0 ? q.Tracks[q.PlaylistInfo.SelectedTrack].Length.ToString(@"hh\:mm\:ss") : q.Tracks[q.PlaylistInfo.SelectedTrack].Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks[q.PlaylistInfo.SelectedTrack].Author}\nRequested by {ctx.Member.Mention}At position: {pos}"));
				if (q.Tracks.Count != 1)
					emb.AddField(new DiscordEmbedField("Playlist added:", $"added {q.Tracks.Count - 1} more"));
				emb.WithTitle("Added");
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(emb.Build()));
			}
		}

		[SlashCommand("skip", "Skip the current song")]
		[RequireUserAndBotVoicechatConnection]
		public static async Task SkipSongAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync(true);
			var g = MikuBot.Guilds[ctx.Guild.Id];
			var lastPlayedSongs = await Database.GetLastPlayingListAsync(ctx.Guild);
			var queue = await Database.GetQueueAsync(ctx.Guild);
			if (await g.IsNotConnected(ctx))
				return;
			g.musicInstance.usedChannel = ctx.Channel;
			g.musicInstance.guildConnection.PlaybackFinished -= Lavalink.LavalinkTrackFinish;
			if (g.musicInstance.currentSong != null)
			{
				if (g.musicInstance.repeatMode != RepeatMode.On && g.musicInstance.repeatMode != RepeatMode.All)
					await Database.RemoveFromQueueAsync(g.musicInstance.currentSong.position, ctx.Guild);
				if (lastPlayedSongs.Count == 0)
					await Database.AddToLastPlayingListAsync(ctx.Guild.Id, g.musicInstance.currentSong.track.TrackString);
				else if (lastPlayedSongs[0]?.track.Uri != g.musicInstance.currentSong.track.Uri)
					await Database.AddToLastPlayingListAsync(ctx.Guild.Id, g.musicInstance.currentSong.track.TrackString);
			}
			queue = await Database.GetQueueAsync(ctx.Guild);
			g.musicInstance.lastSong = g.musicInstance.currentSong;
			g.musicInstance.currentSong = null;
			if (queue.Count != 0)
				await g.musicInstance.PlaySong();
			else
			{
				g.musicInstance.playstate = Playstate.NotPlaying;
				await g.musicInstance.guildConnection.StopAsync();
			}
			if (g.musicInstance.lastSong != null)
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"**Skipped:**\n{g.musicInstance.lastSong.track.Title}").Build()));
			else
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"**Continued!**").Build()));
		}

		[SlashCommand("stop", "Stop Playback")]
		[RequireUserAndBotVoicechatConnection]
		public static async Task StopAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync(true);
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;
			g.musicInstance.usedChannel = ctx.Channel;
			await Task.Run(g.musicInstance.guildConnection.StopAsync, MikuBot._cts.Token);
			var cmd_id = ctx.Client.GetApplicationCommands().GlobalCommands.First(x => x.Name == "music").Id;
			await ctx.EditResponseAsync(builder: new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"**Stopped** (use </music playback resume:{cmd_id}> to start playback again)").Build()));
		}

		[SlashCommand("volume", "Change the music volume")]
		[RequireUserAndBotVoicechatConnection]
		public static async Task ModifyVolumeAsync(InteractionContext ctx,
			[Option("volume", "Level of volume to set (Percentage)"), MinimumValue(0), MaximumValue(150)] int vol = 100
		)
		{
			await ctx.DeferAsync(true);
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;
			g.musicInstance.usedChannel = ctx.Channel;
			if (vol > 150)
				vol = 150;
			await g.musicInstance.guildConnection.SetVolumeAsync(vol);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"**Set volume to {vol}**").Build()));
		}

		[SlashCommand("pause", "Pauses playback")]
		[RequireUserAndBotVoicechatConnection]
		public static async Task PauseAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync(true);
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;
			g.musicInstance.usedChannel = ctx.Channel;
			if (g.musicInstance.playstate == Playstate.Playing)
			{
				await g.musicInstance.guildConnection.PauseAsync();
				g.musicInstance.playstate = Playstate.Paused;
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription("**Paused**").Build()));
			}
			else
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I'm not playing anything right now"));
		}

		[SlashCommand("resume", "Resumes paused playback")]
		[RequireUserAndBotVoicechatConnection]
		public static async Task ResumeAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync(true);
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;
			g.musicInstance.usedChannel = ctx.Channel;
			if (g.musicInstance.playstate == Playstate.Stopped)
			{
				await g.musicInstance.PlaySong();
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription("**Started Playback**").Build()));
			}
			else
			{
				await g.musicInstance.guildConnection.ResumeAsync();
				g.musicInstance.playstate = Playstate.Playing;
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription("**Resumed**").Build()));
			}
		}
	}

	[SlashCommandGroup("queue", "Queue management")]
	[RequireUserAndBotVoicechatConnection]
	public class Queue : ApplicationCommandsModule
	{
		[SlashCommand("show", "Show the current queue")]
		public static async Task ShowQueueAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync(true);
			var queue = await Database.GetQueueAsync(ctx.Guild);
			try
			{
				var g = MikuBot.Guilds[ctx.Guild.Id];
				if (queue.Count == 0)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Queue empty"));
					return;
				}

				var inter = ctx.Client.GetInteractivity();
				int songsPerPage = 0;
				int currentPage = 1;
				int songAmount = 0;
				int totalP = queue.Count / 5;
				if ((queue.Count % 5) != 0)
					totalP++;
				var emb = new DiscordEmbedBuilder();
				List<Page> Pages = new();
				if (g.musicInstance.repeatMode == RepeatMode.All)
				{
					songAmount = g.musicInstance.repeatAllPos;
					foreach (var Track in queue)
					{
						if (songsPerPage == 0 && currentPage == 1)
						{
							emb.WithTitle("Current Queue");
							g.GetPlayingState(out var time1, out var time2);
							emb.AddField(new DiscordEmbedField($"**{songAmount}.{g.musicInstance.currentSong.track.Title.Replace("*", "").Replace("|", "")}** by {g.musicInstance.currentSong.track.Author.Replace("*", "").Replace("|", "")} [{time1}/{time2}]",
								$"Requested by <@{g.musicInstance.currentSong.addedBy}> [Link]({g.musicInstance.currentSong.track.Uri.AbsoluteUri})\nˉˉˉˉˉ"));
						}
						else
						{
							queue.ElementAt(songAmount).GetPlayingState(out var time);
							emb.AddField(new DiscordEmbedField($"**{songAmount}.{queue.ElementAt(songAmount).track.Title.Replace("*", "").Replace("|", "")}** by {queue.ElementAt(songAmount).track.Author.Replace("*", "").Replace("|", "")} [{time}]",
								$"Requested by <@{queue.ElementAt(songAmount).addedBy}> [Link]({queue.ElementAt(songAmount).track.Uri.AbsoluteUri})"));
						}
						songsPerPage++;
						songAmount++;
						if (songAmount == queue.Count)
							songAmount = 0;
						if (songsPerPage == 5)
						{
							songsPerPage = 0;
							emb.AddField(new DiscordEmbedField("Playback options", g.musicInstance.GetPlaybackOptions()));
							emb.WithFooter($"Page {currentPage}/{totalP}");
							Pages.Add(new Page(embed: emb));
							emb.ClearFields();
							emb.WithTitle("more™");
							currentPage++;
						}
						if (songAmount == g.musicInstance.repeatAllPos)
						{
							emb.AddField(new DiscordEmbedField("Playback options", g.musicInstance.GetPlaybackOptions()));
							emb.WithFooter($"Page {currentPage}/{totalP}");
							Pages.Add(new Page(embed: emb));
							emb.ClearFields();
						}
					}
				}
				else
				{
					foreach (var Track in queue)
					{
						if (songsPerPage == 0 && currentPage == 1)
						{
							emb.WithTitle("Current Queue");
							g.GetPlayingState(out var time1, out var time2);
							emb.AddField(new DiscordEmbedField($"**{g.musicInstance.currentSong.track.Title.Replace("*", "").Replace("|", "")}** by {g.musicInstance.currentSong.track.Author.Replace("*", "").Replace("|", "")} [{time1}/{time2}]",
								$"Requested by <@{g.musicInstance.currentSong.addedBy}> [Link]({g.musicInstance.currentSong.track.Uri.AbsoluteUri})\nˉˉˉˉˉ"));
						}
						else
						{
							Track.GetPlayingState(out var time);
							emb.AddField(new DiscordEmbedField($"**{songAmount}.{Track.track.Title.Replace("*", "").Replace("|", "")}** by {Track.track.Author.Replace("*", "").Replace("|", "")} [{time}]",
								$"Requested by <@{Track.addedBy}> [Link]({Track.track.Uri.AbsoluteUri})"));
						}
						songsPerPage++;
						songAmount++;
						if (songsPerPage == 5)
						{
							songsPerPage = 0;
							emb.WithFooter($"Page {currentPage}/{totalP}");
							emb.AddField(new DiscordEmbedField("Playback options", g.musicInstance.GetPlaybackOptions()));
							Pages.Add(new Page(embed: emb));
							emb.ClearFields();
							emb.WithTitle("more™");
							currentPage++;
						}
						if (songAmount == queue.Count)
						{
							emb.WithFooter($"Page {currentPage}/{totalP}");
							emb.AddField(new DiscordEmbedField("Playback options", g.musicInstance.GetPlaybackOptions()));
							Pages.Add(new Page(embed: emb));
							emb.ClearFields();
						}
					}
				}
				if (currentPage == 1)
				{
					emb.AddField(new DiscordEmbedField("Playback options", g.musicInstance.GetPlaybackOptions()));
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(Pages.First().Embed));
					return;
				}
				else if (currentPage == 2 && songsPerPage == 0)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(Pages.First().Embed));
					return;
				}
				foreach (var eP in Pages.Where(x => !x.Embed.Fields.Any(y => y.Name != "Playback keep")).ToList())
					Pages.Remove(eP);
				await inter.SendPaginatedResponseAsync(ctx.Interaction, true, false, ctx.User, Pages);
			}
			catch (Exception ex)
			{
				ctx.Client.Logger.LogError("{ex}", ex.Message);
				ctx.Client.Logger.LogError("{ex}", ex.StackTrace);
			}
		}

		[SlashCommand("clear", "Clears the queue")]
		public static async Task ClearQueueAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync(true);
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;
			g.musicInstance.usedChannel = ctx.Channel;
			await Database.ClearQueueAsync(ctx.Guild);
			if (g.musicInstance.currentSong != null)
				await Database.AddToQueueAsync(ctx.Guild, g.musicInstance.currentSong.addedBy, g.musicInstance.currentSong.track.TrackString);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription("**Cleared queue!**").Build()));
		}

		[SlashCommand("move", "Moves a specific song within the queue")]
		public static async Task MoveWithinQueueAsync(InteractionContext ctx,
			[Option("song", "Song to move within the queue", true), Autocomplete(typeof(AutocompleteProviders.QueueProvider))] string old_posi,
			[Option("position", "Position to move song to", true), Autocomplete(typeof(AutocompleteProviders.QueueProvider))] string new_posi
		)
		{
			await ctx.DeferAsync(true);
			var old_pos = Convert.ToInt32(old_posi);
			var new_pos = Convert.ToInt32(new_posi);
			var g = MikuBot.Guilds[ctx.Guild.Id];
			var queue = await Database.GetQueueAsync(ctx.Guild);
			if (await g.IsNotConnected(ctx))
				return;
			g.musicInstance.usedChannel = ctx.Channel;
			if (old_pos < 1 || new_pos < 1 || old_pos == new_pos || new_pos >= queue.Count)
				return;
			var oldSong = queue[old_pos];
			await Database.MoveQueueItemsAsync(ctx.Guild, old_pos, new_pos);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"**Moved**:\n **{oldSong.track.Title}**\nby {oldSong.track.Author}\n from position **{old_pos}** to **{new_pos}**!").Build()));
		}

		[SlashCommand("remove", "Removes a name_or_url from queue")]
		public static async Task RemoveFromQueueAsync(InteractionContext ctx,
			[Option("song", "Song to remove from queue", true), Autocomplete(typeof(AutocompleteProviders.QueueProvider))] string posi
		)
		{
			var position = Convert.ToInt32(posi);
			await ctx.DeferAsync(true);
			var g = MikuBot.Guilds[ctx.Guild.Id];
			var queue = await Database.GetQueueAsync(ctx.Guild);
			if (await g.IsNotConnected(ctx))
				return;
			g.musicInstance.usedChannel = ctx.Channel;
			var old = queue[position];
			await Database.RemoveFromQueueAsync(position, ctx.Guild);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"**Removed:\n{old.track.Title}**\nby {old.track.Author}").Build()));
		}
	}

	[SlashCommandGroup("options", "Playback Options")]
	[RequireUserAndBotVoicechatConnection]
	public class PlaybackOptions : ApplicationCommandsModule
	{
		[SlashCommand("repeat", "Repeat the current song or the entire queue")]
		public static async Task RepeatAsync(InteractionContext ctx,
			[Option("mode", "New repeat mode"), ChoiceProvider(typeof(FixedOptionProviders.RepeatModeProvider))] RepeatMode mode)
		{
			await ctx.DeferAsync(true);
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;
			g.musicInstance.usedChannel = ctx.Channel;
			g.musicInstance.repeatMode = mode;
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"Set repeat mode to:\n **{g.musicInstance.repeatMode}**").Build()));
		}

		[SlashCommand("shuffle", "Play the queue in shuffle mode")]
		public static async Task ShuffleAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync(true);
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;
			g.musicInstance.usedChannel = ctx.Channel;
			g.musicInstance.shuffleMode = g.musicInstance.shuffleMode == ShuffleMode.Off ? ShuffleMode.On : ShuffleMode.Off;
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"Set shuffle mode to:\n**{g.musicInstance.shuffleMode}**").Build()));
		}
	}

	[SlashCommandGroup("info", "Playing info")]
	public class PlayingInfo : ApplicationCommandsModule
	{
		[SlashCommand("now_playing", "Show whats currently playing")]
		public static async Task ShowNowPlaylingAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync(true);
			var g = MikuBot.Guilds[ctx.Guild.Id];
			g.ShardId = ctx.Client.ShardId;
			var eb = new DiscordEmbedBuilder();
			eb.WithTitle("Now Playing");
			eb.WithDescription("**__Current Song:__**");
			await ctx.SendPlayingInformationAsync(eb, g, null);
		}

		[SlashCommand("last_playing", "Show what played before")]
		public static async Task ShowLastPlaylingAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync(true);
			var lastPlayedSongs = await Database.GetLastPlayingListAsync(ctx.Guild);
			if (!lastPlayedSongs.Any())
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I haven't played anything on this server yet."));
				return;
			}
			var g = MikuBot.Guilds[ctx.Guild.Id];
			g.ShardId = ctx.Client.ShardId;
			var eb = new DiscordEmbedBuilder();
			eb.WithTitle("Last playing");
			eb.WithDescription("**__Previous Song:__**");
			await ctx.SendPlayingInformationAsync(eb, g, lastPlayedSongs);
		}

		[SlashCommand("last_playing_list", "Show what songs were played before")]
		public static async Task ShowLastPlaylingListAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync(true);
			var lastPlayedSongs = await Database.GetLastPlayingListAsync(ctx.Guild);
			if (!lastPlayedSongs.Any())
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I haven't played anything on this server yet."));
				return;
			}
			try
			{
				var g = MikuBot.Guilds[ctx.Guild.Id];
				if (lastPlayedSongs.Count == 0)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Queue empty"));
					return;
				}
				var inter = ctx.Client.GetInteractivity();
				int songsPerPage = 0;
				int currentPage = 1;
				int songAmount = 0;
				int totalP = lastPlayedSongs.Count / 10;
				if ((lastPlayedSongs.Count % 10) != 0)
					totalP++;
				var emb = new DiscordEmbedBuilder();
				List<Page> Pages = new();
				foreach (var Track in lastPlayedSongs)
				{
					Track.GetPlayingState(out var time);
					emb.AddField(new DiscordEmbedField($"{songAmount + 1}.{Track.track.Title.Replace("*", "").Replace("|", "")}", $"by {Track.track.Author.Replace("*", "").Replace("|", "")} [{time}] [Link]({Track.track.Uri})"));
					songsPerPage++;
					songAmount++;
					if (songsPerPage == 10)
					{
						songsPerPage = 0;
						emb.WithTitle("Last played songs in this server:\n");
						emb.WithFooter($"Page {currentPage}/{totalP}");
						Pages.Add(new Page(embed: emb));
						emb.ClearFields();
						emb.WithTitle("more™");
						currentPage++;
					}
					if (songAmount == lastPlayedSongs.Count)
					{
						emb.WithTitle("Last played songs in this server:\n");
						emb.WithFooter($"Page {currentPage}/{totalP}");
						Pages.Add(new Page(embed: emb));
						emb.ClearFields();
					}
				}
				if (currentPage == 1)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(Pages.First().Embed));
					return;
				}
				else if (currentPage == 2 && songsPerPage == 0)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(Pages.First().Embed));
					return;
				}
				foreach (var eP in Pages.Where(x => x.Embed.Fields.Count == 0).ToList())
					Pages.Remove(eP);
				await inter.SendPaginatedResponseAsync(ctx.Interaction, true, false, ctx.User, Pages);
			}
			catch (Exception ex)
			{
				ctx.Client.Logger.LogError("{ex}", ex.Message);
				ctx.Client.Logger.LogError("{ex}", ex.StackTrace);
			}
		}
	}
}
