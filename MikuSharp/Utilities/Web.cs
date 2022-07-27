using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;

using HeyRed.Mime;

using MikuSharp.Entities;

using Newtonsoft.Json;

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Weeb.net;

namespace MikuSharp.Utilities
{
    public class Web
    {
        public static async Task<Nekos_Life> GetNekos_Life(string url)
        {
            var hc = new HttpClient();
            var dl = JsonConvert.DeserializeObject<Nekos_Life>(await hc.GetStringAsync(url));
            MemoryStream str = new(await hc.GetByteArrayAsync(Other.resizeLink(dl.Url)))
            {
                Position = 0
            };
            dl.Data = str;
            dl.Filetype = MimeGuesser.GuessExtension(str);
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{dl.Filetype}");
            em.WithFooter("by nekos.life");
            dl.Embed = em.Build();
            return dl;
        }

        public static async Task<KsoftSiRanImg> GetKsoftSiRanImg(string tag, bool nsfw = false)
        {
            var c = new HttpClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MikuBot.Config.KsoftSiToken);
            var v = JsonConvert.DeserializeObject<KsoftSiRanImg>(await c.GetStringAsync("https://api.ksoft.si/images/random-image?tag=hentai_gif&nsfw=true"));
            MemoryStream img = new(await c.GetByteArrayAsync(Other.resizeLink(v.url)));
            v.Data = img;
            v.Filetype = MimeGuesser.GuessExtension(img);
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{v.Filetype}");
            em.WithFooter("by KSoft.si");
            v.Embed = em.Build();
            return v;
        }

        public static async Task<NekoBot> GetNekobot(string url)
        {
            var hc = new HttpClient();
            var dl = JsonConvert.DeserializeObject<NekoBot>(await hc.GetStringAsync(url));
            MemoryStream str = new(await hc.GetByteArrayAsync(Other.resizeLink(dl.message)))
            {
                Position = 0
            };
            dl.Data = str;
            dl.Filetype = MimeGuesser.GuessExtension(str);
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{dl.Filetype}");
            em.WithFooter("by nekobot.xyz");
            dl.Embed = em.Build();
            return dl;
        }

        public static async Task<Derpy> GetDerpy(string url)
        {
            var hc = new HttpClient();
            var dl = JsonConvert.DeserializeObject<Derpy>(await hc.GetStringAsync(url));
            MemoryStream str = new(await hc.GetByteArrayAsync(Other.resizeLink(dl.url)))
            {
                Position = 0
            };
            dl.Data = str;
            dl.Filetype = MimeGuesser.GuessExtension(str);
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{dl.Filetype}");
            em.WithFooter("by derpyenterprises.org");
            dl.Embed = em.Build();
            return dl;
        }

        public static async Task<WeebSh> GetWeebSh(CommandContext ctx, string query, string[] tags = null, NsfwSearch nsfw = NsfwSearch.False)
        {
            var weeurl = await MikuBot._weebClient.GetRandomAsync(query, tags, nsfw: nsfw);
            var hc = new HttpClient();
            MemoryStream img = new(await hc.GetByteArrayAsync(weeurl.Url))
            {
                Position = 0
            };
            var em = new DiscordEmbedBuilder();
            //em.WithDescription($"{ctx.Member.Mention} hugs {m.Mention} uwu");
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
            em.WithFooter("by weeb.sh");
            //await ctx.RespondWithFileAsync(embed: em.Build(), fileData: img, fileName: $"image.{MimeGuesser.GuessExtension(img)}");
            return new WeebSh {
                ImgData = img,
                Extension = MimeGuesser.GuessExtension(img),
                Embed = em
            };
        }
    }
}
