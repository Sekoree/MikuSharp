using HeyRed.Mime;

using MikuSharp.Entities;

namespace MikuSharp.Utilities;

public static class Web
{
	public static async Task<Nekos_Life> GetNekosLifeAsync(this HttpClient client, string url)
	{
		var dl = JsonConvert.DeserializeObject<Nekos_Life>(await client.GetStringAsync(url));
		var imgBytes = await client.GetByteArrayAsync(Other.resizeLink(dl.Url));
		var str = new MemoryStream(imgBytes)
		{
			Position = 0
		};
		dl.Data = str;
		dl.Filetype = MimeGuesser.GuessExtension(str);
		var em = new DiscordEmbedBuilder
		{
			ImageUrl = $"attachment://image.{dl.Filetype}",
			Footer = new() { Text = "by nekos.life" }
		};
		dl.Embed = em.Build();
		return dl;
	}

	public static async Task<KsoftSiRanImg> GetKsoftSiRanImgAsync(this HttpClient client, string tag, bool nsfw = false)
	{
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MikuBot.Config.KsoftSiToken);
		var v = JsonConvert.DeserializeObject<KsoftSiRanImg>(await client.GetStringAsync("https://api.ksoft.si/images/random-image?tag=hentai_gif&nsfw=true"));
		var imgBytes = await client.GetByteArrayAsync(Other.resizeLink(v.Url));
		var img = new MemoryStream(imgBytes)
		{
			Position = 0
		};
		v.Data = img;
		v.Filetype = MimeGuesser.GuessExtension(img);
		var em = new DiscordEmbedBuilder
		{
			ImageUrl = $"attachment://image.{v.Filetype}",
			Footer = new() { Text = "by KSoft.si" }
		};
		v.Embed = em.Build();
		return v;
	}

	public static async Task<NekoBot> GetNekobotAsync(this HttpClient client, string url)
	{
		var dl = JsonConvert.DeserializeObject<NekoBot>(await client.GetStringAsync(url));
		var imgBytes = await client.GetByteArrayAsync(Other.resizeLink(dl.Message));
		var str = new MemoryStream(imgBytes)
		{
			Position = 0
		};
		dl.Data = str;
		dl.Filetype = MimeGuesser.GuessExtension(str);
		var em = new DiscordEmbedBuilder
		{
			ImageUrl = $"attachment://image.{dl.Filetype}",
			Footer = new() { Text = "by nekobot.xyz" }
		};
		dl.Embed = em.Build();
		return dl;
	}

	public static async Task<WeebSh> GetWeebShAsync(this HttpClient client, string query, string[] tags = null, NsfwSearch nsfw = NsfwSearch.False)
	{
		var weeurl = await MikuBot.WeebClient.GetRandomAsync(query, tags, nsfw: nsfw);
		var imgBytes = await client.GetByteArrayAsync(weeurl.Url);
		var img = new MemoryStream(imgBytes)
		{
			Position = 0
		};
		var em = new DiscordEmbedBuilder
		{
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}",
			Footer = new() { Text = "by weeb.sh" }
		};
		return new WeebSh
		{
			ImgData = img,
			Extension = MimeGuesser.GuessExtension(img),
			Embed = em
		};
	}
}
