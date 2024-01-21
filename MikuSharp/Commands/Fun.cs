using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using HeyRed.Mime;

using MikuSharp.Entities;
using MikuSharp.Utilities;

using Newtonsoft.Json;

using System;
using System.IO;
using System.Threading.Tasks;

namespace MikuSharp.Commands;

[SlashCommandGroup("fun", "Fun commands")]
internal class Fun : ApplicationCommandsModule
{
	[SlashCommand("8ball", "Yes? No? Maybe?")]
	public static async Task EightBallAsync(InteractionContext ctx, [Option("text", "Text to modify")] string text)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new());
		var responses = new[]
		{
			"It is certain.", "It is decidedly so.", "Without a doubt.", "Yes - definitely.", "You may rely on it.", "As I see it, yes.", "Most likely.", "Outlook good.", "Yes.", "Signs point to yes.", "Reply hazy, try again", "Ask again later.", "Better not tell you now.", "Cannot predict now.", "Concentrate and ask again.", "Don't count on it.", "My reply is no.", "My sources say no.",
			"Outlook not so good.", "Very doubtful.", "No."
		};
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"> {text}\n\n{responses[new Random().Next(0, responses.Length)]}"));
	}

	[SlashCommand("cat", "Get a random cat image!")]
	public static async Task CatAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new());
		var imgUrl = await ctx.Client.RestClient.GetNekosLifeAsync("https://nekos.life/api/v2/img/meow");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{imgUrl.Filetype}", imgUrl.Data);
		builder.AddEmbed(imgUrl.Embed);
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("clyde", "Say something as clyde bot")]
	public static async Task ClydeAsync(InteractionContext ctx, [Option("text", "Text to modify")] string text)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new());
		var e = JsonConvert.DeserializeObject<NekoBot>(await ctx.Client.RestClient.GetStringAsync($"https://nekobot.xyz/api/imagegen?type=clyde&text={text}"));
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(e.Message));

		DiscordWebhookBuilder builder = new();
		builder.AddFile("clyde.png", img);
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("coinflip", "Flip a coin lol")]
	public static async Task CoinflipAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new());
		var flip = new[] { $"Heads {DiscordEmoji.FromName(ctx.Client, ":arrow_up_small:")}", $"Tails {DiscordEmoji.FromName(ctx.Client, ":arrow_down_small:")}" };
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(flip[new Random().Next(0, flip.Length)]));
	}

	[SlashCommand("dog", "Random Dog Image")]
	public static async Task DogAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new());
		var dc = JsonConvert.DeserializeObject<DogCeo>(await ctx.Client.RestClient.GetStringAsync("https://dog.ceo/api/breeds/image/random"));
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(dc.Message)));
		var em = new DiscordEmbedBuilder();
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by dog.ceo", "https://dog.ceo/img/favicon.png");
		em.WithDescription($"[Full Image]({dc.Message})");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		await ctx.EditResponseAsync(builder);
	}
	/*
    [SlashCommand("duck", "Random duck image")]
    public static async Task DuckAsync(InteractionContext ctx)
    {
        var dc = JsonConvert.DeserializeObject<Random_D>(await ctx.Client.RestClient.GetStringAsync("https://random-d.uk/api/v1/random"));
        Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.resizeLink(dc.message)));
        var em = new DiscordEmbedBuilder();
        em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
        em.WithFooter("by random-d.uk", "https://random-d.uk/favicon.png");
        em.WithDescription($"[Full Image]({dc.message})");

        DiscordWebhookBuilder builder = new DiscordWebhookBuilder();
        builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
        builder.WithEmbed(em.Build());
        await ctx.EditResponseAsync(builder);
    }*/
	/*
    [SlashCommand("lion", "Get a random lion image")]
    public static async Task Lion(InteractionContext ctx)
    {
        var ImgLink = JsonConvert.DeserializeObject<AnIdiotsGuide>(await ctx.Client.RestClient.GetStringAsync("https://animals.anidiots.guide/lion"));
        Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.resizeLink(ImgLink.link)));

        DiscordWebhookBuilder builder = new DiscordWebhookBuilder();
        builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
        await ctx.EditResponseAsync(builder);
    }*/

	[SlashCommand("lizard", "Get a random lizard image")]
	public static async Task LizardAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new());
		var get = await ctx.Client.RestClient.GetNekosLifeAsync("https://nekos.life/api/lizard");
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(get.Url)));

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		await ctx.EditResponseAsync(builder);
	}
	/*
    [SlashCommand("panda", "Random panda image")]
    public static async Task PandaAsync(InteractionContext ctx)
    {
        var ImgLink = JsonConvert.DeserializeObject<AnIdiotsGuide>(await ctx.Client.RestClient.GetStringAsync("https://animals.anidiots.guide/panda"));
        Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.resizeLink(ImgLink.link)));

        DiscordWebhookBuilder builder = new DiscordWebhookBuilder();
        builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
        await ctx.EditResponseAsync(builder);
    }

    [SlashCommand("penguin", "Radnom penguin image")]
    public static async Task PenguinAsync(InteractionContext ctx)
    {
        var ImgLink = JsonConvert.DeserializeObject<AnIdiotsGuide>(await ctx.Client.RestClient.GetStringAsync("https://animals.anidiots.guide/penguin"));
        Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.resizeLink(ImgLink.link)));

        DiscordWebhookBuilder builder = new DiscordWebhookBuilder();
        builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
        await ctx.EditResponseAsync(builder);
    }*/

	/*
    [SlashCommand("redpanda", "Random red panda image")]
    public static async Task RedPandaAsync(InteractionContext ctx)
    {
        var ImgLink = JsonConvert.DeserializeObject<AnIdiotsGuide>(await ctx.Client.RestClient.GetStringAsync("https://animals.anidiots.guide/red_panda"));
        Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.resizeLink(ImgLink.link)));

        DiscordWebhookBuilder builder = new DiscordWebhookBuilder();
        builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
        await ctx.EditResponseAsync(builder);
    }*/

	[SlashCommand("rps", "Play rock paper scissors!")]
	public static async Task RpsAsync(InteractionContext ctx, [Option("rps", "Your rock paper scissor choice")] string rps)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new());
		var rock = new[] { $"Rock {DiscordEmoji.FromName(ctx.Client, ":black_circle:")}", $"Paper {DiscordEmoji.FromName(ctx.Client, ":pencil:")}", $"Scissors {DiscordEmoji.FromName(ctx.Client, ":scissors:")}" };
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{ctx.User.Mention} choose {rps}!\n\nI choose {rock[new Random().Next(0, rock.Length)]}"));
	}
	/*
    [SlashCommand("tiger", "Random tiger image")]
    public static async Task TigerAsync(InteractionContext ctx)
    {
        var ImgLink = JsonConvert.DeserializeObject<AnIdiotsGuide>(await ctx.Client.RestClient.GetStringAsync("https://animals.anidiots.guide/tiger"));
        Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.resizeLink(ImgLink.link)));

        DiscordWebhookBuilder builder = new DiscordWebhookBuilder();
        builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
        await ctx.EditResponseAsync(builder);
    }*/

	/*
    [SlashCommand("trumptweet", "generate a tweet by Trump")]
    public static async Task TrumpTweetAsync(InteractionContext ctx, [RemainingText]string text)
    {
        //https://nekobot.xyz/api/imagegen?type=trumptweet&text=
        var e = JsonConvert.DeserializeObject<NekoBot>(await ctx.Client.RestClient.GetStringAsync($"https://nekobot.xyz/api/imagegen?type=trumptweet&text={text}"));
        Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(e.message));

        DiscordWebhookBuilder builder = new DiscordWebhookBuilder();
        builder.AddFile($"trump.png", img);
        await ctx.EditResponseAsync(builder);
    }*/
}