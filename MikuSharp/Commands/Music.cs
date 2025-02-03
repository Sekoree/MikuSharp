using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Exceptions;
using DisCatSharp.Lavalink;

using MikuSharp.Attributes;
using MikuSharp.Entities;
using MikuSharp.Utilities;

namespace MikuSharp.Commands;

/// <summary>
///     The music commands
/// </summary>
[SlashCommandGroup("music", "Music commands", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall]), DeferResponseAsync(true), EnsureLavalinkSession]
public class Music : ApplicationCommandsModule
{
	/// <summary>
	///     Joins a voice channel the user is in.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	[SlashCommand("join", "Joins the voice channel you're in"), RequireUserVoicechatConnection, AutomaticallyDisconnectExistingSession]
	public static async Task JoinAsync(InteractionContext ctx)
	{
		var session = ctx.Client.GetLavalink().DefaultSession();
		await ctx.Client.GetLavalink().DefaultSession().ConnectAsync(ctx.Member.VoiceState.Channel);
		MusicSession musicSession = new(ctx.Member.VoiceState.Channel, ctx.Guild, session);
		MikuBot.MusicSessions.Add(ctx.GuildId.Value, musicSession.InjectPlayer());
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Heya {ctx.Member.Mention}!"));
		await musicSession.CurrentChannel.SendMessageAsync("Hatsune Miku at your service!");
		var embed = new DiscordEmbedBuilder()
			.WithAuthor(ctx.Client.CurrentUser.UsernameWithGlobalName, null, ctx.Client.CurrentUser.AvatarUrl)
			.WithColor(DiscordColor.Black)
			.WithTitle("Miku Music Status")
			.WithDescription("Nothing playing yet")
			.AddField(new("State", "Stopped"))
			.AddField(new("Repeat Mode", musicSession.RepeatMode.ToString()))
			.Build();
		musicSession.UpdateStatusMessage(await musicSession.CurrentChannel.SendMessageAsync(embed));
	}

	/// <summary>
	///     Leaves a voice channel.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	[SlashCommand("leave", "Leaves the channel"), RequireUserAndBotVoicechatConnection]
	public static async Task LeaveAsync(InteractionContext ctx)
	{
		if (MikuBot.MusicSessions.Remove(ctx.GuildId.Value, out var musicSession))
		{
			await musicSession.StatusMessage.DeleteAsync("Miku disconnected");
			await musicSession.LavalinkGuildPlayer.DisconnectAsync();
			await musicSession.CurrentChannel.SendMessageAsync("Bye bye humans 💙");
		}

		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cya! 💙"));
	}
}
