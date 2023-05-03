using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

using MikuSharp.Entities;
using MikuSharp.Utilities;

namespace MikuSharp.Commands;

/// <summary>
/// The developer commands.
/// </summary>
public class Developer : ApplicationCommandsModule
{
	private static readonly string[] Units = new[] { "", "ki", "Mi", "Gi" };
	private static string SizeToString(long l)
	{
		double d = l;
		var u = 0;
		while (d >= 900 && u < Units.Length - 2)
		{
			u++;
			d /= 1024;
		}

		return $"{d:#,##0.00} {Units[u]}B";
	}

	[SlashCommand("test", "Testing")]
	public static async Task TestAsync(InteractionContext ctx)
		=> await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent($"Meep meep. Shard {ctx.Client.ShardId}"));

	[SlashCommand("global_lstats", "Global lavalink stats")]
	public static async Task GetGlobalLavalinkStatsAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("Loading statistics for every shard."));
		if (!ctx.Client.CurrentApplication.Team.Members.Where(x => x.User == ctx.User).Any() && ctx.User.Id != 856780995629154305)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
			return;
		}
		foreach (var lavaNode in MikuBot.LavalinkNodeConnections)
		{
			var stats = lavaNode.Value.Statistics;
			var sb = new StringBuilder();
			sb.Append($"Lavalink resources usage statistics for shard {lavaNode.Key}: ```")
				.Append("Uptime:                    ").Append(stats.Uptime).AppendLine()
				.Append("Players:                   ").AppendFormat("{0} active / {1} total", stats.ActivePlayers, stats.TotalPlayers).AppendLine()
				.Append("CPU Cores:                 ").Append(stats.CpuCoreCount).AppendLine()
				.Append("CPU Usage:                 ").AppendFormat("{0:#,##0.0%} lavalink / {1:#,##0.0%} system", stats.CpuLavalinkLoad, stats.CpuSystemLoad).AppendLine()
				.Append("RAM Usage:                 ").AppendFormat("{0} allocated / {1} used / {2} free / {3} reservable", SizeToString(stats.RamAllocated), SizeToString(stats.RamUsed), SizeToString(stats.RamFree), SizeToString(stats.RamReservable)).AppendLine()
				.Append("Audio frames (per minute): ").AppendFormat("{0:#,##0} sent / {1:#,##0} nulled / {2:#,##0} deficit", stats.AverageSentFramesPerMinute, stats.AverageNulledFramesPerMinute, stats.AverageDeficitFramesPerMinute).AppendLine()
				.Append("```");
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent(sb.ToString()));
		}
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
	}

	[SlashCommand("global_ll_restart", "Restarts all lavalink connection nodes")]
	public static async Task Test(InteractionContext ctx, [Option("clear_queues", "Clear all queues?")] bool clearQueues = false)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("Restarting all lavalink connections"));
		if (!ctx.Client.CurrentApplication.Team.Members.Where(x => x.User == ctx.User).Any() && ctx.User.Id != 856780995629154305)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
			return;
		}
		var ll = await MikuBot.ShardedClient.GetLavalinkAsync();
		foreach (var l in ll)
			foreach (var n in l.Value.ConnectedNodes)
			{
				await DisconnectVoiceConnectionsAsync(ctx, n.Value, clearQueues);
				await n.Value.StopAsync();
				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent($"Stopped lavalink on shard {l.Key}"));
			}

		MikuBot.LavalinkNodeConnections.Clear();
		foreach (var l in ll)
		{
			var LCon = await l.Value.ConnectAsync(MikuBot.LavalinkConfig);
			MikuBot.LavalinkNodeConnections.Add(l.Key, LCon);
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent($"Started lavalink on shard {l.Key}"));
		}
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
	}

	private static async Task DisconnectVoiceConnectionsAsync(InteractionContext ctx, LavalinkNodeConnection connection, bool clearQueues)
	{
		foreach(var guildConnection in connection.ConnectedGuilds)
		{
			try
			{
				await MikuBot.Guilds[guildConnection.Key].musicInstance.usedChannel.SendMessageAsync(new DiscordEmbedBuilder().WithAuthor(ctx.User.UsernameWithDiscriminator, ctx.User.ProfileUrl, ctx.User.AvatarUrl).WithTitle("Developer Notice").WithDescription("⚠️ This music instance was forcefully disconnected by the developers ⚠️\n\nReasons could be:\n- Maintenance\n- Fatal Errors").Build());
			}
			catch(Exception)
			{ }

			await guildConnection.Value.StopAsync();
			if (clearQueues)
				_ = Task.Run(async() => await Database.ClearQueueAsync(guildConnection.Value.Guild), MikuBot._cts.Token);
			await guildConnection.Value.DisconnectAsync(true);
			connection.Discord.Logger.LogInformation("Forcefully disconnected lavalink voice from {guild}", guildConnection.Key);
		}
	}

	[SlashCommand("guild_shard_test", "Testing")]
	public static async Task GuildTestAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Meep meep. Shard {ctx.Client.ShardId}"));
		foreach (var shard in MikuBot.ShardedClient.ShardClients.Values)
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Shard {shard.ShardId} has {shard.Guilds.Count} guilds."));
	}

	[ContextMenu(ApplicationCommandType.Message, "Remove message - Miku Dev")]
	public static async Task DeleteMessageAsync(ContextMenuContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Log request").AsEphemeral());
		if (!ctx.Client.CurrentApplication.Team.Members.Where(x => x.User == ctx.User).Any() && ctx.User.Id != 856780995629154305)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
			return;
		}
		await ctx.TargetMessage.DeleteAsync();
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done"));
	}

	/// <summary>
	/// Gets the debug log.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	[SlashCommand("dbg", "Get the logs of today")]
	public static async Task GetDebugLogsAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Log request"));
		if (!ctx.Client.CurrentApplication.Team.Members.Where(x => x.User == ctx.User).Any() && ctx.User.Id != 856780995629154305)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
			return;
		}

		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Trying to get log"));
		DateTime now = DateTime.Now;
		var target_file = $"miku_log{now.ToString("yyyy/MM/dd").Replace("/", "")}.txt";
		if (!File.Exists(target_file))
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Failed to get log"));
			return;
		}
		else
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Found log {Formatter.Bold(target_file)}"));
		try
		{
			if (!File.Exists($"temp-{target_file}"))
				File.Copy(target_file, $"temp-{target_file}");
			else
			{
				File.Delete($"temp-{target_file}");
				File.Copy(target_file, $"temp-{target_file}");
			}
			FileStream log = new(path: $"temp-{target_file}", FileMode.Open, FileAccess.Read);
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddFile(target_file, log, true).WithContent($"Log {Formatter.Bold(target_file)}").AsEphemeral());
			log.Close();
			log.Dispose();
			File.Delete($"temp-{target_file}");
		}
		catch (Exception ex)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(ex.Message).AsEphemeral());
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(ex.StackTrace).AsEphemeral());
		}
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done"));
	}

	/// <summary>
	/// Evals the csharp script.
	/// </summary>
	/// <param name="ctx">The context menu context.</param>
	[ContextMenu(ApplicationCommandType.Message, "Eval - Miku Dev")]
	public static async Task EvalCodeAsync(ContextMenuContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Eval request").AsEphemeral());
		if (!ctx.Client.CurrentApplication.Team.Members.Where(x => x.User == ctx.User).Any() && ctx.User.Id != 856780995629154305)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
			return;
		}

		var msg = ctx.TargetMessage;
		var code = ctx.TargetMessage.Content;
		var cs1 = code.IndexOf("```") + 3;
		cs1 = code.IndexOf('\n', cs1) + 1;
		var cs2 = code.LastIndexOf("```");
		var c = await ctx.Guild.GetActiveThreadsAsync();

		if (cs1 == -1 || cs2 == -1)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You need to wrap the code into a code block."));
			return;
		}

		string cs = code[cs1..cs2];

		await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
			.WithColor(new DiscordColor("#FF007F"))
			.WithDescription("Evaluating...\n\nMeanwhile: https://eval-deez-nuts.xyz/")
			.Build())).ConfigureAwait(false);
		msg = await ctx.GetOriginalResponseAsync();
		try
		{
			var globals = new EvaluationVariables(ctx.TargetMessage, ctx.Client, ctx);

			var sopts = ScriptOptions.Default;
			sopts = sopts.WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.Threading.Tasks", "DisCatSharp", "DisCatSharp.Entities", "DisCatSharp.CommandsNext", "DisCatSharp.CommandsNext.Attributes", "DisCatSharp.Interactivity", "DisCatSharp.Interactivity.Extensions", "DisCatSharp.Enums", "Microsoft.Extensions.Logging", "MikuSharp.Entities", "DisCatSharp.Lavalink");
			sopts = sopts.WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

			var script = CSharpScript.Create(cs, sopts, typeof(EvaluationVariables));
			script.Compile();
			var result = await script.RunAsync(globals).ConfigureAwait(false);

			if (result != null && result.ReturnValue != null && !string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder { Title = "Evaluation Result", Description = result.ReturnValue.ToString(), Color = new DiscordColor("#007FFF") }.Build())).ConfigureAwait(false);
			else
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder { Title = "Evaluation Successful", Description = "No result was returned.", Color = new DiscordColor("#007FFF") }.Build())).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder { Title = "Evaluation Failure", Description = string.Concat("**", ex.GetType().ToString(), "**: ", ex.Message), Color = new DiscordColor("#FF0000") }.Build())).ConfigureAwait(false);
		}
	}
}

