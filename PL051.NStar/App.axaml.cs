global using Avalonia;
global using Avalonia.Controls.ApplicationLifetimes;
global using Avalonia.Markup.Xaml;
global using PL051.NStar.ViewModels;
global using PL051.NStar.Views;
global using NStar.Core;
global using NStar.Linq;
global using System;
global using System.Diagnostics;
global using System.IO;
global using System.Threading.Tasks;
global using static PL051.NStar.BuiltInMemberCollections;
global using static System.Math;
global using G = System.Collections.Generic;
global using String = NStar.Core.String;

namespace PL051.NStar;

public partial class App : Application
{
	public override void Initialize() => AvaloniaXamlLoader.Load(this);

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = new MainWindow
			{
				DataContext = new MainViewModel()
			};
		}
		else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
		{
			singleViewPlatform.MainView = new MainView
			{
				DataContext = new MainViewModel()
			};
		}

		base.OnFrameworkInitializationCompleted();
	}
}
