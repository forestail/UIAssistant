﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Input;
using KeybindHelper;
using KeybindHelper.LowLevel;

using UIAssistant.Core.Input;
using UIAssistant.Core.I18n;

namespace UIAssistant.Plugin.KeybindsManiacs
{
    enum Mode : int
    {
        Normal,
        Insert,
        Visual,
        Search,
        Command,
        UserCustom,
        Unknown,
    }

    static class ModeExtensions
    {
        public static bool EqualsName(this Mode mode, string modeName)
        {
            return Enum.GetNames(typeof(Mode)).FirstOrDefault(x => x == modeName) != null;
        }

        public static Mode GetMode(string modeName)
        {
            var names = Enum.GetNames(typeof(Mode));
            var values = Enum.GetValues(typeof(Mode)).Cast<Mode>();
            if(!values.Any(x => x.EqualsName(modeName)))
            {
                return Mode.Unknown;
            }
            return Enum.GetValues(typeof(Mode)).Cast<Mode>().FirstOrDefault(x => Enum.GetName(typeof(Mode), x) == modeName);
        }
    }

    class KeybindStorage
    {
        public Dictionary<KeySet, bool> OneShotDefined { get; set; } = new Dictionary<KeySet, bool>();
        public KeybindManager OneShotKeybinds { get; set; } = new KeybindManager();
        public KeybindManager OneShotKeyDown { get; set; } = new KeybindManager();
        public KeybindManager OneShotKeyUp { get; set; } = new KeybindManager();
        public KeybindManager Keybinds { get; set; } = new KeybindManager();
        public bool IsEnabledWindowsKeybinds { get; set; } = false;
        public bool IsPrefix { get; set; } = false;

        public KeybindStorage()
        {

        }
    }

    class KeyInputController : AbstractKeyInputController
    {
        private StateController _stateController;
        private KeybindsManiacsSettings _settings;
        private Dictionary<string, KeybindStorage> _keybindsDic { get; set; } = new Dictionary<string, KeybindStorage>();
        private KeybindStorage _currentKeybinds { get; set; } = new KeybindStorage();

        public KeyInputController(StateController controller) : base(controller)
        {
            _stateController = controller;
            _settings = _stateController.Settings;
            InitializeKeybind();
        }

