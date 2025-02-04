using HeyRed.Mime;

using MikuSharp.Utilities;

namespace MikuSharp.Commands;

[SlashCommandGroup("action", "Actions", false, [InteractionContextType.Guild, InteractionContextType.PrivateChannel], [ApplicationCommandIntegrationTypes.GuildInstall, ApplicationCommandIntegrationTypes.UserInstall])]
internal class Action : ApplicationCommandsModule
{
	[SlashCommand("hug", "Hug someone!")]
	public static async Task HugAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} hugs {user.Mention} uwu"));
		var wsh = await ctx.Client.RestClient.GetWeebShAsync("hug", [""]);
		wsh.Embed.WithDescription($"{ctx.User.Mention} hugs {user.Mention} uwu");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{wsh.Extension}", wsh.ImgData);
		builder.AddEmbed(wsh.Embed.Build());
		await ctx.EditResponseAsync(builder);
		if (ctx.Interaction.Context is InteractionContextType.Guild)
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(user.Mention).WithAllowedMention(new UserMention(user)));
	}

	[SlashCommand("kiss", "Kiss someone!")]
	public static async Task KissAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} kisses {user.Mention} >~<"));
		var wsh = await ctx.Client.RestClient.GetWeebShAsync("kiss", [""]);
		wsh.Embed.WithDescription($"{ctx.User.Mention} kisses {user.Mention} >~<");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{wsh.Extension}", wsh.ImgData);
		builder.AddEmbed(wsh.Embed.Build());
		await ctx.EditResponseAsync(builder);
		if (ctx.Interaction.Context is InteractionContextType.Guild)
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(user.Mention).WithAllowedMention(new UserMention(user)));
	}

	[SlashCommand("lick", "Lick someone!")]
	public static async Task LickAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} licks {user.Mention} owo"));
		var wsh = await ctx.Client.RestClient.GetWeebShAsync("lick", [""]);
		wsh.Embed.WithDescription($"{ctx.User.Mention} licks {user.Mention} owo");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{wsh.Extension}", wsh.ImgData);
		builder.AddEmbed(wsh.Embed.Build());
		await ctx.EditResponseAsync(builder);
		if (ctx.Interaction.Context is InteractionContextType.Guild)
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(user.Mention).WithAllowedMention(new UserMention(user)));
	}

	[SlashCommand("pat", "Pat someone!")]
	public static async Task PatAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} pats {user.Mention} #w#"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("pat", [""]);
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(weeurl.Url)));
		var em = new DiscordEmbedBuilder();
		em.WithDescription($"{ctx.User.Mention} pats {user.Mention} #w#");
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		await ctx.EditResponseAsync(builder);
		if (ctx.Interaction.Context is InteractionContextType.Guild)
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(user.Mention).WithAllowedMention(new UserMention(user)));
	}

	[SlashCommand("poke", "Poke someone!")]
	public static async Task PokeAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} pokes {user.Mention} ÓwÒ"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("poke", [""]);
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(weeurl.Url)));
		var em = new DiscordEmbedBuilder();
		em.WithDescription($"{ctx.User.Mention} pokes {user.Mention} ÓwÒ");
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		await ctx.EditResponseAsync(builder);
		if (ctx.Interaction.Context is InteractionContextType.Guild)
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(user.Mention).WithAllowedMention(new UserMention(user)));
	}

	[SlashCommand("slap", "Slap someone!")]
	public static async Task SlapAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} slaps {user.Mention} ÒwÓ"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("slap", [""]);
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(weeurl.Url)));
		var em = new DiscordEmbedBuilder();
		em.WithDescription($"{ctx.User.Mention} slaps {user.Mention} ÒwÓ");
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		await ctx.EditResponseAsync(builder);
		if (ctx.Interaction.Context is InteractionContextType.Guild)
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(user.Mention).WithAllowedMention(new UserMention(user)));
	}

	[SlashCommand("bite", "Bite someone!")]
	public static async Task BiteAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} bites {user.Mention} x~x"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("bite", [""]);
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(weeurl.Url)));
		var em = new DiscordEmbedBuilder();
		em.WithDescription($"{ctx.User.Mention} bites {user.Mention} x~x");
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		await ctx.EditResponseAsync(builder);
		if (ctx.Interaction.Context is InteractionContextType.Guild)
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(user.Mention).WithAllowedMention(new UserMention(user)));
	}

	[SlashCommand("nom", "Nom someone!")]
	public static async Task NomAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} noms {user.Mention} >:3c"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("nom", [""]);
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(weeurl.Url)));
		var em = new DiscordEmbedBuilder();
		em.WithDescription($"{ctx.User.Mention} noms {user.Mention} >:3c");
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		await ctx.EditResponseAsync(builder);
		if (ctx.Interaction.Context is InteractionContextType.Guild)
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(user.Mention).WithAllowedMention(new UserMention(user)));
	}

	[SlashCommand("stare", "Stare at someone!")]
	public static async Task StateAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} stares {user.Mention} O.o"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("stare", [""]);
		Stream img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(Other.ResizeLink(weeurl.Url)));
		var em = new DiscordEmbedBuilder();
		em.WithDescription($"{ctx.User.Mention} stares at {user.Mention} O.o");
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		await ctx.EditResponseAsync(builder);
		if (ctx.Interaction.Context is InteractionContextType.Guild)
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(user.Mention).WithAllowedMention(new UserMention(user)));
	}
}
