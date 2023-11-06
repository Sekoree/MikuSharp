using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

using MikuSharp.Entities;
using MikuSharp.Utilities;

namespace MikuSharp.Commands
{
	/// <summary>
	/// The developer commands.
	/// </summary>
	public class Developer : ApplicationCommandsModule
	{
		private static readonly string[] s_units =
		{
			"", "ki", "Mi", "Gi"
		};

		private static string SizeToString(long l)
		{
			double d = l;
			var u = 0;
			while (d >= 900 && u < s_units.Length - 2)
			{
				u++;
				d /= 1024;
			}

			return $"{d:#,##0.00} {s_units[u]}B";
		}

		[SlashCommand("which_shard", "Which shard are we on?")]
		public static async Task TestAsync(InteractionContext ctx)
			=> await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent($"Meep meep Secret. Shard {ctx.Client.ShardId}"));

		[SlashCommand("global_ll_stats", "Global lavalink stats")]
		public static async Task GetGlobalLavalinkStatsAsync(InteractionContext ctx)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("Loading statistics for every shard."));
			if (ctx.Client.CurrentApplication.Team.Members.All(x => x.User != ctx.User) && ctx.User.Id != 856780995629154305)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
				return;
			}

			foreach (var lavaSession in MikuBot.LavalinkSessions)
			{
				var stats = lavaSession.Value.Statistics;
				var sb = new StringBuilder();
				sb.Append($"Lavalink resources usage statistics for shard {lavaSession.Key}: ```")
					.Append("Uptime:                    ").Append(stats.Uptime).AppendLine()
					.Append("Players:                   ").AppendFormat("{0} active / {1} total", stats.PlayingPlayers, stats.Players).AppendLine()
					.Append("CPU Cores:                 ").Append(stats.Cpu.Cores).AppendLine()
					.Append("CPU Usage:                 ").AppendFormat("{0:#,##0.0%} lavalink / {1:#,##0.0%} system", stats.Cpu.LavalinkLoad, stats.Cpu.SystemLoad).AppendLine()
					.Append("RAM Usage:                 ").AppendFormat("{0} allocated / {1} used / {2} free / {3} reservable", SizeToString(stats.Memory.Allocated), SizeToString(stats.Memory.Used), SizeToString(stats.Memory.Free), SizeToString(stats.Memory.Reservable)).AppendLine()
					.Append("Audio frames (per minute): ").AppendFormat("{0:#,##0} sent / {1:#,##0} nulled / {2:#,##0} deficit", stats.Frames?.Sent, stats.Frames?.Nulled, stats.Frames?.Deficit).AppendLine()
					.Append("```");
				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent(sb.ToString()));
			}

			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
		}

		[SlashCommand("global_restart", "Restarts all lavalink connection nodes")]
		public static async Task RestartGlobalShardsAsync(InteractionContext ctx)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("Restarting all shards"));
			if (ctx.Client.CurrentApplication.Team.Members.All(x => x.User != ctx.User) && ctx.User.Id != 856780995629154305)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
				return;
			}

			foreach (var shard in MikuBot.ShardedClient.ShardClients.Values)
				await shard.ReconnectAsync();
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
		}

		[SlashCommand("global_ll_restart", "Restarts all lavalink connection nodes")]
		public static async Task RestartLalalinkGlobalAsync(InteractionContext ctx, [Option("clear_queues", "Clear all queues?")] bool clearQueues = false)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("Restarting all lavalink connections"));
			if (ctx.Client.CurrentApplication.Team.Members.All(x => x.User != ctx.User) && ctx.User.Id != 856780995629154305)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
				return;
			}

			var ll = await MikuBot.ShardedClient.GetLavalinkAsync();
			foreach (var l in ll)
			foreach (var n in l.Value.ConnectedSessions)
			{
				await DisconnectVoiceConnectionsAsync(ctx, n.Value, clearQueues);
				await n.Value.DestroyAsync();
				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent($"Stopped lavalink on shard {l.Key}"));
			}

			MikuBot.LavalinkSessions.Clear();
			foreach (var l in ll)
			{
				var lCon = await l.Value.ConnectAsync(MikuBot.LavalinkConfig);
				MikuBot.LavalinkSessions.Add(l.Key, lCon);
				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent($"Started lavalink on shard {l.Key}"));
			}

			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
		}

		private static async Task DisconnectVoiceConnectionsAsync(InteractionContext ctx, LavalinkSession connection, bool clearQueues)
		{
			foreach (var guildConnection in connection.ConnectedPlayers)
			{
				try
				{
					await MikuBot.Guilds[guildConnection.Key].MusicInstance.CommandChannel.SendMessageAsync(new DiscordEmbedBuilder().WithAuthor(ctx.User.UsernameWithDiscriminator, ctx.User.ProfileUrl, ctx.User.AvatarUrl).WithTitle("Developer Notice").WithDescription("⚠️ This music instance was forcefully disconnected by the developers ⚠️\n\nReasons could be:\n- Maintenance\n- Fatal Errors").Build());
				}
				catch (Exception)
				{ }

				await guildConnection.Value.StopAsync();
				if (clearQueues)
					_ = Task.Run(async () => await Database.ClearQueueAsync(guildConnection.Value.Guild), MikuBot.CanellationTokenSource.Token);
				await guildConnection.Value.DisconnectAsync();
				connection.Discord.Logger.LogInformation("Forcefully disconnected lavalink voice from {guild}", guildConnection.Key);
			}
		}

		[SlashCommand("global_shards", "Get all shard infos")]
		public static async Task GuildTestAsync(InteractionContext ctx)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Loading shards").AsEphemeral());
			if (ctx.Client.CurrentApplication.Team.Members.All(x => x.User != ctx.User) && ctx.User.Id != 856780995629154305)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
				return;
			}

			foreach (var shard in MikuBot.ShardedClient.ShardClients.Values)
				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(ctx.Client.ShardId == shard.ShardId ? $"Current shard {shard.ShardId} has {shard.Guilds.Count} guilds." : $"Shard {shard.ShardId} has {shard.Guilds.Count} guilds.").AsEphemeral());
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
		}

		[ContextMenu(ApplicationCommandType.Message, "Remove message - Miku Dev")]
		public static async Task DeleteMessageAsync(ContextMenuContext ctx)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Log request").AsEphemeral());
			if (ctx.Client.CurrentApplication.Team.Members.All(x => x.User != ctx.User) && ctx.User.Id != 856780995629154305)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
				return;
			}

			await ctx.TargetMessage.DeleteAsync();
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done"));
		}

		[SlashCommand("premium", "Premium Test"), ApplicationCommandRequirePremiumTest]
		public static async Task TestPremiumAsync(InteractionContext ctx)
			=> await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Test - should be subscribed:\n\n" + JsonConvert.SerializeObject(ctx.Interaction.Entitlements, Formatting.Indented).BlockCode("json")));

		/// <summary>
		/// Gets the debug log.
		/// </summary>
		/// <param name="ctx">The interaction context.</param>
		[SlashCommand("dbg", "Get the debug logs of today")]
		public static async Task GetDebugLogsAsync(InteractionContext ctx)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Log request").AsEphemeral());

			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Trying to get log"));
			var now = DateTime.Now;
			var targetFile = $"miku_log{now.ToString("yyyy/MM/dd").Replace("/", "")}.txt";
			if (!File.Exists(targetFile))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Failed to get log"));
				return;
			}
			else
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Found log {Formatter.Bold(targetFile)}"));

			try
			{
				if (!File.Exists($"temp-{targetFile}"))
					File.Copy(targetFile, $"temp-{targetFile}");
				else
				{
					File.Delete($"temp-{targetFile}");
					File.Copy(targetFile, $"temp-{targetFile}");
				}

				FileStream log = new($"temp-{targetFile}", FileMode.Open, FileAccess.Read);
				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddFile(targetFile, log, true).WithContent($"Log {Formatter.Bold(targetFile)}").AsEphemeral());
				log.Close();
				await log.DisposeAsync();
				File.Delete($"temp-{targetFile}");
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
			if (ctx.Client.CurrentApplication.Team.Members.All(x => x.User != ctx.User) && ctx.User.Id != 856780995629154305)
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

			var cs = code[cs1..cs2];

			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
				.WithColor(new("#FF007F"))
				.WithDescription("Evaluating...\n\nMeanwhile: https://eval-deez-nuts.xyz/")
				.Build())).ConfigureAwait(false);
			msg = await ctx.GetOriginalResponseAsync();
			try
			{
				var globals = new EvaluationVariables(ctx.TargetMessage, ctx.Client, ctx, MikuBot.CanellationTokenSource, MikuBot.GlobalCancellationTokenSource);

				var sopts = ScriptOptions.Default;
				sopts = sopts.WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.Threading.Tasks", "DisCatSharp", "DisCatSharp.Entities", "DisCatSharp.CommandsNext", "DisCatSharp.CommandsNext.Attributes", "DisCatSharp.Interactivity", "DisCatSharp.Interactivity.Extensions", "DisCatSharp.Enums", "Microsoft.Extensions.Logging", "MikuSharp.Entities", "MikuSharp.Enums", "MikuSharp.Utilities",
					"DisCatSharp.Lavalink");
				sopts = sopts.WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

				var script = CSharpScript.Create(cs, sopts, typeof(EvaluationVariables));
				script.Compile();
				var result = await script.RunAsync(globals).ConfigureAwait(false);

				if (result is { ReturnValue: not null } && !string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
					{
						Title = "Evaluation Result", Description = result.ReturnValue.ToString(), Color = new DiscordColor("#007FFF")
					}.Build())).ConfigureAwait(false);
				else
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
					{
						Title = "Evaluation Successful", Description = "No result was returned.", Color = new DiscordColor("#007FFF")
					}.Build())).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
				{
					Title = "Evaluation Failure", Description = string.Concat("**", ex.GetType().ToString(), "**: ", ex.Message), Color = new DiscordColor("#FF0000")
				}.Build())).ConfigureAwait(false);
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
		public Dictionary<int, LavalinkSession> Sessions = MikuBot.LavalinkSessions;

		/// <summary>
		/// Initializes a new instance of the <see cref="EvaluationVariables"/> class.
		/// </summary>
		/// <param name="msg">The message.</param>
		/// <param name="client">The client.</param>
		/// <param name="ctx">The context menu context.</param>
		public EvaluationVariables(DiscordMessage msg, DiscordClient client, ContextMenuContext ctx, CancellationTokenSource cts, CancellationTokenSource globalCts)
		{
			this.Client = client;
			this.Message = msg;
			this.Channel = ctx.Channel;
			this.Guild = ctx.Guild;
			this.User = ctx.User;
			this.Member = ctx.Member;
			this.Context = ctx;
			this.Cts = cts;
			this.GlobalCts = globalCts;
		}

		/// <summary>
		/// Gets the sharded client.
		/// </summary>
		public DiscordShardedClient ShardClient { get; set; } = MikuBot.ShardedClient;

		/// <summary>
		/// Gets or sets the client.
		/// </summary>
		public DiscordClient Client { get; set; }

		/// <summary>
		/// Gets the cancellation token source.
		/// </summary>
		public CancellationTokenSource Cts { get; set; }

		/// <summary>
		/// Gets the global cancellation token source.
		/// </summary>
		public CancellationTokenSource GlobalCts { get; set; }
	}
}
