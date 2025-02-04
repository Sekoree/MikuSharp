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
		var wsh = await ctx.Client.RestClient.GetWeebShAsync("hug");
		if (wsh is null)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent("Failed to get image"));
			return;
		}

		wsh.Embed.WithDescription($"{ctx.User.Mention} hugs {user.Mention} uwu");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{wsh.Extension}", wsh.ImgData);
		builder.AddEmbed(wsh.Embed.Build());
		builder.WithContent(user.Mention);
		builder.WithAllowedMention(new UserMention(user));
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("kiss", "Kiss someone!")]
	public static async Task KissAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} kisses {user.Mention} >~<"));
		var wsh = await ctx.Client.RestClient.GetWeebShAsync("kiss");
		if (wsh is null)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent("Failed to get image"));
			return;
		}

		wsh.Embed.WithDescription($"{ctx.User.Mention} kisses {user.Mention} >~<");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{wsh.Extension}", wsh.ImgData);
		builder.AddEmbed(wsh.Embed.Build());
		builder.WithContent(user.Mention);
		builder.WithAllowedMention(new UserMention(user));
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("lick", "Lick someone!")]
	public static async Task LickAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} licks {user.Mention} owo"));
		var wsh = await ctx.Client.RestClient.GetWeebShAsync("lick");
		if (wsh is null)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent("Failed to get image"));
			return;
		}

		wsh.Embed.WithDescription($"{ctx.User.Mention} licks {user.Mention} owo");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{wsh.Extension}", wsh.ImgData);
		builder.AddEmbed(wsh.Embed.Build());
		builder.WithContent(user.Mention);
		builder.WithAllowedMention(new UserMention(user));
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("pat", "Pat someone!")]
	public static async Task PatAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} pats {user.Mention} #w#"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("pat", []);
		if (weeurl is null)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent("Failed to get image"));
			return;
		}

		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(weeurl.Url.ResizeLink()));
		var em = new DiscordEmbedBuilder();
		em.WithDescription($"{ctx.User.Mention} pats {user.Mention} #w#");
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		builder.WithContent(user.Mention);
		builder.WithAllowedMention(new UserMention(user));
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("poke", "Poke someone!")]
	public static async Task PokeAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} pokes {user.Mention} ÓwÒ"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("poke", []);
		if (weeurl is null)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent("Failed to get image"));
			return;
		}

		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(weeurl.Url.ResizeLink()));
		var em = new DiscordEmbedBuilder();
		em.WithDescription($"{ctx.User.Mention} pokes {user.Mention} ÓwÒ");
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		builder.WithContent(user.Mention);
		builder.WithAllowedMention(new UserMention(user));
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("slap", "Slap someone!")]
	public static async Task SlapAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} slaps {user.Mention} ÒwÓ"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("slap", []);
		if (weeurl is null)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent("Failed to get image"));
			return;
		}

		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(weeurl.Url.ResizeLink()));
		var em = new DiscordEmbedBuilder();
		em.WithDescription($"{ctx.User.Mention} slaps {user.Mention} ÒwÓ");
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		builder.WithContent(user.Mention);
		builder.WithAllowedMention(new UserMention(user));
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("bite", "Bite someone!")]
	public static async Task BiteAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} bites {user.Mention} x~x"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("bite", []);
		if (weeurl is null)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent("Failed to get image"));
			return;
		}

		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(weeurl.Url.ResizeLink()));
		var em = new DiscordEmbedBuilder();
		em.WithDescription($"{ctx.User.Mention} bites {user.Mention} x~x");
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		builder.WithContent(user.Mention);
		builder.WithAllowedMention(new UserMention(user));
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("nom", "Nom someone!")]
	public static async Task NomAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} noms {user.Mention} >:3c"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("nom", []);
		if (weeurl is null)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent("Failed to get image"));
			return;
		}

		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(weeurl.Url.ResizeLink()));
		var em = new DiscordEmbedBuilder();
		em.WithDescription($"{ctx.User.Mention} noms {user.Mention} >:3c");
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		builder.WithContent(user.Mention);
		builder.WithAllowedMention(new UserMention(user));
		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("stare", "Stare at someone!")]
	public static async Task StateAsync(InteractionContext ctx, [Option("user", "The user to execute the action with")] DiscordUser user)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{ctx.User.Mention} stares {user.Mention} O.o"));
		var weeurl = await MikuBot.WeebClient.GetRandomAsync("stare", []);
		if (weeurl is null)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent("Failed to get image"));
			return;
		}

		var img = new MemoryStream(await ctx.Client.RestClient.GetByteArrayAsync(weeurl.Url.ResizeLink()));
		var em = new DiscordEmbedBuilder();
		em.WithDescription($"{ctx.User.Mention} stares at {user.Mention} O.o");
		em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
		em.WithFooter("by nekos.life");

		DiscordWebhookBuilder builder = new();
		builder.AddFile($"image.{MimeGuesser.GuessExtension(img)}", img);
		builder.AddEmbed(em.Build());
		builder.WithContent(user.Mention);
		builder.WithAllowedMention(new UserMention(user));
		await ctx.EditResponseAsync(builder);
	}
}
