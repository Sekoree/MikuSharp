﻿namespace MikuSharp.Commands;

public class MikuGuild : ApplicationCommandsModule
{
	[SlashCommand("smolcar", "#SmolArmy")]
	public static async Task SmolCarAsync(InteractionContext ctx)
	{
		if (ctx.Member.Roles.Any(x => x.Id == 607989212696018945))
		{
			await ctx.Member.RevokeRoleAsync(ctx.Guild.GetRole(607989212696018945));
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(":("));
		}
		else
		{
			await ctx.Member.GrantRoleAsync(ctx.Guild.GetRole(607989212696018945));
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Welcome to smolcar"));
		}
	}
}
