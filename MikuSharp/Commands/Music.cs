using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using MikuSharp.Attributes;

using System.Threading.Tasks;

namespace MikuSharp.Commands;

/// <summary>
///     The music commands
/// </summary>
[SlashCommandGroup("music", "Music commands", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall]), DeferResponseAsync, EnsureLavalinkSession]
public class Music : ApplicationCommandsModule
{
	[SlashCommandGroup("base", "Base commands")]
	public class Base : ApplicationCommandsModule
	{
		[SlashCommand("join", "Joins the voice channel you're in"), RequireUserVoicechatConnection]
		public static async Task JoinAsync(InteractionContext ctx)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Heya {ctx.Member.Mention}!"));
		}

		[SlashCommand("leave", "Leaves the channel"), RequireUserAndBotVoicechatConnection]
		public static async Task LeaveAsync(InteractionContext ctx, [Option("keep", "Whether to keep the queue")] bool keep = false)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cya! 💙"));
		}
	}
}
