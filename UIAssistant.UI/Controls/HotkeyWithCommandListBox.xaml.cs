﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using KeybindHelper;
using UIAssistant.UI.Interfaces;

namespace UIAssistant.UI.Controls
{
    /// <summary>
    /// HotkeyWithCommandListBox.xaml の相互作用ロジック
    /// </summary>
    public partial class HotkeyWithCommandListBox : KeybindListBox
    {
        #region CanidatesGeneratorProperty
        public static readonly DependencyProperty CandidatesGeneratorProperty =
            DependencyProperty.Register(
                nameof(CandidatesGenerator),
                typeof(ICandidatesGenerator),
                typeof(HotkeyWithCommandListBox),
                new PropertyMetadata(null));

        public ICandidatesGenerator CandidatesGenerator
        {
            get { return Dispatcher.Invoke(() => (ICandidatesGenerator)GetValue(CandidatesGeneratorProperty)); }
            set { SetValue(CandidatesGeneratorProperty, value); }
        }
        #endregion

        public HotkeyWithCommandListBox()
        {
            InitializeComponent();
        }
    }
}
