using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;

using System;
using System.Threading.Tasks;

namespace MikuSharp.Attributes
{
    /// <summary>
    /// Defines that usage of this command is restricted to users in a vc.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RequireUserVoicechatConnection : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
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
    public sealed class RequireUserAndBotVoicechatConnection : CheckBaseAttribute
    {
        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
            if (ctx.Member.VoiceState?.Channel != null && bot.VoiceState?.Channel != null)
                return await Task.FromResult(true);

            return await Task.FromResult(false);
        }
    }
}
