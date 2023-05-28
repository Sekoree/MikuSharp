using MikuSharp.Entities;

using NpgsqlTypes;

namespace MikuSharp.Utilities;

public class Database
{
	public static async Task AddToLastPlayingListAsync(ulong guildId, string ts)
	{
		int position = 0;
		var connString = MikuBot.Config.DbConnectString;

		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();

		var countCmd = new NpgsqlCommand("SELECT Count(*) FROM lastplayedsongs WHERE guildId = @guild;", conn);
		var guildParam = countCmd.CreateParameter();
		guildParam.ParameterName = "guild";
		guildParam.Value = guildId;
		countCmd.Parameters.Add(guildParam);

		var reader = await countCmd.ExecuteReaderAsync();

		while (await reader.ReadAsync())
		{
			position = reader.GetInt32(0);
		}

		reader.Close();
		countCmd.Dispose();

		var insertCmd = new NpgsqlCommand("INSERT INTO lastplayedsongs VALUES (@pos, @guild, @ts)", conn);
		var guildParam2 = insertCmd.CreateParameter();
		guildParam2.ParameterName = "guild";
		guildParam2.Value = guildId;
		insertCmd.Parameters.Add(guildParam2);

		var tsParam = insertCmd.CreateParameter();
		tsParam.ParameterName = "ts";
		tsParam.Value = ts;
		insertCmd.Parameters.Add(tsParam);

		var posParam = insertCmd.CreateParameter();
		posParam.ParameterName = "pos";
		posParam.Value = position;
		insertCmd.Parameters.Add(posParam);

		await insertCmd.ExecuteNonQueryAsync();
		insertCmd.Dispose();

		conn.Close();
		conn.Dispose();
	}

	public static async Task ReorderQueueAsync(DiscordGuild guild)
	{
		var connString = MikuBot.Config.DbConnectString;

		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();

		var queueNow = await GetQueueAsync(guild);

		var deleteCmd = new NpgsqlCommand("DELETE FROM queues WHERE guildid = @guild;", conn);
		var guildParam = deleteCmd.CreateParameter();
		guildParam.ParameterName = "guild";
		guildParam.Value = guild.Id;
		deleteCmd.Parameters.Add(guildParam);

		await deleteCmd.ExecuteNonQueryAsync();
		deleteCmd.Dispose();

		if (queueNow.Count == 0)
			return;

		int i = 0;
		var insertCmdBuilder = new StringBuilder();

		foreach (var qi in queueNow)
		{
			var addDateParamName = $"adddate{i}";
			insertCmdBuilder.Append($"INSERT INTO queues VALUES ({i}, @guild, '{qi.addedBy}', '{qi.track.TrackString}', @{addDateParamName});");

			var addDateParam = new NpgsqlParameter(addDateParamName, NpgsqlDbType.Timestamp)
			{
				Value = qi.additionDate.UtcDateTime
			};
			insertCmdBuilder.Append('@').Append(addDateParamName);
			insertCmdBuilder.Append(';');

			i++;
		}

		var insertCmd = new NpgsqlCommand(insertCmdBuilder.ToString(), conn);
		var guildParam2 = insertCmd.CreateParameter();
		guildParam2.ParameterName = "guild";
		guildParam2.Value = guild.Id;
		insertCmd.Parameters.Add(guildParam2);

		foreach (var qi in queueNow)
		{
			var addDateParamName = $"adddate{i}";
			var addDateParam = new NpgsqlParameter(addDateParamName, NpgsqlDbType.Timestamp)
			{
				Value = qi.additionDate.UtcDateTime
			};
			insertCmd.Parameters.Add(addDateParam);
			i++;
		}

		await insertCmd.ExecuteNonQueryAsync();
		insertCmd.Dispose();

		conn.Close();
		conn.Dispose();
	}

