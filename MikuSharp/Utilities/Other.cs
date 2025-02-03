using System.Linq;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;

namespace MikuSharp.Utilities;

public static class Other
{
	public static string ResizeLink(string url)
		=> $"https://api.meek.moe/im/?image={url}&resize=500";

	public static async Task DeferAsync(this InteractionContext ctx, bool ephemeral = true)
	{
		var builder = new DiscordInteractionResponseBuilder();
		if (ephemeral)
			builder.AsEphemeral();
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, builder);
	}

	/// <summary>
	///     Gets the default session.
	/// </summary>
	/// <param name="lavalink">The lavalink extension.</param>
	/// <returns>The first session or <see langword="null" />.</returns>
	public static LavalinkSession? DefaultSession(this LavalinkExtension lavalink)
		=> lavalink.ConnectedSessions.Any()
			? lavalink.ConnectedSessions.First().Value
			: null;
}
