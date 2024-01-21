using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using HeyRed.Mime;

using MikuSharp.Utilities;

using System.IO;
using System.Threading.Tasks;

namespace MikuSharp.Commands;

[SlashCommandGroup("action", "Actions", dmPermission: false)]
internal class Action : ApplicationCommandsModule
{
	[SlashCommand("hug", "Hug someone!")]
	public async static Task HugAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} hugs {user.Mention} uwu"));
		var wsh = await ctx.Client.RestClient.GetWeebShAsync("hug", new[] { "" });
		wsh.Embed.WithDescription($"{ctx.User.Mention} hugs {user.Mention} uwu");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{wsh.Extension}", wsh.ImgData);
		builder.AddEmbed(wsh.Embed.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("kiss", "Kiss someone!")]
	public async static Task KissAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} kisses {user.Mention} >~<"));
		var wsh = await ctx.Client.RestClient.GetWeebShAsync("kiss", new[] { "" });
		wsh.Embed.WithDescription($"{ctx.User.Mention} kisses {user.Mention} >~<");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{wsh.Extension}", wsh.ImgData);
		builder.AddEmbed(wsh.Embed.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("lick", "Lick someone!")]
	public async static Task LickAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} licks {user.Mention} owo"));
		var wsh = await ctx.Client.RestClient.GetWeebShAsync("lick", new[] { "" });
		wsh.Embed.WithDescription($"{ctx.User.Mention} licks {user.Mention} owo");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{wsh.Extension}", wsh.ImgData);
		builder.AddEmbed(wsh.Embed.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("pat", "Pat someone!")]
	public async static Task PatAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} pats {user.Mention} #w#"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("pat", new[] { "" });
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(weeurl.Url)));
		var em = new DiscordEmbedBuilder();
		em.WithDescription($"{ctx.User.Mention} pats {user.Mention} #w#");
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("poke", "Poke someone!")]
	public async static Task PokeAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} pokes {user.Mention} ÓwÒ"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("poke", new[] { "" });
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(weeurl.Url)));
		var em = new DiscordEmbedBuilder();
		em.WithDescription($"{ctx.User.Mention} pokes {user.Mention} ÓwÒ");
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("slap", "Slap someone!")]
	public async static Task SlapAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} slaps {user.Mention} ÒwÓ"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("slap", new[] { "" });
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(weeurl.Url)));
		var em = new DiscordEmbedBuilder();
		em.WithDescription($"{ctx.User.Mention} slaps {user.Mention} ÒwÓ");
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		await ctx.EditResponseAsync(builder);
	}
}