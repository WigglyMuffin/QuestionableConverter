using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
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

    public ConverterWindow(IDalamudPluginInterface pluginInterface, PluginInstaller installer)
        : base("Questionable has moved to WigglyQuest###QuestionableConverter")
    {
        _pluginInterface = pluginInterface;
        _installer = installer;

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
            DrawActionButton(state, "Enable", "Enabling…", "Retry Enable",
                () => _installer.TryEnable(TargetInternalName, RepoUrl, RepoAliases));
        else
            DrawActionButton(state, "Install", "Installing…", "Retry Install",
                () => _installer.TryInstall(TargetInternalName, RepoUrl, RepoAliases));

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
            Wrapped("WigglyQuest copied your settings and local quest files when it started. Uninstall the " +
                    "entry named \"Questionable (moved to WigglyQuest)\" from the plugin installer now; " +
                    "ticking \"delete configuration\" there is safe.");
            if (ImGui.Button("Open plugin installer"))
                _pluginInterface.OpenPluginInstallerTo(PluginInstallerOpenKind.InstalledPlugins, "moved to WigglyQuest");
        }
        else
        {
            Wrapped("Wait until WigglyQuest is running. It copies your settings on its first start, and " +
                    "removing this plugin with \"delete configuration\" before that loses them.");
        }

        ImGui.Unindent();
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