	public static async Task RebuildQueueAsync(DiscordGuild guild, List<QueueEntry> queueEntries)
	{
		var connString = MikuBot.Config.DbConnectString;

		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();

		var cmd = new NpgsqlCommand("DELETE FROM queues WHERE guildid = @guild;", conn);
		var guildParam = cmd.CreateParameter();
		guildParam.ParameterName = "guild";
		guildParam.Value = guild.Id;
		cmd.Parameters.Add(guildParam);

		await cmd.ExecuteNonQueryAsync();
		cmd.Dispose();

		if (queueEntries.Count == 0)
			return;

		int i = 0;
		var insertCmdBuilder = new StringBuilder();

		foreach (var qi in queueEntries)
		{
			var addDateParamName = $"adddate{i}";
			insertCmdBuilder.Append($"INSERT INTO queues VALUES ({i}, @guild, '{qi.addedBy}', '{qi.track.TrackString}', @{addDateParamName});");

			var addDateParam = new NpgsqlParameter(addDateParamName, NpgsqlDbType.Timestamp)
			{
				Value = qi.additionDate.UtcDateTime
			};
			insertCmdBuilder.Append('@').Append(addDateParamName);
			insertCmdBuilder.Append(';');

			i++;
		}

		var insertCmd = new NpgsqlCommand(insertCmdBuilder.ToString(), conn);
		var guildParam2 = insertCmd.CreateParameter();
		guildParam2.ParameterName = "guild";
		guildParam2.Value = guild.Id;
		insertCmd.Parameters.Add(guildParam2);

		foreach (var qi in queueEntries)
		{
			var addDateParamName = $"adddate{i}";
			var addDateParam = new NpgsqlParameter(addDateParamName, NpgsqlDbType.Timestamp)
			{
				Value = qi.additionDate.UtcDateTime
			};
			insertCmd.Parameters.Add(addDateParam);
			i++;
		}

		await insertCmd.ExecuteNonQueryAsync();
		insertCmd.Dispose();

		conn.Close();
		conn.Dispose();
	}

	public static async Task<List<QueueEntry>> GetQueueAsync(DiscordGuild guild)
	{
		var connString = MikuBot.Config.DbConnectString;

		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();

		var cmd = new NpgsqlCommand("SELECT * FROM queues WHERE guildId = @guild ORDER BY position ASC;", conn);
		var guildParam = cmd.CreateParameter();
		guildParam.ParameterName = "guild";
		guildParam.Value = guild.Id;
		cmd.Parameters.Add(guildParam);

		var reader = await cmd.ExecuteReaderAsync();
		var queue = new List<QueueEntry>();

		while (await reader.ReadAsync())
		{
			var trackString = Convert.ToString(reader["trackstring"]);
			var userId = Convert.ToUInt64(reader["userid"]);
			var addTime = DateTimeOffset.Parse(reader["addtime"].ToString());
			var position = Convert.ToInt32(reader["position"]);

			var queueEntry = new QueueEntry(LavalinkUtilities.DecodeTrack(trackString), userId, addTime, position);
			queue.Add(queueEntry);
		}

		reader.Close();
		cmd.Dispose();

		conn.Close();
		conn.Dispose();

		return queue;
	}

