﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;

namespace UIAssistant.Utility.Extensions
{
    public static class RectExtensions
    {
        public static Point Center(this Rect rect)
        {
            return new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
        }

        public static Rect ToClientCoordinate(this Rect rect)
        {
            return new Rect(rect.X - SystemParameters.VirtualScreenLeft, rect.Y - SystemParameters.VirtualScreenTop, rect.Width, rect.Height);
        }

        public static Rect ToScreenCoordinate(this Rect rect)
        {
            return new Rect(rect.X + SystemParameters.VirtualScreenLeft, rect.Y + SystemParameters.VirtualScreenTop, rect.Width, rect.Height);
        }
    }
}
