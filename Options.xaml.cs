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
            var modeNames = new String[]{"abap", "actionscript", "ada", "apache_conf", "asciidoc", "assembly_x86", "autohotkey", "batchfile", "c9search", "c_cpp", "cirru", "clojure", "cobol", "coffee", "coldfusion", "csharp", "css", "curly", "d", "dart", "diff", "django", "dot", "ejs", "erlang", "forth", "ftl", "gherkin", "glsl", "golang", "groovy", "haml", "handlebars", "haskell", "haxe", "html", "html_completions", "html_ruby", "ini", "jack", "jade", "java", "javascript", "json", "jsoniq", "jsp", "jsx", "julia", "latex", "less", "liquid", "lisp", "livescript", "logiql", "lsl", "lua", "luapage", "lucene", "makefile", "markdown", "matlab", "mel", "mushcode", "mushcode_high_rules", "mysql", "nix", "objectivec", "ocaml", "pascal", "perl", "pgsql", "php", "plain_text", "powershell", "prolog", "properties", "protobuf", "python", "r", "rdoc", "rhtml", "ruby", "rust", "sass", "scad", "scala", "scheme", "scss", "sh", "sjs", "smarty", "snippets", "soy_template", "space", "sql", "stylus", "svg", "tcl", "tex", "text", "textile", "toml", "twig", "typescript", "vbscript", "velocity", "verilog", "vhdl", "xml", "xquery", "yaml"};
            var modeSource = new List<Items>();
            foreach (var modeName in modeNames)
            {
                // add all to source
                modeSource.add(new Items() {Name = modeName}); 
            };
            this.ModePicker.ItemsSource = modeSource;
            // initialize theme selector
            var themeNames = new String[]{"ambiance", "chaos", "chrome", "clouds", "clouds_midnight", "cobalt", "crimson_editor", "dawn", "dreamweaver", "eclipse", "github", "idle_fingers", "katzenmilch", "kr", "kuroir", "merbivore", "merbivore_soft", "mono_industrial", "monokai", "pastel_on_dark", "solarized_dark", "solarized_light", "terminal", "textmate", "tomorrow", "tomorrow_night", "tomorrow_night_blue", "tomorrow_night_bright", "tomorrow_night_eighties", "twilight", "vibrant_ink", "xcode"};
            var themeSource = new List<Items>();
            foreach (var themeName in themeNames)
            {
                // add all to source
                themeSource.add(new Items() {Name = themeName}); 
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