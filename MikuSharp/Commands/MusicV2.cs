using MikuSharp.Attributes;
using MikuSharp.Enums;
using MikuSharp.Utilities;

namespace MikuSharp.Commands;

[SlashCommandGroup("music", "Music commands")]
public class MusicV2 : ApplicationCommandsModule
{
	#region Size Converter
	private static readonly string[] s_units = new[] { "", "ki", "Mi", "Gi" };
	private static string SizeToString(long size)
	{
		double d = size;
		var u = 0;
		while (d >= 900 && u < s_units.Length - 2)
		{
			u++;
			d /= 1024;
		}

		return $"{d:#,##0.00} {s_units[u]}B";
	}
	#endregion

	[SlashCommand("join", "Joins a voice channel")]
	public async Task JoinAsync(InteractionContext ctx,
		[Option("channel", "Channel to join"), ChannelTypes(ChannelType.Stage, ChannelType.Voice)]
		DiscordChannel? channel = null)
	{
		await ctx.DeferAsync();
		channel ??= ctx.Member.VoiceState?.Channel;

		if (!ctx.Client.GetLavalink().ConnectedSessions.Any())
		{
			await ctx.EditResponseAsync(
				new DiscordWebhookBuilder().WithContent("Music service not yet connected. Please wait"));
			return;
		}

		if (channel is null)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Could not find a valid voice channel QwQ\n\nDid you specified the channel option or are you in any voice channel currently?"));
			return;
		}

