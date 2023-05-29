namespace MikuSharp.Entities;

public class Guild
{
	public int ShardId { get; set; }

	public MusicInstance? MusicInstance { get; set; }

	public Task AloneCheckThread { get; set; }

	public Guild(int shardId)
	{
		this.ShardId = shardId;
	}

	public async Task CheckAlone()
	{
		if (this.MusicInstance != null)
		{
			while (DateTime.UtcNow.Subtract(this.MusicInstance.AloneTime).Minutes != 5 && !this.MusicInstance.AloneCheckCancellationToken.IsCancellationRequested)
			{
				await Task.Delay(1000);
				if (this.MusicInstance == null || this.MusicInstance.GuildConnection == null)
					return;
			}

			if (DateTime.UtcNow.Subtract(this.MusicInstance.AloneTime).Minutes == 5 && !this.MusicInstance.AloneCheckCancellationToken.IsCancellationRequested)
			{
				await Task.Run(async () => await this.MusicInstance.GuildConnection.DisconnectAsync(), MikuBot._canellationTokenSource.Token);
				await Task.Delay(500);
				this.MusicInstance = null;
			}
		}
	}
}
