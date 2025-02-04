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
			await ctx.ExecuteWithMusicSessionAsync(async (_, musicSession) =>
			{
				await musicSession.LavalinkGuildPlayer.PauseAsync();
				musicSession.UpdatePlaybackState(PlaybackState.Paused);
				await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed());
				await ctx.EditResponseAsync("Paused the playback! ");
			});
		}

		/// <summary>
		///     Resumes the playback.
		/// </summary>
		/// <param name="ctx">The interaction context.</param>
		[SlashCommand("resume", "Resumes the playback"), RequirePlaybackState(PlaybackState.Paused)]
		public async Task ResumeAsync(InteractionContext ctx)
		{
			await ctx.ExecuteWithMusicSessionAsync(async (_, musicSession) =>
			{
				await musicSession.LavalinkGuildPlayer.ResumeAsync();
				musicSession.UpdatePlaybackState(PlaybackState.Playing);
				await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed());
				await ctx.EditResponseAsync("Resumed the playback!");
			});
		}

		/// <summary>
		///     Stops the playback.
		/// </summary>
		/// <param name="ctx">The interaction context.</param>
		[SlashCommand("stop", "Stop Playback"), RequirePlaybackState(PlaybackState.Playing, PlaybackState.Paused)]
		public static async Task StopAsync(InteractionContext ctx)
		{
			await ctx.ExecuteWithMusicSessionAsync(async (_, musicSession) =>
			{
				musicSession.UpdateRepeatMode(RepeatMode.None);
				musicSession.LavalinkGuildPlayer.ClearQueue();
				await musicSession.LavalinkGuildPlayer.StopAsync();
				musicSession.UpdatePlaybackState(PlaybackState.Stopped);
				await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed("Nothing playing"));
				await ctx.EditResponseAsync("Stopped the playback!");
			});
		}

		/// <summary>
		///     Changes the volume of the music player.
		/// </summary>
		/// <param name="ctx">The interaction context.</param>
		/// <param name="volume">The volume to set.</param>
		[SlashCommand("volume", "Change the music volume")]
		public static async Task ModifyVolumeAsync(
			InteractionContext ctx,
			[Option("volume", "Level of volume to set (Percentage)"), MinimumValue(0), MaximumValue(150)]
			int volume = 100
		)
		{
			await ctx.ExecuteWithMusicSessionAsync(async (_, musicSession) =>
			{
				await musicSession.LavalinkGuildPlayer.SetVolumeAsync(volume);
				await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed());
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Set the volume to **{volume}%**!"));
			});
		}

		/// <summary>
		///     Seeks the currently playing song to given position.
		/// </summary>
		/// <param name="ctx">The interaction context.</param>
		/// <param name="position">The position to seek to.</param>
		[SlashCommand("seek", "Seeks the currently playing song to given position"), RequirePlaybackState(PlaybackState.Playing, PlaybackState.Paused)]
		public static async Task SeekAsync(InteractionContext ctx, [Option("position", "Position to seek to")] double position)
		{
			await ctx.ExecuteWithMusicSessionAsync(async (_, musicSession) =>
			{
				var targetSeek = TimeSpan.FromSeconds(position);
				await musicSession.LavalinkGuildPlayer.SeekAsync(targetSeek);
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Seeked to **{targetSeek.FormatTimeSpan()}**!"));
			});
		}

		/// <summary>
		///     Plays a url.
		/// </summary>
		/// <param name="ctx">The interaction context.</param>
		/// <param name="url">The url to play.</param>
		[SlashCommand("play", "Plays a url")]
		public async Task PlayUrlAsync(InteractionContext ctx, [Option("url", "The url to play")] string url)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Searching for `{url}`.."));
			await ctx.ExecuteWithMusicSessionAsync(async (_, musicSession) => await musicSession.LoadAndPlayTrackAsync(ctx, url));
		}
	}
}
