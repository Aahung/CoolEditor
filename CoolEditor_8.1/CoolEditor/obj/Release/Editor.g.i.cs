﻿#pragma checksum "C:\Users\Xinhong\Documents\Visual Studio 2013\Projects\CoolEditor\CoolEditor_8.1\CoolEditor\Editor.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "2F31D12A923677056086BF0720AFA341"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Microsoft.Phone.Controls;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace CoolEditor {
    
    
    public partial class Editor : Microsoft.Phone.Controls.PhoneApplicationPage {
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal System.Windows.Controls.TextBlock TitleTextBlock;
        
        internal System.Windows.Controls.Grid ContentPanel;
        
        internal Microsoft.Phone.Controls.WebBrowser EditorBrowser;
        
        internal System.Windows.Controls.Button ViewToggleButton;
        
        internal System.Windows.Controls.Button TabButton;
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Windows.Application.LoadComponent(this, new System.Uri("/CoolEditor;component/Editor.xaml", System.UriKind.Relative));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.TitleTextBlock = ((System.Windows.Controls.TextBlock)(this.FindName("TitleTextBlock")));
            this.ContentPanel = ((System.Windows.Controls.Grid)(this.FindName("ContentPanel")));
            this.EditorBrowser = ((Microsoft.Phone.Controls.WebBrowser)(this.FindName("EditorBrowser")));
            this.ViewToggleButton = ((System.Windows.Controls.Button)(this.FindName("ViewToggleButton")));
            this.TabButton = ((System.Windows.Controls.Button)(this.FindName("TabButton")));
        }
    }
}

