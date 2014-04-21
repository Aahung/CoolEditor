using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Resources;
using Windows.Storage;
using Windows.Storage.Streams;
using CoolEditor.Class;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Windows.Phone.Storage.SharedAccess;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

// this is for retriving the incoming file

namespace CoolEditor
{
    public partial class Editor : PhoneApplicationPage
    {
        private IDictionary<string, string> _queryStrings;
        private string _fileName;
        private Boolean _viewOnly;
        public Editor()
        {
            InitializeComponent();
            EditorBrowser.Visibility = Visibility.Collapsed;//hide the window
            // we need to reset the global variable mode
            (App.Current as App).Mode = "";
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e); 
            if (e.NavigationMode == NavigationMode.Back)
            {
                // handle the mode
                    // set mode
                EditorBrowser.InvokeScript("eval", string.Format("editor.getSession().setMode(\"ace/mode/{0}\"); " +
                    "editor.setTheme('ace/theme/{1}'); document.getElementById('editor').style.fontSize='{2}px'", 
                    (App.Current as App).Mode, IsolatedStorageSettings.ApplicationSettings["theme"], 
                    IsolatedStorageSettings.ApplicationSettings["fontsize"]));
                System.Threading.Thread.Sleep(200);
                EditorBrowser.InvokeScript("eval", string.Format("resize();")); // reset the browser control's height
                System.Threading.Thread.Sleep(200);
                EditorBrowser.InvokeScript("eval", string.Format("resize();")); // reset the browser control's height
                // avoid reload of code if coming back from option page
                return;
            }
            _queryStrings = this.NavigationContext.QueryString;
            if (!_queryStrings.Any())
            {
                MessageBox.Show("No file is coming, do not play joke with me. :-(");
            }
            EditorBrowser.Navigate(new Uri("./Html/Editor.html", UriKind.Relative));
        }

        private void EditorBrowser_OnLoaded(object sender, RoutedEventArgs e)
        {
            //SaveFilesToIsoStore();
        }

