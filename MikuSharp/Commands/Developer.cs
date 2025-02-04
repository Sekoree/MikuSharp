using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace MikuSharp.Commands;

/// <summary>
///     The developer commands.
/// </summary>
[ApplicationCommandRequireTeamMember]
public class Developer : ApplicationCommandsModule
{
	private static readonly string[] s_units = ["", "ki", "Mi", "Gi"];

	[SlashCommand("test", "Testing")]
	public static async Task TestAsync(InteractionContext ctx)
		=> await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent($"Meep meep. Shard {ctx.Client.ShardId}"));

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

		if (ctx.Client.CurrentApplication.Team.Members.All(x => x.User != ctx.User) && ctx.User.Id != 856780995629154305)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
			return;
		}

		await ctx.TargetMessage.DeleteAsync();
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done"));
	}

	/// <summary>
	///     Gets the debug log.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	[SlashCommand("dbg", "Get the logs of today")]
	public static async Task GetDebugLogAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Log request"));

		if (ctx.Client.CurrentApplication.Team.Members.All(x => x.User != ctx.User) && ctx.User.Id != 856780995629154305)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
			return;
		}

		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Trying to get log"));
		var now = DateTime.Now;
		var targetFile = $"miku_log{now.ToString("yyyy/MM/dd").Replace("/", "")}.txt";

		if (!File.Exists(targetFile))
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Failed to get log"));
			return;
		}

		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Found log {targetFile.Bold()}"));

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
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddFile(targetFile, log, true).WithContent($"Log {targetFile.Bold()}").AsEphemeral());
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
	///     Evals the csharp script.
	/// </summary>
	/// <param name="ctx">The context menu context.</param>
	[ContextMenu(ApplicationCommandType.Message, "Eval - Miku Dev")]
	public static async Task EvalCsAsync(ContextMenuContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Eval request").AsEphemeral());

		if (ctx.Client.CurrentApplication.Team.Members.All(x => x.User != ctx.User) && ctx.User.Id != 856780995629154305)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
			return;
		}

		var msg = ctx.TargetMessage;
		var code = ctx.TargetMessage.Content;
		var cs1 = code.IndexOf("```", StringComparison.Ordinal) + 3;
		cs1 = code.IndexOf('\n', cs1) + 1;
		var cs2 = code.LastIndexOf("```", StringComparison.Ordinal);
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
		await ctx.GetOriginalResponseAsync();

		try
		{
			var globals = new SgTestVariables(ctx.TargetMessage, ctx.Client, ctx, MikuBot.ShardedClient);

			var sopts = ScriptOptions.Default;
			sopts = sopts.WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.Threading.Tasks", "DisCatSharp", "DisCatSharp.Entities", "DisCatSharp.CommandsNext", "DisCatSharp.CommandsNext.Attributes",
				"DisCatSharp.Interactivity", "DisCatSharp.Interactivity.Extensions", "DisCatSharp.Enums", "Microsoft.Extensions.Logging", "MikuSharp.Entities");
			sopts = sopts.WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

			var script = CSharpScript.Create(cs, sopts, typeof(SgTestVariables));
			script.Compile();
			var result = await script.RunAsync(globals).ConfigureAwait(false);

			if (result is { ReturnValue: not null } && !string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
				{
					Title = "Evaluation Result",
					Description = result.ReturnValue.ToString(),
					Color = new DiscordColor("#007FFF")
				}.Build())).ConfigureAwait(false);
			else
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
				{
					Title = "Evaluation Successful",
					Description = "No result was returned.",
					Color = new DiscordColor("#007FFF")
				}.Build())).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Title = "Evaluation Failure",
				Description = string.Concat("**", ex.GetType().ToString(), "**: ", ex.Message),
				Color = new DiscordColor("#FF0000")
			}.Build())).ConfigureAwait(false);
		}
	}

	[SlashCommand("lstats", "Displays Lavalink statistics"), ApplicationCommandRequireTeamDeveloper]
	public static async Task GetLavalinkStatsAsync(InteractionContext ctx)
	{
		var stats = ctx.Client.GetLavalink().ConnectedSessions.First().Value.Statistics;
		var sb = new StringBuilder();
		sb.Append("Lavalink resources usage statistics: ```")
			.Append("Uptime:                    ").Append(stats.Uptime).AppendLine()
			.Append("Players:                   ").Append($"{stats.PlayingPlayers} active / {stats.Players} total").AppendLine()
			.Append("CPU Cores:                 ").Append(stats.Cpu.Cores).AppendLine()
			.Append("CPU Usage:                 ").Append($"{stats.Cpu.LavalinkLoad:#,##0.0%} lavalink / {stats.Cpu.SystemLoad:#,##0.0%} system").AppendLine()
			.Append("RAM Usage:                 ")
			.Append($"{SizeToString(stats.Memory.Allocated)} allocated / {SizeToString(stats.Memory.Used)} used / {SizeToString(stats.Memory.Free)} free / {SizeToString(stats.Memory.Reservable)} reservable").AppendLine()
			.Append("Audio frames (per minute): ").Append($"{stats.Frames.Sent:#,##0} sent / {stats.Frames.Nulled:#,##0} nulled / {stats.Frames.Deficit:#,##0} deficit").AppendLine()
			.Append("```");
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(sb.ToString()));
	}

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
}

/// <summary>
///     The test variables.
/// </summary>
public sealed class SgTestVariables
{
	//public Dictionary<ulong, Guild> Bot = MikuBot.Guilds;

	/// <summary>
	///     Initializes a new instance of the <see cref="SgTestVariables" /> class.
	/// </summary>
	/// <param name="msg">The message.</param>
	/// <param name="client">The client.</param>
	/// <param name="ctx">The context menu context.</param>
	public SgTestVariables(DiscordMessage msg, DiscordClient client, ContextMenuContext ctx, DiscordShardedClient shard)
	{
		this.Client = client;
		this.ShardClient = shard;

		this.Message = msg;
		this.Channel = ctx.Channel;
		this.Guild = ctx.Guild;
		this.User = ctx.User;
		this.Member = ctx.Member;
		this.Context = ctx;
		this.Inter = this.Client.GetInteractivity();
	}

	/// <summary>
	///     Gets or sets the message.
	/// </summary>
	public DiscordMessage Message { get; set; }

	public InteractivityExtension Inter { get; set; }

	/// <summary>
	///     Gets or sets the channel.
	/// </summary>
	public DiscordChannel Channel { get; set; }

	/// <summary>
	///     Gets or sets the guild.
	/// </summary>
	public DiscordGuild Guild { get; set; }

	/// <summary>
	///     Gets or sets the user.
	/// </summary>
	public DiscordUser User { get; set; }

	/// <summary>
	///     Gets or sets the member.
	/// </summary>
	public DiscordMember Member { get; set; }

	/// <summary>
	///     Gets or sets the context menu context.
	/// </summary>
	public ContextMenuContext Context { get; set; }

	public DiscordShardedClient ShardClient { get; set; }

	public DiscordClient Client { get; set; }
}
