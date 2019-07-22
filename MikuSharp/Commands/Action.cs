using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using HeyRed.Mime;
using MikuSharp.Utilities;

namespace MikuSharp.Commands
{
    class Action : BaseCommandModule
    {
        [Command("hug")]
        [Description("Hug someone!")]
        public async Task Hug(CommandContext ctx, DiscordMember m)
        {
            var WSH = await Web.GetWeebSh(ctx, "hug", new[] { "" });
            WSH.Embed.WithDescription($"{ctx.Member.Mention} hugs {m.Mention} uwu");
            await ctx.RespondWithFileAsync(embed: WSH.Embed.Build(), fileData: WSH.ImgData, fileName: $"image.{WSH.Extension}");
        }

        [Command("kiss")]
        [Description("Kiss someone!")]
        public async Task Kiss(CommandContext ctx, DiscordMember m)
        {
            var WSH = await Web.GetWeebSh(ctx, "kiss", new[] { "" });
            WSH.Embed.WithDescription($"{ctx.Member.Mention} kisses {m.Mention} >~<");
            await ctx.RespondWithFileAsync(embed: WSH.Embed.Build(), fileData: WSH.ImgData, fileName: $"image.{WSH.Extension}");
        }

        [Command("lick")]
        [Description("Lick someone?")]
        public async Task Lick(CommandContext ctx, DiscordMember m)
        {
            var WSH = await Web.GetWeebSh(ctx, "lick", new[] { "" });
            WSH.Embed.WithDescription($"{ctx.Member.Mention} licks {m.Mention} owo");
            await ctx.RespondWithFileAsync(embed: WSH.Embed.Build(), fileData: WSH.ImgData, fileName: $"image.{WSH.Extension}");
        }

        [Command("pat")]
        [Description("Pat someone!")]
        public async Task Pat(CommandContext ctx, DiscordMember m)
        {
            var c = new HttpClient();
            var weeurl = await Bot._weeb.GetRandomAsync("pat", new[] { "" });
            Stream img = new MemoryStream(await c.GetByteArrayAsync(Other.resizeLink(weeurl.Url)));
            var em = new DiscordEmbedBuilder();
            em.WithDescription($"{ctx.Member.Mention} pats {m.Mention} #w#");
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
            em.WithFooter("by nekos.life");
            await ctx.RespondWithFileAsync(embed: em.Build(), fileData: img, fileName: $"image.{MimeGuesser.GuessExtension(img)}");
        }

        [Command("poke")]
        [Description("Poke someone!")]
        public async Task Poke(CommandContext ctx, DiscordMember m)
        {
            var c = new HttpClient();
            var weeurl = await Bot._weeb.GetRandomAsync("poke", new[] { "" });
            Stream img = new MemoryStream(await c.GetByteArrayAsync(Other.resizeLink(weeurl.Url)));
            var em = new DiscordEmbedBuilder();
            em.WithDescription($"{ctx.Member.Mention} pokes {m.Mention} ÓwÒ");
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
            em.WithFooter("by nekos.life");
            await ctx.RespondWithFileAsync(embed: em.Build(), fileData: img, fileName: $"image.{MimeGuesser.GuessExtension(img)}");
        }

        [Command("slap")]
        [Description("Slap someone!")]
        public async Task Slap(CommandContext ctx, DiscordMember m)
        {
            var c = new HttpClient();
            var weeurl = await Bot._weeb.GetRandomAsync("slap", new[] { "" });
            Stream img = new MemoryStream(await c.GetByteArrayAsync(Other.resizeLink(weeurl.Url)));
            var em = new DiscordEmbedBuilder();
            em.WithDescription($"{ctx.Member.Mention} slaps {m.Mention} ÒwÓ");
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
            em.WithFooter("by nekos.life");
            await ctx.RespondWithFileAsync(embed: em.Build(), fileData: img, fileName: $"image.{MimeGuesser.GuessExtension(img)}");
        }
    }
}
