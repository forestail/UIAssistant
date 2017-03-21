﻿using System;

using System.ComponentModel.Composition;
using UIAssistant.Interfaces.API;
using UIAssistant.Interfaces.Commands;
using UIAssistant.Interfaces.Plugin;
using UIAssistant.Interfaces.Resource;

using UIAssistant.Plugin.SearchByText.Enumerators;

namespace UIAssistant.Plugin.SearchByText
{
    [Export(typeof(IPlugin))]
    [Export(typeof(IConfigurablePlugin))]
    [Export(typeof(ILocalizablePlugin))]
    [Export(typeof(IDisposable))]
    [ExportMetadata("Guid", "cd70037d-4b69-4c5d-8882-5db95fdd5127")]
    [ExportMetadata("Name", Consts.PluginName)]
    [ExportMetadata("Author", "u338.steven")]
    [ExportMetadata("SupportUri", "https://github.com/u338steven/UIAssistant/")]
    [ExportMetadata("IconUri", "/SearchByText;component/Resources/SearchByText.png")]
    [ExportMetadata("Version", "0.1")]
    [ExportMetadata("CommandName", Consts.Command)]
    public class SearchByText : IPlugin, IConfigurablePlugin, ILocalizablePlugin, IDisposable
    {
        internal static IUIAssistantAPI UIAssistantAPI { get; private set; }
        internal static SearchByTextSettings Settings { get; private set; }
        private static ILocalizer _localizer;
        private StateController _stateController;
        private KeyInputController _keyController;

        public void Initialize(IUIAssistantAPI api)
        {
            UIAssistantAPI = api;

            Settings = SearchByTextSettings.Load();
            _stateController = new StateController(api);
            _keyController = new KeyInputController(api, _stateController);
            _localizer = api.GetLocalizer();
            RegisterCommand();
        }

        private void RegisterCommand()
        {
            var argCommands = UIAssistantAPI.CreateArgmentRule(Consts.Commands, _ => _stateController.ChangeTarget(EnumerateTarget.Commands));
            var argTextsInWindow = UIAssistantAPI.CreateArgmentRule(Consts.TextsInWindow, _ => _stateController.ChangeTarget(EnumerateTarget.TextsInWindow));
            var argTextsInContainer = UIAssistantAPI.CreateArgmentRule(Consts.TextsInContainer, _ => _stateController.ChangeTarget(EnumerateTarget.TextsInContainer));
            var argRunningApps = UIAssistantAPI.CreateArgmentRule(Consts.RunningApps, _ => _stateController.ChangeTarget(EnumerateTarget.RunningApps));
            var argContextMenu = UIAssistantAPI.CreateArgmentRule(Consts.ContextMenu, _ => _stateController.ChangeTarget(EnumerateTarget.ContextMenu));

            var optAutoFire = UIAssistantAPI.CreateArgmentRule(Consts.AutoFire, _ => _stateController.AutoFire = true);

            var command = UIAssistantAPI.CreateCommandRule(Consts.Command, Run,
                new[] { argCommands, argTextsInWindow, argTextsInContainer, argRunningApps, argContextMenu },
                new[] { optAutoFire });
            command.Description = Consts.PluginName;
            UIAssistantAPI.RegisterCommand(command);
        }

        public void Setup()
        {
            _stateController.Initialize();
            var keyController = UIAssistantAPI.CreateKeyboardController(_keyController, _stateController.Session);
            keyController.AddHidingProcess();
            UIAssistantAPI.UIDispatcher.Invoke(() => keyController.AddUsagePanelProcess(new Usage()));
            keyController.Observe();

            UIAssistantAPI.SwitchTheme(UIAssistantAPI.UIAssistantSettings.Theme);
        }

        public void Run(ICommand command)
        {
            try
            {
                _stateController.Enumerate();
                UIAssistantAPI.AddDefaultHUD();
                UIAssistantAPI.AddContextHUD();
                UIAssistantAPI.TopMost = true;
            }
            catch (Exception ex)
            {
                UIAssistantAPI.PrintErrorMessage(ex);
            }
        }

        public System.Windows.FrameworkElement GetConfigrationInterface()
        {
            return new Settings();
        }

        public void Save()
        {
            Settings.Save();
        }

        public void Localize()
        {
            _localizer?.SwitchLanguage(UIAssistantAPI.CurrentLanguage);

            Settings.Expand.Text = _localizer.GetLocalizedText("sbtExpand");
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _keyController?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
