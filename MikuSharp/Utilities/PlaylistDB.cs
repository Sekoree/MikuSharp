using DSharpPlus.Lavalink;
using MikuSharp.Entities;
using MikuSharp.Enums;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MikuSharp.Utilities
{
    public class PlaylistDB
    {
        public static async Task<Dictionary<string,Playlist>> GetPlaylists(ulong u)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"SELECT * FROM playlists WHERE userid = {u} ORDER BY playlistname ASC;", conn);
            var reader = await cmd2.ExecuteReaderAsync();
            Dictionary<string, Playlist> lists = new Dictionary<string, Playlist>(); 
            while (await reader.ReadAsync())
            {
                lists.Add(Convert.ToString(reader["playlistname"]), new Playlist(Other.getExtService(Convert.ToString(reader["extservice"])), Convert.ToString(reader["url"]), Convert.ToString(reader["playlistname"]), Convert.ToUInt64(reader["userid"])));
            }
            reader.Close();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
            return lists;
        }

        public static async Task<Playlist> GetPlaylist(ulong u, string p)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd2 = new NpgsqlCommand($"SELECT * FROM playlists WHERE userid = {u} AND playlistname = '{p}';", conn);
            var reader = await cmd2.ExecuteReaderAsync();
            Playlist pl = null;
            while (await reader.ReadAsync())
            {
                pl = new Playlist(Other.getExtService(Convert.ToString(reader["extservice"])), Convert.ToString(reader["url"]), Convert.ToString(reader["playlistname"]), Convert.ToUInt64(reader["userid"]));
            }
            reader.Close();
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
            return pl;
        }

        public static async Task ReorderList(string p, ulong u)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var listNow = await GetPlaylist(u, p);
            await listNow.GetEntries();
            var cmd = new NpgsqlCommand($"DELETE FROM playlistentries WHERE userid = {u} AND playlistname = '{p}';", conn);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            int i = 0;
            foreach (var qi in listNow.Entries)
            {
                var cmd2 = new NpgsqlCommand("INSERT INTO playlistentries VALUES (@pos,@p,@u,@ts,@add,@mody)", conn);
                cmd2.Parameters.AddWithValue("pos", i);
                cmd2.Parameters.AddWithValue("p", p);
                cmd2.Parameters.AddWithValue("u", Convert.ToInt64(u));
                cmd2.Parameters.AddWithValue("ts", qi.track.TrackString);
                cmd2.Parameters.AddWithValue("add", DateTimeOffset.UtcNow);
                cmd2.Parameters.AddWithValue("mody", DateTimeOffset.UtcNow);
                await cmd2.ExecuteNonQueryAsync();
                cmd2.Dispose();
                i++;
            }
            conn.Close();
            conn.Dispose();
        }

        public static async Task RebuildList(ulong u, string p, List<PlaylistEntry> q)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var queueNow = q;
            var cmd = new NpgsqlCommand($"DELETE FROM playlistentries WHERE userid = {u} AND playlistname = '{p}';", conn);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            int i = 0;
            foreach (var qi in queueNow)
            {
                var cmd2 = new NpgsqlCommand("INSERT INTO playlistentries VALUES (@pos,@p,@u,@ts,@add,@mody)", conn);
                cmd2.Parameters.AddWithValue("pos", i);
                cmd2.Parameters.AddWithValue("p", p);
                cmd2.Parameters.AddWithValue("u", Convert.ToInt64(u));
                cmd2.Parameters.AddWithValue("ts", qi.track.TrackString);
                cmd2.Parameters.AddWithValue("add", DateTimeOffset.UtcNow);
                cmd2.Parameters.AddWithValue("mody", DateTimeOffset.UtcNow);
                await cmd2.ExecuteNonQueryAsync();
                cmd2.Dispose();
                i++;
            }
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
            var cmd = new NpgsqlCommand($"DELETE FROM playlists WHERE playlistname = '{p}' AND userid = {u};", conn);
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
            var cmd2 = new NpgsqlCommand($"SELECT Count(*) FROM playlistentries WHERE userid = {u} AND playlistname = '{p}';", conn);
            var reader = await cmd2.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                position = reader.GetInt32(0);
            }
            Console.WriteLine(position);
            reader.Close();
            cmd2.Dispose();
            var cmd = new NpgsqlCommand("INSERT INTO playlistentries VALUES (@pos,@p,@u,@ts,@add,@mody)", conn);
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

        public static async Task InsertEntry(string p, ulong u, string ts, int pos)
        {
            var qnow = await GetPlaylist(u, p);
            qnow.Entries.Insert(pos, new PlaylistEntry(LavalinkUtilities.DecodeTrack(ts), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, pos));
            await RebuildList(u,p, qnow.Entries);
        }

        public static async Task ClearList(string p, ulong u)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand($"DELETE FROM playlistentries WHERE userid = {u} AND playlistname = '{p}';", conn);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static async Task MoveListItems(string p, ulong u, int oldpos, int newpos)
        {
            var qnow = await GetPlaylist(u,p);
            await qnow.GetEntries();
            var temp = qnow.Entries[oldpos];
            qnow.Entries[oldpos] = qnow.Entries[newpos];
            qnow.Entries[newpos] = temp;
            await RebuildList(u,p, qnow.Entries);
        }

        public static async Task RemoveFromList(int position, string p, ulong u)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand($"DELETE FROM playlistentries WHERE position = {position} AND useris = {u} AND playlistname = '{p}';", conn);
            await cmd.ExecuteNonQueryAsync();
            cmd.Dispose();
            await ReorderList(p,u);
            conn.Close();
            conn.Dispose();
        }

        public static async Task RenameList(string p,ulong u, string newname)
        {
            var connString = Bot.cfg.DbConnectString;
            var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand($"UPDATE playlists SET playlistname = '{newname}' WHERE userid = {u} AND playlistname = '{p}';", conn);
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine("main");
            cmd.Dispose();
            var cmd2 = new NpgsqlCommand($"UPDATE playlistentries SET playlistname = '{newname}' WHERE userid = {u} AND playlistname = '{p}';", conn);
            await cmd2.ExecuteNonQueryAsync();
            Console.WriteLine("alls");
            cmd2.Dispose();
            conn.Close();
            conn.Dispose();
        }
    }
}
