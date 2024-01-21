using System.Collections.Generic;

namespace MikuSharp.Entities;

public class BiliPlayinfo
{
	public int code { get; set; }
	public string message { get; set; }
	public int ttl { get; set; }
	public Data data { get; set; }
	public string session { get; set; }
	public VideoFrame videoFrame { get; set; }
}

public class Durl
{
	public int order { get; set; }
	public int length { get; set; }
	public int size { get; set; }
	public string ahead { get; set; }
	public string vhead { get; set; }
	public string url { get; set; }
	public object backup_url { get; set; }
}

public class Data
{
	public string from { get; set; }
	public string result { get; set; }
	public string message { get; set; }
	public int quality { get; set; }
	public string format { get; set; }
	public int timelength { get; set; }
	public string accept_format { get; set; }
	public List<string> accept_description { get; set; }
	public List<int> accept_quality { get; set; }
	public int video_codecid { get; set; }
	public string seek_param { get; set; }
	public string seek_type { get; set; }
	public List<Durl> durl { get; set; }
}

public class VideoFrame
{ }