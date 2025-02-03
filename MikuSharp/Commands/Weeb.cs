using System.IO;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using HeyRed.Mime;

using MikuSharp.Entities;
using MikuSharp.Utilities;

using Newtonsoft.Json;

namespace MikuSharp.Commands;

[SlashCommandGroup("weeb", "Weeb Stuff!", false, [InteractionContextType.Guild, InteractionContextType.PrivateChannel], [ApplicationCommandIntegrationTypes.GuildInstall, ApplicationCommandIntegrationTypes.UserInstall])]
internal class Weeb : ApplicationCommandsModule
{
	[SlashCommand("awooify", "Awooify your or someones avatar!")]
	public static async Task AwooifyAsync(InteractionContext ctx, [Option("user", "User to awooify")] DiscordUser? user = null)
	{
		await ctx.DeferAsync(false);
		var url = (await (user ?? ctx.User).ConvertToMember(ctx.Guild)).GuildAvatarUrl;
		var e = JsonConvert.DeserializeObject<NekoBot>(await ctx.Client.RestClient.GetStringAsync($"https://nekobot.xyz/api/imagegen?type=awooify&url={url}"));
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithImageUrl(e.Message).Build()));
	}

	[SlashCommand("diva", "Radnom PJD Loading image")]
	public static async Task DivaPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<MeekMoe>(await ctx.Client.RestClient.GetStringAsync("https://api.meek.moe/diva"));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url)))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		emim.WithAuthor("via api.meek.moe", "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithGlobalName, ctx.User.AvatarUrl);
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
		var res = JsonConvert.DeserializeObject<MeekMoe>(await ctx.Client.RestClient.GetStringAsync("https://api.meek.moe/gumi"));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url)))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
			emim.AddField(new("Creator", res.Creator));
		emim.WithAuthor("via api.meek.moe", "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithGlobalName, ctx.User.AvatarUrl);

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("kaito", "Random Kaito image")]
	public static async Task KaitoPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<MeekMoe>(await ctx.Client.RestClient.GetStringAsync("https://api.meek.moe/kaito"));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url)))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
			emim.AddField(new("Creator", res.Creator));
		emim.WithAuthor("via api.meek.moe", "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithGlobalName, ctx.User.AvatarUrl);

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("len", "Random Len image")]
	public static async Task KLenPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<MeekMoe>(await ctx.Client.RestClient.GetStringAsync("https://api.meek.moe/len"));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url)))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
			emim.AddField(new("Creator", res.Creator));
		emim.WithAuthor("via api.meek.moe", "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithGlobalName, ctx.User.AvatarUrl);

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("luka", "Random Luka image")]
	public static async Task LukaPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<MeekMoe>(await ctx.Client.RestClient.GetStringAsync("https://api.meek.moe/luka"));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url)))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
			emim.AddField(new("Creator", res.Creator));
		emim.WithAuthor("via api.meek.moe", "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithGlobalName, ctx.User.AvatarUrl);

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("meiko", "Random Meiko image")]
	public static async Task MeikoPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<MeekMoe>(await ctx.Client.RestClient.GetStringAsync("https://api.meek.moe/meiko"));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url)))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
			emim.AddField(new("Creator", res.Creator));
		emim.WithAuthor("via api.meek.moe", "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithGlobalName, ctx.User.AvatarUrl);

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("miku", "Random Miku image")]
	public static async Task HMikuPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<MeekMoe>(await ctx.Client.RestClient.GetStringAsync("https://api.meek.moe/miku"));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url)))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
			emim.AddField(new("Creator", res.Creator));
		emim.WithAuthor("via api.meek.moe", "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithGlobalName, ctx.User.AvatarUrl);

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
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(imgUrl.Url)));
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
		var res = JsonConvert.DeserializeObject<MeekMoe>(await ctx.Client.RestClient.GetStringAsync("https://api.meek.moe/rin"));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url)))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
			emim.AddField(new("Creator", res.Creator));
		emim.WithAuthor("via api.meek.moe", "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithGlobalName, ctx.User.AvatarUrl);

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("teto", "Random Teto image")]
	public static async Task KTetoPic(InteractionContext ctx)
	{
		await ctx.DeferAsync(false);
		var res = JsonConvert.DeserializeObject<MeekMoe>(await ctx.Client.RestClient.GetStringAsync("https://api.meek.moe/teto"));
		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(res.Url)))
		{
			Position = 0
		};
		var emim = new DiscordEmbedBuilder
		{
			Description = $"[Full Source Image Link]({res.Url})",
			ImageUrl = $"attachment://image.{MimeGuesser.GuessExtension(img)}"
		};
		if (res.Creator.Length != 0)
			emim.AddField(new("Creator", res.Creator));
		emim.WithAuthor("via api.meek.moe", "https://api.meek.moe/");
		emim.WithFooter("Requested by " + ctx.User.UsernameWithGlobalName, ctx.User.AvatarUrl);

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(emim.Build());
		await ctx.EditResponseAsync(builder);
	}
}
