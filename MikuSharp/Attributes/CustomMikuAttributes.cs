using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;

using MikuSharp.Enums;
using MikuSharp.Utilities;

namespace MikuSharp.Attributes;

/// <summary>
///     Defines that usage of this command is restricted to users in a vc.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class RequireUserVoicechatConnection : ApplicationCommandCheckBaseAttribute
{
	/// <inheritdoc />
	public override async Task<bool> ExecuteChecksAsync(BaseContext ctx)
	{
		var connected = ctx.Member.VoiceState?.Channel is not null;

		if (connected)
			return true;

		await ctx.EditResponseAsync("You must be in a voice channel to use this command.");
		return false;
	}
}

/// <summary>
///     Defines that usage of this command is restricted to users in a vc and the bot is in a vc.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class RequireUserAndBotVoicechatConnection : ApplicationCommandCheckBaseAttribute
{
	/// <inheritdoc />
	public override async Task<bool> ExecuteChecksAsync(BaseContext ctx)
	{
		var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
		var connected = ctx.Member.VoiceState?.Channel is not null && bot.VoiceState?.Channel is not null;
		if (connected)
			return true;

		await ctx.EditResponseAsync("You and the bot must be in a voice channel to use this command.");
		return false;
	}
}

/// <summary>
///     Defines that usage of this command is forbidden for discord staff.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Class, Inherited = false)]
public sealed class NotDiscordStaffAttribute : CheckBaseAttribute
{
	/// <inheritdoc />
	public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
		=> Task.FromResult(!ctx.User.IsStaff);
}

/// <summary>
///     Defines that the method or class will defer the response.
/// </summary>
/// <param name="ephemeral">Whether the response should be epehemeral.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class DeferResponseAsyncAttribute(bool ephemeral = false) : ApplicationCommandCheckBaseAttribute
{
	/// <summary>
	///     Gets a value indicating whether the response should be ephemeral.
	/// </summary>
	public bool Ephemeral { get; } = ephemeral;

	/// <inheritdoc />
	public override async Task<bool> ExecuteChecksAsync(BaseContext ctx)
	{
		if (this.Ephemeral)
			await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		else
			await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

		return true;
	}
}

/// <summary>
///     Defines that the method or class needs to ensure that a lavalink session is available.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class EnsureLavalinkSession : ApplicationCommandCheckBaseAttribute
{
	/// <inheritdoc />
	public override async Task<bool> ExecuteChecksAsync(BaseContext ctx)
	{
		var module = ctx.Client.GetLavalink();

		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (module is null)
			return await RespondWithNoSessionAvailableAsync(ctx);

		var session = module.DefaultSession();
		return session is null || !session.IsConnected ? await RespondWithNoSessionAvailableAsync(ctx) : true;
	}

	/// <summary>
	///     Responds with a message indicating that no session is available.
	/// </summary>
	/// <param name="ctx">The context.</param>
	/// <returns><see langword="false" />.</returns>
	public static async Task<bool> RespondWithNoSessionAvailableAsync(BaseContext ctx)
	{
		await ctx.EditResponseAsync("No session found that can handle music at the moment.");
		return false;
	}
}

/// <summary>
///     Defines that the method or class will automatically disconnect an existing session.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class AutomaticallyDisconnectExistingSessionAttribute : ApplicationCommandCheckBaseAttribute
{
	/// <inheritdoc />
	public override async Task<bool> ExecuteChecksAsync(BaseContext ctx)
	{
		if (!MikuBot.MusicSessions.ContainsKey(ctx.GuildId!.Value))
			return true;

		var player = ctx.Client.GetLavalink().GetGuildPlayer(ctx.Guild);
		if (player is not null)
			await player.DisconnectAsync();
		MikuBot.MusicSessions.Remove(ctx.GuildId.Value);

		return true;
	}
}

/// <summary>
///    Defines that the method or class requires specific playback state(s).
/// </summary>
/// <param name="targetStates">The target playback states.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class RequirePlaybackState(params PlaybackState[] targetStates) : ApplicationCommandCheckBaseAttribute
{
	/// <summary>
	///     Gets the target playback state.
	/// </summary>
	public List<PlaybackState> TargetStates { get; } = [..targetStates];

	/// <inheritdoc />
	public override Task<bool> ExecuteChecksAsync(BaseContext ctx)
	{
		var musicSession = MikuBot.MusicSessions[ctx.GuildId!.Value];
		return Task.FromResult(this.TargetStates.Contains(musicSession.PlaybackState));
	}
}