        protected override void InitializeKeybind()
        {
            _keybindsDic.Clear();
            foreach (var keybinds in _settings.KeybindsInMode)
            {
                Keybinds = new KeybindManager();
                var keybindStorage = new KeybindStorage();
                var oneShotDefined = new Dictionary<KeySet, bool>();
                foreach (var keybind in keybinds.Value.Keybinds)
                {
                    if (keybind.InputKeys == null)
                    {
                        continue;
                    }
                    var keyset = new KeySet(keybind.InputKeys);
                    var canDefineOneShot = keybind.CanDefineOneShot;
                    if (canDefineOneShot)
                    {
                        oneShotDefined.Add(keyset, canDefineOneShot);
                    }
                    switch (keybind.Type)
                    {
                        case CommandType.SwapKey:
                            if (canDefineOneShot)
                            {
                                keybindStorage.OneShotKeybinds.Add(keyset, () =>
                                {
                                    var keys = keybind.OneShot;
                                    if (_stateController.Mode == Mode.Visual && ContainsMovingKey(keys))
                                    {
                                        KeyboardOperation.SendKeys(new[] { Key.RightShift }.Concat(keys).ToArray());
                                        return;
                                    }
                                    KeyboardOperation.SendKeys(keys);
                                });
                                keybindStorage.OneShotKeyDown.Add(keyset, () =>
                                {
                                    var keys = keybind.OutputKeys;
                                    if (_stateController.Mode == Mode.Visual && ContainsMovingKey(keys))
                                    {
                                        KeyboardOperation.KeyDown(new[] { Key.RightShift }.Concat(keys).ToArray());
                                        return;
                                    }
                                    KeyboardOperation.KeyDown(keys);
                                });
                                keybindStorage.OneShotKeyUp.Add(keyset, () =>
                                {
                                    var keys = keybind.OutputKeys;
                                    if (_stateController.Mode == Mode.Visual && ContainsMovingKey(keys))
                                    {
                                        KeyboardOperation.KeyUp(new[] { Key.RightShift }.Concat(keys).ToArray());
                                        return;
                                    }
                                    KeyboardOperation.KeyUp(keys);
                                });
                                break;
                            }
                            Keybinds.Add(keyset, () =>
                            {
                                var keys = keybind.OutputKeys;
                                if (_stateController.Mode == Mode.Visual && ContainsMovingKey(keys))
                                {
                                    KeyboardOperation.SendKeys(new[] { Key.RightShift }.Concat(keys).ToArray());
                                    return;
                                }
                                KeyboardOperation.SendKeys(keys);
                            });
                            break;
                        case CommandType.RunEmbeddedCommand:
                            Keybinds.Add(keyset, ParseCommand(keybind.CommandText, keybind.InputKeys));
                            break;
                        case CommandType.RunUIAssistantCommand:
                            Keybinds.Add(keyset, () =>
                            {
                                var command = keybind.CommandText;
                                if (PluginManager.Instance.Exists(command))
                                {
                                    PluginManager.Instance.Execute(command);
                                }
                                else
                                {
                                    UIAssistantAPI.NotifyWarnMessage("Plugin Error", string.Format(TextID.CommandNotFound.GetLocalizedText(), command));
                                }
                            });
                            break;
                        //case CommandType.RunExtensionCommand:
                        //    break;
                        default:
                            break;
                    }
                }
                base.InitializeKeybind();
                keybindStorage.IsEnabledWindowsKeybinds = keybinds.Value.IsEnabledWindowsKeybinds;
                keybindStorage.IsPrefix = keybinds.Value.IsPrefix;
                keybindStorage.OneShotDefined = oneShotDefined;
                keybindStorage.Keybinds = Keybinds;
                _keybindsDic.Add(keybinds.Key, keybindStorage);
            }
            SwitchMode(_settings.Mode, true);
        }

        private bool ContainsMovingKey(Key[] keys)
        {
            return keys.Any(x => IsMovingKey(x));
        }

        private bool IsMovingKey(Key key)
        {
            if (key == Key.Left || key == Key.Right || key == Key.Down || key == Key.Up || key == Key.PageDown || key == Key.PageUp || key == Key.Home || key == Key.End)
            {
                return true;
            }
            return false;
        }

        private int _startIndex = -1;
        private Action ParseCommand(string command, Key[] keys)
        {
            if (string.IsNullOrEmpty(command))
            {
                return null;
            }
            var commands = command.Split(' ');
            if (commands[0] == "Cancel")
            {
                return () =>
                {
                    if (_command != null)
                    {
                        _command = null;
                        return;
                    }
                    SwitchMode(_settings.Mode);
                };
            }
            else if (commands[0] == "Exit")
            {
                return () =>
                {
                    Toggle();
                };
            }
            else if (commands[0] == "ChangeMode")
            {
                if (commands.Length < 2)
                {
                    return null;
                }
                
                var mode = ModeExtensions.GetMode(commands[1]);
                var modeName = commands[1];
                if (mode == Mode.Unknown)
                {
                    return () =>
                    {
                        _startIndex = -1;
                        if (mode == Mode.Visual)
                        {
                            if (_stateController.Mode == Mode.Visual)
                            {
                                //SwitchMode(Mode.Normal);
                                SwitchMode(_settings.Mode);
                                return;
                            }
                            _startIndex = EmbeddedCommand.GetCaretPos();
                        }
                        SwitchMode(modeName);
                    };
                }
                return () =>
                {
                    _startIndex = -1;
                    if (mode == Mode.Visual)
                    {
                        if (_stateController.Mode == Mode.Visual)
                        {
                            //SwitchMode(Mode.Normal);
                            SwitchMode(_settings.Mode);
                            return;
                        }
                        _startIndex = EmbeddedCommand.GetCaretPos();
                    }
                    SwitchMode(mode);
                };
            }
            else if (commands[0] == "VimLike")
            {
                if (commands.Length < 2)
                {
                    return null;
                }
                if (commands[1] == "f")
                {
                    return () => FindCommand();
                }
                else if (commands[1] == "F")
                {
                    return () => FindCommand(true);
                }
                else if (commands[1] == "t")
                {
                    return () => FindCommand(false, true);
                }
                else if (commands[1] == "T")
                {
                    return () => FindCommand(true, true);
                }
                else if (commands[1] == ";")
                {
                    return () => _repeatFind?.Invoke();
                }
                else if (commands[1] == ",")
                {
                    return () => _reverseFind?.Invoke();
                }
                else if (commands[1] == "y")
                {
                    return () => YankCommand(keys);
                }
            }
            else if(commands[0] == "EmacsLike")
            {
                if (commands.Length < 2)
                {
                    return null;
                }
                if (commands[1] == "yank")
                {
                    return () => EmacsLikeCommand.Yank(_stateController, _settings);
                }
                else if(commands[1] == "kill-ring-save")
                {
                    return () => EmacsLikeCommand.KillRingSave(_stateController, _settings);
                }
                else if (commands[1] == "kill-region")
                {
                    return () => EmacsLikeCommand.KillRegion(_stateController, _settings);
                }
            }
            return null;
        }

