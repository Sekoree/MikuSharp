using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;

using HeyRed.Mime;

using MikuSharp.Utilities;

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

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

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{WSH.Extension}", WSH.ImgData);
            builder.WithEmbed(WSH.Embed.Build());
            await ctx.RespondAsync(builder);
        }

        [Command("kiss")]
        [Description("Kiss someone!")]
        public async Task Kiss(CommandContext ctx, DiscordMember m)
        {
            var WSH = await Web.GetWeebSh(ctx, "kiss", new[] { "" });
            WSH.Embed.WithDescription($"{ctx.Member.Mention} kisses {m.Mention} >~<");

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{WSH.Extension}", WSH.ImgData);
            builder.WithEmbed(WSH.Embed.Build());
            await ctx.RespondAsync(builder);
        }

        [Command("lick")]
        [Description("Lick someone?")]
        public async Task Lick(CommandContext ctx, DiscordMember m)
        {
            var WSH = await Web.GetWeebSh(ctx, "lick", new[] { "" });
            WSH.Embed.WithDescription($"{ctx.Member.Mention} licks {m.Mention} owo");

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{WSH.Extension}", WSH.ImgData);
            builder.WithEmbed(WSH.Embed.Build());
            await ctx.RespondAsync(builder);
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

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{MimeGuesser.GuessExtension(img)}", img);
            builder.WithEmbed(em.Build());
            await ctx.RespondAsync(builder);
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

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{MimeGuesser.GuessExtension(img)}", img);
            builder.WithEmbed(em.Build());
            await ctx.RespondAsync(builder);
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

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{MimeGuesser.GuessExtension(img)}", img);
            builder.WithEmbed(em.Build());
            await ctx.RespondAsync(builder);
        }
    }
}
