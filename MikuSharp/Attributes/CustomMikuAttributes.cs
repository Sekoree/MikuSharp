namespace MikuSharp.Attributes;

/// <summary>
/// Defines that usage of this command is restricted to users in a vc.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RequireUserVoicechatConnection : ApplicationCommandCheckBaseAttribute
{
	public override Task<bool> ExecuteChecksAsync(BaseContext ctx)
		=> ctx.Member.VoiceState?.Channel != null ? Task.FromResult(true) : Task.FromResult(false);
}

/// <summary>
/// Defines that usage of this command is restricted to users & the bot in a vc.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RequireUserAndBotVoicechatConnection : ApplicationCommandCheckBaseAttribute
{
	public override async Task<bool> ExecuteChecksAsync(BaseContext ctx)
	{
		var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
		return ctx.Member.VoiceState?.Channel != null && bot.VoiceState?.Channel != null
			? await Task.FromResult(true)
			: await Task.FromResult(false);
	}
}

/// <summary>
/// Defines that the usage is restricted to non staffs.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Class, AllowMultiple = false)]
public sealed class NotStaffAttribute : CheckBaseAttribute
{
	public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
		=> Task.FromResult(!ctx.User.IsStaff);
}
