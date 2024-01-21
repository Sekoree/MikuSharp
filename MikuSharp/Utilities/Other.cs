using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using System.Threading.Tasks;

namespace MikuSharp.Utilities;

public static class Other
{
	public static string resizeLink(string url) =>
		$"https://api.meek.moe/im/?image={url}&resize=500";

	public async static Task DeferAsync(this InteractionContext ctx, bool ephemeral = true)
	{
		var builder = new DiscordInteractionResponseBuilder();
		if (ephemeral)
			builder.AsEphemeral();
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, builder);
	}
}