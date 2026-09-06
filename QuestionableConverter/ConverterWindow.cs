using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using LLib;
using LLib.ImGui;

namespace QuestionableConverter;

internal sealed class ConverterWindow : LWindow
{
    private const string TargetInternalName = "WigglyQuest";

    // The WigglyMuffin plugin master, in the form Dalamud stores it in the user's repo list, plus the
    // spellings Dalamud treats as the same repository.
    private const string RepoUrl = "https://github.com/WigglyMuffin/DalamudPlugins/raw/main/pluginmaster.json";

    private static readonly string[] RepoAliases =
    [
        "https://raw.githubusercontent.com/WigglyMuffin/DalamudPlugins/main/pluginmaster.json",
        "https://github.com/WigglyMuffin/DalamudPlugins/raw/refs/heads/main/pluginmaster.json",
        "https://raw.githubusercontent.com/WigglyMuffin/DalamudPlugins/refs/heads/main/pluginmaster.json",
    ];

    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly PluginInstaller _installer;
    private readonly IPluginLog _pluginLog;

    private string? _settingsCopyResult;

    public ConverterWindow(IDalamudPluginInterface pluginInterface, PluginInstaller installer, IPluginLog pluginLog)
        : base("Questionable has moved to WigglyQuest###QuestionableConverter")
    {
        _pluginInterface = pluginInterface;
        _installer = installer;
        _pluginLog = pluginLog;

        Size = new Vector2(540, 360);
        SizeCondition = ImGuiCond.FirstUseEver;
        AllowPinning = false;
        AllowClickthrough = false;
    }

    public override void DrawContent()
    {
        var target = _pluginInterface.InstalledPlugins.FirstOrDefault(p => p.InternalName == TargetInternalName);
        bool loaded = target is { IsLoaded: true };
        var state = _installer.GetState(TargetInternalName, RepoUrl);

        Wrapped("Questionable now ships under the internal name \"WigglyQuest\". The name changed so it no " +
                "longer shares Dalamud's install and config folders with the PunishXIV fork of Questionable. " +
                "In the plugin installer the new plugin is still called Questionable.");
        Wrapped("This build does no questing. It only helps you switch.");
        ImGui.Separator();

        ImGui.TextUnformatted("1. Install WigglyQuest");
        ImGui.Indent();
        if (loaded)
            ImGui.TextColored(ImGuiColors.HealerGreen, "WigglyQuest is installed and running.");
        else if (target != null && state.Status == PluginInstallStatus.Succeeded)
            ImGui.TextUnformatted("WigglyQuest is starting…");
        else if (target != null)
            DrawActionButton(state, "Enable", "Enabling…", "Retry Enable", () =>
            {
                CopySettingsToWigglyQuest();
                _installer.TryEnable(TargetInternalName, RepoUrl, RepoAliases);
            });
        else
            DrawActionButton(state, "Install", "Installing…", "Retry Install", () =>
            {
                CopySettingsToWigglyQuest();
                _installer.TryInstall(TargetInternalName, RepoUrl, RepoAliases);
            });

        if (_settingsCopyResult != null)
            Wrapped(_settingsCopyResult);

        if (state.Status == PluginInstallStatus.Failed && !string.IsNullOrEmpty(state.ErrorMessage))
        {
            ImGui.PushTextWrapPos(0f);
            ImGui.TextColored(ImGuiColors.DalamudRed, $"Failed: {state.ErrorMessage}");
            ImGui.PopTextWrapPos();
        }

        ImGui.Unindent();
        ImGui.Spacing();

        ImGui.TextUnformatted("2. Remove this plugin");
        ImGui.Indent();
        if (loaded)
        {
            Wrapped("Uninstall the entry named \"Questionable (moved to WigglyQuest)\" from the plugin " +
                    "installer now; ticking \"delete configuration\" there is safe, WigglyQuest keeps its " +
                    "own copy of your settings.");
            if (ImGui.Button("Open plugin installer"))
                _pluginInterface.OpenPluginInstallerTo(PluginInstallerOpenKind.InstalledPlugins, "moved to WigglyQuest");
        }
        else
        {
            Wrapped("Wait until WigglyQuest is running. Removing this plugin with \"delete configuration\" " +
                    "before you have clicked Install here deletes the settings that would be copied.");
        }

        ImGui.Unindent();
    }

    /// <summary>
    /// Copies this plugin's config file and directory - Dalamud keys both on the internal name, so
    /// they are the user's Questionable settings and local quest files - to the WigglyQuest names,
    /// once, while WigglyQuest has no config of its own. WigglyQuest never looks at the old names: a
    /// fresh install starts from defaults, and a user coming from the PunishXIV fork, which shares the
    /// old names, never has that fork's config imported. The source is only read.
    /// </summary>
    private void CopySettingsToWigglyQuest()
    {
        try
        {
            FileInfo source = _pluginInterface.ConfigFile;
            if (source.DirectoryName == null)
                return;

            var target = new FileInfo(Path.Combine(source.DirectoryName, $"{TargetInternalName}.json"));
            if (target.Exists)
            {
                _settingsCopyResult = "WigglyQuest already has settings of its own; nothing was copied.";
                return;
            }

            if (!source.Exists)
            {
                _settingsCopyResult = "No Questionable settings found; WigglyQuest starts with defaults.";
                return;
            }

            DirectoryInfo sourceDirectory = _pluginInterface.ConfigDirectory;
            if (sourceDirectory.Exists)
                CopyDirectory(sourceDirectory,
                    new DirectoryInfo(Path.Combine(source.DirectoryName, TargetInternalName)));

            source.CopyTo(target.FullName);
            _pluginLog.Information($"Copied {source.FullName} and its directory to the WigglyQuest names");
            _settingsCopyResult = "Your settings and local quest files were copied to WigglyQuest.";
        }
        catch (Exception e)
        {
            _pluginLog.Warning(e, "Unable to copy the settings to WigglyQuest");
            _settingsCopyResult = $"Could not copy the settings ({e.Message}); WigglyQuest starts with defaults.";
        }
    }

    private static void CopyDirectory(DirectoryInfo source, DirectoryInfo target)
    {
        target.Create();
        foreach (FileInfo file in source.EnumerateFiles())
        {
            string destination = Path.Combine(target.FullName, file.Name);
            if (!File.Exists(destination))
                file.CopyTo(destination);
        }

        foreach (DirectoryInfo directory in source.EnumerateDirectories())
            CopyDirectory(directory, new DirectoryInfo(Path.Combine(target.FullName, directory.Name)));
    }

    private void DrawActionButton(PluginInstallState state, string label, string busyLabel, string retryLabel,
        Action action)
    {
        (string text, bool enabled) = state.Status switch
        {
            PluginInstallStatus.InProgress => (busyLabel, false),
            PluginInstallStatus.Failed => (retryLabel, !_installer.IsAnyInstallInProgress),
            _ => (label, !_installer.IsAnyInstallInProgress),
        };

        using (ImRaii.Disabled(!enabled))
        {
            if (ImGui.Button(text))
                action();
        }
    }

    private static void Wrapped(string text)
    {
        ImGui.PushTextWrapPos(0f);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
    }
}
