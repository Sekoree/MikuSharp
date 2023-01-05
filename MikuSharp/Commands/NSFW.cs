using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;

using MikuSharp.Utilities;

using System.Threading.Tasks;

namespace MikuSharp.Commands
{
	[RequireNsfw]
	class NSFW : BaseCommandModule
	{
		[Command("4k")]
		[Description("lewd")]
		public async Task FourK(CommandContext ctx)
		{
			var d = await ctx.Client.RestClient.GetNekobotAsync("https://nekobot.xyz/api/image?type=4k");

			DiscordMessageBuilder builder = new();
			builder.WithFile($"image.{d.Filetype}", d.Data);
			builder.WithEmbed(d.Embed);
			await ctx.RespondAsync(builder);
		}

		[Command("anal")]
		[Description("lewd")]
		public async Task Anal(CommandContext ctx)
		{
			var d = await ctx.Client.RestClient.GetNekobotAsync("https://nekobot.xyz/api/image?type=anal");

			DiscordMessageBuilder builder = new();
			builder.WithFile($"image.{d.Filetype}", d.Data);
			builder.WithEmbed(d.Embed);
			await ctx.RespondAsync(builder);
		}

		[Command("ass")]
		[Description("lewd")]
		public async Task Ass(CommandContext ctx)
		{
			var d = await ctx.Client.RestClient.GetNekobotAsync("https://nekobot.xyz/api/image?type=ass");

			DiscordMessageBuilder builder = new();
			builder.WithFile($"image.{d.Filetype}", d.Data);
			builder.WithEmbed(d.Embed);
			await ctx.RespondAsync(builder);
		}

		[Command("gonewild")]
		[Description("lewd")]
		public async Task Gonewild(CommandContext ctx)
		{
			var d = await ctx.Client.RestClient.GetNekobotAsync("https://nekobot.xyz/api/image?type=gonewild");

			DiscordMessageBuilder builder = new();
			builder.WithFile($"image.{d.Filetype}", d.Data);
			builder.WithEmbed(d.Embed);
			await ctx.RespondAsync(builder);
		}

		[Command("lewdkitsune")]
		[Description("lewd")]
		public async Task LewdKitsune(CommandContext ctx)
		{
			var d = await ctx.Client.RestClient.GetNekobotAsync("https://nekobot.xyz/api/image?type=lewdkitsune");

			DiscordMessageBuilder builder = new();
			builder.WithFile($"image.{d.Filetype}", d.Data);
			builder.WithEmbed(d.Embed);
			await ctx.RespondAsync(builder);
		}

		[Command("lewdneko")]
		[Description("lewd")]
		public async Task LewdNeko(CommandContext ctx)
		{
			var d = await ctx.Client.RestClient.GetNekobotAsync("https://nekobot.xyz/api/image?type=lewdneko");

			DiscordMessageBuilder builder = new();
			builder.WithFile($"image.{d.Filetype}", d.Data);
			builder.WithEmbed(d.Embed);
			await ctx.RespondAsync(builder);
		}
		[Command("porngif")]
		[Description("lewd")]
		public async Task PornGif(CommandContext ctx)
		{
			var d = await ctx.Client.RestClient.GetNekobotAsync("https://nekobot.xyz/api/image?type=pgif");

			DiscordMessageBuilder builder = new();
			builder.WithFile($"image.{d.Filetype}", d.Data);
			builder.WithEmbed(d.Embed);
			await ctx.RespondAsync(builder);
		}

		[Command("pussy")]
		[Description("lewd")]
		public async Task Pussy(CommandContext ctx)
		{
			var d = await ctx.Client.RestClient.GetNekobotAsync("https://nekobot.xyz/api/image?type=pussy");

			DiscordMessageBuilder builder = new();
			builder.WithFile($"image.{d.Filetype}", d.Data);
			builder.WithEmbed(d.Embed);
			await ctx.RespondAsync(builder);
		}

		[Command("thighs")]
		[Aliases("thigh")]
		[Description("lewd")]
		public async Task Thighs(CommandContext ctx)
		{
			var d = await ctx.Client.RestClient.GetNekobotAsync("https://nekobot.xyz/api/image?type=thigh");

			DiscordMessageBuilder builder = new();
			builder.WithFile($"image.{d.Filetype}", d.Data);
			builder.WithEmbed(d.Embed);
			await ctx.RespondAsync(builder);
		}
	}
}
