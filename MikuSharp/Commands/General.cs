using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.Extensions;

using MikuSharp.Attributes;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MikuSharp.Commands;

internal class General : ApplicationCommandsModule
{
    private readonly string botdev = "David#9179";
    private readonly string curbotdev = "Sekoree#3939";

    [SlashCommand("donate", "Financial support information")]
    public async Task DonateAsync(InteractionContext ctx)
    {
        var emb = new DiscordEmbedBuilder();
        emb.WithThumbnail(ctx.Client.CurrentUser.AvatarUrl).
            WithTitle("Donate Page!").
            WithAuthor("Miku MikuBot uwu").
            WithUrl("https://meek.moe/").
            WithColor(new DiscordColor("#348573")).
            WithDescription("Thank you for your interest in supporting the bot's development!\n" +
            "Here are some links that may interest you").
            AddField(new DiscordEmbedField("Patreon", "[Link](https://patreon.com/speyd3r)", true)).
            AddField(new DiscordEmbedField("PayPal", "[Link](https://paypal.me/speyd3r)", true));
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(emb.Build()).AsEphemeral(ctx.Guild != null));
    }
	
    [SlashCommand("feedback", "Send feedback!")]
    public async Task FeedbackAsync(InteractionContext ctx, [Option("feedback", "The feedback you want to send")] string feedback)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null));
        var guild = await MikuBot.ShardedClient.GetShard(483279257431441410).GetGuildAsync(id: 483279257431441410);
        var emb = new DiscordEmbedBuilder();
        emb.WithAuthor(name: $"{ctx.User.UsernameWithDiscriminator}", iconUrl: ctx.User.AvatarUrl).
            WithTitle(title: "Feedback").
            WithDescription(feedback).
            WithFooter(text: $"Sent from {ctx.Guild?.Name ?? "DM"}");
        emb.AddField(new DiscordEmbedField(name: "User", value: $"{ctx.User.Mention}", inline: true));
        if (ctx.Guild != null)
            emb.AddField(new DiscordEmbedField(name: "Guild", value: $"{ctx.Guild.Id}", inline: true));
        var embed = await guild.GetChannel(484698873411928075).SendMessageAsync(embed: emb.Build());
        await embed.CreateReactionAsync(DiscordEmoji.FromName(client: ctx.Client, name: ":thumbsup:"));
        await embed.CreateReactionAsync(DiscordEmoji.FromName(client: ctx.Client, name: ":thumbsdown:"));
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Feedback sent {DiscordEmoji.FromGuildEmote(client: MikuBot.ShardedClient.GetShard(483279257431441410), id: 623933340520546306)}"));
    }
	
    [SlashCommand("ping", "Current ping to discord's services")]
    public async Task PingAsync(InteractionContext ctx)
		=> await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null).WithContent($"Ping: {ctx.Client.Ping}ms"));

	[SlashCommand("shard", "What shard am I on?")]
    public async Task ShardAsync(InteractionContext ctx)
		=> await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null).WithContent($"Shard {ctx.Client.ShardId}"));

    [SlashCommand("stats", "Some stats of the MikuBot!")]
    public async Task StatsAsync(InteractionContext ctx)
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
    public async Task SupportAsybc(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null));
		var guild = await MikuBot.ShardedClient.GetShard(483279257431441410).GetGuildAsync(id: 483279257431441410);
        var widget = await guild.GetWidgetAsync();
		var emb = new DiscordEmbedBuilder().
            WithTitle("Support Server").
            WithDescription("Need help or is something broken?\n\n" +
            $"[Join the support server]({widget.InstantInviteUrl})").
            WithThumbnail(ctx.Client.CurrentUser.AvatarUrl);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(emb.Build()));
    }
}
