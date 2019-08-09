using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MikuSharp.Commands
{
    public class Playlist : BaseCommandModule
    {
        [Command("create")]
        public async Task Create(CommandContext ctx, [RemainingText] string name)
        {

        }
    }
}
