using System;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;

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
		[SlashCommand("pause", "Pauses the playback")]
		public async Task PauseAsync(InteractionContext ctx)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);
			var musicSession = MikuBot.MusicSessions[ctx.GuildId.Value];
			await musicSession.LavalinkGuildPlayer.PauseAsync();
			musicSession.UpdatePlayState(PlayState.Paused);
			await musicSession.UpdateStatusMessageAsync(ctx.BuildMusicStatusEmbed(musicSession));
			await ctx.EditResponseAsync("Paused the playback! ");
		}

		/// <summary>
		///     Resumes the playback.
		/// </summary>
		/// <param name="ctx">The interaction context.</param>
		[SlashCommand("resume", "Resumes the playback")]
		public async Task ResumeAsync(InteractionContext ctx)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);
			var musicSession = MikuBot.MusicSessions[ctx.GuildId.Value];
			await musicSession.LavalinkGuildPlayer.ResumeAsync();
			musicSession.UpdatePlayState(PlayState.Playing);
			await musicSession.UpdateStatusMessageAsync(ctx.BuildMusicStatusEmbed(musicSession));
			await ctx.EditResponseAsync("Resumed the playback!");
		}

		[SlashCommand("stop", "Stop Playback"), RequireUserAndBotVoicechatConnection]
		public static async Task StopAsync(InteractionContext ctx)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);
			var musicSession = MikuBot.MusicSessions[ctx.GuildId.Value];
			await musicSession.LavalinkGuildPlayer.StopAsync();
			musicSession.LavalinkGuildPlayer.ClearQueue();
			musicSession.UpdatePlayState(PlayState.Stopped);
			await musicSession.UpdateStatusMessageAsync(ctx.BuildMusicStatusEmbed(musicSession));
			await ctx.EditResponseAsync("Stopped the playback!");
		}

		[SlashCommand("volume", "Change the music volume"), RequireUserAndBotVoicechatConnection]
		public static async Task ModifyVolumeAsync(
			InteractionContext ctx,
			[Option("volume", "Level of volume to set (Percentage)"), MinimumValue(0), MaximumValue(150)]
			int vol = 100
		)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);
			var musicSession = MikuBot.MusicSessions[ctx.GuildId.Value];
			await musicSession.LavalinkGuildPlayer.SetVolumeAsync(vol);
			await musicSession.UpdateStatusMessageAsync(ctx.BuildMusicStatusEmbed(musicSession));
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Set the volume to **{vol}%**!"));
		}

		[SlashCommand("seek", "Seek a song"), RequireUserAndBotVoicechatConnection]
		public static async Task SeekAsync(InteractionContext ctx, [Option("position", "Position to seek to")] double position)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);

			var musicSession = MikuBot.MusicSessions[ctx.GuildId.Value];
			var targetSeek = TimeSpan.FromSeconds(position);
			await musicSession.LavalinkGuildPlayer.SeekAsync(targetSeek);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Seeked to **{targetSeek.FormatTimeSpan()}**!"));
		}
	}
}
