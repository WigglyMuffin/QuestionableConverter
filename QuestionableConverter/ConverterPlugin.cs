using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using LLib;

namespace QuestionableConverter;

/// <summary>
/// The last build shipped under the internal name "Questionable". It does no questing: it tells the
/// user that the plugin moved to the internal name "WigglyQuest", installs it on request, and points
/// at the plugin installer to remove itself once WigglyQuest is running.
///
/// Hard rules: never read or save a plugin config (this build shares pluginConfigs/Questionable.json
/// with the file WigglyQuest copies on its first start), and register neither /qst nor any IPC gate
/// (both belong to WigglyQuest once it is loaded).
/// </summary>
public sealed class ConverterPlugin : IDalamudPlugin
{
    private const string Command = "/qstmove";
    private const string MessageTag = "Questionable";

    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ICommandManager _commandManager;
    private readonly IClientState _clientState;
    private readonly IChatGui _chatGui;
    private readonly WindowSystem _windowSystem = new(nameof(QuestionableConverter));
    private readonly DalamudReflector _reflector;
    private readonly PluginInstaller _installer;
    private readonly ConverterWindow _window;

    public ConverterPlugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IClientState clientState,
        IChatGui chatGui,
        IFramework framework,
        IPluginLog pluginLog)
    {
        _pluginInterface = pluginInterface;
        _commandManager = commandManager;
        _clientState = clientState;
        _chatGui = chatGui;

        _reflector = new DalamudReflector(pluginInterface, framework, pluginLog);
        _installer = new PluginInstaller(_reflector, framework, pluginLog, pluginInterface);
        _window = new ConverterWindow(pluginInterface, _installer);
        _windowSystem.AddWindow(_window);

        _pluginInterface.UiBuilder.Draw += _windowSystem.Draw;
        _pluginInterface.UiBuilder.OpenMainUi += OpenWindow;
        _pluginInterface.UiBuilder.OpenConfigUi += OpenWindow;
        _commandManager.AddHandler(Command, new CommandInfo((_, _) => OpenWindow())
        {
            HelpMessage = "Opens the guide for moving to WigglyQuest",
        });

        _clientState.Login += OnLogin;
        if (_clientState.IsLoggedIn)
            OnLogin();
    }

    private void OnLogin()
    {
        _chatGui.Print($"Questionable has moved to WigglyQuest. Type {Command} for the one-click install.",
            MessageTag);
        OpenWindow();
    }

    private void OpenWindow() => _window.IsOpen = true;

    public void Dispose()
    {
        _clientState.Login -= OnLogin;
        _commandManager.RemoveHandler(Command);
        _pluginInterface.UiBuilder.OpenConfigUi -= OpenWindow;
        _pluginInterface.UiBuilder.OpenMainUi -= OpenWindow;
        _pluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        _windowSystem.RemoveAllWindows();
        _installer.Dispose();
        _reflector.Dispose();
    }
}
