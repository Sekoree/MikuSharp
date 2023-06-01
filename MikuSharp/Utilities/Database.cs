using MikuSharp.Entities;

using NpgsqlTypes;

namespace MikuSharp.Utilities;

public static class Database
{
	public static async Task<bool> CanSendNewFeedback(this DiscordUser user)
	{
		var connString = MikuBot.Config.DbConnectString;
		await using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		await using var cmd = new NpgsqlCommand("SELECT Count(*) FROM feedback WHERE user_id = @userId AND send_at >= @date::date", conn);
		cmd.Parameters.AddWithValue("userId", Convert.ToInt64(user.Id));
		cmd.Parameters.AddWithValue("date", DateTime.UtcNow.Subtract(TimeSpan.FromDays(14)).ToString("yyyy-MM-dd"));

		await using var reader = await cmd.ExecuteReaderAsync(MikuBot._canellationTokenSource.Token);

		var feedbackCountDuringTargetDate = 0;
		while (await reader.ReadAsync(MikuBot._canellationTokenSource.Token))
			feedbackCountDuringTargetDate = reader.GetInt32(0);

		await reader.CloseAsync();

		await conn.CloseAsync();

		return feedbackCountDuringTargetDate == 0;
	}

	public static async Task<Feedback> SaveFeedbackAsync(this Feedback feedback)
	{
		var connString = MikuBot.Config.DbConnectString;
		await using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		await using var cmd = new NpgsqlCommand("INSERT INTO feedback(user_id, message, rating) VALUES(@userId, @message, @rating) RETURNING ufid", conn);
		cmd.Parameters.AddWithValue("userId", Convert.ToInt64(feedback.UserId));
		cmd.Parameters.AddWithValue("message", feedback.Message);
		cmd.Parameters.AddWithValue("rating", feedback.Rating);

		using var reader = await cmd.ExecuteReaderAsync(MikuBot._canellationTokenSource.Token);
		while(await reader.ReadAsync(MikuBot._canellationTokenSource.Token))
			feedback.Ufid = reader.GetInt64(0);

		await reader.CloseAsync();

		await conn.CloseAsync();

		return feedback;
	}

	public static async Task AddToLastPlayingListAsync(ulong guildId, string ts)
	{
		var position = 0;
		var connString = MikuBot.Config.DbConnectString;

		await using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		await using var countCmd = new NpgsqlCommand("SELECT Count(*) FROM lastplayedsongs WHERE guildId = @guild;", conn);
		var guildParam = countCmd.CreateParameter();
		guildParam.ParameterName = "guild";
		guildParam.Value = guildId;
		countCmd.Parameters.Add(guildParam);

		await using var reader = await countCmd.ExecuteReaderAsync(MikuBot._canellationTokenSource.Token);

		while (await reader.ReadAsync(MikuBot._canellationTokenSource.Token))
			position = reader.GetInt32(0);

		await reader.CloseAsync();

		await using var insertCmd = new NpgsqlCommand("INSERT INTO lastplayedsongs VALUES (@pos, @guild, @ts)", conn);
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

		await insertCmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);

