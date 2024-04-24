using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;

using Microsoft.Extensions.Logging;

using MikuSharp.Attributes;
using MikuSharp.Entities;
using MikuSharp.Enums;
using MikuSharp.Events;
using MikuSharp.Utilities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikuSharp.Commands;

/// <summary>
/// The music commands
/// </summary>
[SlashCommandGroup("music", "Music commands", dmPermission: false)]
public class Music : ApplicationCommandsModule
{
	private static readonly string[] Units = { "", "ki", "Mi", "Gi" };

	private static string SizeToString(long l)
	{
		double d = l;
		var u = 0;

		while (d >= 900 && u < Units.Length - 2)
		{
			u++;
			d /= 1024;
		}

		return $"{d:#,##0.00} {Units[u]}B";
	}

	[SlashCommandGroup("base", "Base commands")]
	public class Base : ApplicationCommandsModule
	{
		[SlashCommand("join", "Joins the voice channel you're in"), RequireUserVoicechatConnection]
		public static async Task JoinAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync();
			if (MikuBot.Guilds.All(x => x.Key != ctx.Guild.Id))
				MikuBot.Guilds.TryAdd(ctx.Guild.Id, new(ctx.Client.ShardId));
			var g = MikuBot.Guilds[ctx.Guild.Id];
			g.MusicInstance ??= new(MikuBot.LavalinkSessions[ctx.Client.ShardId], ctx.Client.ShardId);
			await g.ConditionalConnect(ctx);
			g.MusicInstance.UsedChannel = ctx.Channel;
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Heya {ctx.Member.Mention}!"));
		}

		[SlashCommand("leave", "Leaves the channel")]
		public static async Task LeaveAsync(InteractionContext ctx, [Option("keep", "Whether to keep the queue")] bool keep = false)
		{
			await ctx.DeferAsync();
			var g = MikuBot.Guilds[ctx.Guild.Id];

			if (g.MusicInstance == null)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I'm not in a voice channel"));
				return;
			}

			g.MusicInstance.Playstate = Playstate.NotPlaying;

