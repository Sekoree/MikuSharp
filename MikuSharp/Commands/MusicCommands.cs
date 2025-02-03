using System;
using System.Linq;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Enums;

using MikuSharp.Attributes;
using MikuSharp.Entities;
using MikuSharp.Utilities;

namespace MikuSharp.Commands;

/// <summary>
///     The music commands
/// </summary>
[SlashCommandGroup("music", "Music commands", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall]), DeferResponseAsync(true), EnsureLavalinkSession]
public class MusicCommands : ApplicationCommandsModule
{
	/// <summary>
	///     Joins a voice channel the user is in.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	[SlashCommand("join", "Joins the voice channel you're in"), RequireUserVoicechatConnection, AutomaticallyDisconnectExistingSession]
	public static async Task JoinAsync(InteractionContext ctx)
	{
		ArgumentNullException.ThrowIfNull(ctx.Member?.VoiceState?.Channel);
		ArgumentNullException.ThrowIfNull(ctx.Guild);
		ArgumentNullException.ThrowIfNull(ctx.GuildId);

		var session = ctx.Client.GetLavalink().DefaultSession();
		await ctx.Client.GetLavalink().DefaultSession().ConnectAsync(ctx.Member.VoiceState.Channel);
		MusicSession musicSession = new(ctx.Member.VoiceState.Channel, ctx.Guild, session);
		MikuBot.MusicSessions.Add(ctx.GuildId.Value, musicSession.InjectPlayer());
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Heya {ctx.Member.Mention}!"));
		await musicSession.CurrentChannel.SendMessageAsync("Hatsune Miku at your service!");
		musicSession.UpdateStatusMessage(await musicSession.CurrentChannel.SendMessageAsync(ctx.BuildMusicStatusEmbed(musicSession, "Nothing playing yet")));
	}

	/// <summary>
	///     Leaves a voice channel.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	[SlashCommand("leave", "Leaves the voice channel"), RequireUserAndBotVoicechatConnection]
	public static async Task LeaveAsync(InteractionContext ctx)
	{
		ArgumentNullException.ThrowIfNull(ctx.GuildId);

		if (MikuBot.MusicSessions.Remove(ctx.GuildId.Value, out var musicSession))
		{
			await musicSession.LavalinkGuildPlayer.DisconnectAsync();
			await musicSession.CurrentChannel.SendMessageAsync("Bye bye humans 💙");
			await musicSession.StatusMessage.DeleteAsync("Miku disconnected");
		}

		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cya! 💙"));
	}

	/// <summary>
	///     The playback commands.
	/// </summary>
	[SlashCommandGroup("playback", "Music playback commands"), RequireUserAndBotVoicechatConnection]
	public class PlaybackCommands : ApplicationCommandsModule
	{
		/// <summary>
		///     A dummy command.
		/// </summary>
		/// <param name="ctx">The interaction context.</param>
		[SlashCommand("dummy", "Dummy command")]
		public async Task DummyCommand(InteractionContext ctx)
			=> await ctx.EditResponseAsync("This command is a placeholder and does nothing.");
	}

	/// <summary>
	///     The options commands.
	/// </summary>
	[SlashCommandGroup("options", "Music options commands"), RequireUserAndBotVoicechatConnection]
	public class OptionsCommands : ApplicationCommandsModule
	{
		[SlashCommand("repeat", "Repeat the current song or the entire queue")]
		public static async Task RepeatAsync(
			InteractionContext ctx,
			[Option("mode", "New repeat mode"), ChoiceProvider(typeof(FixedOptionProviders.RepeatModeProvider))]
			RepeatMode mode
		)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);
			var musicSession = MikuBot.MusicSessions[ctx.GuildId.Value];
			musicSession.UpdateRepeatMode(mode);
			musicSession.UpdateStatusMessage(await musicSession.CurrentChannel.SendMessageAsync(ctx.BuildMusicStatusEmbed(musicSession, musicSession.StatusMessage.Embeds.First().Description)));
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Set repeat mode to: **{mode}**"));
		}

		[SlashCommand("shuffle", "Shuffle the queue")]
		public static async Task ShuffleAsync(InteractionContext ctx)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);
			var musicSession = MikuBot.MusicSessions[ctx.GuildId.Value];
			musicSession.LavalinkGuildPlayer.ShuffleQueue();
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Shuffled the queue!"));
		}
	}

	/// <summary>
	///     The queue commands.
	/// </summary>
	[SlashCommandGroup("queue", "Music queue commands"), RequireUserAndBotVoicechatConnection]
	public class QueueCommands : ApplicationCommandsModule
	{
		/// <summary>
		///     A dummy command.
		/// </summary>
		/// <param name="ctx">The interaction context.</param>
		[SlashCommand("dummy", "Dummy command")]
		public async Task DummyCommand(InteractionContext ctx)
			=> await ctx.EditResponseAsync("This command is a placeholder and does nothing.");
	}

	/// <summary>
	///     The info commands.
	/// </summary>
	[SlashCommandGroup("info", "Music info commands"), RequireUserAndBotVoicechatConnection]
	public class InfoCommands : ApplicationCommandsModule
	{
		/// <summary>
		///     A dummy command.
		/// </summary>
		/// <param name="ctx">The interaction context.</param>
		[SlashCommand("dummy", "Dummy command")]
		public async Task DummyCommand(InteractionContext ctx)
			=> await ctx.EditResponseAsync("This command is a placeholder and does nothing.");
	}
}
