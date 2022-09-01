using AdonisUI;
using AdonisUI.Controls;

using DisCatSharp.Enums;
using DisCatSharp.TranslationGenerator;

using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Colors = System.Windows.Media.Colors;

namespace DisCatSharp.TranslationGenerator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : AdonisWindow
{
	/// <summary>
	/// Gets or sets whether the app uses dark mode as style.
	/// </summary>
	public static bool IsDark { get; internal set; } = true;

	public MainWindow()
	{
		InitializeComponent();
		ResourceLocator.SetColorScheme(Application.Current.Resources, IsDark ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme);
		DisableTranslationEditStuff();
		App.LoadTranslationTemplates();
		ShowLoadedTranslationFiles();
	}

	private void DisableTranslationEditStuff()
	{
		translationGroupBox.IsEnabled = false;
		translationControlGroupBox.IsEnabled = false;
		translationGroupBox.Visibility = Visibility.Hidden;
		translationControlGroupBox.Visibility = Visibility.Hidden;
		currentFileInfo.Text = "-- translation edit inactive --";
	}

	private void InitTranslationEditStuff(string file)
	{
		translationGroupBox.IsEnabled = false;
		translationControlGroupBox.IsEnabled = true;
		translationGroupBox.Visibility = Visibility.Visible;
		translationControlGroupBox.Visibility = Visibility.Visible;
		currentFileInfo.Text = $"-- editing {file} --";
	}

	public void ShowLoadedTranslationFiles()
	{
		foreach (var name in App.LoadedGroupTranslationTemplates.Keys)
			AddLTtfInformation(name.RemoveLtfBs(), name);
		foreach (var name in App.LoadedNormalTranslationTemplates.Keys)
			AddLTtfInformation(name.RemoveLtfBs(), name);
	}

	public void AddLTtfInformation(string title, string id)
	{
		var rd = new RowDefinition
		{
			SharedSizeGroup = "LTFAUTO",
			Height = new GridLength(32)
		};
		ltfGrid.RowDefinitions.Add(rd);
		var cd = new ColumnDefinition
		{
			SharedSizeGroup = "LTFCAUTO",
			Width = GridLength.Auto
		};
		ltfGrid.ColumnDefinitions.Add(cd);
		var cd2 = new ColumnDefinition
		{
			SharedSizeGroup = "LTFCAUTO",
			Width = GridLength.Auto
		}; ;
		ltfGrid.ColumnDefinitions.Add(cd2);
		TextBox box = new()
		{
			IsReadOnly = true,
			Text = title
		};
		Button btn = new()
		{
			Background = new SolidColorBrush(Colors.Green),
			Content = "Edit translations",
			Tag = id,
		};
		btn.Click += TranslationFileSelectButtonClick;
		ltfGrid.Children.Add(box);
		ltfGrid.Children.Add(btn);
		Grid.SetRow(box, ltfGrid.RowDefinitions.Count - 1);
		Grid.SetRow(btn, ltfGrid.RowDefinitions.Count - 1);
		Grid.SetColumn(box, 0);
		Grid.SetColumn(btn, 1);
	}

	private void TranslationFileSelectButtonClick(object sender, RoutedEventArgs e)
	{
		var btn = (Button)sender;
		foreach(var child in ltfGrid.Children)
		{
			try
			{
				var button = (Button)child;
				button.IsEnabled = false;
			}
			catch (Exception) 
			{ }
		}
		string curFile = (string)btn.Tag;

		InitTranslationEditStuff(curFile);
		if (curFile.Contains("GROUP"))
		{
			var data = App.LoadedGroupTranslationTemplates[curFile];
			foreach (var dat in data)
			{
				ApplicationCommand box = new()
				{
					ACName = $"/{dat.Name}",
					ACDesc = dat.Description,
					Height = 60,
					Width = 600,
					Name = dat.Name
				};
				translationEditStackPanel.Children.Add(box);
			}
		} else
		{
			var data = App.LoadedNormalTranslationTemplates[curFile];
			foreach (var dat in data)
			{
				var prefix = dat.Type == ApplicationCommandType.ChatInput ? "/" : "#";
				ApplicationCommand box = new()
				{
					ACName = $"{prefix}{dat.Name}",
					ACDesc = dat.Description ?? "Not applicable",
					Height = 60,
					Width = 600,
					Name = dat.Name.Replace("-", "").Replace(" ", "")
				};
				translationEditStackPanel.Children.Add(box);
			}
		}
	}

	private void TranslationCloseButton_Click(object sender, RoutedEventArgs e)
	{
		DisableTranslationEditStuff();
		translationEditStackPanel.Children.Clear();
		foreach (var child in ltfGrid.Children)
		{
			try
			{
				var button = (Button)child;
				button.IsEnabled = true;
			}
			catch (Exception)
			{ }
		}
	}
}
