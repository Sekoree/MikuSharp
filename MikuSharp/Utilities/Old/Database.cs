/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DisCatSharp.Entities;
using DisCatSharp.Lavalink.Entities;

using MikuSharp.Entities;

using Npgsql;

namespace MikuSharp.Utilities;

public class Database
{
    public static async Task AddToLastPlayingListAsync(ulong g, string ts)
    {
        var position = 0;
        var connString = MikuBot.Config.DbConnectString;
        var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        var cmd2 = new NpgsqlCommand($"SELECT Count(*) FROM lastplayedsongs WHERE guild_id = '{g.ToString()}';", conn);
        var reader = await cmd2.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            position = reader.GetInt32(0);
        await reader.CloseAsync();
        await cmd2.DisposeAsync();
        var cmd = new NpgsqlCommand("INSERT INTO lastplayedsongs VALUES (@pos,@guild,@ts)", conn);
        var para = cmd.CreateParameter();
        para.ParameterName = "guild";
        para.Value = g.ToString();
        cmd.Parameters.Add(para);
        var para2 = cmd.CreateParameter();
        para2.ParameterName = "ts";
        para2.Value = ts;
        cmd.Parameters.Add(para2);
        var para3 = cmd.CreateParameter();
        para3.ParameterName = "pos";
        para3.Value = position;
        cmd.Parameters.Add(para3);
        await cmd.ExecuteNonQueryAsync();
        await cmd.DisposeAsync();
        await conn.CloseAsync();
        await conn.DisposeAsync();
    }

    public static async Task ReorderQueue(DiscordGuild g)
    {
        var connString = MikuBot.Config.DbConnectString;
        var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        var queueNow = await GetQueueAsync(g);
        var cmd = new NpgsqlCommand($"DELETE FROM queues WHERE guild_id = '{g.Id.ToString()}';", conn);
        await cmd.ExecuteNonQueryAsync();
        await cmd.DisposeAsync();
        var i = 0;
        var longcmd = "";

        foreach (var qi in queueNow)
        {
            longcmd += $"INSERT INTO queues VALUES (@guild,{i},'{qi.AddedBy.ToString()}','{qi.Track.Encoded}',@adddate{i});";
            i++;
        }

        var cmd2 = new NpgsqlCommand();
        if (string.IsNullOrEmpty(longcmd))
            return;

        cmd2.CommandText = longcmd;
        cmd2.Connection = conn;
        var para = cmd2.CreateParameter();
        para.ParameterName = "guild";
        para.Value = g.Id.ToString();
        cmd2.Parameters.Add(para);
        i = 0;

        foreach (var qi in queueNow)
        {
            var para2 = cmd2.CreateParameter();
            para2.ParameterName = $"adddate{i}";
            para2.Value = qi.AdditionDate.UtcDateTime;
            cmd2.Parameters.Add(para2);
            i++;
        }

        await cmd2.ExecuteNonQueryAsync();
        await cmd2.DisposeAsync();
        await conn.CloseAsync();
        await conn.DisposeAsync();
    }

    public static async Task RebuildQueue(DiscordGuild g, List<QueueEntry> q)
    {
        var connString = MikuBot.Config.DbConnectString;
        var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        var queueNow = q;
        var cmd = new NpgsqlCommand($"DELETE FROM queues WHERE guild_id = '{g.Id.ToString()}'", conn);
        await cmd.ExecuteNonQueryAsync();
        await cmd.DisposeAsync();
        var i = 0;
        var longcmd = "";

        foreach (var qi in queueNow)
        {
            longcmd += $"INSERT INTO queues VALUES (@guild,{i},'{qi.AddedBy.ToString()}','{qi.Track.Encoded}',@adddate{i});";
            i++;
        }

        var cmd2 = new NpgsqlCommand();
        if (string.IsNullOrEmpty(longcmd))
            return;

        cmd2.CommandText = longcmd;
        cmd2.Connection = conn;
        var para = cmd2.CreateParameter();
        para.ParameterName = "guild";
        para.Value = g.Id.ToString();
        cmd2.Parameters.Add(para);
        i = 0;

        foreach (var qi in queueNow)
        {
            var para2 = cmd2.CreateParameter();
            para2.ParameterName = $"adddate{i}";
            para2.Value = qi.AdditionDate.UtcDateTime;
            cmd2.Parameters.Add(para2);
            i++;
        }

        await cmd2.ExecuteNonQueryAsync();
        await cmd2.DisposeAsync();
        await conn.CloseAsync();
        await conn.DisposeAsync();
    }

