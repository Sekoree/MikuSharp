using System;
using System.Linq;
using System.Threading.Tasks;

using DisCatSharp.Entities;
using DisCatSharp.Exceptions;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Enums;

using MikuSharp.Enums;

namespace MikuSharp.Entities;

/// <summary>
///     Represents a music session.
/// </summary>
/// <param name="channel">The channel the music session is for.</param>
/// <param name="guild">The guild the music session is for.</param>
/// <param name="lavalinkSession">The Lavalink session.</param>
public sealed class MusicSession(DiscordChannel channel, DiscordGuild guild, LavalinkSession lavalinkSession)
{
	/// <summary>
	///     Gets the current channel.
	/// </summary>
	public DiscordChannel CurrentChannel { get; } = channel;

	/// <summary>
	///     Gets the current guild.
	/// </summary>
	public DiscordGuild CurrentGuild { get; } = guild;

	/// <summary>
	///     Gets the Lavalink session.
	/// </summary>
	public LavalinkSession LavalinkSession { get; } = lavalinkSession;

	/// <summary>
	///     Gets the Lavalink guild player.
	/// </summary>
	public LavalinkGuildPlayer? LavalinkGuildPlayer { get; internal set; }

	/// <summary>
	///     Gets the repeat mode.
	/// </summary>
	public RepeatMode RepeatMode { get; internal set; } = RepeatMode.None;

	/// <summary>
	///     Gets the status message.
	/// </summary>
	public DiscordMessage? StatusMessage { get; internal set; }

	/// <summary>
	///     Gets the play state.
	/// </summary>
	public PlaybackState PlaybackState { get; internal set; } = PlaybackState.Stopped;

	/// <summary>
	///     Injects the player.
	/// </summary>
	/// <returns>The current music session.</returns>
	public async Task<MusicSession> InjectPlayerAsync()
	{
		this.LavalinkGuildPlayer = this.LavalinkSession.GetGuildPlayer(this.CurrentGuild)!;
		this.LavalinkGuildPlayer.SetRepeatMode(this.RepeatMode);
		await this.LavalinkGuildPlayer.SetVolumeAsync(20);
		return this;
	}

	/// <summary>
	///     Updates the repeat mode.
	/// </summary>
	/// <param name="mode">The new repeat mode.</param>
	/// <returns>The current music session.</returns>
	public MusicSession UpdateRepeatMode(RepeatMode mode)
	{
		this.RepeatMode = mode;
		this.LavalinkGuildPlayer?.SetRepeatMode(mode);
		return this;
	}

	/// <summary>
	///     Updates the status message.
	/// </summary>
	/// <param name="message">The new status message.</param>
	/// <returns>The current music session.</returns>
	public MusicSession UpdateStatusMessage(DiscordMessage message)
	{
		this.StatusMessage = message;
		return this;
	}

	/// <summary>
	///     Updates the play state.
	/// </summary>
	/// <param name="state">The new play state.</param>
	/// <returns>The current music session.</returns>
	public MusicSession UpdatePlaybackState(PlaybackState state)
	{
		this.PlaybackState = state;
		return this;
	}

	/// <summary>
	///    Updates the status message.
	/// </summary>
	/// <param name="embed">The new status message embed.</param>
	/// <returns>The current music session.</returns>
	public async Task<MusicSession> UpdateStatusMessageAsync(DiscordEmbed embed)
	{
		try
		{
			if (this.StatusMessage is not null)
				await this.StatusMessage.DeleteAsync("Updating miku status");
			else
			{
				var messages = await this.CurrentChannel.GetMessagesAsync(50);
				var mikuMessages = messages.Where(msg => msg.Author.Id == MikuBot.ShardedClient.CurrentUser.Id).OrderByDescending(msg => msg.CreationTimestamp).ToList();
				var targetMessage = mikuMessages.FirstOrDefault(msg => msg.Embeds.Count is 1);
				if (targetMessage is not null)
					await targetMessage.DeleteAsync("Updating miku status");
			}
		}
		catch (NotFoundException)
		{ }

		return this.UpdateStatusMessage(await this.CurrentChannel.SendMessageAsync(embed));
	}
}
