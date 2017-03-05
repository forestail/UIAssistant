﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

using KeybindHelper.LowLevel;
using UIAssistant.Core.Enumerators;
using UIAssistant.Core.HitaHint;
using UIAssistant.Core.Settings;
using UIAssistant.Core.Themes;
using UIAssistant.Infrastructure.Commands;
using UIAssistant.Infrastructure.Logger;
using UIAssistant.Infrastructure.Resource.Theme;
using UIAssistant.Interfaces.HUD;
using UIAssistant.Interfaces.Commands;
using UIAssistant.UI.Controls;
using UIAssistant.Utility.Win32;

namespace UIAssistant.Plugin
{
    public static class UIAssistantAPI
    {
        private static Window window { get; set; } = Application.Current.MainWindow;
        private static Control hudPanel { get; set; }
        private static Control contextPanel { get; set; }
        private static Control currentPanel { get; set; }

        public static void Initialize(Control defaultHUDPanel, Control defaultContextPanel)
        {
            if (hudPanel != null)
            {
                // already initialized
                return;
            }
            hudPanel = defaultHUDPanel;
            contextPanel = defaultContextPanel;
            currentPanel = hudPanel;
        }

        public static System.Windows.Threading.Dispatcher UIDispatcher => window.Dispatcher;

        public static void SwitchTheme(string name)
        {
            window.Dispatcher.Invoke(() =>
            {
                ThemeDefaultSwitcher.Switch(name);
            });
        }

        public static void NextTheme()
        {
            window.Dispatcher.Invoke(() =>
            {
                ThemeDefaultSwitcher.Next();
            });
        }

        public static Theme CurrentTheme
        {
            get
            {
                return ThemeDefaultSwitcher.CurrentTheme;
            }
        }

        public static IHUDItemEnumerator GetWidgetEnumerator()
        {
            return new WidgetEnumerator();
        }

        public static IEnumerable<string> GenerateHints(string hintKeys, int quantity)
        {
            if (hintKeys.Contains('|'))
            {
                return AlternateHintGenerator.Generate(hintKeys, quantity);
            }
            return HintGenerator.Generate(hintKeys, quantity);
        }

        public static bool IsContextAvailable { get; private set; }

        public static bool IsContextVisible
        {
            get { return (currentPanel == contextPanel); }
        }

        public static IHUD CurrentHUD
        {
            get
            {
                return window.Dispatcher.Invoke(() =>
                {
                    return currentPanel.DataContext as IHUD;
                });
            }
        }

        public static IHUD DefaultHUD
        {
            get
            {
                return window.Dispatcher.Invoke(() =>
                {
                    return hudPanel.DataContext as IHUD;
                });
            }
        }

        public static void AddDefaultHUD()
        {
            AddPanel(hudPanel);
        }

        public static IHUD DefaultContextHUD
        {
            get
            {
                return window.Dispatcher.Invoke(() =>
                {
                    return contextPanel.DataContext as IHUD;
                });
            }
        }

        public static void AddContextHUD()
        {
            AddPanel(contextPanel, Visibility.Hidden);
            IsContextAvailable = true;
        }

        public static void AddPanel(UIElement uielement, Visibility visibility = Visibility.Visible)
        {
            window.Dispatcher.Invoke(() =>
            {
                var panel = window.Content as Panel;
                if (!panel.Children.Contains(uielement))
                {
                    panel.Children.Add(uielement);
                }
                uielement.Visibility = visibility;
            });
        }

        public static void SwitchHUD()
        {
            window.Dispatcher.Invoke(() =>
            {
                currentPanel.Visibility = Visibility.Hidden;
                if (currentPanel == hudPanel)
                {
                    currentPanel = contextPanel;
                }
                else
                {
                    currentPanel = hudPanel;
                }
                currentPanel.Visibility = Visibility.Visible;
            });
        }

        public static void RemoveDefaultHUD()
        {
            RemovePanel(hudPanel);
            DefaultHUD.Initialize();
            currentPanel = hudPanel;
        }

        public static void RemoveContextHUD()
        {
            RemovePanel(contextPanel);
            DefaultContextHUD.Initialize();
            IsContextAvailable = false;
        }

        public static void RemovePanel(UIElement uielement)
        {
            if (uielement == null)
            {
                return;
            }
            window.Dispatcher.Invoke(() =>
            {
                var panel = window.Content as Panel;
                if (panel.Children.Contains(uielement))
                {
                    panel.Children.Remove(uielement);
                }
                uielement.Visibility = Visibility.Collapsed;
            });
        }

        public static bool TopMost
        {
            set
            {
                window.Dispatcher.Invoke(() =>
                {
                    window.Topmost = value;
                    if (value)
                    {
                        // force topmost
                        IntPtr windowHandle = new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow).Handle;
                        Win32Window.SetWindowPos(windowHandle, Win32Window.HWND.TopMost, false);
                    }
                });
            }
        }

