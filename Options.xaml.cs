using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace CoolEditor
{
    public partial class Options : PhoneApplicationPage
    {
        public Options()
        {
            InitializeComponent();
            // initialize mode selector
            var modeSource = new List<Items>
            {
                new Items() {Name = "javascript"},
                new Items() {Name = "c_cpp"},
            };
            this.ModePicker.ItemsSource = modeSource;
            // initialize theme selector
            var themeSource = new List<Items>
            {
                new Items() {Name = "monokai"},
                new Items() {Name = "terminal"}
            };
            this.ThemePicker.ItemsSource = themeSource;
        }

        private void ModePicker_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void ThemePicker_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }

    public class Items
    {
        public string Name
        {   
            get;
            set;
        }
    }
}