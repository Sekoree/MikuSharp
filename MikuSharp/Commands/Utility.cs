using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kitsu.Anime;
using Kitsu.Manga;
using Kitsu;

namespace MikuSharp.Commands
{
    class Utility : BaseCommandModule
    {
        [Command("anime")]
        [Description("Search for an anime")]
        public async Task AnimeGet(CommandContext ctx, [RemainingText] string search)
        {
            try
            {
                var ine = ctx.Client.GetInteractivity();
                var a = await Anime.GetAnimeAsync(search);
                var emb = new DiscordEmbedBuilder();
                List<DiscordEmbedBuilder> res = new List<DiscordEmbedBuilder>();
                List<Page> ress = new List<Page>();
                foreach (var aa in a.Data)
                {
                    emb.WithColor(new DiscordColor(0212255));
                    emb.WithTitle(aa.Attributes.Titles.EnJp);
                    if (aa.Attributes.Synopsis.Length != 0) emb.WithDescription(aa.Attributes.Synopsis);
                    if (aa.Attributes.Subtype.Length != 0) emb.AddField("Type", $"{aa.Attributes.Subtype}", true);
                    if (aa.Attributes.EpisodeCount != null)emb.AddField("Episodes", $"{aa.Attributes.EpisodeCount}", true);
                    if (aa.Attributes.EpisodeLength != null)emb.AddField("Length", $"{aa.Attributes.EpisodeLength}", true);
                    if (aa.Attributes.StartDate != null) emb.AddField("Start Date", $"{aa.Attributes.StartDate}", true);
                    if (aa.Attributes.EndDate != null) emb.AddField("End Date", $"{aa.Attributes.EndDate}", true);
                    if (aa.Attributes.AgeRating != null) emb.AddField("Age Rating", $"{aa.Attributes.AgeRating}", true);
                    if (aa.Attributes.AverageRating != null) emb.AddField("Score", $"{aa.Attributes.AverageRating}", true);
                    emb.AddField("NSFW", $"{aa.Attributes.Nsfw}", true);
                    if (aa.Attributes.CoverImage?.Small != null) emb.WithThumbnailUrl(aa.Attributes.CoverImage.Small);
                    res.Add(emb);
                    emb = new DiscordEmbedBuilder();
                }
                res.Sort((x,y) => x.Title.CompareTo(y.Title));
                int i = 1;
                foreach(var aa in res)
                {
                    aa.WithFooter($"via Kitsu.io -- Page {i}/{a.Data.Count}", "https://kitsu.io/kitsu-256-ed442f7567271af715884ca3080e8240.png");
                    ress.Add(new Page(embed: aa));
                    i++;
                }
                await ine.SendPaginatedMessageAsync(ctx.Channel, ctx.User, ress, timeoutoverride:TimeSpan.FromMinutes(2.5));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                var ee = new DiscordEmbedBuilder();
                ee.WithTitle("Error");
                ee.WithDescription(ex.Message + "\n" + ex.StackTrace);
                await ctx.RespondAsync("No Anime Found!", embed: ee.Build());
            }
        }

        [Command("avatar")]
        [Description("Get the avatar of someone or yourself")]
        [Priority(2)]
        public async Task Avatar(CommandContext ctx, DiscordMember member = null)
        {
            string avartURL = ctx.Member.AvatarUrl;
            if (member != null)
            {
                avartURL = member.AvatarUrl;
            }
            var embed2 = new DiscordEmbedBuilder { };
            embed2.WithImageUrl(avartURL);
            await ctx.RespondAsync(embed: embed2.Build());
        }

        [Command("avatar")]
        [Priority(1)]
        public async Task Avatar(CommandContext ctx, string member)
        {
            var AvatarUser = ctx.Guild.Members.Where(x => x.Value.Username.ToLower().Contains(member) | x.Value.DisplayName.ToLower().Contains(member));
            var embed2 = new DiscordEmbedBuilder { };
            embed2.WithImageUrl(AvatarUser.First().Value.AvatarUrl);
            await ctx.RespondAsync(embed: embed2.Build());
        }

        [Command("emojilist")]
        [Description("Lists all custom emoji on this server")]
        public async Task EmojiList(CommandContext ctx)
        {
            string wat = null;
            foreach (var em in ctx.Guild.Emojis)
            {
                wat += em + "";
            }
            await ctx.RespondAsync(wat);
        }

