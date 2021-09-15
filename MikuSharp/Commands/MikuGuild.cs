using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;

using MikuSharp.Attributes;

using System.Linq;
using System.Threading.Tasks;

namespace MikuSharp.Commands
{
    [RequireSpecialGuild(483279257431441410)]
    public class MikuGuild : BaseCommandModule
    {
        [Command("smolcar")]
        public async Task SmolcarRole(CommandContext ctx)
        {
            if (ctx.Member.Roles.Any(x => x.Id == 607989212696018945))
            {
                await ctx.Member.RevokeRoleAsync(ctx.Guild.GetRole(607989212696018945));
                await ctx.RespondAsync(":(");
                return;
            }
            await ctx.Member.GrantRoleAsync(ctx.Guild.GetRole(607989212696018945));
            await ctx.RespondAsync("Welcome to smolcar");
        }

    }
}