/// <summary>
/// The test variables.
/// </summary>
public class EvaluationVariables
{
	/// <summary>
	/// Gets or sets the message.
	/// </summary>
	public DiscordMessage Message { get; set; }

	/// <summary>
	/// Gets or sets the channel.
	/// </summary>
	public DiscordChannel Channel { get; set; }

	/// <summary>
	/// Gets or sets the guild.
	/// </summary>
	public DiscordGuild Guild { get; set; }

	/// <summary>
	/// Gets or sets the user.
	/// </summary>
	public DiscordUser User { get; set; }

	/// <summary>
	/// Gets or sets the member.
	/// </summary>
	public DiscordMember Member { get; set; }

	/// <summary>
	/// Gets or sets the context menu context.
	/// </summary>
	public ContextMenuContext Context { get; set; }

	/// <summary>
	/// Gets the custom guild entities.
	/// </summary>
	public Dictionary<ulong, Guild> Guilds = MikuBot.Guilds;

	/// <summary>
	/// Gets the lavalink node connections.
	/// </summary>
	public Dictionary<int, LavalinkNodeConnection> Connections = MikuBot.LavalinkNodeConnections;

	/// <summary>
	/// Initializes a new instance of the <see cref="EvaluationVariables"/> class.
	/// </summary>
	/// <param name="msg">The message.</param>
	/// <param name="client">The client.</param>
	/// <param name="ctx">The context menu context.</param>
	public EvaluationVariables(DiscordMessage msg, DiscordClient client, ContextMenuContext ctx)
	{
		Client = client;
		Message = msg;
		Channel = ctx.Channel;
		Guild = ctx.Guild;
		User = ctx.User;
		Member = ctx.Member;
		Context = ctx;
	}

	/// <summary>
	/// Gets the sharded client.
	/// </summary>
	public DiscordShardedClient ShardClient { get; set; } = MikuBot.ShardedClient;

	/// <summary>
	/// Gets or sets the client.
	/// </summary>
	public DiscordClient Client { get; set; }
}