        [Command("guildinfo")]
        [Description("Get some info about this guild")]
        public async Task GuildInfo(CommandContext ctx)
        {
            var emb = new DiscordEmbedBuilder();
            emb.WithTitle(ctx.Guild.Name);
            emb.WithColor(new DiscordColor(0212255));
            emb.WithThumbnailUrl(ctx.Guild.IconUrl);
            emb.AddField("Owner",ctx.Guild.Owner.Mention, true);
            emb.AddField("Region",ctx.Guild.VoiceRegion.Name, true);
            emb.AddField("ID",ctx.Guild.Id.ToString(), true);
            emb.AddField("Created At",ctx.Guild.CreationTimestamp.ToString(), true);
            emb.AddField("Emojis",ctx.Guild.Emojis.Count.ToString(), true);
            emb.AddField("Members(without Bots)",$"{ctx.Guild.MemberCount}({ctx.Guild.Members.Where(x => !x.Value.IsBot).Count()})", true);
            await ctx.RespondAsync(embed: emb.Build());
        }

        [Command("manga")]
        [Description("Search for a manga")]
        public async Task MangaGet(CommandContext ctx, [RemainingText] string search)
        {
            try
            {
                var ine = ctx.Client.GetInteractivity();
                var a = await Manga.GetMangaAsync(search);
                var emb = new DiscordEmbedBuilder();
                List<DiscordEmbedBuilder> res = new List<DiscordEmbedBuilder>();
                List<Page> ress = new List<Page>();
                foreach (var aa in a.Data)
                {
                    emb.WithColor(new DiscordColor(0212255));
                    emb.WithTitle(aa.Attributes.Titles.EnJp);
                    if (aa.Attributes.Synopsis != null)emb.WithDescription(aa.Attributes.Synopsis);
                    if (aa.Attributes.Subtype != null) emb.AddField("Type", $"{aa.Attributes.Subtype}", true);
                    if (aa.Attributes.StartDate != null) emb.AddField("Start Date", $"{aa.Attributes.StartDate}", true);
                    if (aa.Attributes.EndDate != null) emb.AddField("End Date", $"{aa.Attributes.EndDate}", true);
                    if (aa.Attributes.AgeRating != null) emb.AddField("Age Rating", $"{aa.Attributes.AgeRating}", true);
                    if (aa.Attributes.AverageRating != null) emb.AddField("Score", $"{aa.Attributes.AverageRating}", true);
                    if (aa.Attributes.CoverImage?.Small != null) emb.WithThumbnailUrl(aa.Attributes.CoverImage.Small);
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
                await ine.SendPaginatedMessageAsync(ctx.Channel, ctx.User, ress, timeoutoverride: TimeSpan.FromMinutes(2.5));
            }
            catch
            {
                await ctx.RespondAsync("No Manga Found!");
            }
        }

        [Command("userinfo")]
        [Priority(2)]
        [Description("Get some info about a user or yourself")]
        public async Task UserInfo(CommandContext ctx, DiscordMember m)
        {
            Console.WriteLine("VIA MEMBER");
            var emb = new DiscordEmbedBuilder();
            emb.WithColor(new DiscordColor(0212255));
            emb.WithTitle("User Info");
            emb.AddField("Username", $"{m.Username}#{m.Discriminator}", true);
            if (m.DisplayName != m.Username)emb.AddField("Nickname", $"{m.DisplayName}", true);
            emb.AddField("ID", $"{m.Id}", true);
            emb.AddField("Status", $"{m.Presence.Status}", true);
            emb.AddField("Account Creation", $"{m.CreationTimestamp}", true);
            emb.WithThumbnailUrl(m.AvatarUrl);
            await ctx.RespondAsync(embed: emb.Build());
        }

        [Command("userinfo")]
        [Priority(1)]
        public async Task UserInfo(CommandContext ctx, string me = null)
        {
            Console.WriteLine("VIA String");
            var m = ctx.Member;
            if (me != null){
                m = ctx.Guild.Members.First(x => x.Value.Username.ToLower().Contains(me) | x.Value.DisplayName.ToLower().Contains(me)).Value;
                if (m == null) return;
            }
            var emb = new DiscordEmbedBuilder();
            emb.WithColor(new DiscordColor(0212255));
            emb.WithTitle("User Info");
            emb.AddField("Username", $"{m.Username}#{m.Discriminator}", true);
            if (m.DisplayName != m.Username) emb.AddField("Nickname", $"{m.DisplayName}", true);
            emb.AddField("ID", $"{m.Id}", true);
            emb.AddField("Status", $"{m.Presence.Status}", true);
            emb.AddField("Account Creation", $"{m.CreationTimestamp}", true);
            emb.WithThumbnailUrl(m.AvatarUrl);
            await ctx.RespondAsync(embed: emb.Build());
        }
    }
}
