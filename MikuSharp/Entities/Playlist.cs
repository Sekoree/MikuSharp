using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using FluentFTP;
using MikuSharp.Enums;
using MikuSharp.Utilities;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MikuSharp.Entities
{
    public class Playlist
    {
        public string Name { get; set; }
        public ulong UserID { get; set; }
        public ExtService ExternalService { get; set; }
        public string Url { get; set; }
        public List<PlaylistEntry> Entries { get; set; }

        public Playlist(ExtService e, string u, string n, ulong usr)
        {
            ExternalService = e;
            Url = u;
            Name = n;
            UserID = usr;
            Entries = new List<PlaylistEntry>();
        }

        public async Task GetEntries()
        {
            if (Entries.Count != 0)
            {
                Entries.Clear();
            }
            if (ExternalService == ExtService.None)
            {
                var connString = Bot.cfg.DbConnectString;
                var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();
                var cmd2 = new NpgsqlCommand($"SELECT * FROM playlistentries WHERE userid = {UserID} AND playlistname = '{Name}' ORDER BY pos ASC;", conn);
                var reader = await cmd2.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Entries.Add(new PlaylistEntry(LavalinkUtilities.DecodeTrack(Convert.ToString(reader["trackstring"])), DateTimeOffset.Parse(Convert.ToString(reader["addition"])), DateTimeOffset.Parse(Convert.ToString(reader["changed"])), Convert.ToInt32(reader["pos"])));
                }
                reader.Close();
                cmd2.Dispose();
                conn.Close();
                conn.Dispose();
            }
            else
            {
                var trs = await Bot.LLEU.First().Value.GetTracksAsync(new Uri(Url));
                int i = 0;
                foreach (var t in trs.Tracks)
                {
                    Entries.Add(new PlaylistEntry(t, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, i));
                    i++;
                }
            }
        }

        public async Task<TrackResult> GetSong(string n, CommandContext ctx)
        {
            var nodeConnection = Bot.LLEU.First().Value;
            var inter = ctx.Client.GetInteractivity();
            if (n.ToLower().StartsWith("http://nicovideo.jp")
                || n.ToLower().StartsWith("http://sp.nicovideo.jp")
                || n.ToLower().StartsWith("https://nicovideo.jp")
                || n.ToLower().StartsWith("https://sp.nicovideo.jp")
                || n.ToLower().StartsWith("http://www.nicovideo.jp")
                || n.ToLower().StartsWith("https://www.nicovideo.jp"))
            {
                var msg = await ctx.RespondAsync("Processing NND Video...");
                var split = n.Split("/".ToCharArray());
                var nndID = split.First(x => x.StartsWith("sm") || x.StartsWith("nm")).Split("?")[0];
                FtpClient client = new FtpClient(Bot.cfg.NndConfig.FtpConfig.Hostname, new NetworkCredential(Bot.cfg.NndConfig.FtpConfig.User, Bot.cfg.NndConfig.FtpConfig.Password));
                await client.ConnectAsync();
                if (!await client.FileExistsAsync($"{nndID}.mp3"))
                {
                    await msg.ModifyAsync("Preparing download...");
                    var ex = await Utilities.NND.GetNND(nndID, msg);
                    if (ex == null)
                    {
                        await msg.ModifyAsync("Please try again or verify the link");
                        return null;
                    }
                    await msg.ModifyAsync("Uploading");
                    await client.UploadAsync(ex, $"{nndID}.mp3", FtpExists.Skip, true);
                }
                var Track = await nodeConnection.GetTracksAsync(new Uri($"https://nnd.meek.moe/new/{nndID}.mp3"));
                return new TrackResult(Track.PlaylistInfo, Track.Tracks.First());
            }
            if (n.StartsWith("http://") | n.StartsWith("https://"))
            {
                var s = await nodeConnection.GetTracksAsync(new Uri(n));
                switch (s.LoadResultType)
                {
                    case LavalinkLoadResultType.LoadFailed:
                        {
                            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("Loading this song/playlist failed, please try again, reasons could be:\n" +
                                "> Playlist is set to private or unlisted\n" +
                                "> The song is unavailable/deleted").Build());
                            return null;
                        };
                    case LavalinkLoadResultType.NoMatches:
                        {
                            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("No song/playlist was found with this URL, please try again/a different one").Build());
                            return null;
                        };
                    case LavalinkLoadResultType.PlaylistLoaded:
                        {
                            if (s.PlaylistInfo.SelectedTrack == -1)
                            {
                                var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                    .WithTitle("Playlist link detected!")
                                    .WithDescription("Please respond with either:\n" +
                                    "``yes``, ``y`` or ``1`` to add the **entire** playlist or\n" +
                                    "``no``, ``n``, ``0`` or let this time out to cancel")
                                    .WithAuthor($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator} || Timeout 25 seconds", iconUrl: ctx.Member.AvatarUrl)
                                    .Build());
                                var resp = await inter.WaitForMessageAsync(x => x.Author.Id == ctx.User.Id, TimeSpan.FromSeconds(25));
                                await msg.DeleteAsync();
                                if (resp.TimedOut)
                                {
                                    return null;
                                }
                                if (resp.Result.Content == "y" || resp.Result.Content == "yes" || resp.Result.Content == "1")
                                {
                                    return new TrackResult(s.PlaylistInfo, s.Tracks);
                                }
                                else return null;
                            }
                            else
                            {
                                var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                    .WithTitle("Link with Playlist detected!")
                                    .WithDescription("Please respond with either:\n" +
                                    "``yes``, ``y`` or ``1`` to add only the referred song in the link or\n" +
                                    "``all`` or ``a`` to add the entire playlistor\n" +
                                    "``no``, ``n``, ``0`` or let this time out to cancel")
                                    .WithAuthor($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator} || Timeout 25 seconds", iconUrl: ctx.Member.AvatarUrl)
                                    .Build());
                                var resp = await inter.WaitForMessageAsync(x => x.Author.Id == ctx.User.Id, TimeSpan.FromSeconds(25));
                                await msg.DeleteAsync();
                                if (resp.TimedOut)
                                {
                                    return null;
                                }
                                if (resp.Result.Content == "y" || resp.Result.Content == "yes" || resp.Result.Content == "1")
                                {
                                    return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack));
                                }
                                if (resp.Result.Content == "a" || resp.Result.Content == "all")
                                {
                                    return new TrackResult(s.PlaylistInfo, s.Tracks);
                                }
                                else return null;
                            }
                        };
                    default:
                        {
                            return new TrackResult(s.PlaylistInfo, s.Tracks.First());
                        };
                }
            }
            else
            {
                var s = await nodeConnection.GetTracksAsync(n);
                switch (s.LoadResultType)
                {
                    case LavalinkLoadResultType.LoadFailed:
                        {
                            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("Loading this song/playlist failed, please try again, reason could be:\n" +
                                "> The song is unavailable/deleted").Build());
                            return null;
                        };
                    case LavalinkLoadResultType.NoMatches:
                        {
                            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("No song was found, please try again").Build());
                            return null;
                        };
                    default:
                        {
                            var em = new DiscordEmbedBuilder()
                                .WithTitle("Results!")
                                .WithDescription("Please select a track by responding to this with:\n")
                                .WithAuthor($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator} || Timeout 25 seconds", iconUrl: ctx.Member.AvatarUrl);
                            int leng = s.Tracks.Count();
                            if (leng > 5) leng = 5;
                            for (int i = 0; i < leng; i++)
                            {
                                em.AddField($"{i + 1}.{s.Tracks.ElementAt(i).Title} [{s.Tracks.ElementAt(i).Length}]", $"by {s.Tracks.ElementAt(i).Author} [Link]({s.Tracks.ElementAt(i).Uri})");
                            }
                            var msg = await ctx.RespondAsync(embed: em.Build());
                            var resp = await inter.WaitForMessageAsync(x => x.Author.Id == ctx.User.Id, TimeSpan.FromSeconds(25));
                            await msg.DeleteAsync();
                            if (resp.TimedOut)
                            {
                                return null;
                            }
                            if (resp.Result.Content == "1")
                            {
                                return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(0));
                            }
                            if (resp.Result.Content == "2")
                            {
                                return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(1));
                            }
                            if (resp.Result.Content == "3")
                            {
                                return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(2)); ;
                            }
                            if (resp.Result.Content == "4")
                            {
                                return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(3));
                            }
                            if (resp.Result.Content == "5")
                            {
                                return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(4));
                            }
                            else return null;
                        };
                }
            }
        }

        public async Task AddSong(string n, CommandContext ctx, int pos = -1)
        {
            if (Entries.Count == 0)
            {
                await GetEntries();
            }
            var s = await GetSong(n, ctx);
            if (pos == -1)
                foreach (var e in s.Tracks)
                    await PlaylistDB.AddEntry(Name, ctx.Member.Id, e.TrackString);
            else
            {
                s.Tracks.Reverse();
                foreach (var e in s.Tracks)
                {
                    await PlaylistDB.InsertEntry(Name, ctx.Member.Id, e.TrackString, pos);
                }
            }
        }

        public async Task ClearList()
            => await PlaylistDB.ClearList(Name, UserID);

        public async Task MoveListItems(int oldpos, int newpos)
        {
            await PlaylistDB.MoveListItems(Name, UserID, oldpos, newpos);
            Entries.Clear();
            await GetEntries();
        }

        public async Task RemoveFromList(int pos)
        {
            pos = pos - 1;
            Entries.RemoveAt(pos);
            await PlaylistDB.RemoveFromList(pos, Name, UserID);
        }

        public async Task RenameList(string newname)
        {
            await PlaylistDB.RenameList(Name, UserID, newname);
            Name = newname;
        }
    }
}