        private static bool _transparent = false;
        public static bool Transparent
        {
            get
            {
                return _transparent;
            }
            set
            {
                _transparent = value;
                window.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (value)
                    {
                        window.Opacity = 0.1d;
                    }
                    else
                    {
                        window.Opacity = 1d;
                    }
                }));
            }
        }

        public static string Localize(string id)
        {
            return Core.I18n.DefaultLocalizer.GetLocalizedText(id);
        }

        public static void RegisterCommand(CommandRule rule)
        {
            CommandManager.Add(rule);
        }

        public static IEnumerable<ICommand> ParseStatement(string statement)
        {
            return CommandManager.Parse(statement);
        }

        public static UserSettings UIAssistantSettings { get { return UserSettings.Instance; } }
#if DEBUG
        public static void DisplayKeystroke(LowLevelKeyEventArgs e)
        {
            KeyVisualizer.Notify(e);
        }
#endif
        public static void PrintDebugMessage(string message)
        {
            Log.Debug(message);
        }

        public static void PrintErrorMessage(Exception ex, string message = null)
        {
            Log.Error(ex, message);
        }

        public static void NotifyWarnMessage(string title, string message)
        {
            Notification.NotifyMessage(title, message, NotificationIcon.Warning);
        }

        public static void NotifyInfoMessage(string title, string message)
        {
            Notification.NotifyMessage(title, message, NotificationIcon.Information);
        }

        public static void NotifyErrorMessage(string title, string message)
        {
            Notification.NotifyMessage(title, message, NotificationIcon.Error);
        }

        static Control _reticle;
        public static void AddTargetingReticle()
        {
            _reticle = window.FindResource("TargetingReticle") as Control;
            var pt = Core.Input.MouseOperation.GetMousePosition();
            window.Dispatcher.Invoke(() =>
            {
                Canvas.SetLeft(_reticle, pt.X);
                Canvas.SetTop(_reticle, pt.Y);
            });
            AddPanel(_reticle);
        }

        public static void RemoveTargetingReticle()
        {
            RemovePanel(_reticle);
            _reticle = null;
        }

        public static void MoveTargetingReticle(double x, double y)
        {
            if ((Math.Abs(x) < 1 && Math.Abs(y) < 1) || _reticle == null)
            {
                return;
            }
            var pt = Core.Input.MouseOperation.GetMousePosition();
            window.Dispatcher.Invoke(() =>
            {
                Canvas.SetLeft(_reticle, pt.X);
                Canvas.SetTop(_reticle, pt.Y);
            });
        }

        public static Control AddIndicator()
        {
            var Indicator = window.FindResource("Indicator") as Control;
            AddPanel(Indicator);
            return Indicator;
        }

        public static void RemoveIndicator(Control indicator)
        {
            RemovePanel(indicator);
        }

        private static AnimationTimeline GenerateAnimation(FrameworkElement element, string propertyName, double from, double to, double duration)
        {
            var animation = new DoubleAnimation() { From = from, To = to, Duration = new Duration(TimeSpan.FromMilliseconds(duration)) };

            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, new PropertyPath(propertyName));
            return animation;
        }

        private static void AnimateWaitably(Action<Storyboard> animation, Action completion, bool waitable = true)
        {
            var isCompleted = false;
            window.Dispatcher.BeginInvoke((Action)(() =>
            {
                var storyboard = new Storyboard();
                animation?.Invoke(storyboard);

                storyboard.Completed += (o, e) =>
                {
                    completion?.Invoke();
                    isCompleted = true;
                };
                storyboard.Begin();
            }));
            while (!isCompleted && waitable)
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        public static void ScaleIndicatorAnimation(Rect from, Rect to, bool waitable = true, double duration = 300, Action completed = null)
        {
            Control indicator = null;
            AnimateWaitably(storyboard =>
            {
                indicator = AddIndicator();
                storyboard.Children.Add(GenerateAnimation(indicator, "Opacity", 1d, 1d, duration));
                storyboard.Children.Add(GenerateAnimation(indicator, "(Canvas.Left)", from.Left, to.Left, duration));
                storyboard.Children.Add(GenerateAnimation(indicator, "(Canvas.Top)", from.Top, to.Top, duration));
                storyboard.Children.Add(GenerateAnimation(indicator, "Width", from.Width, to.Width, duration));
                storyboard.Children.Add(GenerateAnimation(indicator, "Height", from.Height, to.Height, duration));
            }, () => { RemoveIndicator(indicator); completed?.Invoke(); }, waitable);
        }

        public static void FlashIndicatorAnimation(Rect size, bool waitable = true, double duration = 300, Action completed = null)
        {
            Control indicator = null;
            AnimateWaitably(storyboard =>
            {
                indicator = AddIndicator();
                Canvas.SetLeft(indicator, size.X);
                Canvas.SetTop(indicator, size.Y);
                indicator.Width = size.Width;
                indicator.Height = size.Height;

                var animation = GenerateAnimation(indicator, "Opacity", 0d, 1d, duration);
                animation.RepeatBehavior = new RepeatBehavior(3);
                animation.AutoReverse = true;
                storyboard.Children.Add(animation);
            }, () => { RemoveIndicator(indicator); completed?.Invoke(); }, waitable);
        }
    }
}