	public static async Task AddToQueueAsync(DiscordGuild guild, ulong userId, string trackString)
	{
		var connString = MikuBot.Config.DbConnectString;

		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();

		var position = 0;
		var countCmd = new NpgsqlCommand("SELECT Count(*) FROM queues WHERE guildId = @guild;", conn);
		var guildParam = countCmd.CreateParameter();
		guildParam.ParameterName = "guild";
		guildParam.Value = guild.Id;
		countCmd.Parameters.Add(guildParam);

		var countReader = await countCmd.ExecuteReaderAsync();
		while (await countReader.ReadAsync())
		{
			position = countReader.GetInt32(0);
		}

		countReader.Close();
		countCmd.Dispose();

		var insertCmd = new NpgsqlCommand("INSERT INTO queues VALUES (@pos, @guild, @user, @ts, @time)", conn);
		var guildParam2 = insertCmd.CreateParameter();
		guildParam2.ParameterName = "guild";
		guildParam2.Value = guild.Id;
		insertCmd.Parameters.Add(guildParam2);

		var userParam = insertCmd.CreateParameter();
		userParam.ParameterName = "user";
		userParam.Value = userId;
		insertCmd.Parameters.Add(userParam);

		var tsParam = insertCmd.CreateParameter();
		tsParam.ParameterName = "ts";
		tsParam.Value = trackString;
		insertCmd.Parameters.Add(tsParam);

		var posParam = insertCmd.CreateParameter();
		posParam.ParameterName = "pos";
		posParam.Value = position;
		insertCmd.Parameters.Add(posParam);

		var timeParam = insertCmd.CreateParameter();
		timeParam.ParameterName = "time";
		timeParam.Value = DateTime.UtcNow;
		insertCmd.Parameters.Add(timeParam);

		await insertCmd.ExecuteNonQueryAsync();
		insertCmd.Dispose();

		conn.Close();
		conn.Dispose();
	}

	public static async Task AddToQueueAsync(DiscordGuild guild, ulong userId, List<LavalinkTrack> tracks)
	{
		var connString = MikuBot.Config.DbConnectString;

		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();

		var position = 0;
		var countCmd = new NpgsqlCommand("SELECT Count(*) FROM queues WHERE guildId = @guild;", conn);
		var guildParam = countCmd.CreateParameter();
		guildParam.ParameterName = "guild";
		guildParam.Value = guild.Id;
		countCmd.Parameters.Add(guildParam);

		var countReader = await countCmd.ExecuteReaderAsync();
		while (await countReader.ReadAsync())
		{
			position = countReader.GetInt32(0);
		}

		countReader.Close();
		countCmd.Dispose();

		var longcmd = new StringBuilder();

		foreach (var track in tracks)
		{
			longcmd.AppendLine($"INSERT INTO queues VALUES (@pos, @guild, @user, '{track.TrackString}', @time);");
			position++;
		}

		var insertCmd = new NpgsqlCommand(longcmd.ToString(), conn);

		var guildParam2 = insertCmd.CreateParameter();
		guildParam2.ParameterName = "guild";
		guildParam2.Value = guild.Id;
		insertCmd.Parameters.Add(guildParam2);

		var userParam = insertCmd.CreateParameter();
		userParam.ParameterName = "user";
		userParam.Value = userId;
		insertCmd.Parameters.Add(userParam);

		var timeParam = insertCmd.CreateParameter();
		timeParam.ParameterName = "time";
		timeParam.Value = DateTime.UtcNow;
		insertCmd.Parameters.Add(timeParam);

		await insertCmd.ExecuteNonQueryAsync();
		insertCmd.Dispose();

		conn.Close();
		conn.Dispose();
	}

	public static async Task AddToQueueAsync(DiscordGuild guild, ulong userId, List<PlaylistEntry> entries)
	{
		var connString = MikuBot.Config.DbConnectString;

		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();

		var position = 0;
		var countCmd = new NpgsqlCommand("SELECT Count(*) FROM queues WHERE guildId = @guild;", conn);
		var guildParam = countCmd.CreateParameter();
		guildParam.ParameterName = "guild";
		guildParam.Value = guild.Id;
		countCmd.Parameters.Add(guildParam);

		var countReader = await countCmd.ExecuteReaderAsync();
		while (await countReader.ReadAsync())
		{
			position = countReader.GetInt32(0);
		}

		countReader.Close();
		countCmd.Dispose();

		var longcmd = new StringBuilder();

		foreach (var entry in entries)
		{
			longcmd.AppendLine($"INSERT INTO queues VALUES (@pos, @guild, @user, '{entry.track.TrackString}', @time);");
			position++;
		}

		var insertCmd = new NpgsqlCommand(longcmd.ToString(), conn);

		var guildParam2 = insertCmd.CreateParameter();
		guildParam2.ParameterName = "guild";
		guildParam2.Value = guild.Id;
		insertCmd.Parameters.Add(guildParam2);

		var userParam = insertCmd.CreateParameter();
		userParam.ParameterName = "user";
		userParam.Value = userId;
		insertCmd.Parameters.Add(userParam);

		var timeParam = insertCmd.CreateParameter();
		timeParam.ParameterName = "time";
		timeParam.Value = DateTime.UtcNow;
		insertCmd.Parameters.Add(timeParam);

		await insertCmd.ExecuteNonQueryAsync();
		insertCmd.Dispose();

		conn.Close();
		conn.Dispose();
	}