    public static async Task<List<QueueEntry>> GetQueueAsync(DiscordGuild g)
    {
        List<QueueEntry> queue = [];
        var count = 0;
        var connString = MikuBot.Config.DbConnectString;
        var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        var cmd1 = new NpgsqlCommand($"SELECT Count(*) FROM queues WHERE guild_id = '{g.Id.ToString()}';", conn);
        count = Convert.ToInt32(await cmd1.ExecuteScalarAsync());
        await cmd1.DisposeAsync();

        if (count == 0)
        {
            await conn.CloseAsync();
            await conn.DisposeAsync();
            return queue;
        }

        var cmd2 = new NpgsqlCommand($"SELECT * FROM queues WHERE guild_id = '{g.Id.ToString()}' ORDER BY position ASC;", conn);
        var reader2 = await cmd2.ExecuteReaderAsync();
        while (await reader2.ReadAsync())
            queue.Add(new(await MikuBot.LavalinkSessions.First().Value.DecodeTrackAsync(Convert.ToString(reader2["track"])), Convert.ToUInt64(reader2["user_id"]), DateTimeOffset.Parse(reader2["added_at"].ToString()),
                Convert.ToInt32(reader2["position"])));
        await reader2.CloseAsync();
        await cmd2.DisposeAsync();
        await conn.CloseAsync();
        await conn.DisposeAsync();
        return queue;
    }

    public static async Task AddToQueue(DiscordGuild g, ulong u, string ts)
    {
        var position = 0;
        var connString = MikuBot.Config.DbConnectString;
        var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        var cmd2 = new NpgsqlCommand($"SELECT Count(*) FROM queues WHERE guild_id = '{g.Id.ToString()}';", conn);
        var reader = await cmd2.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            position = reader.GetInt32(0);
        await reader.CloseAsync();
        await cmd2.DisposeAsync();
        var cmd = new NpgsqlCommand("INSERT INTO queues VALUES (@guild,@pos,@user,@ts,@time)", conn);
        var para = cmd.CreateParameter();
        para.ParameterName = "guild";
        para.Value = g.Id.ToString();
        cmd.Parameters.Add(para);
        var para2 = cmd.CreateParameter();
        para2.ParameterName = "user";
        para2.Value = u.ToString();
        cmd.Parameters.Add(para2);
        var para3 = cmd.CreateParameter();
        para3.ParameterName = "ts";
        para3.Value = ts;
        cmd.Parameters.Add(para3);
        var para4 = cmd.CreateParameter();
        para4.ParameterName = "pos";
        para4.Value = position;
        cmd.Parameters.Add(para4);
        var para5 = cmd.CreateParameter();
        para5.ParameterName = "time";
        para5.Value = DateTime.UtcNow;
        cmd.Parameters.Add(para5);
        await cmd.ExecuteNonQueryAsync();
        await cmd.DisposeAsync();
        await conn.CloseAsync();
        await conn.DisposeAsync();
    }

    public static async Task AddToQueue(DiscordGuild g, ulong u, List<LavalinkTrack> ts)
    {
        var position = 0;
        var connString = MikuBot.Config.DbConnectString;
        var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        var cmd2 = new NpgsqlCommand($"SELECT Count(*) FROM queues WHERE guild_id = '{g.Id.ToString()}';", conn);
        var reader = await cmd2.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            position = reader.GetInt32(0);
        await reader.CloseAsync();
        await cmd2.DisposeAsync();
        var longcmd = "";

        foreach (var tt in ts)
        {
            longcmd += $"INSERT INTO queues VALUES (@guild,{position},@user,'{tt.Encoded}',@time);";
            position++;
        }

        var cmd = new NpgsqlCommand(longcmd, conn);
        var para = cmd.CreateParameter();
        para.ParameterName = "guild";
        para.Value = g.Id.ToString();
        cmd.Parameters.Add(para);
        var para2 = cmd.CreateParameter();
        para2.ParameterName = "user";
        para2.Value = u.ToString();
        cmd.Parameters.Add(para2);
        var para3 = cmd.CreateParameter();
        para3.ParameterName = "time";
        para3.Value = DateTime.UtcNow;
        cmd.Parameters.Add(para3);
        await cmd.ExecuteNonQueryAsync();
        await cmd.DisposeAsync();
        await conn.CloseAsync();
        await conn.DisposeAsync();
    }

