using System.Collections.Generic;

namespace MikuSharp.Entities;

public class Durl2
{
	public int order { get; set; }
	public int length { get; set; }
	public int size { get; set; }
	public string ahead { get; set; }
	public string vhead { get; set; }
	public string url { get; set; }
}

public class BiliJson
{
	public string from { get; set; }
	public string result { get; set; }
	public int quality { get; set; }
	public string format { get; set; }
	public int timelength { get; set; }
	public string accept_format { get; set; }
	public List<string> accept_description { get; set; }
	public List<int> accept_quality { get; set; }
	public int video_codecid { get; set; }
	public bool video_project { get; set; }
	public string seek_param { get; set; }
	public string seek_type { get; set; }
	public List<Durl2> durl { get; set; }
}