        private async void EditorBrowser_OnLoadCompleted(object sender, NavigationEventArgs e)
        {
            if (_queryStrings.Any())
            {
                var key = _queryStrings.FirstOrDefault().Key;
                if (key == "name")
                {
                    _fileName = _queryStrings.FirstOrDefault().Value;
                }
                else
                {
                    var fileID = _queryStrings.FirstOrDefault().Value;
                    _fileName = SharedStorageAccessManager.GetSharedFileName(fileID);
                    await SharedStorageAccessManager.CopySharedFileAsync(ApplicationData.Current.LocalFolder,
                        _fileName, NameCollisionOption.GenerateUniqueName,
                        fileID);
                }
                var currentFolder = ApplicationData.Current.LocalFolder;
                IStorageFile file = await currentFolder.GetFileAsync(_fileName);
                _fileName = file.Name;
                //means some file is coming
                TitleTextBlock.Text = "Cool Editor - " + _fileName;
                //var randomAccessStream = await file.OpenReadAsync();
                //var resourceStream = (IInputStream)randomAccessStream.GetInputStreamAt(0);
                //var reader = new DataReader(resourceStream);
                //await reader.LoadAsync((uint)randomAccessStream.Size);
                //string data = reader.ReadString((uint)randomAccessStream.Size);
                //reader.DetachStream();
                string data = await FileIOUtility.ReadFileContentsAsync(_fileName);
                var fileExtension = _fileName.Split('.')[_fileName.Split('.').Count() - 1];
                var escapedFileContent = HttpUtility.UrlEncode(data);
                var modeStr = "";
                if (fileExtension != null)
                {
                    switch (fileExtension.ToLower())
                    {
                        case "html":
                        case "htm":
                            modeStr = "html";
                            break;
                        case "py":
                            modeStr = "python";
                            break;
                        case "c":
                        case "cu":
                        case "cpp":
                        case "cxx":
                        case "hxx":
                        case "h":
                        case "hpp":
                            modeStr = "c_cpp";
                            break;
                        case "java":
                        case "class":
                            modeStr = "java";
                            break;
                        case "css":
                            modeStr = "css";
                            break;
                        case "js":
                            modeStr = "javascript";
                            break;
                        case "json":
                            modeStr = "json";
                            break;
                        case "php":
                        case "inc":
                            modeStr = "php";
                            break;
                        case "cs":
                            modeStr = "csharp";
                            break;
                        case "tex":
                            modeStr = "tex";
                            break;
                        case "xml":
                            modeStr = "xml";
                            break;
                        default:
                            modeStr = "plain_text";
                            break;
                    }
                }
                else
                {
                    modeStr = "plain_text";
                }
                (App.Current as App).Mode = modeStr;
                try
                {
                    EditorBrowser.InvokeScript("eval", string.Format(@"editor = ace.edit('editor');
                        editor.setTheme('ace/theme/{0}');
	                    editor.getSession().setUseWrapMode(true);
                        editor.getSession().on('change', resize);
                        editor.getSession().selection.on('change', resize);
                        document.getElementById('editor').style.fontSize='{1}px';
                    ", IsolatedStorageSettings.ApplicationSettings["theme"], IsolatedStorageSettings.ApplicationSettings["fontsize"]));
                    var result = (string)EditorBrowser.InvokeScript("eval", string.Format("editor.setValue(unescape(\"{0}\"));", escapedFileContent).Replace("+", "%20"));
                    EditorBrowser.InvokeScript("eval", string.Format("editor.getSession().setMode(\"ace/mode/{0}\")", (App.Current as App).Mode));
                    EditorBrowser.InvokeScript("eval", "setViewOnly(true);"); 
                    EditorBrowser.Visibility = Visibility.Visible; // show the editor window
                    // toggle to view only
                    _viewOnly = true;
                    System.Threading.Thread.Sleep(1000);
                    EditorBrowser.InvokeScript("eval", "editor.selection.clearSelection()");
                    EditorBrowser.InvokeScript("eval", string.Format("resize()")); // reset the browser control's height
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    MessageBox.Show("Sorry this file cannot be opened. :-(. Would you mind send this file to me at landxh@gmail.com?");
                }
            }
            else
            {
                MessageBox.Show("No file is coming, do not play joke with me. :-(");
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            // when tab key pressed, insert a tab
            try
            {
                EditorBrowser.InvokeScript("eval", "editor.insert('    ')");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ApplicationBarIconButton_OnClick(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Options.xaml", UriKind.Relative));
        }

        private void ApplicationBarIconButton2_OnClick(object sender, EventArgs e)
        {
            var btn = sender as ApplicationBarIconButton;
            var textbox = new TextBox();
            var searchBox = new CustomMessageBox()
            {
                Caption = "Search",
                Message = "Enter keywords",
                LeftButtonContent = "search",
                RightButtonContent = "cancel",
                Content = textbox
            };

            searchBox.Dismissed += (s1, e1) =>
            {
                switch (e1.Result)
                {
                    case CustomMessageBoxResult.LeftButton:
                        string keyword = textbox.Text;
                        var escapedKeyword = HttpUtility.UrlEncode(keyword);
                        EditorBrowser.InvokeScript("eval", string.Format("editor.find(unescape('{0}'))", escapedKeyword).Replace("+", "%20"));
                        break;
                    default:
                        break;
                }
            };
            searchBox.Show();
        }

        private void SearchAssociate()
        {
            //help with find next and find previous
            var searchBox = new CustomMessageBox()
            {
                Caption = "Search",
                LeftButtonContent = "Previous",
                RightButtonContent = "Next",
            };

            searchBox.Dismissing += (s1, e1) =>
            {
                switch (e1.Result)
                {
                    case CustomMessageBoxResult.LeftButton:
                        EditorBrowser.InvokeScript("eval", string.Format("editor.findPrevious(); editor.gotoLine(editor.selection.getCursor().row);"));
                        e1.Cancel = true;
                        break;
                    case CustomMessageBoxResult.RightButton:
                        EditorBrowser.InvokeScript("eval", string.Format("editor.findNext(); editor.gotoLine(editor.selection.getCursor().row + 2);"));
                        e1.Cancel = true;
                        break;
                    default:
                        break;
                }
            };
            searchBox.Show();
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            //Do your work here
            MessageBoxResult result =
                MessageBox.Show("Do you want to quit?", "Warning",
                    MessageBoxButton.OKCancel);

            if (result != MessageBoxResult.OK)
            {
                e.Cancel = true;
            }
            base.OnBackKeyPress(e);
            //set editor blur in case in mess
            //EditorBrowser.InvokeScript("eval", string.Format("editor.blur()"));
        }

        private void EditorBrowser_OnLostFocus(object sender, RoutedEventArgs e)
        {
            //set editor blur in case in mess
            //EditorBrowser.InvokeScript("eval", string.Format("editor.blur()"));
        }

        private async void ApplicationBarIconButton3_OnClick(object sender, EventArgs e)
        {
            //Save file content
            try
            {
                var fileContent = (string) EditorBrowser.InvokeScript("eval", "editor.getValue()");
                await FileIOUtility.WriteDataToFileAsync(_fileName, fileContent);
                ToastNotification.ShowSimple("Saved!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ToastNotification.ShowSimple("Not saved");
            }
        }

        private async void ApplicationBarIconButton4_OnClick(object sender, EventArgs e)
        {
            //copy to clipboard
            try
            {
                var fileContent = (string)EditorBrowser.InvokeScript("eval", "editor.getValue()");
                Clipboard.SetText(fileContent);
                ToastNotification.ShowSimple("Copied to clipboard!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ToastNotification.ShowSimple("Failed :-(");
            }
        }

        private void ViewToggleButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_viewOnly)
            {
                var result = (string)EditorBrowser.InvokeScript("eval", "setViewOnly(false);");
                ((Button) sender).Content = "view";
                TabButton.IsEnabled = true;
            }
            else
            {
                var result = (string)EditorBrowser.InvokeScript("eval", "setViewOnly(true);");
                ((Button)sender).Content = "edit";
                TabButton.IsEnabled = false;
            }
            _viewOnly = !_viewOnly;
        }

        private void ApplicationBarMenuItem_OnClick(object sender, EventArgs e)
        {
            EditorBrowser.InvokeScript("eval", "editor.undo()");
        }
        private void ApplicationBarMenuItem2_OnClick(object sender, EventArgs e)
        {
            EditorBrowser.InvokeScript("eval", "editor.redo()");
            EditorBrowser.InvokeScript("eval", "editor.selection.clearSelection()");
            System.Threading.Thread.Sleep(200);
            EditorBrowser.InvokeScript("eval", string.Format("resize()")); // reset the browser control's height
        }

        private void EditorBrowser_OnKeyDown(object sender, KeyEventArgs e)
        {
            //handle enter key
            //fail to handle it :-(
        }
    }
}