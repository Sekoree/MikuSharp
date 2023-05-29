namespace MikuSharp.Entities;

public class BiliPlayinfo
{
	[JsonProperty("code")]
	public int Code { get; set; }

	[JsonProperty("message")]
	public string Message { get; set; }

	[JsonProperty("ttl")]
	public int TTL { get; set; }

	[JsonProperty("data")]
	public BiliData Data { get; set; }

	[JsonProperty("session")]
	public string Session { get; set; }

	[JsonProperty("videoFrame")]
	public VideoFrame VideoFrame { get; set; }
}

public class DownloadUrl
{
	[JsonProperty("order")]
	public int Order { get; set; }

	[JsonProperty("length")]
	public int Length { get; set; }

	[JsonProperty("size")]
	public int Size { get; set; }

	[JsonProperty("ahead")]
	public string Ahead { get; set; }

	[JsonProperty("vhead")]
	public string Vhead { get; set; }

	[JsonProperty("url")]
	public string Url { get; set; }

	[JsonProperty("backup_url")]
	public string BackupUrl { get; set; }
}

public class BiliData
{
	[JsonProperty("from")]
	public string From { get; set; }

	[JsonProperty("result")]
	public string Result { get; set; }

	[JsonProperty("message")]
	public string Message { get; set; }

	[JsonProperty("quality")]
	public int Quality { get; set; }

	[JsonProperty("format")]
	public string Format { get; set; }

	[JsonProperty("timelength")]
	public int TimeLength { get; set; }

	[JsonProperty("accept_format")]
	public string AcceptFormat { get; set; }

	[JsonProperty("accept_description")]
	public List<string> AcceptDescription { get; set; }

	[JsonProperty("accept_quality")]
	public List<int> AcceptQuality { get; set; }

	[JsonProperty("video_codecid")]
	public int VideoCodecId { get; set; }

	[JsonProperty("video_project")]
	public bool VideoProject { get; set; }

	[JsonProperty("seek_param")]
	public string SeekParam { get; set; }

	[JsonProperty("seek_type")]
	public string SeekType { get; set; }

	[JsonProperty("durl")]
	public List<DownloadUrl> DownloadUrl { get; set; }
}

public class VideoFrame
{
}
