using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;

using System.Linq;
using System.Threading.Tasks;

namespace MikuSharp.Commands
{
    public class Slash : ApplicationCommandsModule
    {
        [SlashCommand("smolcar", "#SmolArmy", true)]
        public static async Task SmolCarAsync(InteractionContext ctx)
        {

            if (ctx.Member.Roles.Any(x => x.Id == 607989212696018945))
            {
                await ctx.Member.RevokeRoleAsync(ctx.Guild.GetRole(607989212696018945));
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(":("));
                return;
            }
            await ctx.Member.GrantRoleAsync(ctx.Guild.GetRole(607989212696018945));
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Welcome to smolcar"));
        }
    }
}
