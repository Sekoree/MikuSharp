using MikuSharp.Entities;
using Npgsql;
using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MikuSharp.Utilities
{
    public class Database
    {
        public static async Task AddToLPL(ulong g, string ts)
        {
            return;
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
        
        public static async Task CacheLPL(ulong g)
        {
            return;
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand($"SELECT * FROM lastplayedsongs WHERE guildId = {g} ORDER BY lastplayedsongs.trackposition DESC LIMIT 1000", conn);
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Bot.Guilds[g].lastPlayedSongs.Add(new Entry(LavalinkUtilities.DecodeTrack(reader.GetString(2))));
            }
            reader.Close();
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }
    }
}
