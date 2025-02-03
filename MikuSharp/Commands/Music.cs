using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;

using MikuSharp.Attributes;
using MikuSharp.Utilities;

namespace MikuSharp.Commands;

/// <summary>
///     The music commands
/// </summary>
[SlashCommandGroup("music", "Music commands", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall]), DeferResponseAsync, EnsureLavalinkSession]
public class Music : ApplicationCommandsModule
{
	/// <summary>
	///     Joins a voice channel the user is in.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	[SlashCommand("join", "Joins the voice channel you're in"), RequireUserVoicechatConnection, AutomaticallyDisconnectExistingSession]
	public static async Task JoinAsync(InteractionContext ctx)
	{
		await ctx.Client.GetLavalink().DefaultSession().ConnectAsync(ctx.Member.VoiceState.Channel);
		MikuBot.MusicSessions.Add(ctx.GuildId.Value, new());
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Heya {ctx.Member.Mention}!"));
	}

	/// <summary>
	///     Leaves a voice channel.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	[SlashCommand("leave", "Leaves the channel"), RequireUserAndBotVoicechatConnection, AutomaticallyDisconnectExistingSession]
	public static async Task LeaveAsync(InteractionContext ctx)
	{
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cya! 💙"));
	}
}
