using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using static LatihasDupDll.Plugin;

namespace LatihasDupDll;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "InvertIf")]
public class MainWindow() : Window("LatihasDupDll") {
	private static string? GetLatestVersion(string path) {
		var versionDirectories = Directory.GetDirectories(path);
		if (versionDirectories.Length > 0) {
			return versionDirectories
				.Select(dir => new {
					Path = dir,
					Version = Version.TryParse(Path.GetFileName(dir), out var version) ? version : new Version()
				})
				.OrderByDescending(x => x.Version)
				.First()
				.Path;
		}
		return null;
	}

	private static readonly OrderedDictionary<string, OrderedDictionary<Version, HashSet<string>>> dllStatistics = [];

	internal static void Refresh() {
		dllStatistics.Clear();
		var tempRawData = new Dictionary<string, Dictionary<Version, HashSet<string>>>();
		foreach (var pluginRootDir in Directory.EnumerateDirectories(rootPluginPath)) {
			var pluginName = Path.GetFileName(pluginRootDir);
			var latestVerDir = GetLatestVersion(pluginRootDir);
			if (latestVerDir is null) continue;
			foreach (var dllFilePath in Directory.EnumerateFiles(latestVerDir, "*.dll")) {
				var dllFileName = Path.GetFileName(dllFilePath);
				var dllVerInfo = FileVersionInfo.GetVersionInfo(dllFilePath);
				var dllVersion = new Version(dllVerInfo.FileVersion ?? "0.0.0.0");
				if (!tempRawData.TryGetValue(dllFileName, out var verDict)) {
					verDict = new Dictionary<Version, HashSet<string>>();
					tempRawData[dllFileName] = verDict;
				}
				if (!verDict.TryGetValue(dllVersion, out var pluginSet)) {
					pluginSet = [];
					verDict[dllVersion] = pluginSet;
				}
				pluginSet.Add(pluginName);
			}
		}
		var sortedOuterItems = tempRawData
			.Select(kv => new {
				DllName = kv.Key,
				VersionData = kv.Value,
				TotalAllCount = kv.Value.Sum(p => p.Value.Count)
			})
			.OrderBy(item => item.DllName != "OmenTools.dll")
			.ThenByDescending(item => item.TotalAllCount)
			.ToList();
		foreach (var outer in sortedOuterItems) {
			var innerOrderedDict = new OrderedDictionary<Version, HashSet<string>>();
			foreach (var innerKv in outer.VersionData.OrderByDescending(k => k.Key))
				innerOrderedDict.Add(innerKv.Key, innerKv.Value);
			dllStatistics.Add(outer.DllName, innerOrderedDict);
		}
	}

	private static string rootPluginPath => Path.Combine(PluginInterface.DalamudAssetDirectory.Parent!.Parent!.ToString(), "installedPlugins");

	public override void Draw() {
		ImGui.Text(rootPluginPath);
		ImGui.Text($"共扫描DLL种类：{dllStatistics.Count}");
		ImGui.SameLine();
		if (ImGui.Button("刷新列表")) Refresh();
		ImGui.Text($"请注意，同步最新版本后需要重新加载插件才会生效");
		try {
			foreach (var (dllName, value) in dllStatistics) {
				if (ImGui.CollapsingHeader($"{dllName}（版本总数：{value.Sum(i => i.Value.Count)}）")) {
					var maxVer = value.Max(item => item.Key)!;
					foreach (var (ver, valueTuple) in value) {
						ImGui.Text($"├ 版本 {ver} | 出现次数：{valueTuple.Count}");
						if (dllName == "OmenTools.dll") {
							ImGui.Text($"│ 所属插件：{string.Join(", ", valueTuple)}");
							if (valueTuple.Contains("DailyRoutines") && ImGui.Button("全部同步为DR版本")) {
								foreach (var item in valueTuple.Where(item => item != "DailyRoutines")) {
									File.Copy(
										Path.Combine(rootPluginPath, "DailyRoutines", GetLatestVersion(Path.Combine(rootPluginPath, "DailyRoutines"))!, dllName),
										Path.Combine(rootPluginPath, item, GetLatestVersion(Path.Combine(rootPluginPath, item))!, dllName),
										overwrite: true);
									Refresh();
								}
							}
						} else {
							if (ver == maxVer)
								ImGui.Text($"│ 所属插件：{string.Join(", ", valueTuple)}");
							else {
								ImGui.Indent();
								foreach (var item in valueTuple) {
									if (ImGui.Button($"[{item}]同步最新版本")) {
										var latest = value[maxVer].First();
										File.Copy(
											Path.Combine(rootPluginPath, latest, GetLatestVersion(Path.Combine(rootPluginPath, latest))!, dllName),
											Path.Combine(rootPluginPath, item, GetLatestVersion(Path.Combine(rootPluginPath, item))!, dllName),
											overwrite: true);
										Refresh();
									}
								}
								ImGui.Unindent();
							}
						}
					}
				}
			}
		} catch (InvalidOperationException) { } catch (Exception e) { Log.Error(e.ToString()); }
	}
}