    public static async Task AddToQueue(DiscordGuild g, ulong u, List<PlaylistEntry> ts)
    {
        var position = 0;
        var connString = MikuBot.Config.DbConnectString;
        var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        var cmd2 = new NpgsqlCommand($"SELECT Count(*) FROM queues WHERE guild_id = '{g.Id.ToString()}';", conn);
        var reader = await cmd2.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            position = reader.GetInt32(0);
        await reader.CloseAsync();
        await cmd2.DisposeAsync();
        var longcmd = "";

        foreach (var tt in ts)
        {
            longcmd += $"INSERT INTO queues VALUES (@guild,{position},@user,'{tt.Track.Encoded}',@time);";
            position++;
        }

        var cmd = new NpgsqlCommand(longcmd, conn);
        var para = cmd.CreateParameter();
        para.ParameterName = "guild";
        para.Value = g.Id.ToString();
        cmd.Parameters.Add(para);
        var para2 = cmd.CreateParameter();
        para2.ParameterName = "user";
        para2.Value = u.ToString();
        cmd.Parameters.Add(para2);
        var para3 = cmd.CreateParameter();
        para3.ParameterName = "time";
        para3.Value = DateTime.UtcNow;
        cmd.Parameters.Add(para3);
        await cmd.ExecuteNonQueryAsync();
        await cmd.DisposeAsync();
        await conn.CloseAsync();
        await conn.DisposeAsync();
    }

    public static async Task InsertToQueue(DiscordGuild g, ulong u, string ts, int pos)
    {
        var qnow = await GetQueueAsync(g);
        qnow.Insert(pos, new(await MikuBot.LavalinkSessions.First().Value.DecodeTrackAsync(ts), u, DateTimeOffset.UtcNow, pos));
        await RebuildQueue(g, qnow);
    }

    public static async Task InsertToQueue(DiscordGuild g, ulong u, List<LavalinkTrack> ts, int pos)
    {
        var qnow = await GetQueueAsync(g);
        foreach (var tt in ts)
            qnow.Insert(pos, new(tt, u, DateTimeOffset.UtcNow, pos));
        await RebuildQueue(g, qnow);
    }

    public static async Task RemoveFromQueueAsync(int position, DiscordGuild g)
    {
        var connString = MikuBot.Config.DbConnectString;
        var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        var cmd = new NpgsqlCommand($"DELETE FROM queues WHERE position = {position} AND guild_id = '{g.Id.ToString()}';", conn);
        await cmd.ExecuteNonQueryAsync();
        await cmd.DisposeAsync();
        await ReorderQueue(g);
        await conn.CloseAsync();
        await conn.DisposeAsync();
    }

    public static async Task ClearQueue(DiscordGuild g)
    {
        var connString = MikuBot.Config.DbConnectString;
        var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        var cmd = new NpgsqlCommand($"DELETE FROM queues WHERE guild_id = '{g.Id.ToString()}';", conn);
        await cmd.ExecuteNonQueryAsync();
        await cmd.DisposeAsync();
        await conn.CloseAsync();
        await conn.DisposeAsync();
    }

    public static async Task MoveQueueItems(DiscordGuild g, int oldpos, int newpos)
    {
        var qnow = await GetQueueAsync(g);
        var temp = qnow[oldpos];
        qnow.RemoveAt(oldpos);
        qnow.Insert(newpos, temp);
        await RebuildQueue(g, qnow);
    }

    public static async Task<List<Entry>> GetLastPlayingListAsync(DiscordGuild g)
    {
        var connString = MikuBot.Config.DbConnectString;
        var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        var cmd2 = new NpgsqlCommand($"SELECT * FROM lastplayedsongs WHERE guild_id = '{g.Id.ToString()}' ORDER BY lastplayedsongs.trackposition DESC LIMIT 1000", conn);
        var reader = await cmd2.ExecuteReaderAsync();
        List<Entry> queue = [];
        while (await reader.ReadAsync())
            queue.Add(new(await MikuBot.LavalinkSessions.First().Value.DecodeTrackAsync((string)reader["trackstring"]), DateTimeOffset.UtcNow));
        await reader.CloseAsync();
        await cmd2.DisposeAsync();
        await conn.CloseAsync();
        await conn.DisposeAsync();
        return queue;
    }
}
*/