        private void YankCommand(Key[] keys)
        {
            if (_stateController.Mode == Mode.Visual)
            {
                KeyboardOperation.SendKeys(Key.RightCtrl, Key.C);
                //SwitchMode(Mode.Normal);
                SwitchMode(_settings.Mode, true);
                return;
            }
            _command = (_, keysState) =>
            {
                if (keysState.Equals(new KeySet(keys)))
                {
                    KeyboardOperation.SendKeys(Key.Home, Key.None, Key.RightShift, Key.End, Key.None, Key.RightCtrl, Key.C);
                    _command = null;
                    return;
                }

                if (_currentKeybinds.Keybinds.Contains(keysState))
                {
                    SwitchMode(Mode.Visual, true);
                    _currentKeybinds.Keybinds[keysState].Invoke();
                }
                KeyboardOperation.SendKeys(Key.RightCtrl, Key.C);
                if (_stateController.Mode == Mode.Visual)
                {
                    //SwitchMode(Mode.Normal, true);
                    SwitchMode(_settings.Mode, true);
                }
                _command = null;
            };
        }

        private void FindCommand(bool backward = false, bool before = false)
        {
            _command = (x, _) =>
            {
                Task.Run(() => EmbeddedCommand.Find(_startIndex, x, backward, before));
                _repeatFind = () => Task.Run(() => EmbeddedCommand.Find(_startIndex, x, backward, before));
                _reverseFind = () => Task.Run(() => EmbeddedCommand.Find(_startIndex, x, !backward, before));
                _command = null;
            };
        }

        private void SwitchModeCommon(string modeName, bool isSilent = false)
        {
            bool before = _currentKeybinds.IsEnabledWindowsKeybinds;
            if (_keybindsDic.ContainsKey(modeName))
            {
                _currentKeybinds = _keybindsDic.FirstOrDefault(x => x.Key == modeName).Value;
            }
            else
            {
                if (_keybindsDic.ContainsKey(_settings.Mode))
                {
                    _currentKeybinds = _keybindsDic[_settings.Mode];
                }
                else
                {
                    _currentKeybinds = _keybindsDic[Consts.DefaultMode];
                }
            }
            Keybinds = _currentKeybinds.Keybinds;

            if (_currentKeybinds.IsPrefix)
            {
                _command = (_, keysState) =>
                {
                    if (Keybinds.Contains(keysState))
                    {
                        Keybinds[keysState].Invoke();
                    }
                    SwitchMode(_settings.Mode, true);
                    _command = null;
                };
            }

            if (before && !_currentKeybinds.IsEnabledWindowsKeybinds)
            {
                if (_currentKeySet.Keys.Any(x => x.IsModifiersKey()))
                {
                    var keys = _currentKeySet.Keys.Where(x => x.IsModifiersKey()).ToArray();
                    KeyboardOperation.KeyUp(keys);
                }
            }
        }

