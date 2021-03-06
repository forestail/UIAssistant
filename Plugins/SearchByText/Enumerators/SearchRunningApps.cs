﻿using System;
using System.Collections.Generic;
using System.Linq;

using UIAssistant.Interfaces.HUD;
using UIAssistant.Plugin.SearchByText.Items;

namespace UIAssistant.Plugin.SearchByText.Enumerators
{
    class SearchRunningApps : ISearchByTextEnumerator
    {
        public event EventHandler Updated;
        public event EventHandler Finished;

        public void Enumerate(ICollection<IHUDItem> resultsContainer)
        {
            var results = new List<IHUDItem>();
            SearchByText.UIAssistantAPI.WindowAPI.EnumerateWindows((window) =>
            {
                if (window.IsAltTabWindow())
                {
                    return true;
                }

                var item = new RunningApp(window.Title, window);
                results.Add(item);
                return true;
            });
            results.OrderBy(x => x.DisplayText).ForEach(x => resultsContainer.Add(x));
            Updated?.Invoke(this, EventArgs.Empty);
            Finished?.Invoke(this, EventArgs.Empty);
        }

        public void Cancel()
        {
            Dispose();
        }

        public void Dispose()
        {
            Updated = null;
            Finished = null;
        }
    }
}
