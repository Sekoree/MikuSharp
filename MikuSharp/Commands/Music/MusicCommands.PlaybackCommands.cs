using MikuSharp.Attributes;
using MikuSharp.Enums;
using MikuSharp.Utilities;

namespace MikuSharp.Commands.Music;

public partial class MusicCommands
{
	/// <summary>
	///     The playback commands.
	/// </summary>
	[SlashCommandGroup("playback", "Music playback commands"), RequireUserAndBotVoicechatConnection]
	public class PlaybackCommands : ApplicationCommandsModule
	{
		/// <summary>
		///     Pauses the playback.
		/// </summary>
		/// <param name="ctx">The interaction context.</param>
		[SlashCommand("pause", "Pauses the playback"), RequirePlaybackState(PlaybackState.Playing)]
		public async Task PauseAsync(InteractionContext ctx)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);
			var guildId = ctx.GuildId.Value;
			var asyncLock = MikuBot.MusicSessionLocks.GetOrAdd(guildId, _ => new());
			using (await asyncLock.LockAsync(MikuBot.Cts.Token))
			{
				if (MikuBot.MusicSessions.TryGetValue(guildId, out var musicSession))
				{
					await musicSession.LavalinkGuildPlayer.PauseAsync();
					musicSession.UpdatePlaybackState(PlaybackState.Paused);
					await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed());
					await ctx.EditResponseAsync("Paused the playback! ");
				}
			}
		}

		/// <summary>
		///     Resumes the playback.
		/// </summary>
		/// <param name="ctx">The interaction context.</param>
		[SlashCommand("resume", "Resumes the playback"), RequirePlaybackState(PlaybackState.Paused)]
		public async Task ResumeAsync(InteractionContext ctx)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);
			var guildId = ctx.GuildId.Value;
			var asyncLock = MikuBot.MusicSessionLocks.GetOrAdd(guildId, _ => new());
			using (await asyncLock.LockAsync(MikuBot.Cts.Token))
			{
				if (MikuBot.MusicSessions.TryGetValue(guildId, out var musicSession))
				{
					await musicSession.LavalinkGuildPlayer.ResumeAsync();
					musicSession.UpdatePlaybackState(PlaybackState.Playing);
					await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed());
					await ctx.EditResponseAsync("Resumed the playback!");
				}
			}
		}

		[SlashCommand("stop", "Stop Playback"), RequirePlaybackState(PlaybackState.Playing, PlaybackState.Paused)]
		public static async Task StopAsync(InteractionContext ctx)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);
			var guildId = ctx.GuildId.Value;
			var asyncLock = MikuBot.MusicSessionLocks.GetOrAdd(guildId, _ => new());
			using (await asyncLock.LockAsync(MikuBot.Cts.Token))
			{
				if (MikuBot.MusicSessions.TryGetValue(guildId, out var musicSession))
				{
					musicSession.UpdateRepeatMode(RepeatMode.None);
					musicSession.LavalinkGuildPlayer.ClearQueue();
					await musicSession.LavalinkGuildPlayer.StopAsync();
					musicSession.UpdatePlaybackState(PlaybackState.Stopped);
					await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed());
					await ctx.EditResponseAsync("Stopped the playback!");
				}
			}
		}

		[SlashCommand("volume", "Change the music volume")]
		public static async Task ModifyVolumeAsync(
			InteractionContext ctx,
			[Option("volume", "Level of volume to set (Percentage)"), MinimumValue(0), MaximumValue(150)]
			int volume = 100
		)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);
			var guildId = ctx.GuildId.Value;
			var asyncLock = MikuBot.MusicSessionLocks.GetOrAdd(guildId, _ => new());
			using (await asyncLock.LockAsync(MikuBot.Cts.Token))
			{
				if (MikuBot.MusicSessions.TryGetValue(guildId, out var musicSession))
				{
					await musicSession.LavalinkGuildPlayer.SetVolumeAsync(volume);
					await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed());
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Set the volume to **{volume}%**!"));
				}
			}
		}

		[SlashCommand("seek", "Seeks the currently playing song to given position"), RequirePlaybackState(PlaybackState.Playing, PlaybackState.Paused)]
		public static async Task SeekAsync(InteractionContext ctx, [Option("position", "Position to seek to")] double position)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);
			var guildId = ctx.GuildId.Value;
			var asyncLock = MikuBot.MusicSessionLocks.GetOrAdd(guildId, _ => new());
			using (await asyncLock.LockAsync(MikuBot.Cts.Token))
			{
				if (MikuBot.MusicSessions.TryGetValue(guildId, out var musicSession))
				{
					var targetSeek = TimeSpan.FromSeconds(position);
					await musicSession.LavalinkGuildPlayer.SeekAsync(targetSeek);
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Seeked to **{targetSeek.FormatTimeSpan()}**!"));
				}
			}
		}

		[SlashCommand("play", "Plays a url")]
		public async Task PlayUrlAsync(InteractionContext ctx, [Option("url", "The url to play")] string url)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Searching for `{url}`.."));
			ArgumentNullException.ThrowIfNull(ctx.GuildId);
			var guildId = ctx.GuildId.Value;
			var asyncLock = MikuBot.MusicSessionLocks.GetOrAdd(guildId, _ => new());
			using (await asyncLock.LockAsync(MikuBot.Cts.Token))
			{
				if (MikuBot.MusicSessions.TryGetValue(guildId, out var musicSession))
					await musicSession.LoadAndPlayTrackAsync(ctx, url);
			}
		}

		[SlashCommand("skip", "Skips to the next song")]
		public async Task SkipAsync(InteractionContext ctx)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);
			var guildId = ctx.GuildId.Value;
			var asyncLock = MikuBot.MusicSessionLocks.GetOrAdd(guildId, _ => new());
			using (await asyncLock.LockAsync(MikuBot.Cts.Token))
			{
				if (MikuBot.MusicSessions.TryGetValue(guildId, out var musicSession))
				{
					if (musicSession.LavalinkGuildPlayer.TryPeekQueue(out _))
					{
						await musicSession.LavalinkGuildPlayer.SkipAsync();
						await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Successfully skipped the song!"));
					}
					else
						await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cannot skip as there are no more songs in the queue."));
				}
			}
		}
	}
}
