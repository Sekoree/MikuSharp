using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using System.Threading.Tasks;

namespace MikuSharp.Utilities;

public static class Other
{
	public static string resizeLink(string url)
	{
		return $"https://api.meek.moe/im/?image={url}&resize=500";
	}

	public static async Task DeferAsync(this InteractionContext ctx, bool ephemeral = true)
		=> await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ephemeral));
}
