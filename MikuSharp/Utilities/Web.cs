using DisCatSharp.Entities;

using HeyRed.Mime;

using MikuSharp.Entities;

using Newtonsoft.Json;

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Weeb.net;

namespace MikuSharp.Utilities;

public static class Web
{
	public static async Task<Nekos_Life> GetNekosLifeAsync(this HttpClient client, string url)
	{
		var dl = JsonConvert.DeserializeObject<Nekos_Life>(await client.GetStringAsync(url));
		MemoryStream str = new(await client.GetByteArrayAsync(Other.resizeLink(dl.Url)))
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

	public static async Task<KsoftSiRanImg> GetKsoftSiRanImgAsync(this HttpClient client, string tag, bool nsfw = false)
	{
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MikuBot.Config.KsoftSiToken);
		var v = JsonConvert.DeserializeObject<KsoftSiRanImg>(await client.GetStringAsync("https://api.ksoft.si/images/random-image?tag=hentai_gif&nsfw=true"));
		MemoryStream img = new(await client.GetByteArrayAsync(Other.resizeLink(v.url)));
		v.Data = img;
		v.Filetype = MimeGuesser.GuessExtension(img);
		var em = new DiscordEmbedBuilder();
		em.WithImageUrl($"attachment://image.{v.Filetype}");
		em.WithFooter("by KSoft.si");
		v.Embed = em.Build();
		return v;
	}

	public static async Task<NekoBot> GetNekobotAsync(this HttpClient client, string url)
	{
		var dl = JsonConvert.DeserializeObject<NekoBot>(await client.GetStringAsync(url));
		MemoryStream str = new(await client.GetByteArrayAsync(Other.resizeLink(dl.message)))
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

	public static async Task<WeebSh> GetWeebShAsync(this HttpClient client, string query, string[] tags = null, NsfwSearch nsfw = NsfwSearch.False)
	{
		var weeurl = await MikuBot._weebClient.GetRandomAsync(query, tags, nsfw: nsfw);
		MemoryStream img = new(await client.GetByteArrayAsync(weeurl.Url))
		{
			Position = 0
		};
		var em = new DiscordEmbedBuilder();
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by weeb.sh");
		return new WeebSh
		{
			ImgData = img,
			Extension = MimeGuesser.GuessExtension(img),
			Embed = em
		};
	}
}