		await conn.CloseAsync();
	}

	public static async Task ReorderQueueAsync(DiscordGuild guild)
	{
		var connString = MikuBot.Config.DbConnectString;

		await using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		var queueNow = await GetQueueAsync(guild);

		await using var deleteCmd = new NpgsqlCommand("DELETE FROM queues WHERE guildid = @guild;", conn);
		var guildParam = deleteCmd.CreateParameter();
		guildParam.ParameterName = "guild";
		guildParam.Value = guild.Id;
		deleteCmd.Parameters.Add(guildParam);

		await deleteCmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);

		if (queueNow.Count == 0)
			return;

		var i = 0;
		var insertCmdBuilder = new StringBuilder();

		foreach (var qi in queueNow)
		{
			var addDateParamName = $"adddate{i}";
			insertCmdBuilder.Append($"INSERT INTO queues VALUES ({i}, @guild, '{qi.AddedBy}', '{qi.Track.TrackString}', @{addDateParamName});");

			var addDateParam = new NpgsqlParameter(addDateParamName, NpgsqlDbType.Timestamp)
			{
				Value = qi.AdditionDate.UtcDateTime
			};
			insertCmdBuilder.Append('@').Append(addDateParamName);
			insertCmdBuilder.Append(';');

			i++;
		}

		await using var insertCmd = new NpgsqlCommand(insertCmdBuilder.ToString(), conn);
		var guildParam2 = insertCmd.CreateParameter();
		guildParam2.ParameterName = "guild";
		guildParam2.Value = guild.Id;
		insertCmd.Parameters.Add(guildParam2);

		foreach (var qi in queueNow)
		{
			var addDateParamName = $"adddate{i}";
			var addDateParam = new NpgsqlParameter(addDateParamName, NpgsqlDbType.Timestamp)
			{
				Value = qi.AdditionDate.UtcDateTime
			};
			insertCmd.Parameters.Add(addDateParam);
			i++;
		}

		await insertCmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);

		await conn.CloseAsync();
	}

	public static async Task RebuildQueueAsync(DiscordGuild guild, List<QueueEntry> queueEntries)
	{
		var connString = MikuBot.Config.DbConnectString;

		await using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		await using var cmd = new NpgsqlCommand("DELETE FROM queues WHERE guildid = @guild;", conn);
		var guildParam = cmd.CreateParameter();
		guildParam.ParameterName = "guild";
		guildParam.Value = guild.Id;
		cmd.Parameters.Add(guildParam);

		await cmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);

		if (queueEntries.Count == 0)
			return;

		var i = 0;
		var insertCmdBuilder = new StringBuilder();

		foreach (var qi in queueEntries)
		{
			var addDateParamName = $"adddate{i}";
			insertCmdBuilder.Append($"INSERT INTO queues VALUES ({i}, @guild, '{qi.AddedBy}', '{qi.Track.TrackString}', @{addDateParamName});");

			var addDateParam = new NpgsqlParameter(addDateParamName, NpgsqlDbType.Timestamp)
			{
				Value = qi.AdditionDate.UtcDateTime
			};
			insertCmdBuilder.Append('@').Append(addDateParamName);
			insertCmdBuilder.Append(';');

			i++;
		}

		await using var insertCmd = new NpgsqlCommand(insertCmdBuilder.ToString(), conn);
		var guildParam2 = insertCmd.CreateParameter();
		guildParam2.ParameterName = "guild";
		guildParam2.Value = guild.Id;
		insertCmd.Parameters.Add(guildParam2);

		foreach (var qi in queueEntries)
		{
			var addDateParamName = $"adddate{i}";
			var addDateParam = new NpgsqlParameter(addDateParamName, NpgsqlDbType.Timestamp)
			{
				Value = qi.AdditionDate.UtcDateTime
			};
			insertCmd.Parameters.Add(addDateParam);
			i++;
		}

		await insertCmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);

		await conn.CloseAsync();
	}

	public static async Task<List<QueueEntry>> GetQueueAsync(DiscordGuild guild)
	{
		var connString = MikuBot.Config.DbConnectString;

		await using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		await using var cmd = new NpgsqlCommand("SELECT * FROM queues WHERE guildId = @guild ORDER BY position ASC;", conn);
		var guildParam = cmd.CreateParameter();
		guildParam.ParameterName = "guild";
		guildParam.Value = guild.Id;
		cmd.Parameters.Add(guildParam);

		await using var reader = await cmd.ExecuteReaderAsync(MikuBot._canellationTokenSource.Token);
		var queue = new List<QueueEntry>();

		while (await reader.ReadAsync(MikuBot._canellationTokenSource.Token))
		{
			var trackString = Convert.ToString(reader["trackstring"]);
			var userId = Convert.ToUInt64(reader["userid"]);
			var addTime = DateTimeOffset.Parse(reader["addtime"].ToString());
			var position = Convert.ToInt32(reader["position"]);

			var queueEntry = new QueueEntry(LavalinkUtilities.DecodeTrack(trackString), userId, addTime, position);
			queue.Add(queueEntry);
		}

		await reader.CloseAsync();

		await conn.CloseAsync();

		return queue;
	}

	public static async Task AddToQueueAsync(DiscordGuild guild, ulong userId, string trackString)
	{
		var connString = MikuBot.Config.DbConnectString;

		await using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		var position = 0;
		await using var countCmd = new NpgsqlCommand("SELECT Count(*) FROM queues WHERE guildId = @guild;", conn);
		var guildParam = countCmd.CreateParameter();
		guildParam.ParameterName = "guild";
		guildParam.Value = guild.Id;
		countCmd.Parameters.Add(guildParam);

		await using var countReader = await countCmd.ExecuteReaderAsync(MikuBot._canellationTokenSource.Token);
		while (await countReader.ReadAsync(MikuBot._canellationTokenSource.Token))
			position = countReader.GetInt32(0);

		await countReader.CloseAsync();

		await using var insertCmd = new NpgsqlCommand("INSERT INTO queues VALUES (@pos, @guild, @user, @ts, @time)", conn);
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

		await insertCmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);

		await conn.CloseAsync();
	}

	public static async Task AddToQueueAsync(DiscordGuild guild, ulong userId, List<LavalinkTrack> tracks)
	{
		var connString = MikuBot.Config.DbConnectString;

		await using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		var position = 0;
		await using var countCmd = new NpgsqlCommand("SELECT Count(*) FROM queues WHERE guildId = @guild;", conn);
		var guildParam = countCmd.CreateParameter();
		guildParam.ParameterName = "guild";
		guildParam.Value = guild.Id;
		countCmd.Parameters.Add(guildParam);

		await using var countReader = await countCmd.ExecuteReaderAsync(MikuBot._canellationTokenSource.Token);
		while (await countReader.ReadAsync(MikuBot._canellationTokenSource.Token))
			position = countReader.GetInt32(0);

		await countReader.CloseAsync();

		var longcmd = new StringBuilder();

		foreach (var track in tracks)
		{
			longcmd.AppendLine($"INSERT INTO queues VALUES (@pos, @guild, @user, '{track.TrackString}', @time);");
			position++;
		}

		await using var insertCmd = new NpgsqlCommand(longcmd.ToString(), conn);

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

		await insertCmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);

		await conn.CloseAsync();
	}

	public static async Task AddToQueueAsync(DiscordGuild guild, ulong userId, List<PlaylistEntry> entries)
	{
		var connString = MikuBot.Config.DbConnectString;

		await using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);

		var position = 0;
		await using var countCmd = new NpgsqlCommand("SELECT Count(*) FROM queues WHERE guildId = @guild;", conn);
		var guildParam = countCmd.CreateParameter();
		guildParam.ParameterName = "guild";
		guildParam.Value = guild.Id;
		countCmd.Parameters.Add(guildParam);

		await using var countReader = await countCmd.ExecuteReaderAsync(MikuBot._canellationTokenSource.Token);
		while (await countReader.ReadAsync(MikuBot._canellationTokenSource.Token))
			position = countReader.GetInt32(0);

		await countReader.CloseAsync();

		var longcmd = new StringBuilder();

		foreach (var entry in entries)
		{
			longcmd.AppendLine($"INSERT INTO queues VALUES (@pos, @guild, @user, '{entry.Track.TrackString}', @time);");
			position++;
		}

		await using var insertCmd = new NpgsqlCommand(longcmd.ToString(), conn);

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

		await insertCmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);

		await conn.CloseAsync();
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
			queueNow.Insert(position, new QueueEntry(LavalinkUtilities.DecodeTrack(track.TrackString), userId, DateTimeOffset.UtcNow, position));
		await RebuildQueueAsync(guild, queueNow);
	}

	public static async Task RemoveFromQueueAsync(int position, DiscordGuild guild)
	{
		var connString = MikuBot.Config.DbConnectString;
		await using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);
		await using var cmd = new NpgsqlCommand("DELETE FROM queues WHERE position = @pos AND guildid = @guild;", conn);
		cmd.Parameters.AddWithValue("guild", guild.Id);
		cmd.Parameters.AddWithValue("pos", position);
		await cmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);
		await conn.CloseAsync();
		await ReorderQueueAsync(guild);
	}

	public static async Task ClearQueueAsync(DiscordGuild guild)
	{
		var connString = MikuBot.Config.DbConnectString;
		await using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);
		await using var cmd = new NpgsqlCommand("DELETE FROM queues WHERE guildid = @guild;", conn);
		cmd.Parameters.AddWithValue("guild", guild.Id);
		await cmd.ExecuteNonQueryAsync(MikuBot._canellationTokenSource.Token);
		await conn.CloseAsync();
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
		await using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync(MikuBot._canellationTokenSource.Token);
		await using var cmd = new NpgsqlCommand("SELECT * FROM lastplayedsongs WHERE guildId = @guild ORDER BY lastplayedsongs.trackposition DESC LIMIT 1000", conn);
		cmd.Parameters.AddWithValue("guild", guild.Id);
		await using var reader = await cmd.ExecuteReaderAsync(MikuBot._canellationTokenSource.Token);
		List<Entry> queue = new();
		while (await reader.ReadAsync(MikuBot._canellationTokenSource.Token))
		{
			var trackString = reader.GetString(reader.GetOrdinal("trackstring"));
			var track = LavalinkUtilities.DecodeTrack(trackString);
			var entry = new Entry(track, DateTimeOffset.UtcNow);
			queue.Add(entry);
		}
		await reader.CloseAsync();
		await conn.CloseAsync();
		return queue;
	}
}
