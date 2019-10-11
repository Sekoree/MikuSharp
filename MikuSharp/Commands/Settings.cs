using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MikuSharp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikuSharp.Commands
{
    class Settings : BaseCommandModule
    {
        [Command("prefix")]
        [Aliases("prefixes")]
        public async Task ListPrefixes(CommandContext ctx)
        {
            var gld = await PrefixDB.GetGuildPrefixes(ctx.Guild.Id);
            var usr = await PrefixDB.GetAllUserPrefixes(ctx.User.Id);
        }

        [Command("addprefix")]
        [Aliases("newprefix","ap")]
        public async Task AddUserPrefix(CommandContext ctx, string newprefix, string global = null)
        {
            if (global != null && global?.StartsWith("g") != true) return;
            var prefixes = await PrefixDB.GetAllUserPrefixes(ctx.User.Id);
            ulong gld = ctx.Guild.Id;
            if (global != null)
            {
                gld = 0;
                if (prefixes[0].Any(x => x == newprefix))
                {
                    await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Add Prefix").WithDescription("**Error** You already have this as global prefix").Build());
                    return;
                }
            }
            else
            {
                if (prefixes[ctx.Guild.Id].Any(x => x == newprefix))
                {
                    await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Add Prefix").WithDescription($"**Error** You already have {newprefix} as prefix for this guild!").Build());
                    return;
                }
            }
            await PrefixDB.AddUserPrefix(ctx.User.Id, gld, newprefix);
            if (gld == 0)
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Add Prefix").WithDescription($"Added personal global prefix: {newprefix}").Build());
            else
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Add Prefix").WithDescription($"Added personal guild prefix: {newprefix}").Build());
        }

        [Command("addguildprefix")]
        [Aliases("agp")]
        [RequireUserPermissions(Permissions.ManageGuild | Permissions.Administrator)]
        public async Task AddGuildPrefix(CommandContext ctx, string newprefix)
        {
            var gp = await PrefixDB.GetGuildPrefixes(ctx.Guild.Id);
            if (gp.Contains(newprefix))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Add Guild Prefix").WithDescription("**Error** This guild already has this prefix").Build());
                return;
            }
            await PrefixDB.AddGuildPrefix(ctx.Guild.Id, newprefix);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Add Prefix").WithDescription($"Added guild prefix: {newprefix}").Build());
        }

        [Command("removeprefix")]
        [Aliases("rp")]
        public async Task RemoveUserPrefix(CommandContext ctx, string prefix, string global = null)
        {
            if (global != null && global?.StartsWith("g") != true) return;
            var p = await PrefixDB.GetAllUserPrefixes(ctx.User.Id);
            var gld = ctx.Guild.Id;
            if (global != null)
            {
                gld = 0;
                if (!p[0].Any(x => x == prefix))
                {
                    await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Remove Prefix").WithDescription("**Error** You already have this as global prefix").Build());
                    return;
                }
            }
            else
            {
                if (!p[ctx.Guild.Id].Any(x => x == prefix))
                {
                    await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Remove Prefix").WithDescription($"**Error** You already have {prefix} as prefix for this guild!").Build());
                    return;
                }
            }
            await PrefixDB.RemoveUserPrefix(ctx.User.Id, gld, prefix);
            if (gld == 0)
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Remove Prefix").WithDescription($"Removed personal global prefix: {prefix}").Build());
            else
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Remove Prefix").WithDescription($"Removed personal guild prefix: {prefix}").Build());
        }

        [Command("removeguildprefix")]
        [Aliases("rgp")]
        [RequireUserPermissions(Permissions.ManageGuild | Permissions.Administrator)]
        public async Task RemoveGlobalPrefix(CommandContext ctx, string prefix)
        {
            var gp = await PrefixDB.GetGuildPrefixes(ctx.Guild.Id);
            if (!gp.Contains(prefix))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Remove Guild Prefix").WithDescription($"**Error** This guild doesnt have: {prefix} as prefix").Build());
                return;
            }
            await PrefixDB.RemoveGuildPrefix(ctx.Guild.Id, prefix);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Remove Guild Prefix").WithDescription($"Removed guild prefix: {prefix}").Build());
        }
    }
}
