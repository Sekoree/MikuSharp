using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Newtonsoft.Json;
using HeyRed.Mime;
using MikuSharp.Utilities;

namespace MikuSharp.Commands
{
    class Weeb : BaseCommandModule
    {
        [Command("awooify")]
        [Priority(2)]
        [Description("Awooify your or someones avatar!")]
        public async Task Awooify(CommandContext ctx, DiscordMember member = null)
        {
            string avartURL = ctx.Member.AvatarUrl;
            if (member != null)
            {
                avartURL = member.AvatarUrl;
            }
            var e = JsonConvert.DeserializeObject<NekoBot>(await new WebClient().DownloadStringTaskAsync($"https://nekobot.xyz/api/imagegen?type=clyde&text={avartURL}"));
            var embed2 = new DiscordEmbedBuilder();
            embed2.WithImageUrl(e.message);
            await ctx.RespondAsync(embed: embed2.Build());
        }

        [Command("awooify")]
        [Priority(1)]
        public async Task Awooify(CommandContext ctx, string member)
        {
            var AvatarUser = ctx.Guild.Members.Where(x => x.Value.Username.ToLower().Contains(member) | x.Value.DisplayName.ToLower().Contains(member));
            var e = JsonConvert.DeserializeObject<NekoBot>(await new WebClient().DownloadStringTaskAsync($"https://nekobot.xyz/api/imagegen?type=clyde&text={AvatarUser.First().Value.AvatarUrl}"));
            var embed2 = new DiscordEmbedBuilder();
            embed2.WithImageUrl(e.message);
            await ctx.RespondAsync(embed: embed2.Build());
        }

        [Command("ddlc")]
        [Description("Radon DDLC image")]
        public async Task DDLC(CommandContext ctx)
        {
            var e = JsonConvert.DeserializeObject<Derpy>(await new WebClient().DownloadStringTaskAsync($"https://derpyapi.glitch.me/ddlc"));
            Stream img = new MemoryStream(await new WebClient().DownloadDataTaskAsync(Other.resizeLink(e.url)));
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
            em.WithFooter("by Derpy API");
            em.WithDescription($"{e.url}");
            await ctx.RespondWithFileAsync(fileData: img, fileName: $"image.{MimeGuesser.GuessExtension(img)}", embed: em.Build());
        }

        [Command("diva")]
        [Description("Radnom PJD Loading image")]
        public async Task DivaPic(CommandContext ctx)
        {
            var myresponse = JsonConvert.DeserializeObject<ImgRet>(await new WebClient().DownloadStringTaskAsync($"https://api.meek.moe/diva"));
            Stream dataStream2 = new MemoryStream(await new WebClient().DownloadDataTaskAsync(Other.resizeLink(myresponse.url)));
            var emim = new DiscordEmbedBuilder
            {
                Description = $"[Full Source Image Link]({myresponse.url.ToString()})",
                ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(dataStream2)}"
            };
            emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
            emim.WithFooter("Requested by " + ctx.Message.Author.Username, ctx.Message.Author.AvatarUrl);
            Console.WriteLine(MimeGuesser.GuessExtension(dataStream2));
            await ctx.RespondWithFileAsync(fileName: $"image.{MimeGuesser.GuessExtension(dataStream2)}", fileData: dataStream2, embed: emim.Build());
        }

