﻿using System.Windows;
using System.Windows.Automation;
using System.Threading.Tasks;

namespace UIAssistant.Plugin.SearchByText.Items
{
    class ItemInContainer : SearchByTextItem
    {
        AutomationElement Element { get; set; }
        AutomationElement Container { get; set; }

        public ItemInContainer(string name, Rect bounds, bool isEnabled, AutomationElement element, AutomationElement container)
            : base(name, name, bounds, isEnabled, false)
        {
            Element = element;
            Container = container;
        }

        public override void Execute()
        {
            if (!IsEnabled)
            {
                return;
            }
            Task.Run(() =>
            {
                Element.ScrollIntoView();
                Element.TrySelectItem();

                var pt = SearchByText.UIAssistantAPI.MouseAPI.MouseOperation.GetMousePosition();
                SearchByText.UIAssistantAPI.MouseAPI.MouseOperation.Click(Element.Current.BoundingRectangle);
                SearchByText.UIAssistantAPI.MouseAPI.MouseOperation.Move(pt);
            });
        }
    }
}
