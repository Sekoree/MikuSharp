using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MikuSharp.Events.MusicCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikuSharp.MusicCommands
{
    [Group("music"),Aliases("m")]
    class Base : BaseCommandModule
    {
        [GroupCommand]
        public async Task MusicGroup(CommandContext ctx)
        {
            var emb = new DiscordEmbedBuilder();
            emb.WithTitle("MeekPlush Help");
            string prefix = "m!";
            if (Bot.Guilds[ctx.Guild.Id].Prefix != "m!" && Bot.Members[ctx.Member.Id].Prefix != null) prefix = Bot.Members[ctx.Member.Id].Prefix;
            else if (Bot.Guilds[ctx.Guild.Id].Prefix != "m!" && Bot.Members[ctx.Member.Id].Prefix == null) prefix = Bot.Guilds[ctx.Guild.Id].Prefix;
            else if (Bot.Guilds[ctx.Guild.Id].Prefix == "m!" && Bot.Members[ctx.Member.Id].Prefix != null) prefix = Bot.Members[ctx.Member.Id].Prefix;
            emb.WithThumbnailUrl(ctx.Client.CurrentUser.AvatarUrl);
            emb.WithDescription($"**__Ganeral Music Commands__**\n\n" +
                $"**{prefix}join** || joins the Voice Channel you're in\n" +
                $"**{prefix}leave (keep)** || leaves the Channel and optionally keeps the Queue\n" +
                $"**{prefix}p (<SearchtermOrURL>)** || Play or Queue a song! of just use '{prefix}p' to start a preloaded playlist\n" +
                $"**{prefix}skip** || skip the current song\n" +
                $"**{prefix}stop** || stop playback\n" +
                $"**{prefix}vol (0-150)** || change global volume\n" +
                $"**{prefix}pause** || pause playback\n" +
                $"**{prefix}resume** || resume playback\n" +
                $"**{prefix}q** || shows the queue\n" +
                $"**{prefix}qc** || clears the queue\n" +
                $"**{prefix}qr <number>** || removes a song from the queue\n" +
                $"**{prefix}r** || repeat the current song\n" +
                $"**{prefix}ra** || repeat the entire queue\n" +
                $"**{prefix}s** || play the queue in shuffle mode\n" +
                $"**{prefix}np** || shows you what song is playing in more detail");
            emb.AddField("GuildInfo", $"Your prefix is: {Bot.Members[ctx.Member.Id].Prefix}\n" +
    $"This Guilds Prefix is: {Bot.Guilds[ctx.Guild.Id].Prefix}");
            emb.AddField("Info", $"Avatar by Chillow#1945 :heart: [Twitter](https://twitter.com/SaikoSamurai)\n" +
                $"Bot Github: soon™\n" +
                $"DBL: [Link](https://discordbots.org/bot/465675368775417856) every upvote helps and is appreciated uwu\n" +
                $"Support Server: [Invite](https://discord.gg/YPPA2Pu)\n" +
                $"Support me on [Paypal](https://www.paypal.me/speyd3r) or [Patreon](https://www.patreon.com/speyd3r) uwu");
            emb.WithColor(new DiscordColor("6785A9"));
            await ctx.RespondAsync(embed: emb.Build());
        }
    }
}
