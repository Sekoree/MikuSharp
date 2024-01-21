using System.Collections.Generic;

namespace MikuSharp.Entities;

public class Durl2
{
	public int Order { get; set; }
	public int Length { get; set; }
	public int Size { get; set; }
	public string Ahead { get; set; }
	public string Vhead { get; set; }
	public string Url { get; set; }
}

public class BiliJson
{
	public string From { get; set; }
	public string Result { get; set; }
	public int Quality { get; set; }
	public string Format { get; set; }
	public int Timelength { get; set; }
	public string AcceptFormat { get; set; }
	public List<string> AcceptDescription { get; set; }
	public List<int> AcceptQuality { get; set; }
	public int VideoCodecid { get; set; }
	public bool VideoProject { get; set; }
	public string SeekParam { get; set; }
	public string SeekType { get; set; }
	public List<Durl2> Durl { get; set; }
}