using MikuSharp.Attributes;
using MikuSharp.Entities;
using MikuSharp.Utilities;

namespace MikuSharp.Commands.Music;

/// <summary>
///     The music commands
/// </summary>
[SlashCommandGroup("music", "Music commands", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall]), DeferResponseAsync(true), EnsureLavalinkSession]
public partial class MusicCommands : ApplicationCommandsModule
{
	/// <summary>
	///     Joins a voice channel the user is in.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	[SlashCommand("join", "Joins the voice channel you're in"), RequireUserVoicechatConnection, AutomaticallyDisconnectExistingSession]
	public async Task JoinAsync(InteractionContext ctx)
	{
		ArgumentNullException.ThrowIfNull(ctx.Member?.VoiceState?.Channel);
		ArgumentNullException.ThrowIfNull(ctx.Guild);
		ArgumentNullException.ThrowIfNull(ctx.GuildId);

		var guildId = ctx.GuildId.Value;
		var asyncLock = MikuBot.MusicSessionLocks.GetOrAdd(guildId, _ => new());

		using (await asyncLock.LockAsync(MikuBot.Cts.Token))
		{
			if (!MikuBot.MusicSessions.TryGetValue(guildId, out var musicSession))
			{
				var session = ctx.Client.GetLavalink().DefaultSession();
				ArgumentNullException.ThrowIfNull(session);
				await session.ConnectAsync(ctx.Member.VoiceState.Channel);
				musicSession = await new MusicSession(ctx.Member.VoiceState.Channel, ctx.Guild, session).InjectPlayerAsync();
				MikuBot.MusicSessions[guildId] = musicSession;
			}

			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Heya {ctx.Member.Mention}!"));
			await musicSession.CurrentChannel.SendMessageAsync("Hatsune Miku at your service!");
			await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed("Nothing playing yet"));
		}
	}

	/// <summary>
	///     Leaves a voice channel.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	[SlashCommand("leave", "Leaves the voice channel"), RequireUserAndBotVoicechatConnection]
	public async Task LeaveAsync(InteractionContext ctx)
	{
		ArgumentNullException.ThrowIfNull(ctx.GuildId);

		var guildId = ctx.GuildId.Value;
		var asyncLock = MikuBot.MusicSessionLocks.GetOrAdd(guildId, _ => new());
		using (await asyncLock.LockAsync(MikuBot.Cts.Token))
		{
			if (MikuBot.MusicSessions.TryRemove(ctx.GuildId.Value, out var musicSession))
			{
				if (musicSession.LavalinkGuildPlayer is not null)
					await musicSession.LavalinkGuildPlayer.DisconnectAsync();
				await musicSession.CurrentChannel.SendMessageAsync("Bye bye humans 💙");
				if (musicSession.StatusMessage is not null)
					await musicSession.StatusMessage.DeleteAsync("Miku disconnected");
			}

			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cya! 💙"));
		}

		MikuBot.MusicSessionLocks.TryRemove(guildId, out _);
	}
}