			try
			{
				if (keep)
					await g.MusicInstance.GuildConnection.StopAsync();
				await g.MusicInstance.GuildConnection.DisconnectAsync();
				if (!keep)
					await Database.ClearQueue(ctx.Guild);
				g.MusicInstance = null;
			}
			catch (Exception)
			{ }

			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cya! 💙"));
		}

		[SlashCommand("lstats", "Displays Lavalink statistics"), ApplicationCommandRequireTeamDeveloper]
		public static async Task GetLavalinkStatsAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync();
			var stats = MikuBot.LavalinkSessions[ctx.Client.ShardId].Statistics;
			var sb = new StringBuilder();
			sb.Append("Lavalink resources usage statistics: ```")
				.Append("Uptime:                    ").Append(stats.Uptime).AppendLine()
				.Append("Players:                   ").Append($"{stats.PlayingPlayers} active / {stats.Players} total").AppendLine()
				.Append("CPU Cores:                 ").Append(stats.Cpu.Cores).AppendLine()
				.Append("CPU Usage:                 ").Append($"{stats.Cpu.LavalinkLoad:#,##0.0%} lavalink / {stats.Cpu.SystemLoad:#,##0.0%} system").AppendLine()
				.Append("RAM Usage:                 ").Append($"{SizeToString(stats.Memory.Allocated)} allocated / {SizeToString(stats.Memory.Used)} used / {SizeToString(stats.Memory.Free)} free / {SizeToString(stats.Memory.Reservable)} reservable").AppendLine()
				.Append("Audio frames (per minute): ").Append($"{stats.Frames.Sent:#,##0} sent / {stats.Frames.Nulled:#,##0} nulled / {stats.Frames.Deficit:#,##0} deficit").AppendLine()
				.Append("```");
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(sb.ToString()));
		}
	}

	[SlashCommandGroup("playback", "Playback controls")]
	public class Playback : ApplicationCommandsModule
	{
		[SlashCommand("seek", "Seek a song"), RequireUserAndBotVoicechatConnection]
		public static async Task SeekAsync(InteractionContext ctx, [Option("position", "Position to seek to")] double position)
		{
			await ctx.DeferAsync();
			if (MikuBot.Guilds.All(x => x.Key != ctx.Guild.Id))
				MikuBot.Guilds.TryAdd(ctx.Guild.Id, new(ctx.Client.ShardId));
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;

			if (g.MusicInstance.Playstate != Playstate.Playing && g.MusicInstance.Playstate != Playstate.Paused)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I don't play anything right now"));
				return;
			}

			g.MusicInstance.UsedChannel = ctx.Channel;
			var ts = TimeSpan.FromSeconds(position);
			await g.MusicInstance.GuildConnection.SeekAsync(ts);
			var pos = ts.Hours < 1 ? ts.ToString(@"mm\:ss") : ts.ToString(@"hh\:mm\:ss");
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Seeked {g.MusicInstance.CurrentSong.Track.Info.Title} to {pos}"));
		}

		[SlashCommand("play", "Play or queue a song"), RequireUserVoicechatConnection]
		public static async Task PlayAsync(
			InteractionContext ctx,
			[Option("song", "Song name or url to play")] string nameOrUrl = null,
			[Option("music_file", "Music file to play")] DiscordAttachment musicFile = null
		)
		{
			await ctx.DeferAsync();
			if (MikuBot.Guilds.All(x => x.Key != ctx.Guild.Id))
				MikuBot.Guilds.TryAdd(ctx.Guild.Id, new(ctx.Client.ShardId));
			var g = MikuBot.Guilds[ctx.Guild.Id];
			g.MusicInstance ??= new(MikuBot.LavalinkSessions[ctx.Client.ShardId], ctx.Client.ShardId);
			var curq = await Database.GetQueueAsync(ctx.Guild);

			if (curq.Count != 0 && g.MusicInstance.Playstate == Playstate.NotPlaying)
			{
				var inter = ctx.Client.GetInteractivity();
				List<DiscordButtonComponent> buttons = new(2)
				{
					new(ButtonStyle.Success, "restore", "Restore old queue"),
					new(ButtonStyle.Danger, "clear", "Clear old queue")
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
					await g.MusicInstance.ConnectToChannel(ctx.Member.VoiceState.Channel);
					await g.MusicInstance.PlaySong();
					return;
				}
				else
				{
					await hmm.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
					await Database.ClearQueue(ctx.Guild);
					buttons.ForEach(x => x.Disable());
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cleared").AddComponents(buttons));
				}
			}

			await g.ConditionalConnect(ctx);

			if (musicFile == null && nameOrUrl == null)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error: No song or file choosen"));
				return;
			}

			g.MusicInstance.UsedChannel = ctx.Channel;
			nameOrUrl = musicFile.SearchUrlOrAttachment(nameOrUrl);
			var oldState = g.MusicInstance.Playstate;
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Trying to play/search {nameOrUrl}..."));
			var q = await g.MusicInstance.QueueSong(nameOrUrl, ctx);

			if (q == null)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error: Song not found"));
				return;
			}

			var emb = new DiscordEmbedBuilder();

			if (oldState == Playstate.Playing)
			{
				emb.AddField(new(q.Tracks.First().Info.Title + "[" + (q.Tracks.First().Info.Length.Hours != 0 ? q.Tracks.First().Info.Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Info.Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Info.Author}\n" + $"Requested by {ctx.Member.Mention}"));
				if (q.Tracks.Count != 1)
					emb.AddField(new("Playlist added:", $"added {q.Tracks.Count - 1} more"));
				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(emb.WithTitle("Added").Build()).AsEphemeral());
			}
			else
			{
				if (q.PlaylistInfo.SelectedTrack == -1 || q.PlaylistInfo.Name == null)
					emb.AddField(new(q.Tracks.First().Info.Title + "[" + (q.Tracks.First().Info.Length.Hours != 0 ? q.Tracks.First().Info.Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Info.Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Info.Author}\nRequested by {ctx.Member.Mention}"));
				else
					emb.AddField(new(q.Tracks[q.PlaylistInfo.SelectedTrack].Info.Title + "[" + (q.Tracks[q.PlaylistInfo.SelectedTrack].Info.Length.Hours != 0 ? q.Tracks[q.PlaylistInfo.SelectedTrack].Info.Length.ToString(@"hh\:mm\:ss") : q.Tracks[q.PlaylistInfo.SelectedTrack].Info.Length.ToString(@"mm\:ss")) + "]",
						$"by {q.Tracks[q.PlaylistInfo.SelectedTrack].Info.Author}\nRequested by {ctx.Member.Mention}"));
				if (q.Tracks.Count != 1)
					emb.AddField(new("Playlist added:", $"added {q.Tracks.Count - 1} more"));
				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(emb.WithTitle("Playing").Build()).AsEphemeral());
			}
		}

		[SlashCommand("insert", "Queue a song at a specific position!"), RequireUserVoicechatConnection]
		public static async Task InsertToQueueAsync(
			InteractionContext ctx,
			[Option("position", "Position to move song to", true), Autocomplete(typeof(AutocompleteProviders.QueueProvider))]
			string posi,
			[Option("song", "Song name or url to play")] string nameOrUrl = null,
			[Option("music_file", "Music file to play")] DiscordAttachment musicFile = null
		)
		{
			await ctx.DeferAsync();
			var pos = Convert.ToInt32(posi);
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (pos < 1)
				return;

			g.MusicInstance ??= new(MikuBot.LavalinkSessions[ctx.Client.ShardId], ctx.Client.ShardId);

			await g.ConditionalConnect(ctx);

			if (musicFile == null && nameOrUrl == null)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error: No song or file choosen"));
				return;
			}

			g.MusicInstance.UsedChannel = ctx.Channel;
			nameOrUrl = musicFile.SearchUrlOrAttachment(nameOrUrl);
			var oldState = g.MusicInstance.Playstate;
			var q = await g.MusicInstance.QueueSong(nameOrUrl, ctx, pos);

			if (q == null)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error: Song not found"));
				return;
			}

			var emb = new DiscordEmbedBuilder();

			if (oldState == Playstate.Playing)
			{
				emb.AddField(new(q.Tracks.First().Info.Title + "[" + (q.Tracks.First().Info.Length.Hours != 0 ? q.Tracks.First().Info.Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Info.Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Info.Author}\n" + $"Requested by {ctx.Member.Mention}\nAt position: {pos}"));
				if (q.Tracks.Count != 1)
					emb.AddField(new("Playlist added:", $"added {q.Tracks.Count - 1} more"));
				emb.WithTitle("Playing");
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(emb.Build()));
			}
			else
			{
				if (q.PlaylistInfo.SelectedTrack == -1 || q.PlaylistInfo.Name == null)
					emb.AddField(new(q.Tracks.First().Info.Title + "[" + (q.Tracks.First().Info.Length.Hours != 0 ? q.Tracks.First().Info.Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Info.Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Info.Author}\nRequested by {ctx.Member.Mention}"));
				else
					emb.AddField(new(q.Tracks[q.PlaylistInfo.SelectedTrack].Info.Title + "[" + (q.Tracks[q.PlaylistInfo.SelectedTrack].Info.Length.Hours != 0 ? q.Tracks[q.PlaylistInfo.SelectedTrack].Info.Length.ToString(@"hh\:mm\:ss") : q.Tracks[q.PlaylistInfo.SelectedTrack].Info.Length.ToString(@"mm\:ss")) + "]",
						$"by {q.Tracks[q.PlaylistInfo.SelectedTrack].Info.Author}\nRequested by {ctx.Member.Mention}At position: {pos}"));
				if (q.Tracks.Count != 1)
					emb.AddField(new("Playlist added:", $"added {q.Tracks.Count - 1} more"));
				emb.WithTitle("Added");
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(emb.Build()));
			}
		}

		[SlashCommand("skip", "Skip the current song"), RequireUserAndBotVoicechatConnection]
		public static async Task SkipSongAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync();
			var g = MikuBot.Guilds[ctx.Guild.Id];
			var lastPlayedSongs = await Database.GetLastPlayingListAsync(ctx.Guild);
			var queue = await Database.GetQueueAsync(ctx.Guild);
			if (await g.IsNotConnected(ctx))
				return;

			g.MusicInstance.UsedChannel = ctx.Channel;
			g.MusicInstance.GuildConnection.TrackEnded -= Lavalink.LavalinkTrackFinish;

			if (g.MusicInstance.CurrentSong != null)
			{
				if (g.MusicInstance.RepeatMode != RepeatMode.On && g.MusicInstance.RepeatMode != RepeatMode.All)
					await Database.RemoveFromQueueAsync(g.MusicInstance.CurrentSong.Position, ctx.Guild);
				if (lastPlayedSongs.Count == 0)
					await Database.AddToLastPlayingListAsync(ctx.Guild.Id, g.MusicInstance.CurrentSong.Track.Encoded);
				else if (lastPlayedSongs[0]?.Track.Info.Uri != g.MusicInstance.CurrentSong.Track.Info.Uri)
					await Database.AddToLastPlayingListAsync(ctx.Guild.Id, g.MusicInstance.CurrentSong.Track.Encoded);
			}

			queue = await Database.GetQueueAsync(ctx.Guild);
			g.MusicInstance.LastSong = g.MusicInstance.CurrentSong;
			g.MusicInstance.CurrentSong = null;

			if (queue.Count != 0)
				await g.MusicInstance.PlaySong();
			else
			{
				g.MusicInstance.Playstate = Playstate.NotPlaying;
				await g.MusicInstance.GuildConnection.StopAsync();
			}

			if (g.MusicInstance.LastSong != null)
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"**Skipped:**\n{g.MusicInstance.LastSong.Track.Info.Title}").Build()));
			else
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"**Continued!**").Build()));
		}

		[SlashCommand("stop", "Stop Playback"), RequireUserAndBotVoicechatConnection]
		public static async Task StopAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync();
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;

			g.MusicInstance.UsedChannel = ctx.Channel;
			await Task.Run(async () => await g.MusicInstance.GuildConnection.StopAsync());
			var cmdId = ctx.Client.GetApplicationCommands().GlobalCommands.First(x => x.Name == "music").Id;
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"**Stopped** (use </music playback resume:{cmdId}> to start playback again)").Build()));
		}

		[SlashCommand("volume", "Change the music volume"), RequireUserAndBotVoicechatConnection]
		public static async Task ModifyVolumeAsync(
			InteractionContext ctx,
			[Option("volume", "Level of volume to set (Percentage)"), MinimumValue(0), MaximumValue(150)]
			int vol = 100
		)
		{
			await ctx.DeferAsync();
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;

			g.MusicInstance.UsedChannel = ctx.Channel;
			if (vol > 150) vol = 150;
			await g.MusicInstance.GuildConnection.SetVolumeAsync(vol);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"**Set volume to {vol}**").Build()));
		}

		[SlashCommand("pause", "Pauses playback"), RequireUserAndBotVoicechatConnection]
		public static async Task PauseAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync();
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;

			g.MusicInstance.UsedChannel = ctx.Channel;

			if (g.MusicInstance.Playstate == Playstate.Playing)
			{
				await g.MusicInstance.GuildConnection.PauseAsync();
				g.MusicInstance.Playstate = Playstate.Paused;
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription("**Paused**").Build()));
			}
			else
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I'm not playing anything right now"));
		}

		[SlashCommand("resume", "Resumes paused playback"), RequireUserAndBotVoicechatConnection]
		public static async Task ResumeAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync();
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;

			g.MusicInstance.UsedChannel = ctx.Channel;

			if (g.MusicInstance.Playstate == Playstate.Stopped)
			{
				await g.MusicInstance.PlaySong();
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription("**Started Playback**").Build()));
			}
			else
			{
				await g.MusicInstance.GuildConnection.ResumeAsync();
				g.MusicInstance.Playstate = Playstate.Playing;
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription("**Resumed**").Build()));
			}
		}
	}

	[SlashCommandGroup("queue", "Queue management"), RequireUserAndBotVoicechatConnection]
	public class Queue : ApplicationCommandsModule
	{
		[SlashCommand("show", "Show the current queue")]
		public static async Task ShowQueueAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync();
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
				var songsPerPage = 0;
				var currentPage = 1;
				var songAmount = 0;
				var totalP = queue.Count / 5;
				if (queue.Count % 5 != 0)
					totalP++;
				var emb = new DiscordEmbedBuilder();
				List<Page> pages = new();

				if (g.MusicInstance.RepeatMode == RepeatMode.All)
				{
					songAmount = g.MusicInstance.RepeatAllPos;

					foreach (var track in queue)
					{
						if (songsPerPage == 0 && currentPage == 1)
						{
							emb.WithTitle("Current Queue");
							g.GetPlayingState(out var time1, out var time2);
							emb.AddField(new($"**{songAmount}.{g.MusicInstance.CurrentSong.Track.Info.Title.Replace("*", "").Replace("|", "")}** by {g.MusicInstance.CurrentSong.Track.Info.Author.Replace("*", "").Replace("|", "")} [{time1}/{time2}]",
								$"Requested by <@{g.MusicInstance.CurrentSong.AddedBy}> [Link]({g.MusicInstance.CurrentSong.Track.Info.Uri.AbsoluteUri})\nˉˉˉˉˉ"));
						}
						else
						{
							queue.ElementAt(songAmount).GetPlayingState(out var time);
							emb.AddField(new($"**{songAmount}.{queue.ElementAt(songAmount).Track.Info.Title.Replace("*", "").Replace("|", "")}** by {queue.ElementAt(songAmount).Track.Info.Author.Replace("*", "").Replace("|", "")} [{time}]",
								$"Requested by <@{queue.ElementAt(songAmount).AddedBy}> [Link]({queue.ElementAt(songAmount).Track.Info.Uri.AbsoluteUri})"));
						}

						songsPerPage++;
						songAmount++;
						if (songAmount == queue.Count)
							songAmount = 0;

						if (songsPerPage == 5)
						{
							songsPerPage = 0;
							emb.AddField(new("Playback options", g.MusicInstance.GetPlaybackOptions()));
							emb.WithFooter($"Page {currentPage}/{totalP}");
							pages.Add(new(embed: emb));
							emb.ClearFields();
							emb.WithTitle("more™");
							currentPage++;
						}

						if (songAmount == g.MusicInstance.RepeatAllPos)
						{
							emb.AddField(new("Playback options", g.MusicInstance.GetPlaybackOptions()));
							emb.WithFooter($"Page {currentPage}/{totalP}");
							pages.Add(new(embed: emb));
							emb.ClearFields();
						}
					}
				}
				else
					foreach (var track in queue)
					{
						if (songsPerPage == 0 && currentPage == 1)
						{
							emb.WithTitle("Current Queue");
							g.GetPlayingState(out var time1, out var time2);
							emb.AddField(new($"**{g.MusicInstance.CurrentSong.Track.Info.Title.Replace("*", "").Replace("|", "")}** by {g.MusicInstance.CurrentSong.Track.Info.Author.Replace("*", "").Replace("|", "")} [{time1}/{time2}]",
								$"Requested by <@{g.MusicInstance.CurrentSong.AddedBy}> [Link]({g.MusicInstance.CurrentSong.Track.Info.Uri.AbsoluteUri})\nˉˉˉˉˉ"));
						}
						else
						{
							track.GetPlayingState(out var time);
							emb.AddField(new($"**{songAmount}.{track.Track.Info.Title.Replace("*", "").Replace("|", "")}** by {track.Track.Info.Author.Replace("*", "").Replace("|", "")} [{time}]",
								$"Requested by <@{track.AddedBy}> [Link]({track.Track.Info.Uri.AbsoluteUri})"));
						}

						songsPerPage++;
						songAmount++;

						if (songsPerPage == 5)
						{
							songsPerPage = 0;
							emb.WithFooter($"Page {currentPage}/{totalP}");
							emb.AddField(new("Playback options", g.MusicInstance.GetPlaybackOptions()));
							pages.Add(new(embed: emb));
							emb.ClearFields();
							emb.WithTitle("more™");
							currentPage++;
						}

						if (songAmount == queue.Count)
						{
							emb.WithFooter($"Page {currentPage}/{totalP}");
							emb.AddField(new("Playback options", g.MusicInstance.GetPlaybackOptions()));
							pages.Add(new(embed: emb));
							emb.ClearFields();
						}
					}

				if (currentPage == 1)
				{
					emb.AddField(new("Playback options", g.MusicInstance.GetPlaybackOptions()));
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(pages.First().Embed));
					return;
				}
				else if (currentPage == 2 && songsPerPage == 0)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(pages.First().Embed));
					return;
				}

				foreach (var eP in pages.Where(x => x.Embed.Fields.All(y => y.Name == "Playback keep")).ToList())
					pages.Remove(eP);
				await inter.SendPaginatedResponseAsync(ctx.Interaction, true, false, ctx.User, pages);
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
			await ctx.DeferAsync();
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;

			g.MusicInstance.UsedChannel = ctx.Channel;
			await Database.ClearQueue(ctx.Guild);
			if (g.MusicInstance.CurrentSong != null)
				await Database.AddToQueue(ctx.Guild, g.MusicInstance.CurrentSong.AddedBy, g.MusicInstance.CurrentSong.Track.Encoded);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription("**Cleared queue!**").Build()));
		}

		[SlashCommand("move", "Moves a specific song within the queue")]
		public static async Task MoveWithinQueueAsync(
			InteractionContext ctx,
			[Option("song", "Song to move within the queue", true), Autocomplete(typeof(AutocompleteProviders.QueueProvider))]
			string oldPosi,
			[Option("position", "Position to move song to", true), Autocomplete(typeof(AutocompleteProviders.QueueProvider))]
			string newPosi
		)
		{
			await ctx.DeferAsync();
			var oldPos = Convert.ToInt32(oldPosi);
			var newPos = Convert.ToInt32(newPosi);
			var g = MikuBot.Guilds[ctx.Guild.Id];
			var queue = await Database.GetQueueAsync(ctx.Guild);
			if (await g.IsNotConnected(ctx))
				return;

			g.MusicInstance.UsedChannel = ctx.Channel;
			if (oldPos < 1 || newPos < 1 || oldPos == newPos || newPos >= queue.Count)
				return;

			var oldSong = queue[oldPos];
			await Database.MoveQueueItems(ctx.Guild, oldPos, newPos);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"**Moved**:\n **{oldSong.Track.Info.Title}**\nby {oldSong.Track.Info.Author}\n from position **{oldPos}** to **{newPos}**!").Build()));
		}

		[SlashCommand("remove", "Removes a name_or_url from queue")]
		public static async Task RemoveFromQueueAsync(
			InteractionContext ctx,
			[Option("song", "Song to remove from queue", true), Autocomplete(typeof(AutocompleteProviders.QueueProvider))]
			string posi
		)
		{
			var position = Convert.ToInt32(posi);
			await ctx.DeferAsync();
			var g = MikuBot.Guilds[ctx.Guild.Id];
			var queue = await Database.GetQueueAsync(ctx.Guild);
			if (await g.IsNotConnected(ctx))
				return;

			g.MusicInstance.UsedChannel = ctx.Channel;
			var old = queue[position];
			await Database.RemoveFromQueueAsync(position, ctx.Guild);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"**Removed:\n{old.Track.Info.Title}**\nby {old.Track.Info.Author}").Build()));
		}
	}

	[SlashCommandGroup("options", "Playback Options"), RequireUserAndBotVoicechatConnection]
	public class PlaybackOptions : ApplicationCommandsModule
	{
		[SlashCommand("repeat", "Repeat the current song or the entire queue")]
		public static async Task RepeatAsync(
			InteractionContext ctx,
			[Option("mode", "New repeat mode"), ChoiceProvider(typeof(FixedOptionProviders.RepeatModeProvider))]
			RepeatMode mode
		)
		{
			await ctx.DeferAsync();
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;

			g.MusicInstance.UsedChannel = ctx.Channel;
			g.MusicInstance.RepeatMode = mode;
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"Set repeat mode to:\n **{g.MusicInstance.RepeatMode}**").Build()));
		}

		[SlashCommand("shuffle", "Play the queue in shuffle mode")]
		public static async Task ShuffleAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync();
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;

			g.MusicInstance.UsedChannel = ctx.Channel;
			g.MusicInstance.ShuffleMode = g.MusicInstance.ShuffleMode == ShuffleMode.Off ? ShuffleMode.On : ShuffleMode.Off;
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"Set shuffle mode to:\n**{g.MusicInstance.ShuffleMode}**").Build()));
		}
	}

	[SlashCommandGroup("info", "Playing info")]
	public class PlayingInfo : ApplicationCommandsModule
	{
		[SlashCommand("now_playing", "Show whats currently playing")]
		public static async Task ShowNowPlaylingAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync();
			var g = MikuBot.Guilds[ctx.Guild.Id];
			g.ShardId = ctx.Client.ShardId;
			var eb = new DiscordEmbedBuilder();
			eb.WithTitle("Now Playing");
			eb.WithDescription("**__Current Song:__**");
			await ctx.SendPlayingInformationAsync(eb, g);
		}

		[SlashCommand("last_playing", "Show what played before")]
		public static async Task ShowLastPlaylingAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync();
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
			await ctx.DeferAsync();
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
				var songsPerPage = 0;
				var currentPage = 1;
				var songAmount = 0;
				var totalP = lastPlayedSongs.Count / 10;
				if (lastPlayedSongs.Count % 10 != 0)
					totalP++;
				var emb = new DiscordEmbedBuilder();
				List<Page> pages = new();

				foreach (var track in lastPlayedSongs)
				{
					track.GetPlayingState(out var time);
					emb.AddField(new($"{songAmount + 1}.{track.Track.Info.Title.Replace("*", "").Replace("|", "")}", $"by {track.Track.Info.Author.Replace("*", "").Replace("|", "")} [{time}] [Link]({track.Track.Info.Uri})"));
					songsPerPage++;
					songAmount++;

					if (songsPerPage == 10)
					{
						songsPerPage = 0;
						emb.WithTitle("Last played songs in this server:\n");
						emb.WithFooter($"Page {currentPage}/{totalP}");
						pages.Add(new(embed: emb));
						emb.ClearFields();
						emb.WithTitle("more™");
						currentPage++;
					}

					if (songAmount != lastPlayedSongs.Count)
						continue;

					emb.WithTitle("Last played songs in this server:\n");
					emb.WithFooter($"Page {currentPage}/{totalP}");
					pages.Add(new(embed: emb));
					emb.ClearFields();
				}

				switch (currentPage)
				{
					case 1:
						await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(pages.First().Embed));
						return;
					case 2 when songsPerPage == 0:
						await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(pages.First().Embed));
						return;
				}

				foreach (var eP in pages.Where(x => x.Embed.Fields.Count == 0).ToList())
					pages.Remove(eP);
				await inter.SendPaginatedResponseAsync(ctx.Interaction, true, false, ctx.User, pages);
			}
			catch (Exception ex)
			{
				ctx.Client.Logger.LogError("{ex}", ex.Message);
				ctx.Client.Logger.LogError("{ex}", ex.StackTrace);
			}
		}
	}
}