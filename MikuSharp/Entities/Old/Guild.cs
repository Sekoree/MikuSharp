/*using System;
using System.Threading.Tasks;

namespace MikuSharp.Entities;

public class Guild
{
    public Guild(int id, MusicInstance? mi = null)
    {
        this.ShardId = id;
        this.MusicInstance = mi;
    }

    public int ShardId { get; set; }

    //CustomPrefix stuff
    public MusicInstance? MusicInstance { get; set; }
    public Task AloneCheckThread { get; set; }

    public async Task CheckAlone()
    {
        while (DateTime.UtcNow.Subtract(this.MusicInstance.AloneTime).Minutes != 5 && !this.MusicInstance.AloneCts.IsCancellationRequested)
        {
            await Task.Delay(1000);
            if (this.MusicInstance?.GuildConnection is null)
                return;
        }

        if (DateTime.UtcNow.Subtract(this.MusicInstance.AloneTime).Minutes == 5 && !this.MusicInstance.AloneCts.IsCancellationRequested)
        {
            await Task.Run(async () => await this.MusicInstance.GuildConnection.DisconnectAsync());
            await Task.Delay(500);
            this.MusicInstance = null;
        }
    }
}
*/


