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
	public static async Task HugAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} hugs {user.Mention} uwu"));
		var wsh = await ctx.Client.RestClient.GetWeebShAsync("hug", new[] { "" });

		DiscordWebhookBuilder builder = new();
		builder.WithContent($"{ctx.User.Mention} hugs {user.Mention} uwu");
		builder.AddFile($"image.{wsh.Extension}", wsh.ImgData);
		builder.AddEmbed(wsh.Embed.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("kiss", "Kiss someone!")]
	public static async Task KissAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} kisses {user.Mention} >~<"));
		var wsh = await ctx.Client.RestClient.GetWeebShAsync("kiss", new[] { "" });

		DiscordWebhookBuilder builder = new();
		builder.WithContent($"{ctx.User.Mention} kisses {user.Mention} >~<");
		builder.AddFile($"image.{wsh.Extension}", wsh.ImgData);
		builder.AddEmbed(wsh.Embed.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("lick", "Lick someone!")]
	public static async Task LickAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} licks {user.Mention} owo"));
		var wsh = await ctx.Client.RestClient.GetWeebShAsync("lick", new[] { "" });

		DiscordWebhookBuilder builder = new();
		builder.WithContent($"{ctx.User.Mention} licks {user.Mention} owo");
		builder.AddFile($"image.{wsh.Extension}", wsh.ImgData);
		builder.AddEmbed(wsh.Embed.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("pat", "Pat someone!")]
	public static async Task PatAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} pats {user.Mention} #w#"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("pat", new[] { "" });
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(weeurl.Url)));
		var em = new DiscordEmbedBuilder();
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.WithContent($"{ctx.User.Mention} pats {user.Mention} #w#");
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("poke", "Poke someone!")]
	public static async Task PokeAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} pokes {user.Mention} ÓwÒ"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("poke", new[] { "" });
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(weeurl.Url)));
		var em = new DiscordEmbedBuilder();
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.WithContent($"{ctx.User.Mention} pokes {user.Mention} ÓwÒ");
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("slap", "Slap someone!")]
	public static async Task SlapAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} slaps {user.Mention} ÒwÓ"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("slap", new[] { "" });
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(weeurl.Url)));
		var em = new DiscordEmbedBuilder();
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.WithContent($"{ctx.User.Mention} slaps {user.Mention} ÒwÓ");
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		await ctx.EditResponseAsync(builder);
	}
}