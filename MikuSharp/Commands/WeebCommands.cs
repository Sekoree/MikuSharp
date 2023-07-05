using HeyRed.Mime;

using MikuSharp.Utilities;

namespace MikuSharp.Commands;

[SlashCommandGroup("weeb", "Weeb Stuff!", dmPermission: false)]
internal class WeebCommands : ApplicationCommandsModule
{
	[SlashCommand("awooify", "Awooify your or someones avatar!")]
	public static async Task AwooifyAsync(InteractionContext ctx, [Option("user", "User to awooify")] DiscordUser? user = null)
	{
		await ctx.DeferAsync(false);
		var url = (await (user ?? ctx.User).ConvertToMember(ctx.Guild)).GuildAvatarUrl;
		var e = JsonConvert.DeserializeObject<Entities.NekoBot>(await ctx.Client.RestClient.GetStringAsync($"https://nekobot.xyz/api/imagegen?type=awooify&url={url}", MikuBot.CanellationTokenSource.Token));
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithImageUrl(e.Message).Build()));
	}

	[SlashCommand("diva", "Radnom PJD Loading image")]
	public static async Task DivaPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<Entities.MeekMoe>(await ctx.Client.RestClient.GetStringAsync($"https://api.meek.moe/diva", MikuBot.CanellationTokenSource.Token));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url), MikuBot.CanellationTokenSource.Token))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl);
		//ctx.Client.Logger.LogDebug(MimeGuesser.GuessExtension(img));

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("gumi", "Random Gumi image")]
	public static async Task GumiPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<Entities.MeekMoe>(await ctx.Client.RestClient.GetStringAsync($"https://api.meek.moe/gumi", MikuBot.CanellationTokenSource.Token));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url), MikuBot.CanellationTokenSource.Token))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
		{
			emim.AddField(new DiscordEmbedField("Creator", res.Creator));
		}
		emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl);

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("kaito", "Random Kaito image")]
	public static async Task KaitoPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<Entities.MeekMoe>(await ctx.Client.RestClient.GetStringAsync($"https://api.meek.moe/kaito", MikuBot.CanellationTokenSource.Token));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url), MikuBot.CanellationTokenSource.Token))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
		{
			emim.AddField(new DiscordEmbedField("Creator", res.Creator));
		}
		emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl);

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("len", "Random Len image")]
	public static async Task KLenPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<Entities.MeekMoe>(await ctx.Client.RestClient.GetStringAsync($"https://api.meek.moe/len", MikuBot.CanellationTokenSource.Token));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url), MikuBot.CanellationTokenSource.Token))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
		{
			emim.AddField(new DiscordEmbedField("Creator", res.Creator));
		}
		emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl);

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("luka", "Random Luka image")]
	public static async Task LukaPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<Entities.MeekMoe>(await ctx.Client.RestClient.GetStringAsync($"https://api.meek.moe/luka", MikuBot.CanellationTokenSource.Token));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url), MikuBot.CanellationTokenSource.Token))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
		{
			emim.AddField(new DiscordEmbedField("Creator", res.Creator));
		}
		emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl);

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("meiko", "Random Meiko image")]
	public static async Task MeikoPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<Entities.MeekMoe>(await ctx.Client.RestClient.GetStringAsync($"https://api.meek.moe/meiko", MikuBot.CanellationTokenSource.Token));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url), MikuBot.CanellationTokenSource.Token))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
		{
			emim.AddField(new DiscordEmbedField("Creator", res.Creator));
		}
		emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl);

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("miku", "Random Miku image")]
	public static async Task HMikuPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<Entities.MeekMoe>(await ctx.Client.RestClient.GetStringAsync($"https://api.meek.moe/miku", MikuBot.CanellationTokenSource.Token));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url), MikuBot.CanellationTokenSource.Token))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
		{
			emim.AddField(new DiscordEmbedField("Creator", res.Creator));
		}
		emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl);

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("neko", "Get a random neko image")]
	public static async Task Cat(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var imgUrl = await ctx.Client.RestClient.GetNekosLifeAsync("https://nekos.life/api/v2/img/neko");
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(imgUrl.Url), MikuBot.CanellationTokenSource.Token));
		var em = new DiscordEmbedBuilder();
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("rin", "Random Rin image")]
	public static async Task KRinPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<Entities.MeekMoe>(await ctx.Client.RestClient.GetStringAsync($"https://api.meek.moe/rin", MikuBot.CanellationTokenSource.Token));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url), MikuBot.CanellationTokenSource.Token))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
		{
			emim.AddField(new DiscordEmbedField("Creator", res.Creator));
		}
		emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl);

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("teto", "Random Teto image")]
	public static async Task KTetoPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<Entities.MeekMoe>(await ctx.Client.RestClient.GetStringAsync($"https://api.meek.moe/teto", MikuBot.CanellationTokenSource.Token));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url), MikuBot.CanellationTokenSource.Token))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
		{
			emim.AddField(new DiscordEmbedField("Creator", res.Creator));
		}
		emim.WithAuthor(name: "via api.meek.moe", url: "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl);

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}
}