        [Command("gumi")]
        [Description("Random Gumi image")]
        public async Task GumiPic(CommandContext ctx)
        {
            var myresponse = JsonConvert.DeserializeObject<ImgRet>(await new WebClient().DownloadStringTaskAsync($"https://api.meek.moe/gumi"));
            Stream dataStream2 = new MemoryStream(await new WebClient().DownloadDataTaskAsync(Other.resizeLink(myresponse.url)));
            var emim = new DiscordEmbedBuilder
            {
                Description = $"[Full Source Image Link]({myresponse.url.ToString()})",
                ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(dataStream2)}"
            };
            if (myresponse.creator.Length != 0)
            {
                emim.AddField("Creator", myresponse.creator);
            }
            emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
            emim.WithFooter("Requested by " + ctx.Message.Author.Username, ctx.Message.Author.AvatarUrl);
            await ctx.RespondWithFileAsync(fileName: $"image.{MimeGuesser.GuessExtension(dataStream2)}", fileData: dataStream2, embed: emim.Build());
        }

        [Command("kaito")]
        [Description("Random Kaito image")]
        public async Task KaitoPic(CommandContext ctx)
        {
            var myresponse = JsonConvert.DeserializeObject<ImgRet>(await new WebClient().DownloadStringTaskAsync($"https://api.meek.moe/kaito"));
            Stream dataStream2 = new MemoryStream(await new WebClient().DownloadDataTaskAsync(Other.resizeLink(myresponse.url)));
            var emim = new DiscordEmbedBuilder
            {
                Description = $"[Full Source Image Link]({myresponse.url.ToString()})",
                ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(dataStream2)}"
            };
            if (myresponse.creator.Length != 0)
            {
                emim.AddField("Creator", myresponse.creator);
            }
            emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
            emim.WithFooter("Requested by " + ctx.Message.Author.Username, ctx.Message.Author.AvatarUrl);

            await ctx.RespondWithFileAsync(fileName: $"image.{MimeGuesser.GuessExtension(dataStream2)}", fileData: dataStream2, embed: emim.Build());
        }

        [Command("k-on")]
        [Description("Random K-On gif")]
        public async Task K_On(CommandContext ctx)
        {
            var e = JsonConvert.DeserializeObject<Derpy>(await new WebClient().DownloadStringTaskAsync($"https://derpyapi.glitch.me/k-on"));
            Stream img = new MemoryStream(await new WebClient().DownloadDataTaskAsync(Other.resizeLink(e.url)));
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
            em.WithFooter("by Derpy API");
            em.WithDescription($"{e.url}");
            await ctx.RespondWithFileAsync(fileData: img, fileName: $"image.{MimeGuesser.GuessExtension(img)}", embed: em.Build());
        }

        [Command("konosuba")]
        [Description("Random Konosuba image")]
        public async Task Konosuba(CommandContext ctx)
        {
            var e = JsonConvert.DeserializeObject<Derpy>(await new WebClient().DownloadStringTaskAsync($"https://derpyapi.glitch.me/konosuba"));
            Stream img = new MemoryStream(await new WebClient().DownloadDataTaskAsync(Other.resizeLink(e.url)));
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
            em.WithFooter("by Derpy API");
            em.WithDescription($"{e.url}");
            await ctx.RespondWithFileAsync(fileData: img, fileName: $"image.{MimeGuesser.GuessExtension(img)}", embed: em.Build());
        }

        [Command("len")]
        [Description("Random Len image")]
        public async Task KLenPic(CommandContext ctx)
        {
            var e = JsonConvert.DeserializeObject<ImgRet>(await new WebClient().DownloadStringTaskAsync($"https://api.meek.moe/len"));
            Stream dataStream2 = new MemoryStream(await new WebClient().DownloadDataTaskAsync(Other.resizeLink(e.url)));
            var emim = new DiscordEmbedBuilder
            {
                Description = $"[Full Source Image Link]({e.url.ToString()})",
                ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(dataStream2)}"
            };
            if (e.creator.Length != 0)
            {
                emim.AddField("Creator", e.creator);
            }
            emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
            emim.WithFooter("Requested by " + ctx.Message.Author.Username, ctx.Message.Author.AvatarUrl);
            await ctx.RespondWithFileAsync(fileName: $"image.{MimeGuesser.GuessExtension(dataStream2)}", fileData: dataStream2, embed: emim.Build());
        }

        [Command("lovelive")]
        [Description("Random Love Live gif")]
        public async Task LoveLive(CommandContext ctx)
        {
            var e = JsonConvert.DeserializeObject<Derpy>(await new WebClient().DownloadStringTaskAsync($"https://derpyapi.glitch.me/lovelive"));
            Stream img = new MemoryStream(await new WebClient().DownloadDataTaskAsync(Other.resizeLink(e.url)));
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
            em.WithFooter("by Derpy API");
            em.WithDescription($"{e.url}");
            await ctx.RespondWithFileAsync(fileData: img, fileName: $"image.{MimeGuesser.GuessExtension(img)}", embed: em.Build());
        }

        [Command("luka")]
        [Description("Random Luka image")]
        public async Task LukaPic(CommandContext ctx)
        {
            var myresponse = JsonConvert.DeserializeObject<ImgRet>(await new WebClient().DownloadStringTaskAsync($"https://api.meek.moe/luka"));
            Stream dataStream2 = new MemoryStream(await new WebClient().DownloadDataTaskAsync(Other.resizeLink(myresponse.url)));
            var emim = new DiscordEmbedBuilder
            {
                Description = $"[Full Source Image Link]({myresponse.url.ToString()})",
                ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(dataStream2)}"
            };
            if (myresponse.creator.Length != 0)
            {
                emim.AddField("Creator", myresponse.creator);
            }
            emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
            emim.WithFooter("Requested by " + ctx.Message.Author.Username, ctx.Message.Author.AvatarUrl);
            await ctx.RespondWithFileAsync(fileName: $"image.{MimeGuesser.GuessExtension(dataStream2)}", fileData: dataStream2, embed: emim.Build());
        }

        [Command("meiko")]
        [Description("Random Meiko image")]
        public async Task MeikoPic(CommandContext ctx)
        {
            var myresponse = JsonConvert.DeserializeObject<ImgRet>(await new WebClient().DownloadStringTaskAsync($"https://api.meek.moe/meiko"));
            Stream dataStream2 = new MemoryStream(await new WebClient().DownloadDataTaskAsync(Other.resizeLink(myresponse.url)));
            var emim = new DiscordEmbedBuilder
            {
                Description = $"[Full Source Image Link]({myresponse.url.ToString()})",
                ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(dataStream2)}"
            };
            if (myresponse.creator.Length != 0)
            {
                emim.AddField("Creator", myresponse.creator);
            }
            emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
            emim.WithFooter("Requested by " + ctx.Message.Author.Username, ctx.Message.Author.AvatarUrl);
            await ctx.RespondWithFileAsync(fileName: $"image.{MimeGuesser.GuessExtension(dataStream2)}", fileData: dataStream2, embed: emim.Build());
        }

        [Command("miku")]
        [Description("Random Miku image")]
        public async Task HMikuPic(CommandContext ctx)
        {
            var myresponse = JsonConvert.DeserializeObject<ImgRet>(await new WebClient().DownloadStringTaskAsync($"https://api.meek.moe/miku"));
            Stream dataStream2 = new MemoryStream(await new WebClient().DownloadDataTaskAsync(Other.resizeLink(myresponse.url)));
            var emim = new DiscordEmbedBuilder
            {
                Description = $"[Full Source Image Link]({myresponse.url.ToString()})",
                ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(dataStream2)}"
            };
            if (myresponse.creator.Length != 0)
            {
                emim.AddField("Creator", myresponse.creator);
            }
            emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
            emim.WithFooter("Requested by " + ctx.Message.Author.Username, ctx.Message.Author.AvatarUrl);
            await ctx.RespondWithFileAsync(fileName: $"image.{MimeGuesser.GuessExtension(dataStream2)}", fileData: dataStream2, embed: emim.Build());
        }

        [Command("neko")]
        [Description("Get a random neko image")]
        public async Task Cat(CommandContext ctx)
        {
            var ImgURL = await Web.GetNekos_Life("https://nekos.life/api/v2/img/neko");
            Stream img = new MemoryStream(await new WebClient().DownloadDataTaskAsync(Other.resizeLink(ImgURL.Url)));
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
            em.WithFooter("by nekos.life");
            await ctx.RespondWithFileAsync(embed: em.Build(), fileData: img, fileName: $"image.{MimeGuesser.GuessExtension(img)}");
        }

        [Command("rin")]
        [Description("Random Rin image")]
        public async Task KRinPic(CommandContext ctx)
        {
            var myresponse = JsonConvert.DeserializeObject<ImgRet>(await new WebClient().DownloadStringTaskAsync($"https://api.meek.moe/rin"));
            Stream dataStream2 = new MemoryStream(await new WebClient().DownloadDataTaskAsync(Other.resizeLink(myresponse.url)));
            var emim = new DiscordEmbedBuilder
            {
                Description = $"[Full Source Image Link]({myresponse.url.ToString()})",
                ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(dataStream2)}"
            };
            if (myresponse.creator.Length != 0)
            {
                emim.AddField("Creator", myresponse.creator);
            }
            emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
            emim.WithFooter("Requested by " + ctx.Message.Author.Username, ctx.Message.Author.AvatarUrl);
            await ctx.RespondWithFileAsync(fileName: $"image.{MimeGuesser.GuessExtension(dataStream2)}", fileData: dataStream2, embed: emim.Build());
        }

        [Command("takagi")]
        [Description("Random Takagi image")]
        public async Task Takagi(CommandContext ctx)
        {
            var e = JsonConvert.DeserializeObject<Derpy>(await new WebClient().DownloadStringTaskAsync($"https://derpyapi.glitch.me/takagi"));
            Stream img = new MemoryStream(await new WebClient().DownloadDataTaskAsync(Other.resizeLink(e.url)));
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
            em.WithFooter("by Derpy API");
            em.WithDescription($"[Full Image]({e.url})");
            await ctx.RespondWithFileAsync(fileData: img, fileName: $"image.{MimeGuesser.GuessExtension(img)}", embed: em.Build());
        }

        [Command("teto")]
        [Description("Random Teto image")]
        public async Task KTetoPic(CommandContext ctx)
        {
            var myresponse = JsonConvert.DeserializeObject<ImgRet>(await new WebClient().DownloadStringTaskAsync($"https://api.meek.moe/teto"));
            Stream dataStream2 = new MemoryStream(await new WebClient().DownloadDataTaskAsync(Other.resizeLink(myresponse.url)));
            var emim = new DiscordEmbedBuilder
            {
                Description = $"[Full Source Image Link]({myresponse.url.ToString()})",
                ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(dataStream2)}"
            };
            if (myresponse.creator.Length != 0)
            {
                emim.AddField("Creator", myresponse.creator);
            }
            emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
            emim.WithFooter("Requested by " + ctx.Message.Author.Username, ctx.Message.Author.AvatarUrl);
            await ctx.RespondWithFileAsync(fileName: $"image.{MimeGuesser.GuessExtension(dataStream2)}", fileData: dataStream2, embed: emim.Build());
        }

        public class NekoBot
        {
            public string message { get; set; }
            public int status { get; set; }
            public bool success { get; set; }
        }

        public class Derpy
        {
            public string url { get; set; }
        }

        public class ImgRet
        {
            public string url { get; set; }
            public string creator { get; set; }
        }
    }
}
