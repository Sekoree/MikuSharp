using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MikuSharp.Commands;

/// <summary>
/// The developer commands.
/// </summary>
public class Developer : ApplicationCommandsModule
{
	[SlashCommand("test", "Testing")]
	public async static Task TestAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent($"Meep meep. Shard {ctx.Client.ShardId}"));
	}

	[SlashCommand("guild_shard_test", "Testing")]
	public async static Task GuildTestAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Meep meep. Shard {ctx.Client.ShardId}"));
		foreach (var shard in MikuBot.ShardedClient.ShardClients.Values)
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Shard {shard.ShardId} has {shard.Guilds.Count} guilds."));
	}

	[ContextMenu(ApplicationCommandType.Message, "Remove message - Miku Dev")]
	public async static Task DeleteMessageAsync(ContextMenuContext ctx)
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
	/// Gets the debug log.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	[SlashCommand("dbg", "Get the logs of today")]
	public async static Task GetDebugLogAsync(InteractionContext ctx)
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
		else
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
			log.Dispose();
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
	public async static Task EvalCsAsync(ContextMenuContext ctx)
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
		msg = await ctx.GetOriginalResponseAsync();
		try
		{
			var globals = new SgTestVariables(ctx.TargetMessage, ctx.Client, ctx, MikuBot.ShardedClient);

			var sopts = ScriptOptions.Default;
			sopts = sopts.WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.Threading.Tasks", "DisCatSharp", "DisCatSharp.Entities", "DisCatSharp.CommandsNext", "DisCatSharp.CommandsNext.Attributes", "DisCatSharp.Interactivity", "DisCatSharp.Interactivity.Extensions", "DisCatSharp.Enums", "Microsoft.Extensions.Logging", "MikuSharp.Entities",
				"DisCatSharp.Lavalink");
			sopts = sopts.WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

			var script = CSharpScript.Create(cs, sopts, typeof(SgTestVariables));
			script.Compile();
			var result = await script.RunAsync(globals).ConfigureAwait(false);

			if (result is { ReturnValue: not null } && !string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
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
public sealed class SgTestVariables
{
	/// <summary>
	/// Gets or sets the message.
	/// </summary>
	public DiscordMessage Message { get; set; }

	public InteractivityExtension Inter { get; set; }

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

	//public Dictionary<ulong, Guild> Bot = MikuBot.Guilds;

	/// <summary>
	/// Initializes a new instance of the <see cref="SgTestVariables"/> class.
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

	public DiscordShardedClient ShardClient { get; set; }

	public DiscordClient Client { get; set; }
}