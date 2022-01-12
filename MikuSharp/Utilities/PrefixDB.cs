using Npgsql;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MikuSharp.Utilities
{
    public class PrefixDB
    {

        public static async Task<Dictionary<ulong, List<string>>> GetAllUserPrefixes(ulong u)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"SELECT * FROM prefixes WHERE userid = {u};", conn);
            var reader = await cmd2.ExecuteReaderAsync();
            Dictionary<ulong,List<string>> lists = new();
            while (await reader.ReadAsync())
            {
                if (!lists.Any(x => x.Key == Convert.ToUInt64(reader["guild"])))
                {
                    lists.Add(Convert.ToUInt64(reader["guild"]), new List<string>());
                }
                lists[Convert.ToUInt64(reader["guild"])].Add(Convert.ToString(reader["prefix"]));
            }
            reader.Close();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
            return lists;
        }

        public static async Task<List<string>> GetGuildPrefixes(ulong g)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"SELECT * FROM guildprefixes WHERE guildid = {g};", conn);
            var reader = await cmd2.ExecuteReaderAsync();
            List<string> lists = new();
            while (await reader.ReadAsync())
            {
                lists.Add(Convert.ToString(reader["prefix"]));
            }
            reader.Close();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
            return lists;
        }

        public static async Task AddUserPrefix(ulong u, ulong g, string p)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"INSERT INTO prefixes VALUES (@u,@g,@p);", conn);
            cmd2.Parameters.AddWithValue("u", Convert.ToInt64(u));
            cmd2.Parameters.AddWithValue("g", Convert.ToInt64(g));
            cmd2.Parameters.AddWithValue("p", p);
            await cmd2.ExecuteNonQueryAsync();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task AddGuildPrefix(ulong g, string p)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"INSERT INTO guildprefixes VALUES (@g,@p);", conn);
            cmd2.Parameters.AddWithValue("g", Convert.ToInt64(g));
            cmd2.Parameters.AddWithValue("p", p);
            await cmd2.ExecuteNonQueryAsync();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task RemoveUserPrefix(ulong u, ulong g, string p)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"DELETE FROM prefixes WHERE userid= @u AND guildid= @g AND prefix= @p;", conn);
            cmd2.Parameters.AddWithValue("u", Convert.ToInt64(u));
            cmd2.Parameters.AddWithValue("g", Convert.ToInt64(g));
            cmd2.Parameters.AddWithValue("p", p);
            await cmd2.ExecuteNonQueryAsync();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task RemoveGuildPrefix(ulong g, string p)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"DELETE FROM guildprefixes WHERE guildid=@g AND prefix=@p;", conn);
            cmd2.Parameters.AddWithValue("g", Convert.ToInt64(g));
            cmd2.Parameters.AddWithValue("p", p);
            await cmd2.ExecuteNonQueryAsync();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
        }
    }
}