		if (MikuBot.Guilds.All(x => x.Key != ctx.Guild.Id))
			MikuBot.Guilds.TryAdd(ctx.Guild.Id, new(ctx.Client.ShardId));
		var g = MikuBot.Guilds[ctx.Guild.Id];
		g.MusicInstance ??= new(MikuBot.LavalinkSessions[ctx.Client.ShardId], ctx.Client.ShardId);
		await g.ConditionalConnect(ctx);
		g.MusicInstance.CommandChannel = ctx.Channel;
		await ctx.DeleteResponseAsync();
		await g.MusicInstance.CommandChannel.SendMessageAsync($"Connected to {channel.Mention} :3");
	}

	[SlashCommand("leave", "Leaves the voice channel")]
	public async Task LeaveAsync(InteractionContext ctx)
	{
		await ctx.DeferAsync();
		var g = MikuBot.Guilds[ctx.Guild.Id];
		if (await g.IsNotConnected(ctx))
			return;

		g.MusicInstance.PlayState = PlayState.NotPlaying;
		await ctx.DeleteResponseAsync();
		await g.MusicInstance.CommandChannel.SendMessageAsync(
			$"Disconnected from {g.MusicInstance.VoiceChannel.Mention} :3");
		await g.MusicInstance.GuildPlayer.DisconnectAsync();
		g.MusicInstance = null;
	}
	
	[SlashCommand("stats", "Displays Lavalink statistics")]
	[ApplicationCommandRequireOwner]
	public static async Task GetLavalinkStatsAsync(InteractionContext ctx)
	{
		await ctx.DeferAsync();
		var session = MikuBot.LavalinkSessions[ctx.Client.ShardId];
		var stats = await session.GetLavalinkStatsAsync();
		var info = await session.GetLavalinkInfoAsync();
		var tsb = new StringBuilder();
		if (stats.Frames is not null)
			tsb.Append("Audio frames (per minute): ")
				.AppendFormat("{0:#,##0} sent / {1:#,##0} nulled / {2:#,##0} deficit", stats.Frames?.Sent,
					stats.Frames?.Nulled, stats.Frames?.Deficit).AppendLine();
		var sb = new StringBuilder();
		sb.Append("Lavalink resources usage statistics: ```")
			.Append("Uptime:                    ").Append(stats.Uptime).AppendLine()
			.Append("Players:                   ")
			.AppendFormat("{0} active / {1} total", stats.PlayingPlayers, stats.Players).AppendLine()
			.Append("CPU Cores:                 ").Append(stats.Cpu.Cores).AppendLine()
			.Append("CPU Usage:                 ").AppendFormat("{0:#,##0.0%} lavalink / {1:#,##0.0%} system",
				stats.Cpu.LavalinkLoad, stats.Cpu.SystemLoad).AppendLine()
			.Append("RAM Usage:                 ").AppendFormat(
				"{0} allocated / {1} used / {2} free / {3} reservable", SizeToString(stats.Memory.Allocated),
				SizeToString(stats.Memory.Used), SizeToString(stats.Memory.Free),
				SizeToString(stats.Memory.Reservable)).AppendLine()
			.Append(tsb)
			.Append("```").AppendLine()
			.Append("Lavalink Version: ").Append(info.Version.Semver).AppendLine()
			.Append("Lavalink Player Version: ").Append(info.Lavaplayer).AppendLine()
			.Append("Java Version: ").Append(info.Jvm);
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(sb.ToString()));
	}

	[SlashCommand("pause", "Pauses the playback")]
	public async Task PauseAsync(InteractionContext ctx)
	{
		await ctx.DeferAsync();
		var g = MikuBot.Guilds[ctx.Guild.Id];
		if (await g.IsNotConnected(ctx))
			return;

		if (g.MusicInstance.PlayState is PlayState.NotPlaying or PlayState.Stopped || g.MusicInstance.CurrentSong is null)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I don't play anything right now :3"));
			return;
		}

		if (g.MusicInstance.PlayState is PlayState.Paused)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I am already paused ^^"));
			return;
		}

		await g.MusicInstance.GuildPlayer.PauseAsync();
		g.MusicInstance.PlayState = PlayState.Paused;
		await ctx.DeleteResponseAsync();
		await g.MusicInstance.CommandChannel.SendMessageAsync("Paused the music OwO");
	}

	[SlashCommand("resume", "Resumes the playback")]
	public async Task ResumeAsync(InteractionContext ctx)
	{
		await ctx.DeferAsync();
		var g = MikuBot.Guilds[ctx.Guild.Id];
		if (await g.IsNotConnected(ctx))
			return;

		if (g.MusicInstance.PlayState is PlayState.NotPlaying or PlayState.Stopped || g.MusicInstance.CurrentSong is null)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I don't play anything right now :3"));
			return;
		}

		if (g.MusicInstance.PlayState is not PlayState.Paused)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I am not paused ^^"));
			return;
		}

		await g.MusicInstance.GuildPlayer.ResumeAsync();
		g.MusicInstance.PlayState = PlayState.Playing;
		await ctx.DeleteResponseAsync();
		await g.MusicInstance.CommandChannel.SendMessageAsync("Resumed the music OwO");
	}

	[SlashCommand("stop", "Stops the playback")]
	public async Task StopAsync(InteractionContext ctx)
	{
		await ctx.DeferAsync();
		var g = MikuBot.Guilds[ctx.Guild.Id];
		if (await g.IsNotConnected(ctx))
			return;

		if (g.MusicInstance.PlayState is PlayState.Stopped || g.MusicInstance.CurrentSong is null)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I don't play anything right now :3"));
			return;
		}

		if (g.MusicInstance.PlayState is PlayState.NotPlaying)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I don't play anything, no use :3"));
			return;
		}

		await g.MusicInstance.GuildPlayer.StopAsync();
		g.MusicInstance.PlayState = PlayState.Stopped;
		await ctx.DeleteResponseAsync();
		await g.MusicInstance.CommandChannel.SendMessageAsync("Stopped the music x~x");
	}

	#region Playback Options

	[SlashCommandGroup("options", "Playback Options")]
	[RequireUserAndBotVoicechatConnection]
	public class PlaybackOptions : ApplicationCommandsModule
	{
		[SlashCommand("repeat", "Repeat the current song or the entire queue")]
		public static async Task RepeatAsync(InteractionContext ctx,
			[Option("mode", "New repeat mode"), ChoiceProvider(typeof(FixedOptionProviders.RepeatModeProvider))] RepeatMode mode)
		{
			await ctx.DeferAsync();
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;
			g.MusicInstance.Config.RepeatMode = mode;
			await ctx.DeleteResponseAsync();
			await g.MusicInstance.CommandChannel.SendMessageAsync(
				$"Set repeat mode to: {g.MusicInstance.Config.RepeatMode.ToString().Bold()}");
		}

		[SlashCommand("shuffle", "Play the queue in shuffle mode")]
		public static async Task ShuffleAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync();
			var g = MikuBot.Guilds[ctx.Guild.Id];
			if (await g.IsNotConnected(ctx))
				return;
			g.MusicInstance.Config.ShuffleMode = g.MusicInstance.Config.ShuffleMode == ShuffleMode.Off ? ShuffleMode.On : ShuffleMode.Off;
			await ctx.DeleteResponseAsync();
			await g.MusicInstance.CommandChannel.SendMessageAsync(
				$"Set shuffle mode to: {g.MusicInstance.Config.ShuffleMode.ToString().Bold()}");
		}
	}
	#endregion
}
