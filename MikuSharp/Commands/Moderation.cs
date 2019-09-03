using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;

namespace MikuSharp.Commands
{
    class Moderation : BaseCommandModule
    {
        [Command("ban")]
        [RequirePermissions(DSharpPlus.Permissions.Administrator | DSharpPlus.Permissions.BanMembers)]
        [Description("Ban someone")]
        public async Task Ban(CommandContext ctx, DiscordMember m)
        {
            await ctx.RespondAsync("Banned: " + m.Mention);
            await ctx.Guild.BanMemberAsync(m);
        }

        [Command("kick")]
        [RequirePermissions(DSharpPlus.Permissions.Administrator | DSharpPlus.Permissions.KickMembers)]
        [Description("Kick someone")]
        public async Task Kick(CommandContext ctx, DiscordMember m)
        {
            await ctx.RespondAsync("Kicked: " + m.Mention);
            await m.RemoveAsync();
        }

        [Command("purge")]
        [RequirePermissions(DSharpPlus.Permissions.Administrator 
            | DSharpPlus.Permissions.ManageMessages 
            | DSharpPlus.Permissions.ManageChannels
            | DSharpPlus.Permissions.ManageGuild)]
        [Description("Delete a large amount of messages fast")]
        public async Task Purge(CommandContext ctx, int amount)
        {
            if (amount > 100) await ctx.RespondAsync("Can only delete 100 Messages at a time for now");
            var msgs = await ctx.Channel.GetMessagesAsync(amount);
            await ctx.Channel.DeleteMessagesAsync(msgs);
        }

        [Command("unban")]
        [Priority(2)]
        [RequirePermissions(DSharpPlus.Permissions.Administrator
            | DSharpPlus.Permissions.BanMembers
            | DSharpPlus.Permissions.ManageGuild)]
        [Description("Unban someone by their ID or username")]
        public async Task UnBan(CommandContext ctx, ulong id)
        {
            var m = await ctx.Guild.GetBansAsync();
            await ctx.Guild.UnbanMemberAsync(m.First(x => x.User.Id == id).User);
        }

        [Command("unban")]
        [Priority(1)]
        [RequirePermissions(DSharpPlus.Permissions.Administrator
            | DSharpPlus.Permissions.BanMembers
            | DSharpPlus.Permissions.ManageGuild)]
        public async Task UnBan(CommandContext ctx, string name)
        {
            var m = await ctx.Guild.GetBansAsync();
            await ctx.Guild.UnbanMemberAsync(m.First(x => x.User.Username.StartsWith(name) | $"{x.User.Username}#{x.User.Discriminator}".StartsWith(name)).User);
        }
    }
}
