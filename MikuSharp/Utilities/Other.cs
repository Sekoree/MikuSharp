using DisCatSharp.Lavalink.Entities;

using MikuSharp.Entities;
using MikuSharp.Enums;

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
	///     Builds a music status embed.
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
	///     Builds a music status embed.
	/// </summary>
	/// <param name="session">The music session.</param>
	/// <param name="additionalEmbedFields">The additional embed fields.</param>
	/// <returns>The built embed.</returns>
	public static DiscordEmbed BuildMusicStatusEmbed(this MusicSession session, List<DiscordEmbedField>? additionalEmbedFields = null)
		=> session.StatusMessage is not null ? BuildMusicStatusEmbed(session, session.StatusMessage.Embeds.First().Description, additionalEmbedFields) : throw new NullReferenceException();

	/// <summary>
	///     Formats a <see cref="TimeSpan" /> into a human-readable string.
	/// </summary>
	/// <param name="timeSpan">The time span to format.</param>
	/// <returns>The formatted time span.</returns>
	public static string FormatTimeSpan(this TimeSpan timeSpan)
		=> timeSpan.TotalHours >= 1
			? $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
			: timeSpan.TotalMinutes >= 1
				? $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
				: $"{timeSpan.Seconds:D2} sec";

	/// <summary>
	///     Loads and plays an <paramref name="identifier" />.
	/// </summary>
	/// <param name="musicSession">The music session.</param>
	/// <param name="ctx">The interaction context.</param>
	/// <param name="identifier">The identifier to load.</param>
	/// <param name="searchType">The optional search type. Defaults to <see cref="LavalinkSearchType.Plain" />.</param>
	/// <returns>Whether the track was successfully loaded and added to the queue.</returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static async Task<MusicSession> LoadAndPlayTrackAsync(this MusicSession musicSession, InteractionContext ctx, string identifier, LavalinkSearchType searchType = LavalinkSearchType.Plain)
	{
		var loadResult = await musicSession.LavalinkGuildPlayer.LoadTracksAsync(searchType, identifier);
		switch (loadResult.LoadType)
		{
			case LavalinkLoadResultType.Track:
				var track = loadResult.GetResultAs<LavalinkTrack>();
				musicSession.LavalinkGuildPlayer.AddToQueue(track);
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Added {track.Info.Title.Bold()} to the queue!"));
				break;
			case LavalinkLoadResultType.Playlist:
				var playlist = loadResult.GetResultAs<LavalinkPlaylist>();
				musicSession.LavalinkGuildPlayer.AddToQueue(playlist);
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Added playlist {playlist.Info.Name.Bold()} to the queue."));
				break;
			case LavalinkLoadResultType.Search:
				var tracks = loadResult.GetResultAs<List<LavalinkTrack>>();
				musicSession.LavalinkGuildPlayer.AddToQueue(tracks.First());
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Added {tracks.First().Info.Title.Bold()} to the queue!"));
				break;
			case LavalinkLoadResultType.Empty:
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"No results found for `{identifier.InlineCode()}`"));
				throw new("No results found");
			case LavalinkLoadResultType.Error:
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Something went wrong..\nReason: {loadResult.GetResultAs<LavalinkException>().Message ?? "unknown"}"));
				throw new("Lavalink error");
			default:
				throw new ArgumentOutOfRangeException();
		}

		switch (musicSession.PlaybackState)
		{
			case PlaybackState.Stopped:
				musicSession.LavalinkGuildPlayer.PlayQueue();
				break;
			case PlaybackState.Paused:
				await musicSession.LavalinkGuildPlayer.ResumeAsync();
				musicSession.UpdatePlaybackState(PlaybackState.Playing);
				await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed());
				break;
			case PlaybackState.Playing:
			default:
				break;
		}

		return musicSession;
	}
}
