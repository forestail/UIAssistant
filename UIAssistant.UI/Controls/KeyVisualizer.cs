﻿#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeybindHelper;
using KeybindHelper.LowLevel;
using KeystrokeVisualizer;

namespace UIAssistant.UI.Controls
{
    // For Debug
    // corner-cutting...
    public class KeyVisualizer
    {
        static KeystrokeVisualizer.KeystrokeVisualizer _visualizer;
        static System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        static Keystroke current;
        public static bool IsActive { get; private set; } = false;

        public static void Initialize()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => _visualizer = new KeystrokeVisualizer.KeystrokeVisualizer());
            IsActive = true;
        }

        public static void Notify(System.Windows.Input.Key key, KeySet keysState)
        {
            if (!IsActive)
            {
                Initialize();
            }
            var input = keysState.ConvertToCurrentLanguage();
            if (input.Trim().Length > 0 && char.IsLetterOrDigit(input[0]))
            {
                _visualizer?.Dispatcher.BeginInvoke((Action)(() => Notify(input)));
            }
            else if (!key.IsModifiersKey())
            {
                _visualizer?.Dispatcher.BeginInvoke((Action)(() => Notify(keysState.ToString(), true)));
            }
        }

        static bool beforeIsNew = false;
        private static void Notify(string message, bool isNew = false)
        {
            sw.Stop();
            if (sw.ElapsedMilliseconds > 1000 || current == null || isNew || (!isNew && beforeIsNew))
            {
                current = new Keystroke { Keys = message };
                _visualizer.AddNotification(current);
                sw.Reset();
                sw.Start();
                beforeIsNew = isNew;
                return;
            }
            current.Keys += message;
            sw.Reset();
            sw.Start();
            beforeIsNew = isNew;
        }
    }
}
#endif