        private void SwitchMode(string modeName, bool isSilent = false)
        {
            SwitchModeCommon(modeName, isSilent);
            _stateController.SwitchMode(modeName, isSilent | _currentKeybinds.IsPrefix);
        }

        private void SwitchMode(Mode mode, bool isSilent = false)
        {
            SwitchModeCommon(mode.ToString(), isSilent);
            _stateController.SwitchMode(mode, isSilent | _currentKeybinds.IsPrefix);
        }

        Action<string, KeySet> _command;
        Action _repeatFind;
        Action _reverseFind;
        bool _isActive = false;
        public void Toggle()
        {
            if (!_isActive)
            {
                UIAssistantAPI.NotifyInfoMessage("Keybinds Maniacs", KeybindsManiacs.Localizer.GetLocalizedText(Consts.Activate));
                Initialize();
                Hook.IgnoreInjected = true;
            }
            else
            {
                UIAssistantAPI.NotifyInfoMessage("Keybinds Maniacs", KeybindsManiacs.Localizer.GetLocalizedText(Consts.Deactivate));
                _stateController.Quit();
            }
            _isActive = !_isActive;
        }

        private void ReleaseKeys(KeySet keysState)
        {
            if (_currentKeybinds.IsEnabledWindowsKeybinds)
            {
                var pressedKeys = keysState.Keys.Where(x => x.IsModifiersKey()).ToArray();
                KeyboardOperation.Initialize(pressedKeys);
            }
            else
            {
                KeyboardOperation.Initialize(Key.None);
            }
        }

        private KeySet GenerateKeySet(Key key, KeySet keysState)
        {
            var mod = keysState.Keys.Where(x => x.IsModifiersKey());
            var result = new KeySet(mod.Concat(new[] { key }).ToArray());
            return result;
        }

        bool _isOneShotCandidate = false;
        Key _repeatKey;
        KeySet _currentKeySet;
        protected override void OnKeyDown(KeyEvent keyEvent, Key key, KeySet keysState, ref bool handled)
        {
            if (keysState.IsInjected)
            {
                handled = false;
                return;
            }

            _currentKeySet = keysState;
            if (_isOneShotCandidate && _repeatKey == key)
            {
                return;
            }

            if (keysState.Keys.Count == 1)
            {
                if (_currentKeybinds.OneShotDefined.ContainsKey(keysState))
                {
                    if (_currentKeybinds.OneShotDefined[keysState])
                    {
                        _isOneShotCandidate = true;
                        _currentKeybinds.OneShotKeyDown[keysState].Invoke();
                        _repeatKey = key;
                        return;
                    }
                }
            }

            var input = keysState.ConvertToCurrentLanguage();
            if (input.Length > 0 && _command != null)
            {
                _command.Invoke(input, keysState);
                return;
            }
            if (Keybinds.Contains(keysState))
            {
                var operation = Keybinds[keysState];
                ReleaseKeys(keysState);
                operation?.Invoke();
                return;
            }

            //var keyset = new KeySet(key);
            var keyset = GenerateKeySet(key, keysState);
            if (Keybinds.Contains(keyset))
            {
                var operation = Keybinds[keyset];
                ReleaseKeys(keysState);
                operation?.Invoke();
                return;
            }

            handled = !_currentKeybinds.IsEnabledWindowsKeybinds;
            return;
        }

        protected override void OnKeyUp(KeyEvent keyEvent, Key key, KeySet keysState, ref bool handled)
        {
            if (keysState.IsInjected)
            {
                handled = false;
                return;
            }

            var oldKeysState = new KeySet(key);
            if (_currentKeybinds.OneShotKeybinds.Contains(oldKeysState))
            {
                _currentKeybinds.OneShotKeyUp[oldKeysState].Invoke();
                if (_isOneShotCandidate)
                {
                    _currentKeybinds.OneShotKeybinds[oldKeysState].Invoke();
                }
                _isOneShotCandidate = false;
                return;
            }
            _isOneShotCandidate = false;

            if (Keybinds.Contains(keysState) && !key.IsModifiersKey())
            {
                return;
            }

            handled = !_currentKeybinds.IsEnabledWindowsKeybinds;
            return;
        }
    }
}