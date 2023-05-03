namespace MikuSharp.Utilities;

public static class Other
{
	public static string resizeLink(string url)
	{
		return $"https://api.meek.moe/im/?image={url}&resize=500";
	}

	public static async Task DeferAsync(this InteractionContext ctx, bool ephemeral = true)
	{
		var builder = new DiscordInteractionResponseBuilder();
		if (ephemeral)
		{
			builder.AsEphemeral();
		}
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, builder);
	}
}
