using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity.Extensions;
using DisCatSharp.Lavalink;

using FluentFTP;

using MikuSharp.Entities;
using MikuSharp.Enums;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MikuSharp.Utilities
{
    public class PlaylistDB
    {
        public static async Task<Dictionary<string,Playlist>> GetPlaylists(DiscordGuild guild, ulong u)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"SELECT * FROM playlists WHERE userid = {u} ORDER BY playlistname ASC;", conn);
            var reader = await cmd2.ExecuteReaderAsync();
            Dictionary<string, Playlist> lists = new Dictionary<string, Playlist>(); 
            while (await reader.ReadAsync())
            {
                lists.Add(Convert.ToString(reader["playlistname"]), await GetPlaylist(guild, u, Convert.ToString(reader["playlistname"])));
            }
            reader.Close();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
            return lists;
        }

        public static async Task<List<string>> GetPlaylistsSimple(ulong u)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"SELECT * FROM playlists WHERE userid = {u} ORDER BY playlistname ASC;", conn);
            var reader = await cmd2.ExecuteReaderAsync();
            List<string> lists = new List<string>();
            while (await reader.ReadAsync())
            {
                lists.Add(Convert.ToString(reader["playlistname"]));
            }
            reader.Close();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
            return lists;
        }

        public static async Task<Playlist> GetPlaylist(DiscordGuild guild, ulong u, string p)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            int am = 0;
            var cmd = new NpgsqlCommand($"SELECT COUNT(*)" +
                $"FROM playlistentries " +
                $"WHERE userid = {u} " +
                $"AND playlistname = @pl;", conn);
            cmd.Parameters.AddWithValue("pl", p);
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                am = Convert.ToInt32(reader["count"]);
            }
            reader.Close();
            cmd.Dispose();
            var cmd2 = new NpgsqlCommand($"SELECT *" +
                $"FROM playlists " +
                $"WHERE userid = {u} " +
                $"AND playlistname = @pl;", conn);
            cmd2.Parameters.AddWithValue("pl", p);
            var reader2 = await cmd2.ExecuteReaderAsync();
            Playlist pl = null;
            while (await reader2.ReadAsync())
            {
                if (Other.getExtService(Convert.ToString(reader2["extservice"])) != ExtService.None)
                {
                    try
                    {
                        var ss = await Bot.LLEU.First().Value.Rest.GetTracksAsync(new Uri(Convert.ToString(reader2["url"])));
                        am = ss.Tracks.Count();
                    }
                    catch { }
                }
                pl = new Playlist(Other.getExtService(Convert.ToString(reader2["extservice"])), Convert.ToString(reader2["url"]), Convert.ToString(reader2["playlistname"]), Convert.ToUInt64(reader2["userid"]), am, DateTimeOffset.Parse(Convert.ToString(reader2["creation"])), DateTimeOffset.Parse(Convert.ToString(reader2["changed"])));
            }
            reader2.Close();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
            if (pl == null) throw new Exception("Tf is up? " + p);
            return pl;
        }

        public static async Task ReorderList(DiscordGuild guild, string p, ulong u)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var listNow = await GetPlaylist(guild, u, p);
            var ln = await listNow.GetEntries();
            var cmd = new NpgsqlCommand($"DELETE FROM playlistentries WHERE userid = {u} AND playlistname = @pl;", conn);
            cmd.Parameters.AddWithValue("pl", p);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            int i = 0;
            string longcmd = "";
            foreach (var qi in ln)
            {
                string adddate = $"{qi.additionDate.UtcDateTime.Year}-{qi.additionDate.UtcDateTime.Month}-{qi.additionDate.UtcDateTime.Day} {qi.additionDate.UtcDateTime.Hour}:{qi.additionDate.UtcDateTime.Minute}:{qi.additionDate.UtcDateTime.Second}";
                string moddate = $"{qi.modifyDate.UtcDateTime.Year}-{qi.modifyDate.UtcDateTime.Month}-{qi.modifyDate.UtcDateTime.Day} {qi.modifyDate.UtcDateTime.Hour}:{qi.modifyDate.UtcDateTime.Minute}:{qi.modifyDate.UtcDateTime.Second}";
                longcmd += $"INSERT INTO playlistentries VALUES ({i},@p,@u,{qi.track.TrackString},'{adddate}','{moddate}');";
                i++;
            }
            var cmd2 = new NpgsqlCommand(longcmd, conn);
            cmd2.Parameters.AddWithValue("p", p);
            cmd2.Parameters.AddWithValue("u", Convert.ToInt64(u));
            await cmd2.ExecuteNonQueryAsync();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task RebuildList(ulong u, string p, List<PlaylistEntry> q)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var queueNow = q;
            var cmd = new NpgsqlCommand($"DELETE FROM playlistentries WHERE userid = {u} AND playlistname = @pl;", conn);
            cmd.Parameters.AddWithValue("pl", p);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            int i = 0;
            string longcmd = "";
            foreach (var qi in queueNow)
            {
                string adddate = $"{qi.additionDate.UtcDateTime.Year}-{qi.additionDate.UtcDateTime.Month}-{qi.additionDate.UtcDateTime.Day} {qi.additionDate.UtcDateTime.Hour}:{qi.additionDate.UtcDateTime.Minute}:{qi.additionDate.UtcDateTime.Second}";
                string moddate = $"{qi.modifyDate.UtcDateTime.Year}-{qi.modifyDate.UtcDateTime.Month}-{qi.modifyDate.UtcDateTime.Day} {qi.modifyDate.UtcDateTime.Hour}:{qi.modifyDate.UtcDateTime.Minute}:{qi.modifyDate.UtcDateTime.Second}";
                longcmd += $"INSERT INTO playlistentries VALUES ({i},@p,@u,{qi.track.TrackString},'{adddate}','{moddate}');";
                i++;
            }
            var cmd2 = new NpgsqlCommand(longcmd, conn);
            cmd2.Parameters.AddWithValue("p", p);
            cmd2.Parameters.AddWithValue("u", Convert.ToInt64(u));
            await cmd2.ExecuteNonQueryAsync();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task AddPlaylist(string p, ulong u, ExtService e = ExtService.None, string url = "")
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand("INSERT INTO playlists VALUES (@u,@p,@url,@ext,@cre,@mody)", conn);
            cmd.Parameters.AddWithValue("u", Convert.ToInt64(u));
            cmd.Parameters.AddWithValue("p", p);
            cmd.Parameters.AddWithValue("url", url);
            cmd.Parameters.AddWithValue("ext", e.ToString());
            cmd.Parameters.AddWithValue("cre", DateTimeOffset.UtcNow);
            cmd.Parameters.AddWithValue("mody", DateTimeOffset.UtcNow);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task RemovePlaylist(string p, ulong u)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            Console.WriteLine("delete init");
            var cmd = new NpgsqlCommand($"DELETE FROM playlists WHERE playlistname = @pl AND userid = {u};", conn);
            cmd.Parameters.AddWithValue("pl", p);
            await cmd.ExecuteNonQueryAsync();
            await ClearList(p, u);
            Console.WriteLine("delete yes?");
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task AddEntry(string p, ulong u, string ts)
        {
            int position = 0;
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"SELECT Count(*) FROM playlistentries WHERE userid = {u} AND playlistname = @pl;", conn);
            cmd2.Parameters.AddWithValue("pl", p);
            var reader = await cmd2.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                position = reader.GetInt32(0);
            }
            Console.WriteLine(position);
            reader.Close();
            cmd2.Dispose();
            var cmd = new NpgsqlCommand("INSERT INTO playlistentries VALUES (@pos,@p,@u,@ts,@add,@mody);UPDATE playlists SET changed=@mody WHERE userid=@u AND playlistname=@p;", conn);
            cmd.Parameters.AddWithValue("pos", position);
            cmd.Parameters.AddWithValue("p", p);
            cmd.Parameters.AddWithValue("u", Convert.ToInt64(u));
            cmd.Parameters.AddWithValue("ts", ts);
            cmd.Parameters.AddWithValue("add", DateTimeOffset.UtcNow);
            cmd.Parameters.AddWithValue("mody", DateTimeOffset.UtcNow);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task AddEntry(string p, ulong u, List<LavalinkTrack> ts)
        {
            int position = 0;
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"SELECT Count(*) FROM playlistentries WHERE userid = {u} AND playlistname = @pl;", conn);
            cmd2.Parameters.AddWithValue("pl", p);
            var reader = await cmd2.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                position = reader.GetInt32(0);
            }
            Console.WriteLine(position);
            reader.Close();
            cmd2.Dispose();
            string longcmd = "UPDATE playlists SET changed=@mody WHERE userid=@u AND playlistname=@p;";
            foreach (var tt in ts)
            {
                longcmd += $"INSERT INTO playlistentries VALUES ({position},@p,@u,'{tt.TrackString}',@add,@mody);";
                position++;
            }
            var cmd = new NpgsqlCommand(longcmd, conn);
            cmd.Parameters.AddWithValue("p", p);
            cmd.Parameters.AddWithValue("u", Convert.ToInt64(u));
            cmd.Parameters.AddWithValue("add", DateTimeOffset.UtcNow);
            cmd.Parameters.AddWithValue("mody", DateTimeOffset.UtcNow);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task InsertEntry(DiscordGuild guild, string p, ulong u, string ts, int pos)
        {
            var qnow = await GetPlaylist(guild, u, p);
            var q = await qnow.GetEntries();
            q.Insert(pos, new PlaylistEntry(LavalinkUtilities.DecodeTrack(ts), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
            await RebuildList(u, p, q);
        }

        public static async Task InsertEntry(DiscordGuild guild, string p, ulong u, List<LavalinkTrack> ts, int pos)
        {
            var qnow = await GetPlaylist(guild, u, p);
            var q = await qnow.GetEntries();
            foreach (var tt in ts)
            {
                q.Insert(pos, new PlaylistEntry(LavalinkUtilities.DecodeTrack(tt.TrackString), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
            }
            await RebuildList(u, p, q);
        }

        public static async Task ClearList(string p, ulong u)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand($"DELETE FROM playlistentries WHERE userid = {u} AND playlistname = @pl;", conn);
            cmd.Parameters.AddWithValue("pl", p);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task MoveListItems(DiscordGuild guild, string p, ulong u, int oldpos, int newpos)
        {
            var qnow = await GetPlaylist(guild, u,p);
            var q = await qnow.GetEntries();
            var temp = q[oldpos];
            q[oldpos] = q[newpos];
            q[newpos] = temp;
            await RebuildList(u, p, q);
        }

        public static async Task RemoveFromList(DiscordGuild guild, int position, string p, ulong u)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand($"DELETE FROM playlistentries WHERE pos = {position} AND userid = {u} AND playlistname = @pl;UPDATE playlists SET changed=@mody WHERE userid= {u} AND playlistname=@pl;", conn);
            cmd.Parameters.AddWithValue("pl", p);
            cmd.Parameters.AddWithValue("mody", DateTimeOffset.UtcNow);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            await ReorderList(guild, p,u);
            conn.Close();
            conn.Dispose();
        }

        public static async Task RenameList(string p,ulong u, string newname)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand($"UPDATE playlists SET playlistname = @newn WHERE userid = {u} AND playlistname = @pl;", conn);
            cmd.Parameters.AddWithValue("pl", p);
            cmd.Parameters.AddWithValue("newn", newname);
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine("main");
            cmd.Dispose();
            var cmd2 = new NpgsqlCommand($"UPDATE playlistentries SET playlistname = @newn WHERE userid = {u} AND playlistname = @pl;UPDATE playlists SET changed = @mody WHERE userid= {u} AND playlistname = @newn;", conn);
            cmd2.Parameters.AddWithValue("pl", p);
            cmd2.Parameters.AddWithValue("newn", newname);
            cmd2.Parameters.AddWithValue("mody", DateTimeOffset.UtcNow);
            await cmd2.ExecuteNonQueryAsync();
            Console.WriteLine("alls");
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task<TrackResult> GetSong(string n, CommandContext ctx)
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
                    var ex = await NND.GetNND(nndID, msg);
                    if (ex == null)
                    {
                        await msg.ModifyAsync("Please try again or verify the link");
                        return null;
                    }
                    await msg.ModifyAsync("Uploading");
                    await client.UploadAsync(ex, $"{nndID}.mp3", FtpRemoteExists.Skip, true);
                }
                var Track = await nodeConnection.Rest.GetTracksAsync(new Uri($"https://nnd.meek.moe/new/{nndID}.mp3"));
                return new TrackResult(Track.PlaylistInfo, Track.Tracks.First());
            }
            if (n.StartsWith("http://") | n.StartsWith("https://"))
            {
                var s = await nodeConnection.Rest.GetTracksAsync(new Uri(n));
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
                var s = await nodeConnection.Rest.GetTracksAsync(n);
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
    }
}
