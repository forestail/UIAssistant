﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

using Livet;

using UIAssistant.Core.Settings;
using UIAssistant.Core.I18n;
using UIAssistant.Infrastructure.Commands;
using UIAssistant.UI.Controls;
using UIAssistant.Plugin;
using KeybindHelper;

namespace UIAssistant.Models
{
    public class SettingsWindowModel : NotificationObject
    {
        /*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */
        public SettingsWindowModel()
        {
        }

        public static void RegisterHotkeys()
        {
            var setting = UserSettings.Instance;
            HotkeyResistrant.UnregisterAll();
            foreach (var hotkey in setting.Commands)
            {
                var validationResult = CommandManager.GetValidator(DefaultLocalizer.Instance).Validate(hotkey.Text);
                if (validationResult != ValidationResult.Success)
                {
                    UIAssistantAPI.NotifyWarnMessage("Warning", string.Format(TextID.RegisterHotkeyFailed.GetLocalizedText(), validationResult.ErrorMessage));
                    continue;
                }
                var action = PluginManager.Instance.GenerateAction(hotkey.Text);
                try
                {
                    HotkeyResistrant.Register(hotkey.ModifierKeys, hotkey.Key, action);
                }
                catch (DuplicateHotkeyException ex)
                {
                    Notification.NotifyMessage("Waring", string.Format(TextID.HotkeyDuplication.GetLocalizedText(), ex.Message), NotificationIcon.Warning);
                }
            }
        }
    }
}
