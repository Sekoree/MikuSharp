using System;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;

namespace MikuSharp.Attributes;

/// <summary>
///     Defines that usage of this command is restricted to users in a vc.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class RequireUserVoicechatConnection : ApplicationCommandCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(BaseContext ctx)
        => Task.FromResult(ctx.Member.VoiceState?.Channel != null);
}

/// <summary>
///     Defines that usage of this command is restricted to users & the bot in a vc.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class RequireUserAndBotVoicechatConnection : ApplicationCommandCheckBaseAttribute
{
    public override async Task<bool> ExecuteChecksAsync(BaseContext ctx)
    {
        var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
        return ctx.Member.VoiceState?.Channel is not null && bot.VoiceState?.Channel is not null
            ? await Task.FromResult(true)
            : await Task.FromResult(false);
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Class)]
public sealed class NotStaffAttribute : CheckBaseAttribute
{
    public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        => Task.FromResult(!ctx.User.IsStaff);
}
