using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;

using MikuSharp.Entities;

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
	public static LavalinkSession DefaultSession(this LavalinkExtension lavalink)
		=> lavalink.ConnectedSessions.First().Value;

	public static DiscordEmbed BuildMusicStatusEmbed(this InteractionContext ctx, MusicSession session, string description, List<DiscordEmbedField>? additionalEmbedFields = null)
	{
		var builder = new DiscordEmbedBuilder()
			.WithAuthor(ctx.Client.CurrentUser.UsernameWithGlobalName, iconUrl: ctx.Client.CurrentUser.AvatarUrl)
			.WithColor(DiscordColor.Black)
			.WithTitle("Miku Music Status")
			.WithDescription(description);

		builder.AddField(new("State", session.PlayState.ToString()));
		builder.AddField(new("Repeat Mode", session.RepeatMode.ToString()));

		if (additionalEmbedFields is null)
			return builder.Build();

		ArgumentOutOfRangeException.ThrowIfGreaterThan(additionalEmbedFields.Count, 23, nameof(additionalEmbedFields));
		builder.AddFields(additionalEmbedFields);

		return builder.Build();
	}
}
