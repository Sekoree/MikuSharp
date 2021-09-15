using DisCatSharp.Entities;
using DisCatSharp.Lavalink;

using MikuSharp.Entities;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MikuSharp.Utilities
{
    public class Database
    {
        public static async Task AddToLPL(ulong g, string ts)
        {
            int position = 0;
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"SELECT Count(*) FROM lastplayedsongs WHERE guildId = {g};", conn);
            var reader = await cmd2.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                position = reader.GetInt32(0);
            }
            Console.WriteLine(position);
            reader.Close();
            cmd2.Dispose();
            var cmd = new NpgsqlCommand("INSERT INTO lastplayedsongs VALUES (@pos,@guild,@ts)", conn);
            cmd.Parameters.AddWithValue("pos", position);
            cmd.Parameters.AddWithValue("guild", Convert.ToInt64(g));
            cmd.Parameters.AddWithValue("ts", ts);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task ReorderQueue(DiscordGuild g)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var queueNow = await GetQueue(g);
            var cmd = new NpgsqlCommand($"DELETE FROM queues WHERE guildid = {g.Id};", conn);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            int i = 0;
            string longcmd = "";
            foreach (var qi in queueNow)
            {
                string adddate = $"{qi.additionDate.UtcDateTime.Year}-{qi.additionDate.UtcDateTime.Month}-{qi.additionDate.UtcDateTime.Day} {qi.additionDate.UtcDateTime.Hour}:{qi.additionDate.UtcDateTime.Minute}:{qi.additionDate.UtcDateTime.Second}";
                longcmd += $"INSERT INTO queues VALUES ({i},@guild,'{qi.addedBy}','{qi.track.TrackString}','{adddate}');";
                i++;
            }
            var cmd2 = new NpgsqlCommand(longcmd, conn);
            cmd2.Parameters.AddWithValue("guild", Convert.ToInt64(g.Id));
            await cmd2.ExecuteNonQueryAsync();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task RebuildQueue(DiscordGuild g, List<QueueEntry> q)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var queueNow = q;
            var cmd = new NpgsqlCommand($"DELETE FROM queues WHERE guildid = {g.Id};", conn);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            int i = 0;
            string longcmd = "";
            foreach (var qi in queueNow)
            {
                string adddate = $"{qi.additionDate.UtcDateTime.Year}-{qi.additionDate.UtcDateTime.Month}-{qi.additionDate.UtcDateTime.Day} {qi.additionDate.UtcDateTime.Hour}:{qi.additionDate.UtcDateTime.Minute}:{qi.additionDate.UtcDateTime.Second}";
                longcmd += $"INSERT INTO queues VALUES ({i},@guild,'{qi.addedBy}','{qi.track.TrackString}','{adddate}');";
                i++;
            }
            var cmd2 = new NpgsqlCommand(longcmd, conn);
            cmd2.Parameters.AddWithValue("guild", Convert.ToInt64(g.Id));
            await cmd2.ExecuteNonQueryAsync();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task<List<QueueEntry>> GetQueue(DiscordGuild g)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"SELECT * FROM queues WHERE guildId = {g.Id} ORDER BY position ASC;", conn);
            var reader = await cmd2.ExecuteReaderAsync();
            List<QueueEntry> queue = new List<QueueEntry>();
            while (await reader.ReadAsync())
            {
                queue.Add(new QueueEntry(LavalinkUtilities.DecodeTrack(Convert.ToString(reader["trackstring"])), Convert.ToUInt64(reader["userid"]), DateTimeOffset.Parse(reader["addtime"].ToString()), Convert.ToInt32(reader["position"])));
            }
            reader.Close();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
            return queue;
        }

        public static async Task AddToQueue(DiscordGuild g, ulong u, string ts)
        {
            int position = 0;
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"SELECT Count(*) FROM queues WHERE guildId = {g.Id};", conn);
            var reader = await cmd2.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                position = reader.GetInt32(0);
            }
            //Console.WriteLine(position);
            reader.Close();
            cmd2.Dispose();
            var cmd = new NpgsqlCommand("INSERT INTO queues VALUES (@pos,@guild,@user,@ts,@time)", conn);
            cmd.Parameters.AddWithValue("pos", position);
            cmd.Parameters.AddWithValue("guild", Convert.ToInt64(g.Id));
            cmd.Parameters.AddWithValue("user", Convert.ToInt64(u));
            cmd.Parameters.AddWithValue("ts", ts);
            cmd.Parameters.AddWithValue("time", DateTimeOffset.UtcNow);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task AddToQueue(DiscordGuild g, ulong u, List<LavalinkTrack> ts)
        {
            int position = 0;
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"SELECT Count(*) FROM queues WHERE guildId = {g.Id};", conn);
            var reader = await cmd2.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                position = reader.GetInt32(0);
            }
            //Console.WriteLine(position);
            reader.Close();
            cmd2.Dispose();
            string longcmd = "";
            foreach (var tt in ts)
            {
                longcmd += $"INSERT INTO queues VALUES ({position},@guild,@user,'{tt.TrackString}',@time);";
                position++;
            }
            var cmd = new NpgsqlCommand(longcmd, conn);
            cmd.Parameters.AddWithValue("guild", Convert.ToInt64(g.Id));
            cmd.Parameters.AddWithValue("user", Convert.ToInt64(u));
            cmd.Parameters.AddWithValue("time", DateTimeOffset.UtcNow);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task AddToQueue(DiscordGuild g, ulong u, List<PlaylistEntry> ts)
        {
            int position = 0;
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"SELECT Count(*) FROM queues WHERE guildId = {g.Id};", conn);
            var reader = await cmd2.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                position = reader.GetInt32(0);
            }
            //Console.WriteLine(position);
            reader.Close();
            cmd2.Dispose();
            string longcmd = "";
            foreach (var tt in ts)
            {
                longcmd += $"INSERT INTO queues VALUES ({position},@guild,@user,'{tt.track.TrackString}',@time);";
                position++;
            }
            var cmd = new NpgsqlCommand(longcmd, conn);
            cmd.Parameters.AddWithValue("guild", Convert.ToInt64(g.Id));
            cmd.Parameters.AddWithValue("user", Convert.ToInt64(u));
            cmd.Parameters.AddWithValue("time", DateTimeOffset.UtcNow);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task InsertToQueue(DiscordGuild g, ulong u, string ts, int pos)
        {
            var qnow = await GetQueue(g);
            qnow.Insert(pos, new QueueEntry(LavalinkUtilities.DecodeTrack(ts), u, DateTimeOffset.UtcNow, pos));
            await RebuildQueue(g, qnow);
        }

        public static async Task InsertToQueue(DiscordGuild g, ulong u, List<LavalinkTrack> ts, int pos)
        {
            var qnow = await GetQueue(g);
            foreach (var tt in ts)
            {
                qnow.Insert(pos, new QueueEntry(LavalinkUtilities.DecodeTrack(tt.TrackString), u, DateTimeOffset.UtcNow, pos));
            }
            await RebuildQueue(g, qnow);
        }

        public static async Task RemoveFromQueue(int position, DiscordGuild g)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand($"DELETE FROM queues WHERE position = {position} AND guildid = {g.Id};", conn);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            await ReorderQueue(g);
            conn.Close();
            conn.Dispose();
        }

        public static async Task ClearQueue(DiscordGuild g)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand($"DELETE FROM queues WHERE guildid = {g.Id};", conn);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task MoveQueueItems(DiscordGuild g, int oldpos, int newpos)
        {
            var qnow = await GetQueue(g);
            var temp = qnow[oldpos];
            qnow.RemoveAt(oldpos);
            qnow.Insert(newpos,temp);
            await RebuildQueue(g, qnow);
        }

        public static async Task<List<Entry>> GetLPL(DiscordGuild g)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"SELECT * FROM lastplayedsongs WHERE guildId = {g.Id} ORDER BY lastplayedsongs.trackposition DESC LIMIT 1000", conn);
            var reader = await cmd2.ExecuteReaderAsync();
            List<Entry> queue = new List<Entry>();
            while (await reader.ReadAsync())
            {
                //queue.Add(new Entry(LavalinkUtilities.DecodeTrack((string)reader["trackstring"]), DateTimeOffset.Parse(NpgsqlDateTime.Parse((string)reader["addtime"]).ToString())));
                queue.Add(new Entry(LavalinkUtilities.DecodeTrack((string)reader["trackstring"]), DateTimeOffset.UtcNow));
            }
            reader.Close();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
            return queue;
        }
    }
}
