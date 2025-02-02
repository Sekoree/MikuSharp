using System.Linq;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using MikuSharp.Attributes;

using System.Threading.Tasks;

using DisCatSharp.Lavalink;

namespace MikuSharp.Commands;

/// <summary>
///     The music commands
/// </summary>
[SlashCommandGroup("music", "Music commands", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall]), DeferResponseAsync, EnsureLavalinkSession]
public class Music : ApplicationCommandsModule
{
	[SlashCommandGroup("base", "Base commands (Join & Leave)")]
	public class Base : ApplicationCommandsModule
	{
		[SlashCommand("join", "Joins the voice channel you're in"), RequireUserVoicechatConnection]
		public static async Task JoinAsync(InteractionContext ctx)
		{
			await ctx.Client.GetLavalink().ConnectedSessions.First().Value.ConnectAsync(ctx.Member.VoiceState.Channel);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Heya {ctx.Member.Mention}!"));
		}

		[SlashCommand("leave", "Leaves the channel"), RequireUserAndBotVoicechatConnection]
		public static async Task LeaveAsync(InteractionContext ctx, [Option("keep", "Whether to keep the queue")] bool keep = false)
		{
			await ctx.Client.GetLavalink().GetGuildPlayer(ctx.Guild).DisconnectAsync();
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cya! 💙"));
		}
	}
}
