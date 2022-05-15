using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;

using System.Threading.Tasks;

namespace MikuSharp.Commands
{
	internal class Test : ApplicationCommandsModule
	{
		[SlashCommand("shard_test", "Shard Testing")]
		public static async Task TestAsync(InteractionContext ctx)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent($"Guild Meep meep. Shard {ctx.Client.ShardId}"));
		}
	}
}