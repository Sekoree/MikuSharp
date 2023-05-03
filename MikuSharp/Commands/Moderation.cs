using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using MikuSharp.Utilities;

namespace MikuSharp.Commands;

[SlashCommandGroup("mod", "Moderation", defaultMemberPermissions: (long)Permissions.BanMembers, dmPermission: false)]
internal class Moderation : ApplicationCommandsModule
{
	[SlashCommand("disable_invites", "Disable invites usage for guild")]
	public static async Task DisableInvitesAsync(InteractionContext ctx, [Option("reason", "Auditlog reason")] string? reason = null)
	{
		await ctx.DeferAsync(false);
		try
		{
			await ctx.Guild.DisableInvitesAsync(reason);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Disabled invites"));
		}
		catch (Exception)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Could not disable invites"));
		}
	}

	[SlashCommand("enable_invites", "Enable invites usage for guild")]
	public static async Task EnableInvitesAsync(InteractionContext ctx, [Option("reason", "Auditlog reason")] string? reason = null)
	{
		await ctx.DeferAsync(false);
		try
		{
			await ctx.Guild.EnableInvitesAsync(reason);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Enabled invites"));
		}
		catch (Exception)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Could not enable invites"));
		}
	}

	[SlashCommand("ban", "Ban someone")]
	public static async Task BanAsync(InteractionContext ctx, [Option("user", "User to ban")] DiscordUser user, [Option("deletion_days", "Delete messages of x days"), MaximumValue(7)] int deletionDays = 0, [Option("reason", "Auditlog reason")] string? reason = null)
	{
		await ctx.DeferAsync(false);
		try
		{
			await ctx.Guild.BanMemberAsync(user.Id, deletionDays, reason);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Banned {user.UsernameWithDiscriminator}"));
		}
		catch (Exception)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Could not ban {user.UsernameWithDiscriminator}"));
		}
	}

	[SlashCommand("unban", "Unban someone")]
	public static async Task UnbanAsync(InteractionContext ctx, [Option("username", "User to unban", true), Autocomplete(typeof(AutocompleteProviders.BanProvider))] string id, [Option("reason", "Auditlog reason")] string? reason = null)
	{
		await ctx.DeferAsync(false);
		var userId = Convert.ToUInt64(id);
		var user = await ctx.Client.GetUserAsync(userId, true);
		await ctx.Guild.UnbanMemberAsync(user, reason);
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Unbanned {user.UsernameWithDiscriminator}"));
	}

	[SlashCommand("kick", "Kick someone")]
	public static async Task KickAsync(InteractionContext ctx, [Option("user", "User to kick")] DiscordUser user, [Option("reason", "Auditlog reason")] string? reason = null)
	{
		await ctx.DeferAsync(false);
		try
		{
			var member = await user.ConvertToMember(ctx.Guild);
			await member.RemoveAsync(reason);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Kicked {user.UsernameWithDiscriminator}"));
		}
		catch (Exception)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Could not kick {user.UsernameWithDiscriminator}"));
		}
	}

	[SlashCommand("purge", "Delete a large amount of messages fast")]
	public static async Task PurgeAsync(InteractionContext ctx, [Option("amount", "Amount of messages to purge"), MinimumValue(1), MaximumValue(100)] int amount, [Option("reason", "Auditlog reason")] string? reason = null)
	{
		await ctx.DeferAsync(true);
		try
		{
			var msgs = await ctx.Channel.GetMessagesAsync(amount);
			var under14DaysOld = msgs.Where(x => (DateTime.Now - x.CreationTimestamp.DateTime).TotalDays < 14).ToList().AsReadOnly();
			if (under14DaysOld.Any())
				await ctx.Channel.DeleteMessagesAsync(under14DaysOld, reason);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Purged {under14DaysOld.Count} messages"));
		}
		catch (DisCatSharp.Exceptions.BadRequestException ex)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(Formatter.BlockCode(ex.JsonMessage, "json")));
		}
	}
}
