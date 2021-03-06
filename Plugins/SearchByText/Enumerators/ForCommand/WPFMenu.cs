﻿using System;
using System.Collections.Generic;
using System.Linq;

using System.Windows.Automation;

using UIAssistant.Interfaces.HUD;
using UIAssistant.Plugin.SearchByText.Items;

namespace UIAssistant.Plugin.SearchByText.Enumerators.ForCommand
{
    class WPFMenu : AbstarctSearchForCommand
    {
        bool _isCanceled = false;
        bool _isFinished = false;
        public override void Dispose()
        {
            _isCanceled = true;
            while (!_isFinished)
            {
                System.Threading.Thread.Sleep(100);
            }
            base.Dispose();
        }

        public override void Enumerate(ICollection<IHUDItem> results)
        {
            _results = results;
            var element = AutomationElement.FromHandle(MainWindowHandle);
            GetMenuBars(element).ForEach(x => GetMenuItems(x, null, new List<string>()));
            _isFinished = true;
            return;
        }

        private List<AutomationElement> GetMenuBars(AutomationElement element)
        {
            var menuBars = new List<AutomationElement>();
            var ret = GetMenuBarsInternal(element, menuBars);
            return menuBars;
        }

        private bool GetMenuBarsInternal(AutomationElement element, List<AutomationElement> menuBars)
        {
            int retCounter = 0;
            PropertyCondition menubarCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuBar);
            PropertyCondition paneCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Pane);
            PropertyCondition windowCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
            Condition propCondition = new OrCondition(menubarCondition, paneCondition, windowCondition);

            var includeMenuBar = element.FindAll(TreeScope.Children, propCondition).Cast<AutomationElement>();
            foreach (var el in includeMenuBar)
            {
                ControlType type = el.Current.ControlType;

                if (type == ControlType.MenuBar)
                {
                    menuBars.Add(el);
                    ++retCounter;
                }
                else
                {
                    if (GetMenuBarsInternal(el, menuBars))
                    {
                        ++retCounter;
                    }
                }
            }
            return retCounter > 0;
        }

        private bool HasChild(AutomationElement element)
        {
            var menuItemCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem);
            return element.FindAll(TreeScope.Children, menuItemCondition).Count > 0;
        }

        private bool GetMenuItems(AutomationElement element, string parent, List<string> groups)
        {
            bool ret = false;
            var menuItems = element.FindAll(TreeScope.Children, Condition.TrueCondition).Cast<AutomationElement>();
            foreach (var item in menuItems)
            {
                if (_isCanceled)
                {
                    break;
                }

                try
                {
                    var elementInfo = item.Current;
                    ControlType elementType = elementInfo.ControlType;
                    if (elementType.IsThroughElement())
                    {
                        continue;
                    }

                    string addName = elementInfo.Name;
                    if (addName == null || addName == "")
                    {
                        continue;
                    }
                    string itemName = addName.Clone() as string;
                    string fullpath;
                    string shortcutKey;
                    bool canExpand;
                    if (FormatName(parent, elementType, item, ref addName, out fullpath, out shortcutKey, out canExpand))
                    {
                        //bool isOffscreen = false;
                        if (item.CanExpandElement(elementType))
                        {
                            // Expand
                            item.TryDoDefaultAction();
                            groups.Add(itemName);
                            string ancestor = parent;
                            if (parent == null)
                            {
                                ancestor = itemName;
                            }
                            else
                            {
                                ancestor = parent + Consts.Delimiter + itemName;
                            }
                            GetMenuItems(item, ancestor, groups);
                            groups.RemoveAt(groups.Count - 1);
                            // Collapse
                            item.TryDoDefaultAction();
                            //isOffscreen = true;
                        }
                        else if (HasChild(item))
                        {
                            string ancestor = parent;
                            if (parent == null)
                            {
                                ancestor = itemName;
                            }
                            else
                            {
                                ancestor = parent + Consts.Delimiter + itemName;
                            }
                            groups.Add(itemName);
                            GetMenuItems(item, ancestor, groups);
                            groups.RemoveAt(groups.Count - 1);
                            continue;
                        }
                        var result = new MenuItemUIA(itemName, fullpath, elementInfo.BoundingRectangle, elementInfo.IsEnabled, canExpand, item, element, groups);
                        _results.Add(result);
                    }

                    ret = true;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.Print("{0}", e.Message);
                }
            }
            Update();
            return ret;
        }
    }
}
