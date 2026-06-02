using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace LatihasDupDll;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class Plugin : IDalamudPlugin {
	private readonly MainWindow _mainWindow;
	// ReSharper disable once MemberCanBePrivate.Global
	public readonly WindowSystem WindowSystem = new("LatihasDupDll");

	public Plugin() {
		Configuration = PluginInterface.GetPluginConfig() as MConfiguration ?? new MConfiguration();
		_mainWindow = new MainWindow();
		WindowSystem.AddWindow(_mainWindow);
		var p = new CommandInfo(OnCommand) {
			HelpMessage = "打开主界面"
		};
		CommandManager.AddHandler("/ldd", p);
		PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
		PluginInterface.UiBuilder.OpenMainUi += OnCommand;
	}

	internal static MConfiguration Configuration { get; private set; } = null!;

	[PluginService] public static INotificationManager NotificationManager { get; private set; } = null!;
	[PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
	[PluginService] internal static IDataManager DataManager { get; private set; } = null!;
	[PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
	[PluginService] internal static IPluginLog Log { get; private set; } = null!;
	[PluginService] private static ICommandManager CommandManager { get; set; } = null!;
	[PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
	[PluginService] private static IFramework Framework { get; set; } = null!;
	[PluginService] internal static IObjectTable ObjectTable { get; set; } = null!;
	[PluginService] internal static IGameGui GameGui { get; set; } = null!;
	[PluginService] internal static ICondition Condition { get; set; } = null!;

	public void Dispose() {
		WindowSystem.RemoveAllWindows();
		CommandManager.RemoveHandler("/ldd");
	}

	private void OnCommand(string command, string args) => OnCommand();

	private void OnCommand() {
		_mainWindow.Toggle();
		MainWindow.Refresh();
	}
}