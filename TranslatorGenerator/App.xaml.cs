using DisCatSharp.ApplicationCommands.Entities;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;

namespace DisCatSharp.TranslationGenerator;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	internal static Dictionary<string, List<GroupTranslator>> LoadedGroupTranslationTemplates = new();
	internal static Dictionary<string, List<CommandTranslator>> LoadedNormalTranslationTemplates = new();

	public App()
	{
		if (!Directory.Exists("in/"))
		{
			Directory.CreateDirectory("in/");
			var res = MessageBox.Show("Please place your template files in the \"in/\" folder and restart the app.", "Translation in folder created", MessageBoxButton.OK);
			if (res == MessageBoxResult.OK)
			{
				Current.Shutdown(1);
				Environment.Exit(1);
			}
		}
	}

	public static void LoadTranslationTemplates()
	{
		var translation_path = "in/";
		var files = Directory.GetFiles(translation_path, "translation_generator_export*", SearchOption.TopDirectoryOnly);
		foreach (var file in files.Where(x => x.Contains("GROUP")))
			LoadedGroupTranslationTemplates.Add(file.Replace("in/", ""), JsonConvert.DeserializeObject<List<GroupTranslator>>(File.ReadAllText(file)));
		foreach (var file in files.Where(x => x.Contains("SINGLE")))
			LoadedNormalTranslationTemplates.Add(file.Replace("in/", ""), JsonConvert.DeserializeObject<List<CommandTranslator>>(File.ReadAllText(file)));
	}
}
