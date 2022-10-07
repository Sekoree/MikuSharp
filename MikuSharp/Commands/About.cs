using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity.Extensions;

using Google.Apis.YouTube.v3.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MikuSharp.Commands;

[SlashCommandGroup("about", "About")]
internal class About : ApplicationCommandsModule
{
	[SlashCommand("donate", "Financial support information")]
	public static async Task DonateAsync(InteractionContext ctx)
	{
		var emb = new DiscordEmbedBuilder();
		emb.WithThumbnail(ctx.Client.CurrentUser.AvatarUrl).
			WithTitle("Donate Page!").
			WithAuthor("Miku MikuBot uwu").
			WithUrl("https://meek.moe/").
			WithColor(new DiscordColor("#348573")).
			WithDescription("Thank you for your interest in supporting the bot's development!\n" +
			"Here are some links that may interest you").
			AddField(new DiscordEmbedField("Patreon", "[Link](https://patreon.com/sekoree)", true)).
			AddField(new DiscordEmbedField("PayPal", "[Link](https://paypal.me/speyd3r)", true));
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(emb.Build()).AsEphemeral(ctx.Guild != null));
	}

	[SlashCommand("bot", "About the bot")]
	public static async Task BotAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null));
		var emb = new DiscordEmbedBuilder();
		emb.WithThumbnail(ctx.Client.CurrentUser.AvatarUrl).
			WithTitle($"About {ctx.Client.CurrentUser.UsernameWithDiscriminator}!").
			WithAuthor("Miku MikuBot uwu").
			WithUrl("https://meek.moe/").
			WithColor(new DiscordColor("#348573")).
			WithDescription(ctx.Client.CurrentApplication.Description);
		foreach (var member in ctx.Client.CurrentApplication.Team.Members.OrderByDescending(x => x.User.Username))
			emb.AddField(new DiscordEmbedField(member.User.Id == ctx.Client.CurrentApplication.Team.Owner.Id ? "Owner" : "Developer", member.User.UsernameWithDiscriminator));
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(emb.Build()));
	}

	[SlashCommand("news", "Get news about the bot in your server", dmPermission: false)]
	public static async Task FollowNewsAsync(InteractionContext ctx, [Option("target_channel", "Target channel to post updates."), ChannelTypes(ChannelType.Text)] DiscordChannel channel, [Option("name", "Name of webhook")] string name = "Miku Bot Announcements")
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder());
		if (!ctx.Client.CurrentApplication.Team.Members.Where(x => x.User == ctx.User).Any() && ctx.User.Id != ctx.Guild.OwnerId)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
			return;
		}
		var announcementChannel = await ctx.Client.GetChannelAsync(483290389047017482);
		var f = await announcementChannel.FollowAsync(channel);
		await Task.Delay(5000);
		var msgs = await channel.GetMessagesAsync();
		var target = msgs.First(x => x.MessageType == MessageType.ChannelFollowAdd);
		await target.DeleteAsync("Message cleanup");
		var webhooks = await channel.GetWebhooksAsync();
		var webhook = webhooks.First(x => x.Id == f.WebhookId);
		var selfAvatarUrl = ctx.Client.CurrentUser.AvatarUrl;
		var stream = await ctx.Client.RestClient.GetStreamAsync(selfAvatarUrl);
		var memoryStream = new System.IO.MemoryStream();
		await stream.CopyToAsync(memoryStream);
		memoryStream.Position = 0;
		await webhook.ModifyAsync(name, memoryStream, reason: "Dev update follow");
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"News setup complete {DiscordEmoji.FromGuildEmote(client: MikuBot.ShardedClient.GetShard(483279257431441410), id: 623933340520546306)}\n\nYou'll get the newest news about the bot in your server in {channel.Mention}!"));
	}

	[SlashCommand("feedback", "Send feedback!")]
	public static async Task FeedbackAsync(InteractionContext ctx)
	{
		DiscordInteractionModalBuilder modalBuilder = new();
		modalBuilder.WithTitle("Feedback modal");
		modalBuilder.AddTextComponent(new DiscordTextComponent(TextComponentStyle.Small, "feedbacktitle", "Title of feedback", null, 5, null, true, "Feedback"));
		modalBuilder.AddTextComponent(new DiscordTextComponent(TextComponentStyle.Paragraph, "feedbackbody", "Your feedback", null, 20, null, true, null));
		await ctx.CreateModalResponseAsync(modalBuilder);

		var res = await ctx.Client.GetInteractivity().WaitForModalAsync(modalBuilder.CustomId, TimeSpan.FromMinutes(1));
		if (!res.TimedOut)
		{
			await res.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null));
			var title = res.Result.Interaction.Data.Components.First(x => x.CustomId == "feedbacktitle").Value;
			var body = res.Result.Interaction.Data.Components.First(x => x.CustomId == "feedbackbody").Value;
			var guild = await MikuBot.ShardedClient.GetShard(483279257431441410).GetGuildAsync(id: 483279257431441410);
			var emb = new DiscordEmbedBuilder();
			emb.WithAuthor($"{ctx.User.UsernameWithDiscriminator}", iconUrl: ctx.User.AvatarUrl).
				WithTitle(title).
				WithDescription(body);
			if (ctx.Guild != null)
				emb.AddField(new DiscordEmbedField("Guild", $"{ctx.Guild.Id}", true));
			var forum = guild.GetChannel(1020433162662322257);
			List<ForumPostTag> tags = new();
			if (ctx.Guild != null)
				tags.Add(forum.AvailableTags.First(x => x.Id == 1020434799493648404));
			else
				tags.Add(forum.AvailableTags.First(x => x.Id == 1020434935502360576));
			var thread = await forum.CreatePostAsync("Feedback", new DiscordMessageBuilder().AddEmbed(emb.Build()).WithContent($"Feedback from {ctx.User.UsernameWithDiscriminator}"), null, tags, "Feedback");
			var msg = await thread.GetMessageAsync(thread.Id);
			await msg.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsdown:"));
			await msg.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
			await res.Result.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Feedback sent {DiscordEmoji.FromGuildEmote(MikuBot.ShardedClient.GetShard(483279257431441410), 623933340520546306)}"));

		}
		else
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"You were too slow :(\nThe time limit is one minute.").AsEphemeral(ctx.Guild != null));

	}

	[SlashCommand("ping", "Current ping to discord's services")]
	public static async Task PingAsync(InteractionContext ctx)
		=> await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null).WithContent($"Ping: {ctx.Client.Ping}ms"));

	[SlashCommand("which_shard", "What shard am I on?")]
	public static async Task GetExecutingShardAsync(InteractionContext ctx)
		=> await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null).WithContent($"Shard {ctx.Client.ShardId}"));

	[SlashCommand("stats", "Some stats of the MikuBot!")]
	public static async Task StatsAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null));
		int GuildCount = 0;
		int UserCount = 0;
		int ChannelCount = 0;
		foreach (var client in MikuBot.ShardedClient.ShardClients)
		{
			GuildCount += client.Value.Guilds.Count;
			foreach (var guild in client.Value.Guilds)
			{
				UserCount += guild.Value.MemberCount;
				ChannelCount += guild.Value.Channels.Count;
			}
		}
		var emb = new DiscordEmbedBuilder().
			WithTitle("Stats").
			WithDescription("Some stats of the MikuBot!").
			AddField(new DiscordEmbedField("Guilds", GuildCount.ToString(), true)).
			AddField(new DiscordEmbedField("Users", UserCount.ToString(), true)).
			AddField(new DiscordEmbedField("Channels", ChannelCount.ToString(), true)).
			AddField(new DiscordEmbedField("Ping", ctx.Client.Ping.ToString(), true)).
			AddField(new DiscordEmbedField("Lib (Version)", ctx.Client.BotLibrary + " " + ctx.Client.VersionString, true)).
			WithThumbnail(ctx.Client.CurrentUser.AvatarUrl);
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(emb.Build()));
	}

	[SlashCommand("support", "Link to my support server")]
	public static async Task SupportAsybc(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null));
		var guild = await MikuBot.ShardedClient.GetShard(483279257431441410).GetGuildAsync(id: 483279257431441410);
		var widget = await guild.GetWidgetAsync();
		var emb = new DiscordEmbedBuilder().
			WithTitle("Support Server").
			WithDescription("Need help or is something broken?").
			WithThumbnail(ctx.Client.CurrentUser.AvatarUrl);
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(emb.Build()).AddComponents(new DiscordLinkButtonComponent(widget.InstantInviteUrl, "Support Server", false, new DiscordComponentEmoji(704733597655105634))));
	}
}
