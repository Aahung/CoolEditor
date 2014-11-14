using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Streams;
using Coding4Fun.Toolkit.Controls;
using CoolEditor.Class;
using CoolEditor.Class.DropNetRt;
using CoolEditor.Class.DropNetRt.Models;
using Microsoft.Live;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using CoolEditor.Resources;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace CoolEditor
{
    public partial class MainPage : PhoneApplicationPage
    {
        private const int DataFormatVersion = 2;
        private ObservableCollection<FileItem> _files;
        private ObservableCollection<AlphaKeyGroup<FileItem>> _dataSource;
        private MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();

        private LocalDatabase _localDB;
        private FileItemDataContext _fileDB;
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            //handle first login in
            var setting = IsolatedStorageSettings.ApplicationSettings;
            // initialize database
            _localDB = new LocalDatabase();
            _fileDB = new FileItemDataContext(FileItemDataContext.DBConnectionString);
            this.DataContext = this;

            if (setting.Contains("firstuse") && (string)setting["firstuse"] == "false")
            {
                if (!setting.Contains("data-format-version") || Convert.ToInt16(setting["data-format-version"]) < DataFormatVersion)
                {
                    UpdateDataFormat();
                }
            }
            else
            {
                setting.Add("data-format-version", DataFormatVersion);
                //move sample code to folder
                LoadSampleFiles();
                //
                setting.Add("firstuse", "false");
                setting.Save();
            }

            BuildLocalizedApplicationBar();
            ListFiles();
            // for WP8 users
            if (Environment.OSVersion.Version.Minor == 0) // WP8.0
            {
                Wp81.Visibility = Visibility.Collapsed;
            }
            else
            {
                Wp80.Visibility = Visibility.Collapsed;
            }
            DropboxButtonBlock.Text = AppResources.Click_to_login;
            OnedriveButtonBlock.Text = AppResources.Click_to_login;
            DropboxButtonBlock.Visibility = (Authentication.DropboxIsLogin())
                ? Visibility.Collapsed
                : Visibility.Visible;
            OnedriveButtonBlock.Visibility = (Authentication.OnedriveIsLogin())
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private async Task UpdateDataFormat()
        {
            // update data format
            IsolatedStorageSettings setting = IsolatedStorageSettings.ApplicationSettings;
            if (!setting.Contains("data-format-version"))
            {
                setting.Add("data-format-version", 1);
            }
            switch (Convert.ToInt16(setting["data-format-version"]))
            {
                case 1:
                    // move files to database
                    var storageFiles = IsolatedStorageFile.GetUserStoreForApplication();
                    var storageFolder = ApplicationData.Current.LocalFolder;
                    var files = await storageFolder.GetFilesAsync();
                    foreach (var file in files)
                    {
                        var storageFile = file as StorageFile;
                        if (storageFile == null) continue;
                        if (storageFile.Name == "__ApplicationSettings") continue;
                        if (storageFile.Name.Contains(".tmp") || storageFile.Name.Contains("FileItem_Cooleditor.sdf")) continue;
                        var dt = storageFiles.GetLastWriteTime(storageFile.Path).UtcDateTime;
                        _fileDB.FileItems.InsertOnSubmit(new FileItem()
                        {
                            Id = Guid.NewGuid().GetHashCode(),
                            FileName = storageFile.Name, 
                            ActualFileName = storageFile.Name,
                            LocalPath = storageFile.Path,
                            LastModifiedTime = dt,
                            LastSyncTime = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc)
                        });
                    }
                    _fileDB.SubmitChanges();
                    break;
            }
            setting["data-format-version"] = DataFormatVersion;
            await ListFiles();
        }

        private void BuildLocalizedApplicationBar()
        {
            // Set the page's ApplicationBar to a new instance of ApplicationBar.
            ApplicationBar = new ApplicationBar();

            // Create a new button and set the text value to the localized string from AppResources.
            var appBarButton =
                new ApplicationBarIconButton(new
                Uri("/Assets/AppBar/add.png", UriKind.Relative)) {Text = AppResources.Create};
            appBarButton.Click += ApplicationBarIconButton1_OnClick;
            ApplicationBar.Buttons.Add(appBarButton);

            //sync button
            appBarButton =
                new ApplicationBarIconButton(new
                Uri("/Assets/AppBar/sync.png", UriKind.Relative)) { Text = AppResources.Sync };
            appBarButton.Click += SyncWithOnlineFile;
            ApplicationBar.Buttons.Add(appBarButton);

            //delete all button
            appBarButton =
                new ApplicationBarIconButton(new
                Uri("/Assets/AppBar/delete.png", UriKind.Relative)) { Text = AppResources.Clear_all };
            appBarButton.Click += ApplicationBarIconButton_OnClick;
            ApplicationBar.Buttons.Add(appBarButton);

            // Create a new menu item with the localized string from AppResources.
            var appBarMenuItem =
                new ApplicationBarMenuItem(AppResources.Feedback);
            appBarMenuItem.Click += ApplicationBarMenuItem_OnClick;
            ApplicationBar.MenuItems.Add(appBarMenuItem);

            ApplicationBar.Mode = ApplicationBarMode.Default;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LittleWatson.CheckForPreviousException();
            //handle back navigation
            FileListSelector.SelectedItem = null;
            ListFiles();
            // SyncWithOnlineFile(null, null); // sync

            if (e.NavigationMode != NavigationMode.Back)
            {
                //trail version
                if ((App.Current as App).IsTrial)
                {
                    var trialMessageBox = new CustomMessageBox()
                    {
                        Caption = AppResources.Buy_caption,
                        Message = AppResources.Buy_message,
                        LeftButtonContent = AppResources.Buy,
                        RightButtonContent = AppResources.Continue_trial
                    };

                    trialMessageBox.Dismissed += (s, e1) =>
                    {
                        if (e1.Result == CustomMessageBoxResult.LeftButton)
                        {
                            _marketPlaceDetailTask.Show();
                        }
                    };

                    trialMessageBox.Show();
                }
                Panorama.DefaultItem = Panorama.Items[1];
            }
            Update_Log();
#if DEBUG
            PanoramaItemAbout.Header = "about";//specify when is debugging
#endif
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            if (AuthGrid.Visibility == Visibility.Visible)
            {
                AuthGrid.Visibility = Visibility.Collapsed;
                e.Cancel = true;
            }
        }

        private async void LoadSampleFiles()
        {
            var samples = new string[]
                {
                    "sample.php",
                    "sample.css",
                    "sample.java",
                    "sample.js",
                    "sample.cpp"
                };
            foreach (var sample in samples)
            {
                var uri = "CoolEditor;component/Assets/Sample_Code/" + sample;
                System.Windows.Resources.StreamResourceInfo strm = Application.GetResourceStream(new Uri(uri, UriKind.Relative));
                var reader = new System.IO.StreamReader(strm.Stream);
                string data = reader.ReadToEnd();
                var uniqueFileName = Guid.NewGuid().ToString();
                await FileIOUtility.WriteDataToFileAsync(uniqueFileName, data);
                _fileDB.FileItems.InsertOnSubmit(new FileItem()
                {
                    Id = Guid.NewGuid().GetHashCode(),
                    FileName = sample,
                    ActualFileName = uniqueFileName,
                    LastModifiedTime = new DateTime(2000, 01, 01),
                    LastSyncTime = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc)
                });
                _fileDB.SubmitChanges();
            }
            await ListFiles();
        }

        public async Task ListFiles()
        {
            _fileDB = new FileItemDataContext(FileItemDataContext.DBConnectionString);
            _files = new ObservableCollection<FileItem>(from FileItem file in _fileDB.FileItems select file);
            _dataSource = new ObservableCollection<AlphaKeyGroup<FileItem>>(
                AlphaKeyGroup<FileItem>.CreateGroups(_files,
                System.Threading.Thread.CurrentThread.CurrentUICulture,
                (FileItem s) => s.FileName, true));
            FileListSelector.ItemsSource = _dataSource;
            NoFile.Visibility = !_files.Any() ? Visibility.Visible : Visibility.Collapsed;
            FileListSelector.Visibility = _files.Any() ? Visibility.Visible : Visibility.Collapsed;
            //
            DropboxButtonBlock.Visibility = (Authentication.DropboxIsLogin())
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public void OpenFile(string fileName, string actualFileName)
        {
            NavigationService.Navigate(new Uri(string.Format("/Editor.xaml?name={0}&actualname={1}", fileName, actualFileName), UriKind.Relative));
        }

        public void OpenFile(string actualFileName)
        {
            var theFile = (new ObservableCollection<FileItem>(
                from FileItem file in _fileDB.FileItems where file.ActualFileName == actualFileName select file))
                .FirstOrDefault();
            if (theFile != null) OpenFile(theFile.FileName, theFile.ActualFileName);
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            // share
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                var file = (FileItem)menuItem.DataContext;
                var shareBox = new ShareBox(file.ActualFileName);
                shareBox.Show();
            }
        }

        private async void MenuItem2_OnClick(object sender, RoutedEventArgs e)
        {
            // delete
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                var file = (FileItem)menuItem.DataContext;
                MessageBoxResult result =
                MessageBox.Show(AppResources.Delete_comfirm + " " + file.FileName + "?", AppResources.Warning,
                    MessageBoxButton.OKCancel);

                if (result != MessageBoxResult.OK)
                {
                    return;
                }
                if (await FileIOUtility.DeleteFileAsync(file.ActualFileName))
                {
                    _fileDB.SubmitChanges(); // in case the last commit not take effect
                    // idea from http://stackoverflow.com/questions/11204491/im-sure-someone-has-seen-this-cannot-remove-an-entity-that-has-not-been-attach
                    var toBeDelete = _fileDB.FileItems.FirstOrDefault(f => f.Id == file.Id);
                    if (toBeDelete != null)
                    {
                        _fileDB.FileItems.DeleteOnSubmit(toBeDelete);
                        _fileDB.SubmitChanges();
                        await ListFiles();
                        ToastNotification.ShowSimple(file.FileName + " " + AppResources.Delete_success);
                    }
                    //ListFiles();
                }
                else
                {
                    ToastNotification.ShowSimple(AppResources.Delete_fail);
                }
            }
            //var file = (File)FileListSelector.SelectedItem;
        }

        private void MenuItem3_OnClick(object sender, RoutedEventArgs e)
        {
            //rename
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                var file = (FileItem)menuItem.DataContext;
                
                var prompt = new InputPrompt {Title = AppResources.Rename_caption, Message = AppResources.Rename_message, Value = file.FileName};
                prompt.Show();

                prompt.Completed += async (s1, e1) =>
                {
                    if (e1.PopUpResult != PopUpResult.Ok) return;
                    try
                    {
                        file.FileName = e1.Result;
                        _fileDB.SubmitChanges();
                        ToastNotification.ShowSimple(file.FileName + " " + AppResources.Rename_to + " " + e1.Result);
                        await ListFiles();
                    }
                    catch (Exception)
                    {
                        ToastNotification.ShowSimple(AppResources.Rename_fail);
                    }
                };
            }
        }

        private async void OverwriteLocal_OnClick(object sender, RoutedEventArgs e)
        {
            SimpleProgressIndicator.Set(true);
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                var file = (FileItem)menuItem.DataContext;
                if (file.OnlineProvider == "dropbox")
                {
                    var theFile = _files.FirstOrDefault(x => x.Id == file.Id);
                    if (theFile == null)
                    {
                        ToastNotification.ShowSimple(AppResources.Fail);
                        return;
                    }
                    Metadata fileMetaData;
                    try
                    {
                        fileMetaData = await (App.Current as App).DropboxClient.GetMetaData(theFile.OnlinePath);
                    }
                    catch (Exception)
                    {
                        ToastNotification.ShowSimple(AppResources.Fail);
                        return;
                    }
                    byte[] fileBytes;
                    try
                    {
                        fileBytes = await (App.Current as App).DropboxClient.GetFile(theFile.OnlinePath);
                    }
                    catch (Exception)
                    {
                        ToastNotification.ShowSimple(AppResources.Fail);
                        return;
                    }
                    var content = Encoding.UTF8.GetString(fileBytes, 0, fileBytes.Length);
                    await FileIOUtility.WriteDataToFileAsync(theFile.ActualFileName, content);
                    theFile.LastModifiedTime = fileMetaData.UTCDateModified;
                    theFile.LastSyncTime = DateTime.UtcNow;
                    theFile.Revision = fileMetaData.Revision;
                    theFile.ModifiedSinceLastSync = false;
                }
                if (file.OnlineProvider == "onedrive")
                {
                    await Authentication.OnedriveAuthentication();
                    var theFile = _files.FirstOrDefault(x => x.Id == file.Id);
                    if (theFile == null)
                    {
                        ToastNotification.ShowSimple(AppResources.Fail);
                        return;
                    }
                    try
                    {
                        LiveOperationResult operationResult =
                        await (App.Current as App).OnedriveClient.GetAsync(theFile.LocalPath);
                        dynamic result = operationResult.Result;
                        var downloadPath = theFile.LocalPath + "/content";
                        LiveDownloadOperation downloadOperation = await
                            (App.Current as App).OnedriveClient.CreateBackgroundDownloadAsync(downloadPath);
                        var downloadResult = await downloadOperation.StartAsync();
                        if (downloadResult != null && downloadResult.Stream != null)
                        {
                            var randomAccessStream = await downloadResult.GetRandomAccessStreamAsync();
                            var resourceStream = (IInputStream)randomAccessStream.GetInputStreamAt(0);
                            var reader = new DataReader(resourceStream);
                            await reader.LoadAsync((uint)randomAccessStream.Size);
                            var content = reader.ReadString((uint)randomAccessStream.Size);
                            reader.DetachStream();
                            // overwrite
                            await FileIOUtility.WriteDataToFileAsync(theFile.ActualFileName, content);
                            theFile.ModifiedSinceLastSync = false;
                            theFile.LastModifiedTime = Convert.ToDateTime(result.client_updated_time);
                            theFile.LastSyncTime = DateTime.UtcNow;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        if (ex.Message.EndsWith("doesn't exist."))
                        {
                            theFile.OnlineProvider = "";
                            ToastNotification.ShowSimple(AppResources.online_file_missing);
                        }
                    }
                }
                _fileDB.SubmitChanges();
                ListFiles();
            }
            SimpleProgressIndicator.Set(false);
        }

        private async void OverwriteOnline_OnClick(object sender, RoutedEventArgs e)
        {
            SimpleProgressIndicator.Set(true);
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                var file = (FileItem)menuItem.DataContext;
                if (file.OnlineProvider == "dropbox")
                {
                    var theFile = _files.FirstOrDefault(x => x.Id == file.Id);
                    if (theFile == null)
                    {
                        ToastNotification.ShowSimple(AppResources.Fail);
                        return;
                    }
                    Metadata fileMetaData;
                    try
                    {
                        fileMetaData = await(App.Current as App).DropboxClient.GetMetaData(theFile.OnlinePath);
                    }
                    catch (Exception)
                    {
                        ToastNotification.ShowSimple(AppResources.Fail);
                        return;
                    }
                    var content = await FileIOUtility.ReadFileContentsAsync(theFile.ActualFileName);
                    var onlineFolderPath = fileMetaData.Path.TrimEnd(fileMetaData.Name.ToArray());
                    fileMetaData = await (App.Current as App).DropboxClient.Upload(onlineFolderPath, fileMetaData.Name, Encoding.UTF8.GetBytes(content));
                    theFile.LastSyncTime = DateTime.UtcNow;
                    // get revision number
                    theFile.Revision = fileMetaData.Revision;
                    theFile.ModifiedSinceLastSync = false;
                }
                if (file.OnlineProvider == "onedrive")
                {
                    var theFile = _files.FirstOrDefault(x => x.Id == file.Id);
                    if (theFile == null)
                    {
                        ToastNotification.ShowSimple(AppResources.Fail);
                        return;
                    }
                    await Authentication.OnedriveAuthentication();
                    LiveOperationResult operationResult =
                        await (App.Current as App).OnedriveClient.GetAsync(theFile.LocalPath);
                    dynamic result = operationResult.Result;
                    string parentFolder = result.parent_id;
                    // get file
                    IStorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
                    IStorageFile storageFile = await applicationFolder.GetFileAsync(theFile.ActualFileName);

                    // upload
                    LiveUploadOperation uploadOperation =
                                    await (App.Current as App).OnedriveClient.CreateBackgroundUploadAsync(
                                        parentFolder, result.name, storageFile, OverwriteOption.Overwrite);
                    LiveOperationResult uploadResult = await uploadOperation.StartAsync();
                    dynamic dyResult = uploadResult.Result;
                    theFile.LocalPath = dyResult.id;
                    theFile.LastSyncTime = DateTime.UtcNow;
                    theFile.ModifiedSinceLastSync = false;
                }
                _fileDB.SubmitChanges();
                ListFiles();
            }
            SimpleProgressIndicator.Set(false);
        }

        private async void SyncWithOnlineFile(object sender, EventArgs e)
        {
            // display text
            var setting = IsolatedStorageSettings.ApplicationSettings;
            if (!setting.Contains("notice-20140819"))
            {
                var noticeMessageBox = new CustomMessageBox()
                {
                    Caption = AppResources.notice_20140819_caption,
                    Message = AppResources.notice_20140819_body,
                    LeftButtonContent = AppResources.notice_20140819_left,
                    RightButtonContent = AppResources.notice_20140819_right
                };

                noticeMessageBox.Dismissed += (s, e1) =>
                {
                    if (e1.Result == CustomMessageBoxResult.RightButton)
                    {
                        setting.Add("notice-20140819", "");
                        setting.Save();
                    }
                };

                noticeMessageBox.Show();
            }
            // sync
            if (!Authentication.DropboxIsLogin())
            {
                return;
            }
            SimpleProgressIndicator.Set(true);
            var theFiles = _files.Where(x => x.OnlineProvider == "dropbox" || false);
            if (!theFiles.Any())
            {
                SimpleProgressIndicator.Set(false);
                return;
            }
            foreach (var theFile in theFiles)
            {
                Metadata fileMetaData;
                try
                {
                    fileMetaData = await (App.Current as App).DropboxClient.GetMetaData(theFile.OnlinePath);
                }
                catch (Exception)
                {
                    continue;
                }
                // deal with sync
                if (fileMetaData.Revision > theFile.Revision && fileMetaData.UTCDateModified > theFile.LastModifiedTime &&
                    fileMetaData.UTCDateModified > theFile.LastSyncTime)
                {
                    // download
                    byte[] fileBytes;
                    try
                    {
                        fileBytes = await (App.Current as App).DropboxClient.GetFile(theFile.OnlinePath);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    var content = Encoding.UTF8.GetString(fileBytes, 0, fileBytes.Length);
                    await FileIOUtility.WriteDataToFileAsync(theFile.ActualFileName, content);
                    theFile.LastModifiedTime = fileMetaData.UTCDateModified;
                    theFile.LastSyncTime = DateTime.UtcNow;
                    theFile.Revision = fileMetaData.Revision;
                    theFile.ModifiedSinceLastSync = false;
                }
                else if (fileMetaData.Revision == theFile.Revision && theFile.ModifiedSinceLastSync)
                {
                    // upload
                    var content = await FileIOUtility.ReadFileContentsAsync(theFile.ActualFileName);
                    var onlineFolderPath = fileMetaData.Path.TrimEnd(fileMetaData.Name.ToArray());
                    fileMetaData = await (App.Current as App).DropboxClient.Upload(onlineFolderPath, fileMetaData.Name, Encoding.UTF8.GetBytes(content));
                    theFile.LastSyncTime = DateTime.UtcNow;
                    // get revision number
                    theFile.Revision = fileMetaData.Revision;
                    theFile.ModifiedSinceLastSync = false;
                }
                _fileDB.SubmitChanges();
            }
            ListFiles();
            SimpleProgressIndicator.Set(false);
        }

        private void FileListSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileListSelector.SelectedItem == null)
            {
                return;
            }
            var file = (FileItem) FileListSelector.SelectedItem;
            OpenFile(file.FileName, file.ActualFileName);
        }

        private async void ApplicationBarIconButton_OnClick(object sender, EventArgs e)
        {
            var result =
                MessageBox.Show(
                AppResources.Delete_all_message, AppResources.Warning, MessageBoxButton.OKCancel);

            if (result != MessageBoxResult.OK)
            {
                return;
            }
            var storageFiles = IsolatedStorageFile.GetUserStoreForApplication();
            try
            {
                foreach (var fileName in storageFiles.GetFileNames())
                {
                    await FileIOUtility.DeleteFileAsync(fileName);
                }
                _fileDB.FileItems.DeleteAllOnSubmit(_files);
                _fileDB.SubmitChanges();
                ToastNotification.ShowSimple(AppResources.Delete_all_success);
                await ListFiles();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowSimple(AppResources.Delete_all_fail);
            }
        }

        

        private async void ApplicationBarIconButton1_OnClick(object sender, EventArgs e)
        {
            //create file
            var textbox = new TextBox();
            var newFileBox = new CustomMessageBox()
            {
                Caption = AppResources.Create_caption,
                Message = AppResources.Create_message,
                LeftButtonContent = AppResources.Create,
                RightButtonContent = AppResources.Cancel,
                Content = textbox
            };

            newFileBox.Dismissed += async (s1, e1) =>
            {
                switch (e1.Result)
                {
                    case CustomMessageBoxResult.LeftButton:
                        string fileName = textbox.Text;
                        var uniqueFileName = Guid.NewGuid().ToString();
                        await FileIOUtility.WriteDataToFileAsync(uniqueFileName, "");
                        _fileDB.FileItems.InsertOnSubmit(new FileItem()
                        {
                            Id = Guid.NewGuid().GetHashCode(),
                            FileName = fileName,
                            ActualFileName = uniqueFileName,
                            LastModifiedTime = DateTime.UtcNow,
                            LastSyncTime = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc)
                        });
                        _fileDB.SubmitChanges();
                        NavigationService.Navigate(new Uri(string.Format("/Editor.xaml?name={0}&actualname={1}", fileName, uniqueFileName), UriKind.Relative));
                        break;
                    default:
                        break;
                }
            };
            newFileBox.Show();
        }

        private void ApplicationBarMenuItem_OnClick(object sender, EventArgs e)
        {
            //connect the feedback
            var email = new EmailComposeTask
            {
                To = "landxh@gmail.com", 
                Subject = AppResources.Feedback_mail_title
            };
            email.Show();
        }

        private void RichTextBox_ContentChanged(object sender, ContentChangedEventArgs e)
        {

        }

        private async void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            //download file by url
            if (e.Key != Key.Enter) return;
            //download file
            var phoneTextBox = sender as PhoneTextBox;
            if (phoneTextBox == null) return;
            var url = phoneTextBox.Text;
            try
            {
                var targetUri = new UriBuilder(url).Uri;
                var client = new WebClient();
                client.DownloadStringCompleted += async (s1, e1) =>
                {
                    var headers = ((WebClient) s1).ResponseHeaders;
                    if (headers == null)
                    {
                        SimpleProgressIndicator.Set(false);
                        MessageBox.Show(AppResources.Invalid_url);
                        return;
                    }
                    string fileName = headers.AllKeys.Contains("Content-Disposition") ?  // if has the header, use the header's file name
                        headers["Content-Disposition"] : url.Split('/')[url.Split('/').Count() - 1];
                    if (fileName.Contains("filename="))
                        fileName = fileName.Split(new string[] {"filename="}, StringSplitOptions.None)[1];
                    fileName = fileName.Replace("\\", ""); // remove slash
                    fileName = fileName.Replace("'", ""); // remove '
                    fileName = fileName.Replace("\"", ""); // remove "
                    var content = e1.Result;
                    try
                    {
                        var uniqueFileName = Guid.NewGuid().ToString();
                        await FileIOUtility.CreateFileAndWriteDataAsync(uniqueFileName, content); // write to local
                        _fileDB.FileItems.InsertOnSubmit(new FileItem()
                        {
                            Id = Guid.NewGuid().GetHashCode(),
                            FileName = fileName,
                            ActualFileName = uniqueFileName,
                            LastModifiedTime = DateTime.UtcNow,
                            LastSyncTime = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc)
                        });
                        _fileDB.SubmitChanges();
                        ToastNotification.ShowSimple(AppResources.Save_success);
                        ListFiles();
                    }
                    catch (Exception)
                    {
                        phoneTextBox.Text = "";
                        SimpleProgressIndicator.Set(false);
                        MessageBox.Show(AppResources.Something_wrong);
                        return;
                    }
                    phoneTextBox.Text = "";
                    SimpleProgressIndicator.Set(false);
                };
                SimpleProgressIndicator.Set(true);
                FileListSelector.Focus();
                client.DownloadStringAsync(targetUri);
            } 
            catch(Exception ex)
            {
                phoneTextBox.Text = "";
                SimpleProgressIndicator.Set(false);
                MessageBox.Show(AppResources.Invalid_url);
            }

        }

        private async void Dropbox_OnTap(object sender, GestureEventArgs e)
        {
            if (Authentication.DropboxIsLogin())
            {
                NavigationService.Navigate(new Uri("/OnlineFileSelect.xaml?e=dropbox", UriKind.Relative));
                return;
            }
            DropboxAuthentication();
        }

        public async void DropboxAuthentication()
        {
            SimpleProgressIndicator.Set(true);
            try
            {
                var app = Application.Current as App;
                if (app != null)
                    (Application.Current as App).DropboxClient = new DropNetClient(
                        app.DropboxApiKey,
                        app.DropboxApiSecret);
            }
            catch (Exception ex)
            {
                var x = ex.InnerException;
                Console.WriteLine(ex.Message);
            }
            var requestToken = await (App.Current as App).DropboxClient.GetRequestToken();
            var url = (App.Current as App).DropboxClient.BuildAuthorizeUrl(requestToken, "https://www.google.com/robots.txt");
            AuthGrid.Visibility = Visibility.Visible;
            AuthBrowser.Navigate(new Uri(url));
            AuthBrowser.LoadCompleted += async (s1, e1) =>
            {
                SimpleProgressIndicator.Set(false);
                if (e1.Uri.Host.Contains("www.google.com"))
                {
                    var accessToken = await (App.Current as App).DropboxClient.GetAccessToken();
                    // save to setting
                    Authentication.SetDropboxInfo(accessToken.Token, accessToken.Secret);
                    // end save
                    AuthGrid.Visibility = Visibility.Collapsed;
                    // success message
                    ToastNotification.ShowSimple("Successfully connected to Dropbox.");
                    ListFiles();
                }
                else
                {
                }
            };
        }

        private void DropboxLogOff(object s, EventArgs e)
        {
            Authentication.DropboxLogOff();
            DropboxButtonBlock.Visibility = (Authentication.DropboxIsLogin())
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void OnedriveLogOff(object s, EventArgs e)
        {
            Authentication.DropboxLogOff();
            DropboxButtonBlock.Visibility = (Authentication.DropboxIsLogin())
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private async void Onedrive_OnTap(object sender, GestureEventArgs e)
        {
            SimpleProgressIndicator.Set(true);
            var success = await Authentication.OnedriveAuthentication();
            if (success)
            {
                SimpleProgressIndicator.Set(false);
                NavigationService.Navigate(new Uri("/OnlineFileSelect.xaml?e=onedrive", UriKind.Relative));
            }
            else
            {
                SimpleProgressIndicator.Set(false);
                ToastNotification.ShowSimple(AppResources.Fail);
            }
        }

        private void Update_Log()
        {
            // display text
            var setting = IsolatedStorageSettings.ApplicationSettings;
            if (!setting.Contains("update-20141116"))
            {
                var noticeMessageBox = new CustomMessageBox()
                {
                    Caption = AppResources.what_new,
                    Message = AppResources.update_20141116,
                    LeftButtonContent = AppResources.ok
                };

                noticeMessageBox.Dismissed += (s, e1) =>
                {
                    setting.Add("update-20141116", "");
                    setting.Save();
                };

                noticeMessageBox.Show();
            }
        }


    }
}