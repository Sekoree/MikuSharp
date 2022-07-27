using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.Extensions;

using Kitsu.Anime;
using Kitsu.Manga;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DisCatSharp.Exceptions;

namespace MikuSharp.Commands;

[SlashCommandGroup("utility", "Utilities")]
internal class Utility : ApplicationCommandsModule
    {
        [SlashCommandGroup("am", "Anime & Mange")]
	internal class AnimeMangaUtility : ApplicationCommandsModule
	{
		[SlashCommand("anime_search", "Search for an anime")]
		public async Task SearchAnimeAsync(InteractionContext ctx, [Option("search_query", "Search query")] string search_query)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null));
			try
			{
				var ine = ctx.Client.GetInteractivity();
				var a = await Anime.GetAnimeAsync(search_query);
				var emb = new DiscordEmbedBuilder();
				List<DiscordEmbedBuilder> res = new();
				List<Page> ress = new();
				foreach (var aa in a.Data)
				{
					emb.WithColor(new DiscordColor(0212255));
					emb.WithTitle(aa.Attributes.Titles.EnJp);
					if (aa.Attributes.Synopsis.Length != 0)
						emb.WithDescription(aa.Attributes.Synopsis);
					if (aa.Attributes.Subtype.Length != 0)
						emb.AddField(new DiscordEmbedField("Type", $"{aa.Attributes.Subtype}", true));
					if (aa.Attributes.EpisodeCount != null)
						emb.AddField(new DiscordEmbedField("Episodes", $"{aa.Attributes.EpisodeCount}", true));
					if (aa.Attributes.EpisodeLength != null)
						emb.AddField(new DiscordEmbedField("Length", $"{aa.Attributes.EpisodeLength}", true));
					if (aa.Attributes.StartDate != null)
						emb.AddField(new DiscordEmbedField("Start Date", $"{aa.Attributes.StartDate}", true));
					if (aa.Attributes.EndDate != null)
						emb.AddField(new DiscordEmbedField("End Date", $"{aa.Attributes.EndDate}", true));
					if (aa.Attributes.AgeRating != null)
						emb.AddField(new DiscordEmbedField("Age Rating", $"{aa.Attributes.AgeRating}", true));
					if (aa.Attributes.AverageRating != null)
						emb.AddField(new DiscordEmbedField("Score", $"{aa.Attributes.AverageRating}", true));
					emb.AddField(new DiscordEmbedField("NSFW", $"{aa.Attributes.Nsfw}", true));
					if (aa.Attributes.CoverImage?.Small != null) emb.WithThumbnail(aa.Attributes.CoverImage.Small);
					res.Add(emb);
					emb = new DiscordEmbedBuilder();
				}
				res.Sort((x, y) => x.Title.CompareTo(y.Title));
				int i = 1;
				foreach (var aa in res)
				{
					aa.WithFooter($"via Kitsu.io -- Page {i}/{a.Data.Count}", "https://kitsu.io/kitsu-256-ed442f7567271af715884ca3080e8240.png");
					ress.Add(new Page(embed: aa));
					i++;
				}
				await ine.SendPaginatedResponseAsync(ctx.Interaction, true, ctx.Guild != null, ctx.User, ress, behaviour: PaginationBehaviour.WrapAround, deletion: ButtonPaginationBehavior.Disable);
			}
			catch (Exception ex)
			{
				ctx.Client.Logger.LogError(ex.Message);
				ctx.Client.Logger.LogError(ex.StackTrace);
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No Anime found!"));
			}
		}
		
		[SlashCommand("manga_search", "Search for an manga")]
		public async Task SearchMangaAsync(InteractionContext ctx, [Option("search_query", "Search query")] string search_query)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null));
			try
			{
				var ine = ctx.Client.GetInteractivity();
				var a = await Manga.GetMangaAsync(search_query);
				var emb = new DiscordEmbedBuilder();
				List<DiscordEmbedBuilder> res = new();
				List<Page> ress = new();
				foreach (var aa in a.Data)
				{
					emb.WithColor(new DiscordColor(0212255));
					emb.WithTitle(aa.Attributes.Titles.EnJp);
					if (aa.Attributes.Synopsis != null)
						emb.WithDescription(aa.Attributes.Synopsis);
					if (aa.Attributes.Subtype != null)
						emb.AddField(new DiscordEmbedField("Type", $"{aa.Attributes.Subtype}", true));
					if (aa.Attributes.StartDate != null)
						emb.AddField(new DiscordEmbedField("Start Date", $"{aa.Attributes.StartDate}", true));
					if (aa.Attributes.EndDate != null)
						emb.AddField(new DiscordEmbedField("End Date", $"{aa.Attributes.EndDate}", true));
					if (aa.Attributes.AgeRating != null)
						emb.AddField(new DiscordEmbedField("Age Rating", $"{aa.Attributes.AgeRating}", true));
					if (aa.Attributes.AverageRating != null)
						emb.AddField(new DiscordEmbedField("Score", $"{aa.Attributes.AverageRating}", true));
					if (aa.Attributes.CoverImage?.Small != null)
						emb.WithThumbnail(aa.Attributes.CoverImage.Small);
					emb.WithFooter("via Kitsu.io", "https://kitsu.io/kitsu-256-ed442f7567271af715884ca3080e8240.png");
					res.Add(emb);
					emb = new DiscordEmbedBuilder();
				}
				res.Sort((x, y) => x.Title.CompareTo(y.Title));
				int i = 1;
				foreach (var aa in res)
				{
					aa.WithFooter($"via Kitsu.io -- Page {i}/{a.Data.Count}", "https://kitsu.io/kitsu-256-ed442f7567271af715884ca3080e8240.png");
					ress.Add(new Page(embed: aa));
					i++;
				}
				await ine.SendPaginatedResponseAsync(ctx.Interaction, true, ctx.Guild != null, ctx.User, ress, behaviour: PaginationBehaviour.WrapAround, deletion: ButtonPaginationBehavior.Disable);
			}
			catch (Exception ex)
			{
				ctx.Client.Logger.LogError(ex.Message);
				ctx.Client.Logger.LogError(ex.StackTrace);
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No Manga found!"));
			}
		}
	}

	[SlashCommandGroup("discord", "Discord Utilities")]
	internal class DiscordUtility : ApplicationCommandsModule
	{
		[SlashCommand("avatar", "Get the avatar of someone or yourself")]
		public async Task GetAvatarAsync(InteractionContext ctx, [Option("user", "User to get the avatar from")] DiscordUser? user = null)
			=> await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null).AddEmbed(new DiscordEmbedBuilder().WithImageUrl(user != null ? user.AvatarUrl : ctx.User.AvatarUrl).Build()));

		[SlashCommand("server_info", "Get information about the server")]
		public async Task GuildInfoAsync(InteractionContext ctx)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null));

			if (ctx.Guild == null)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You have to execute this command on a server!"));
				return;
			}

			var members = await ctx.Guild.GetAllMembersAsync();
			var bots = members.Where(x => x.IsBot).Count();

			var emb = new DiscordEmbedBuilder();
			emb.WithTitle(ctx.Guild.Name);
			emb.WithColor(new DiscordColor(0212255));
			emb.WithThumbnail(ctx.Guild.IconUrl);
			emb.AddField(new DiscordEmbedField("Owner", ctx.Guild.Owner.Mention, true));
			emb.AddField(new DiscordEmbedField("Language", ctx.Guild.PreferredLocale, true));
			emb.AddField(new DiscordEmbedField("ID", ctx.Guild.Id.ToString(), true));
			emb.AddField(new DiscordEmbedField("Created At", Formatter.Timestamp(ctx.Guild.CreationTimestamp, TimestampFormat.LongDateTime), true));
			emb.AddField(new DiscordEmbedField("Emojis", ctx.Guild.Emojis.Count.ToString(), true));
			emb.AddField(new DiscordEmbedField("Members (Bots)", $"{members.Count} ({bots})", true));
			
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(emb.Build()));
		}

		[SlashCommand("user_info", "Get information about a user")]
		public async Task UserInfoAsync(InteractionContext ctx, [Option("user", "The user to view")] DiscordUser? user = null)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null));

			if (user == null)
				user = ctx.User;

			DiscordMember? member = null;
			if (ctx.Guild != null)
			{
				try
				{
					member = await user.ConvertToMember(ctx.Guild);
				}
				catch (NotFoundException) { }
			}

			var emb = new DiscordEmbedBuilder();
			emb.WithColor(new DiscordColor(0212255));
			emb.WithTitle("User Info");
			emb.AddField(new DiscordEmbedField("Username", $"{user.Username}#{user.Discriminator}", true));
			if (member != null)
				if (member.DisplayName != user.Username)
					emb.AddField(new DiscordEmbedField("Nickname", $"{member.DisplayName}", true));
			emb.AddField(new DiscordEmbedField("ID", $"{user.Id}", true));
			emb.AddField(new DiscordEmbedField("Account Creation", $"{Formatter.Timestamp(user.CreationTimestamp)}", true));
			if (member != null)
				emb.AddField(new DiscordEmbedField("Join Date", $"{Formatter.Timestamp(member.JoinedAt)}", true));
			emb.WithThumbnail(user.AvatarUrl);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(emb.Build()));
		}

		[SlashCommand("emojilist", "Lists all custom emoji on this server")]
		public async Task EmojiListAsync(InteractionContext ctx)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(ctx.Guild != null));
			string wat = "You have to execute this command in a server!";
			if (ctx.Guild != null && ctx.Guild.Emojis.Any())
			{
				wat = "**Emojies:** ";
				foreach (var em in ctx.Guild.Emojis.Values)
				{
					wat += em + " ";
				}
			}
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(wat));
		}
	}
}
