﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Input;

using KeybindHelper;
using UIAssistant.Core.API;
using UIAssistant.Core.I18n;
using UIAssistant.Interfaces.Settings;
using UIAssistant.Utility;

namespace UIAssistant.Core.Settings
{
    public class UserSettings : IUserSettings
    {
        public bool RunAtLogin { get; set; }
        public bool UseMigemo { get; set; }
        public HashSet<string> DisabledPlugins { get; set; } = new HashSet<string>();
        public string Culture { get; set; }
        public string MigemoDllPath { get; set; }
        public string MigemoDictionaryPath { get; set; }
        public List<Keybind> Commands { get; set; } = new List<Keybind>();
        public string Theme { get; set; } = "General";

        public int ItemsCountPerPage { get; set; }

        #region UIAssistant default keybinds
        public Keybind Quit { get; set; } = new Keybind();
        public Keybind Back { get; set; } = new Keybind();
        public Keybind Delete { get; set; } = new Keybind();
        public Keybind Clear { get; set; } = new Keybind();

        public Keybind Left { get; set; } = new Keybind();
        public Keybind Right { get; set; } = new Keybind();
        public Keybind Up { get; set; } = new Keybind();
        public Keybind Down { get; set; } = new Keybind();
        public Keybind PageUp { get; set; } = new Keybind();
        public Keybind PageDown { get; set; } = new Keybind();
        public Keybind Home { get; set; } = new Keybind();
        public Keybind End { get; set; } = new Keybind();

        public Keybind Execute { get; set; } = new Keybind();
        public Keybind ShowExtraActions { get; set; } = new Keybind();
        public Keybind TemporarilyHide { get; set; } = new Keybind();
        public Keybind SwitchKeyboardLayout { get; set; } = new Keybind();
        public Keybind SwitchTheme { get; set; } = new Keybind();
        public Keybind Usage { get; set; } = new Keybind();
        public Keybind EmergencySwitch { get; set; } = new Keybind();
        #endregion

        private const string FileName = "Settings.yml";
        public static readonly string FilePath = Path.Combine(UIAssistantDirectory.Configurations, FileName);

        public UserSettings()
        {
            RunAtLogin = AutoRunAtLoginScheduler.Exists();
        }

        public void SetValuesDefault()
        {
            UseMigemo = false;
            Culture = DefaultLocalizer.SuggestedCulture;
            if (Culture == "ja-JP")
            {
                UseMigemo = true;
            }
            else if (Culture == "zh-CN" || Culture == "zh-TW")
            {
                UseMigemo = true;
                MigemoDictionaryPath = @".\dict\migemo-dict-zh";
            }
            Theme = "General";

            Commands.Add(new Keybind(Key.E, LRExtendedModifierKeys.Alt | LRExtendedModifierKeys.Control, "hah WidgetsInWindow Click"));
            Commands.Add(new Keybind(Key.Q, LRExtendedModifierKeys.Alt | LRExtendedModifierKeys.Control, "hah RunningApps Switch"));
            Commands.Add(new Keybind(Key.R, LRExtendedModifierKeys.Alt | LRExtendedModifierKeys.Control, "hah WidgetsInTaskbar RightClick"));
            Commands.Add(new Keybind(Key.G, LRExtendedModifierKeys.Alt | LRExtendedModifierKeys.Control, "hah Dividedscreen MouseEmulation"));

            Commands.Add(new Keybind(Key.T, LRExtendedModifierKeys.Alt | LRExtendedModifierKeys.Control, "/ TextsInWindow"));
            Commands.Add(new Keybind(Key.L, LRExtendedModifierKeys.Alt | LRExtendedModifierKeys.Control, "/ TextsInContainer"));
            Commands.Add(new Keybind(Key.K, LRExtendedModifierKeys.Alt | LRExtendedModifierKeys.Control, "/ Commands"));

            ItemsCountPerPage = 15;
            Quit = new Keybind(Key.Escape);
            Back = new Keybind(Key.H, LRExtendedModifierKeys.LControl);
            Delete = new Keybind(Key.D, LRExtendedModifierKeys.LControl);
            Clear = new Keybind(Key.K, LRExtendedModifierKeys.LControl);

            Left = new Keybind(Key.B, LRExtendedModifierKeys.LControl);
            Right = new Keybind(Key.F, LRExtendedModifierKeys.LControl);
            Up = new Keybind(Key.P, LRExtendedModifierKeys.LControl);
            Down = new Keybind(Key.N, LRExtendedModifierKeys.LControl);
            PageUp = new Keybind(Key.V, LRExtendedModifierKeys.LAlt);
            PageDown = new Keybind(Key.V, LRExtendedModifierKeys.LControl);
            Home = new Keybind(Key.A, LRExtendedModifierKeys.LControl);
            End = new Keybind(Key.E, LRExtendedModifierKeys.LControl);

            Execute = new Keybind(Key.Enter);
            ShowExtraActions = new Keybind(Key.Tab);

            TemporarilyHide = new Keybind(Key.T, LRExtendedModifierKeys.LControl);
            SwitchKeyboardLayout = new Keybind(Key.Space, LRExtendedModifierKeys.LWindows);
            SwitchTheme = new Keybind(Key.S, LRExtendedModifierKeys.LAlt | LRExtendedModifierKeys.LControl);
            Usage = new Keybind(Key.U, LRExtendedModifierKeys.LControl);
            EmergencySwitch = new Keybind(Key.Escape, LRExtendedModifierKeys.LWindows);
        }

        //public static UserSettings Load(IFileIO fileIO)
        //{
        //    return fileIO.Read(typeof(UserSettings), FilePath) as UserSettings;
        //}

        public void Save()
        {
            UIAssistantAPI.Instance.DefaultSettingsFileIO.Write(typeof(UserSettings), this, FilePath);
        }
    }
}
