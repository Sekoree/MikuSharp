using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using DisCatSharp.Entities;

using HeyRed.Mime;

using MikuSharp.Entities;

using Newtonsoft.Json;

using Weeb.net;

namespace MikuSharp.Utilities;

public static class Web
{
	public static async Task<NekosLife> GetNekosLifeAsync(this HttpClient client, string url)
	{
		var dl = JsonConvert.DeserializeObject<NekosLife>(await client.GetStringAsync(url));
		MemoryStream str = new(await client.GetByteArrayAsync(Other.ResizeLink(dl.Url)))
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
		client.DefaultRequestHeaders.Authorization = new("Bearer", MikuBot.Config.KsoftSiToken);
		var v = JsonConvert.DeserializeObject<KsoftSiRanImg>(await client.GetStringAsync("https://api.ksoft.si/images/random-image?tag=hentai_gif&nsfw=true"));
		MemoryStream img = new(await client.GetByteArrayAsync(Other.ResizeLink(v.Url)));
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
		MemoryStream str = new(await client.GetByteArrayAsync(Other.ResizeLink(dl.Message)))
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
		var weeurl = await MikuBot.WeebClient.GetRandomAsync(query, tags, nsfw: nsfw);
		MemoryStream img = new(await client.GetByteArrayAsync(weeurl.Url))
		{
			Position = 0
		};
		var em = new DiscordEmbedBuilder();
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by weeb.sh");
		return new()
		{
			ImgData = img,
			Extension = MimeGuesser.GuessExtension(img),
			Embed = em
		};
	}
}
