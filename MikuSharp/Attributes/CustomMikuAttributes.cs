using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;

using System;
using System.Threading.Tasks;

namespace MikuSharp.Attributes;

/// <summary>
/// Defines that usage of this command is restricted to users in a vc.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RequireUserVoicechatConnection : SlashCheckBaseAttribute
{
	public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
	{
		if (ctx.Member.VoiceState?.Channel != null)
			return Task.FromResult(true);

		return Task.FromResult(false);
	}
}

/// <summary>
/// Defines that usage of this command is restricted to users & the bot in a vc.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RequireUserAndBotVoicechatConnection : SlashCheckBaseAttribute
{
	public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
	{
		var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
		if (ctx.Member.VoiceState?.Channel != null && bot.VoiceState?.Channel != null)
			return await Task.FromResult(true);

		return await Task.FromResult(false);
	}
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Class, AllowMultiple = false)]
public sealed class NotStaffAttribute : CheckBaseAttribute
{
	public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
	{
		return Task.FromResult(!ctx.User.IsStaff);
	}
}