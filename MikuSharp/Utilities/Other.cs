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

	/// <summary>
	///    Builds a music status embed.
	/// </summary>
	/// <param name="session">The music session.</param>
	/// <param name="description">The description.</param>
	/// <param name="additionalEmbedFields">The additional embed fields.</param>
	/// <returns>The built embed.</returns>
	public static DiscordEmbed BuildMusicStatusEmbed(this MusicSession session, string description, List<DiscordEmbedField>? additionalEmbedFields = null)
	{
		var builder = new DiscordEmbedBuilder()
			.WithAuthor(MikuBot.ShardedClient.CurrentUser.UsernameWithGlobalName, iconUrl: MikuBot.ShardedClient.CurrentUser.AvatarUrl)
			.WithColor(DiscordColor.Black)
			.WithTitle("Miku Music Status")
			.WithDescription(description);

		builder.AddField(new("State", session.PlaybackState.ToString()));
		builder.AddField(new("Repeat Mode", session.RepeatMode.ToString()));

		if (additionalEmbedFields is null)
			return builder.Build();

		ArgumentOutOfRangeException.ThrowIfGreaterThan(additionalEmbedFields.Count, 23, nameof(additionalEmbedFields));
		builder.AddFields(additionalEmbedFields);

		return builder.Build();
	}

	/// <summary>
	///   Builds a music status embed.
	/// </summary>
	/// <param name="session">The music session.</param>
	/// <param name="additionalEmbedFields">The additional embed fields.</param>
	/// <returns>The built embed.</returns>
	public static DiscordEmbed BuildMusicStatusEmbed(this MusicSession session, List<DiscordEmbedField>? additionalEmbedFields = null)
		=> BuildMusicStatusEmbed(session, session.StatusMessage.Embeds.First().Description, additionalEmbedFields);

	/// <summary>
	/// Formats a <see cref="TimeSpan" /> into a human-readable string.
	/// </summary>
	/// <param name="timeSpan">The time span to format.</param>
	/// <returns>The formatted time span.</returns>
	public static string FormatTimeSpan(this TimeSpan timeSpan)
		=> timeSpan.TotalHours >= 1
			? $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
			: timeSpan.TotalMinutes >= 1
				? $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
				: $"{timeSpan.Seconds:D2} sec";
}
