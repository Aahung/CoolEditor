using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
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
            var modeNames = new String[]{"actionscript", "assembly_x86", "batchfile", "c9search", "c_cpp", "coffee", "csharp", "css", "dart", "golang", "groovy", "html", "html_completions", "html_ruby", "ini", "java", "javascript", "json", "jsp", "latex", "less", "lisp", "makefile", "markdown", "matlab", "mysql", "objectivec", "ocaml", "pascal", "perl", "pgsql", "php", "plain_text", "powershell", "python", "r", "ruby", "sh", "space", "sql", "svg", "tcl", "tex", "text", "typescript", "vbscript", "verilog","xml", "xquery", "yaml"};
            var modeSource = modeNames.Select(modeName => new Items() {Name = modeName}).ToList();
            ModePicker.ItemsSource = modeSource;
            var selectedModeName = (App.Current as App).Mode;
            ModePicker.SelectedIndex = Array.IndexOf(modeNames, selectedModeName);
            ModePicker.SelectionChanged += ModePicker_OnSelectionChanged;
            
            // initialize theme selector
            var themeNames = new String[]{"ambiance", "chaos", "chrome", "clouds", "clouds_midnight", "cobalt", "crimson_editor", "dawn", "dreamweaver", "eclipse", "github", "idle_fingers", "katzenmilch", "kr", "kuroir", "merbivore", "merbivore_soft", "mono_industrial", "monokai", "pastel_on_dark", "solarized_dark", "solarized_light", "terminal", "textmate", "tomorrow", "tomorrow_night", "tomorrow_night_blue", "tomorrow_night_bright", "tomorrow_night_eighties", "twilight", "vibrant_ink", "xcode"};
            var themeSource = themeNames.Select(themeName => new Items() {Name = themeName}).ToList();
            ThemePicker.ItemsSource = themeSource;
            var selectedThemeName = IsolatedStorageSettings.ApplicationSettings["theme"].ToString();
            ThemePicker.SelectedIndex = Array.IndexOf(themeNames, selectedThemeName);
            ThemePicker.SelectionChanged += ThemePicker_OnSelectionChanged;

            // initialize theme selector
            var fontSizes = new int[] { 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            var fontSource = fontSizes.Select(fontSize => new Items() { Name = String.Format("{0}{1}", fontSize, "px") }).ToList();
            FontPicker.ItemsSource = fontSource;
            var selectedFontSize = (int) (IsolatedStorageSettings.ApplicationSettings["fontsize"]);
            FontPicker.SelectedIndex = Array.IndexOf(fontSizes, selectedFontSize);
            FontPicker.SelectionChanged += FontPicker_OnSelectionChanged;
        }

        private void ModePicker_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var items = (sender as ListPicker).SelectedItem as Items;
            if (items != null) (App.Current as App).Mode = items.Name;
        }

        private void ThemePicker_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var items = (sender as ListPicker).SelectedItem as Items;
            if (items != null)
            {
                IsolatedStorageSettings.ApplicationSettings["theme"] = items.Name;
                IsolatedStorageSettings.ApplicationSettings.Save();
            }
        }

        private void FontPicker_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var items = (sender as ListPicker).SelectedItem as Items;
            if (items != null)
            {
                IsolatedStorageSettings.ApplicationSettings["fontsize"] = Convert.ToInt32(items.Name.Split('p')[0]);
                IsolatedStorageSettings.ApplicationSettings.Save();
            }
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