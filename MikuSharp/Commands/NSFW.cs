using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MikuSharp.Utilities;
using Booru.Net;
using System.Linq;
using System.Net.Http;
using System.IO;
using DSharpPlus.Entities;
using HeyRed.Mime;

namespace MikuSharp.Commands
{
    [RequireNsfw]
    class NSFW : BaseCommandModule
    {
        [Command("4k")]
        [Description("lewd")]
        public async Task FourK(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/image?type=4k");
            await ctx.RespondWithFileAsync($"image.{d.Filetype}", d.Data, embed: d.Embed);
        }

        [Command("anal")]
        [Description("lewd")]
        public async Task Anal(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/image?type=anal");
            await ctx.RespondWithFileAsync($"image.{d.Filetype}", d.Data, embed: d.Embed);
        }

        [Command("ass")]
        [Description("lewd")]
        public async Task Ass(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/image?type=ass");
            await ctx.RespondWithFileAsync($"image.{d.Filetype}", d.Data, embed: d.Embed);
        }

        [Command("booru")]
        [Description("lewd")]
        public async Task Booru(CommandContext ctx, params string[] tag)
        {
            if (tag.Any(x => x.ToLower().Contains("loli")) || tag.Any(x => x.ToLower().Contains("shota")))
            {
                await ctx.RespondAsync("no.");
                return;
            }
            var dan = new BooruClient();
            var res = await dan.GetDanbooruImagesAsync(tag);
            if (res.Count == 0)
            {
                await ctx.RespondAsync("No Results!");
            }
            var hc = new HttpClient();
            MemoryStream str = new MemoryStream(await hc.GetByteArrayAsync(Other.resizeLink(res[new Random().Next(0, res.Count)].ImageUrl)));
            str.Position = 0;
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(str)}");
            em.WithFooter("by Danbooru");
            await ctx.RespondWithFileAsync($"image.{MimeGuesser.GuessExtension(str)}", str, embed: em.Build());
        }

        [Command("gonewild")]
        [Description("lewd")]
        public async Task Gonewild(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/image?type=gonewild");
            await ctx.RespondWithFileAsync($"image.{d.Filetype}", d.Data, embed: d.Embed);
        }

        [Command("hentai")]
        [Description("lewd")]
        public async Task Hentai(CommandContext ctx)
        {
            var d = await Web.GetKsoftSiRanImg("hentai_gif", true);
            await ctx.RespondWithFileAsync($"image.{d.Filetype}", d.Data, embed: d.Embed);
        }

        [Command("konachan")]
        [Description("lewd")]
        public async Task Konachan(CommandContext ctx, params string[] tag)
        {
            if (tag.Any(x => x.ToLower().Contains("loli")) || tag.Any(x => x.ToLower().Contains("shota")))
            {
                await ctx.RespondAsync("no.");
                return;
            }
            var dan = new BooruClient();
            var res = await dan.GetKonaChanImagesAsync(tag);
            if (res.Count == 0)
            {
                await ctx.RespondAsync("No Results!");
            }
            var hc = new HttpClient();
            MemoryStream str = new MemoryStream(await hc.GetByteArrayAsync(Other.resizeLink(res[new Random().Next(0, res.Count)].ImageUrl)));
            str.Position = 0;
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(str)}");
            em.WithFooter("by Konachan");
            await ctx.RespondWithFileAsync($"image.{MimeGuesser.GuessExtension(str)}", str, embed: em.Build());
        }

        [Command("lewdkitsune")]
        [Description("lewd")]
        public async Task LewdKitsune(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/image?type=lewdkitsune");
            await ctx.RespondWithFileAsync($"image.{d.Filetype}", d.Data, embed: d.Embed);
        }

        [Command("lewdneko")]
        [Description("lewd")]
        public async Task LewdNeko(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/image?type=lewdneko");
            await ctx.RespondWithFileAsync($"image.{d.Filetype}", d.Data, embed: d.Embed);
        }

        [Command("nekopara")]
        [Description("lewd")]
        public async Task Nekopara(CommandContext ctx)
        {
            var d = await Web.GetDerpy("https://derpyapi.glitch.me/nekoparastatic");
            await ctx.RespondWithFileAsync($"image.{d.Filetype}", d.Data, embed: d.Embed);
        }

        [Command("nekoparagif")]
        [Description("lewd")]
        public async Task NekoparaGif(CommandContext ctx)
        {
            var d = await Web.GetDerpy("https://derpyapi.glitch.me/nekoparagif");
            await ctx.RespondWithFileAsync($"image.{d.Filetype}", d.Data, embed: d.Embed);
        }

        [Command("porngif")]
        [Description("lewd")]
        public async Task PornGif(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/image?type=pgif");
            await ctx.RespondWithFileAsync($"image.{d.Filetype}", d.Data, embed: d.Embed);
        }

        [Command("pussy")]
        [Description("lewd")]
        public async Task Pussy(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/image?type=pussy");
            await ctx.RespondWithFileAsync($"image.{d.Filetype}", d.Data, embed: d.Embed);
        }

        [Command("rule34")]
        [Description("lewd")]
        public async Task Rule34(CommandContext ctx, params string[] tag)
        {
            if (tag.Any(x => x.ToLower().Contains("loli")) || tag.Any(x => x.ToLower().Contains("shota")))
            {
                await ctx.RespondAsync("no.");
                return;
            }
            var dan = new BooruClient();
            var res = await dan.GetRule34ImagesAsync(tag);
            if (res.Count == 0)
            {
                await ctx.RespondAsync("No Results!");
            }
            var hc = new HttpClient();
            MemoryStream str = new MemoryStream(await hc.GetByteArrayAsync(Other.resizeLink(res[new Random().Next(0,res.Count)].ImageUrl)));
            str.Position = 0;
            var em = new DiscordEmbedBuilder();
            Console.WriteLine(MimeGuesser.GuessExtension(str));
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(str)}");
            em.WithFooter("by Rule34");
            await ctx.RespondWithFileAsync($"image.{MimeGuesser.GuessExtension(str)}", str, embed: em.Build());
        }

        [Command("tighs"), Aliases("thigh")]
        [Description("lewd")]
        public async Task Thighs(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/v2/image/thighs");
            await ctx.RespondWithFileAsync($"image.{d.Filetype}", d.Data, embed: d.Embed);
        }
    }
}
