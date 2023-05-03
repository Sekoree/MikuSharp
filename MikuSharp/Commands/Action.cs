using HeyRed.Mime;

using MikuSharp.Utilities;

namespace MikuSharp.Commands;

[SlashCommandGroup("action", "Actions", dmPermission: false)]
internal class Action : ApplicationCommandsModule
{
	[SlashCommand("hug", "Hug someone!")]
	public static async Task HugAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder());
		var WSH = await ctx.Client.RestClient.GetWeebShAsync("hug", new[] { "" });
		WSH.Embed.WithDescription($"{ctx.User.Mention} hugs {user.Mention} uwu");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{WSH.Extension}", WSH.ImgData);
		builder.AddEmbed(WSH.Embed.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("kiss", "Kiss someone!")]
	public static async Task KissAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder());
		var WSH = await ctx.Client.RestClient.GetWeebShAsync("kiss", new[] { "" });
		WSH.Embed.WithDescription($"{ctx.User.Mention} kisses {user.Mention} >~<");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{WSH.Extension}", WSH.ImgData);
		builder.AddEmbed(WSH.Embed.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("lick", "Lick someone!")]
	public static async Task LickAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder());
		var WSH = await ctx.Client.RestClient.GetWeebShAsync("lick", new[] { "" });
		WSH.Embed.WithDescription($"{ctx.User.Mention} licks {user.Mention} owo");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{WSH.Extension}", WSH.ImgData);
		builder.AddEmbed(WSH.Embed.Build());
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("pat", "Pat someone!")]
	public static async Task PatAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder());
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("pat", new[] { "" });
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.resizeLink(weeurl.Url)));
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
	public static async Task PokeAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder());
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("poke", new[] { "" });
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.resizeLink(weeurl.Url)));
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
	public static async Task SlapAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder());
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("slap", new[] { "" });
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.resizeLink(weeurl.Url)));
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