	public static async Task InsertToQueueAsync(DiscordGuild guild, ulong userId, string trackString, int position)
	{
		var queueNow = await GetQueueAsync(guild);
		queueNow.Insert(position, new QueueEntry(LavalinkUtilities.DecodeTrack(trackString), userId, DateTimeOffset.UtcNow, position));
		await RebuildQueueAsync(guild, queueNow);
	}

	public static async Task InsertToQueueAsync(DiscordGuild guild, ulong userId, List<LavalinkTrack> tracks, int position)
	{
		var queueNow = await GetQueueAsync(guild);
		foreach (var track in tracks)
		{
			queueNow.Insert(position, new QueueEntry(LavalinkUtilities.DecodeTrack(track.TrackString), userId, DateTimeOffset.UtcNow, position));
		}
		await RebuildQueueAsync(guild, queueNow);
	}

	public static async Task RemoveFromQueueAsync(int position, DiscordGuild guild)
	{
		var connString = MikuBot.Config.DbConnectString;
		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();
		var cmd = new NpgsqlCommand("DELETE FROM queues WHERE position = @pos AND guildid = @guild;", conn);
		cmd.Parameters.AddWithValue("guild", guild.Id);
		cmd.Parameters.AddWithValue("pos", position);
		await cmd.ExecuteNonQueryAsync();
		cmd.Dispose();
		await ReorderQueueAsync(guild);
	}

	public static async Task ClearQueueAsync(DiscordGuild guild)
	{
		var connString = MikuBot.Config.DbConnectString;
		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();
		var cmd = new NpgsqlCommand("DELETE FROM queues WHERE guildid = @guild;", conn);
		cmd.Parameters.AddWithValue("guild", guild.Id);
		await cmd.ExecuteNonQueryAsync();
		cmd.Dispose();
	}

	public static async Task MoveQueueItemsAsync(DiscordGuild guild, int oldPos, int newPos)
	{
		var queueNow = await GetQueueAsync(guild);
		var temp = queueNow[oldPos];
		queueNow.RemoveAt(oldPos);
		queueNow.Insert(newPos, temp);
		await RebuildQueueAsync(guild, queueNow);
	}

	public static async Task<List<Entry>> GetLastPlayingListAsync(DiscordGuild guild)
	{
		var connString = MikuBot.Config.DbConnectString;
		using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();
		var cmd = new NpgsqlCommand("SELECT * FROM lastplayedsongs WHERE guildId = @guild ORDER BY lastplayedsongs.trackposition DESC LIMIT 1000", conn);
		cmd.Parameters.AddWithValue("guild", guild.Id);
		var reader = await cmd.ExecuteReaderAsync();
		List<Entry> queue = new();
		while (await reader.ReadAsync())
		{
			var trackString = reader.GetString(reader.GetOrdinal("trackstring"));
			var track = LavalinkUtilities.DecodeTrack(trackString);
			var entry = new Entry(track, DateTimeOffset.UtcNow);
			queue.Add(entry);
		}
		reader.Close();
		cmd.Dispose();
		conn.Close();
		conn.Dispose();
		return queue;
	}
}
