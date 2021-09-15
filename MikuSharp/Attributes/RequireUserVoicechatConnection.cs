using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;

using System;
using System.Threading.Tasks;

namespace MikuSharp.Attributes
{
    /// <summary>
    /// Defines that usage of this command is restricted to the owner of the bot.
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
}
