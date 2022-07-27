using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;

using HeyRed.Mime;

using MikuSharp.Utilities;

using System.Threading.Tasks;

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

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{d.Filetype}", d.Data);
            builder.WithEmbed(d.Embed);
            await ctx.RespondAsync(builder);
        }

        [Command("anal")]
        [Description("lewd")]
        public async Task Anal(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/image?type=anal");

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{d.Filetype}", d.Data);
            builder.WithEmbed(d.Embed);
            await ctx.RespondAsync(builder);
        }

        [Command("ass")]
        [Description("lewd")]
        public async Task Ass(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/image?type=ass");

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{d.Filetype}", d.Data);
            builder.WithEmbed(d.Embed);
            await ctx.RespondAsync(builder);
        }

        /*[Command("booru")]
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

            DiscordMessageBuilder builder = new DiscordMessageBuilder();
            builder.WithFile($"image.{MimeGuesser.GuessExtension(str)}", srt);
            builder.WithEmbed(d.Embed);
            await ctx.RespondAsync(builder);
        }*/

        [Command("gonewild")]
        [Description("lewd")]
        public async Task Gonewild(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/image?type=gonewild");

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{d.Filetype}", d.Data);
            builder.WithEmbed(d.Embed);
            await ctx.RespondAsync(builder);
        }
        /*
        [Command("hentai")]
        [Description("lewd")]
        public async Task Hentai(CommandContext ctx)
        {

            await ctx.RespondAsync("The API we're using sadly ended their service.");
			
            var d = await Web.GetKsoftSiRanImg("hentai_gif", true);

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{d.Filetype}", d.Data);
            builder.WithEmbed(d.Embed);
            await ctx.RespondAsync(builder);
            */
        }
             - discontinued
        /*[Command("konachan")]
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

            DiscordMessageBuilder builder = new DiscordMessageBuilder();
            builder.WithFile($"image.{MimeGuesser.GuessExtension(str)}", srt);
            builder.WithEmbed(d.Embed);
            await ctx.RespondAsync(builder);
        }*/

        [Command("lewdkitsune")]
        [Description("lewd")]
        public async Task LewdKitsune(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/image?type=lewdkitsune");

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{d.Filetype}", d.Data);
            builder.WithEmbed(d.Embed);
            await ctx.RespondAsync(builder);
        }

        [Command("lewdneko")]
        [Description("lewd")]
        public async Task LewdNeko(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/image?type=lewdneko");

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{d.Filetype}", d.Data);
            builder.WithEmbed(d.Embed);
            await ctx.RespondAsync(builder);
        }
        /*
        [Command("nekopara")]
        [Description("lewd - discontinued")]
        public async Task Nekopara(CommandContext ctx)
        {
            await ctx.RespondAsync("The API we're using sadly ended their service.");
            
            var d = await Web.GetDerpy("https://miku.derpyenterprises.org/nekoparajson");

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{d.Filetype}", d.Data);
            builder.WithEmbed(d.Embed);
            await ctx.RespondAsync(builder);*/
        }
    
        [Command("nekoparagif")]
        [Description("lewd - discontinued")]
        public async Task NekoparaGif(CommandContext ctx)
        {
            await ctx.RespondAsync("The API we're using sadly ended their service.");
            
            var d = await Web.GetDerpy("https://miku.derpyenterprises.org/nekoparagifjson");

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{d.Filetype}", d.Data);
            builder.WithEmbed(d.Embed);
            await ctx.RespondAsync(builder);
            
        }
        */
        [Command("porngif")]
        [Description("lewd")]
        public async Task PornGif(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/image?type=pgif");

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{d.Filetype}", d.Data);
            builder.WithEmbed(d.Embed);
            await ctx.RespondAsync(builder);
        }

        [Command("pussy")]
        [Description("lewd")]
        public async Task Pussy(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/image?type=pussy");

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{d.Filetype}", d.Data);
            builder.WithEmbed(d.Embed);
            await ctx.RespondAsync(builder);
        }

        /*[Command("rule34")]
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

            DiscordMessageBuilder builder = new DiscordMessageBuilder();
            builder.WithFile($"image.{MimeGuesser.GuessExtension(str)}", srt);
            builder.WithEmbed(d.Embed);
            await ctx.RespondAsync(builder);
        }*/

            [Command("thighs")]
        [Aliases("thigh")]
        [Description("lewd")]
        public async Task Thighs(CommandContext ctx)
        {
            var d = await Web.GetNekobot("https://nekobot.xyz/api/v2/image/thighs");

            DiscordMessageBuilder builder = new();
            builder.WithFile($"image.{d.Filetype}", d.Data);
            builder.WithEmbed(d.Embed);
            await ctx.RespondAsync(builder);
        }
    }
}
