using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Windows.Storage.Streams;
using CoolEditor.Class;
using CoolEditor.Class.DropNetRt.Models;
using CoolEditor.Resources;
using Microsoft.Live;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace CoolEditor
{
    class FileMetaData
    {
        private readonly string _fileName;

        public string FileName
        {
            get { return _fileName; }
        }

        public string Size { get; private set; }

        public string Description
        {
            get
            {
                if (IsDirectory)
                {
                    return "Click to open this directiory";
                }
                else
                {
                    return "Size: " + Size;
                }
            }
        }

        public DateTime LastModifiedTime { get; set; }

        public string LastModifiedStr
        {
            get { return string.Format("{0}: {1}", AppResources.Last_modified, LastModifiedTime.ToLocalTime()); }
        }

        public string LastModifiedVisibilityStr
        {
            get { return (IsDirectory) ? "Collapsed" : "Visible"; }
        }

        public string ID { get; set; }

        public string Path;

        public bool IsDirectory;

        public int Revision;

        public ObservableCollection<FileMetaData> Children = new ObservableCollection<FileMetaData>();

        public FileMetaData()
        {
        }

        public FileMetaData(string size, string fileName, bool isDirectory, string path, DateTime lastModifiedTime)
        {
            this.Size = size;
            this._fileName = fileName;
            IsDirectory = isDirectory;
            Path = path;
            if (lastModifiedTime == null)
            {
                LastModifiedTime = DateTime.UtcNow;
            }
            else
            {
                LastModifiedTime = lastModifiedTime;
            }
        }

        public FileMetaData(string size, string fileName, bool isDirectory, string path, DateTime lastModifiedTime,
            int revision)
        {
            this.Size = size;
            this._fileName = fileName;
            IsDirectory = isDirectory;
            Path = path;
            if (lastModifiedTime == null)
            {
                LastModifiedTime = DateTime.UtcNow;
            }
            else
            {
                LastModifiedTime = lastModifiedTime;
            }
            Revision = revision;
        }

        public void SetOnedriveId(string id)
        {
            ID = id;
        }
    }
    public partial class OnlineFileSelect : PhoneApplicationPage
    {
        private ObservableCollection<FileMetaData> _source;
        private Stack<FileMetaData> _folderDataStack;
        private FileMetaData _currentFolderData;
        private string provider = null;
        public OnlineFileSelect()
        {
            InitializeComponent();
            _source = new ObservableCollection<FileMetaData>();
            _folderDataStack = new Stack<FileMetaData>();
            ListSelector.ItemsSource = _source;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var _queryStrings = this.NavigationContext.QueryString;
            if (!_queryStrings.Any()) NavigationService.GoBack();
            provider = _queryStrings["e"];
            ProviderTextBlock.Text = provider;
            if (provider == "dropbox")
                ListDropboxFilesInFolder("/");
            else
            {
                ListOnedriveFilesInFolder("", "/");
            }
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            if (_folderDataStack.Any())
            {
                e.Cancel = true;
                _currentFolderData = _folderDataStack.Pop();
                UpdateListAndTitle();
            }
        }

        private async void ListOnedriveFilesInFolder(string _id, string path)
        {
            ListSelector.SelectedItem = null;
            SimpleProgressIndicator.Set(true);
            string id = _id;
            if (path == "/")
            {
                id = "me/skydrive";
            }
            LiveOperationResult operationResult =
                await (App.Current as App).OnedriveClient.GetAsync(id + "/files");
            dynamic result = operationResult.Result;
            _currentFolderData = DynamicToFileMetaData(result.data, path, id);
            UpdateListAndTitle();
            SimpleProgressIndicator.Set(false);
        }

        private async void ListDropboxFilesInFolder(string path)
        {
            ListSelector.SelectedItem = null;
            SimpleProgressIndicator.Set(true);
            var result = await (App.Current as App).DropboxClient.GetMetaData(path);
            if (result == null)
            {
                ToastNotification.ShowSimple(AppResources.Network_error);
            }
            else
            {
                _currentFolderData = DropboxMetadataToFileMetaData(result);
                UpdateListAndTitle();
            }
            SimpleProgressIndicator.Set(false);
        }

        private void UpdateListAndTitle()
        {
            FolderPathBlock.Text = _currentFolderData.Path;
            _source.Clear();
            foreach (var item in _currentFolderData.Children)
            {
                _source.Add(item);
            }
        }


        private async void ListSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = (sender as LongListSelector).SelectedItem as FileMetaData;
            if (selectedItem == null)
            {
                return;
            }
            if (selectedItem.IsDirectory)
            {
                // enter the directory
                _folderDataStack.Push(_currentFolderData);
                if (provider == "dropbox")
                    ListDropboxFilesInFolder(selectedItem.Path);
                if (provider == "onedrive")
                {
                    string id = selectedItem.ID;
                    ListOnedriveFilesInFolder(id, _currentFolderData.Path + selectedItem.FileName + "/");
                }
            }
            else
            {
                string oneDriveId = null;
                // open the file
                SimpleProgressIndicator.Set(true);
                string content = AppResources.Fail;
                if (provider == "dropbox")
                {
                    var fileBytes = await (App.Current as App).DropboxClient.GetFile(selectedItem.Path);
                    content = Encoding.UTF8.GetString(fileBytes, 0, fileBytes.Length);
                }
                if (provider == "onedrive")
                {
                    try
                    {
                        var downloadPath = selectedItem.ID + "/content";
                        LiveDownloadOperation downloadOperation = await
                            (App.Current as App).OnedriveClient.CreateBackgroundDownloadAsync(downloadPath);
                        var downloadResult = await downloadOperation.StartAsync();
                        if (downloadResult != null && downloadResult.Stream != null)
                        {
                            var randomAccessStream = await downloadResult.GetRandomAccessStreamAsync();
                            var resourceStream = (IInputStream)randomAccessStream.GetInputStreamAt(0);
                            var reader = new DataReader(resourceStream);
                            await reader.LoadAsync((uint)randomAccessStream.Size);
                            content = reader.ReadString((uint)randomAccessStream.Size);
                            reader.DetachStream();
                            oneDriveId = selectedItem.ID;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                var uniqueFileName = Guid.NewGuid().ToString();
                var fileDB = new FileItemDataContext(FileItemDataContext.DBConnectionString);
                var theFile = new FileItem()
                {
                    Id = Guid.NewGuid().GetHashCode(),
                    FileName = selectedItem.FileName,
                    Revision = selectedItem.Revision,
                    ActualFileName = uniqueFileName,
                    OnlinePath = selectedItem.Path,
                    OnlineProvider = provider,
                    LastModifiedTime = selectedItem.LastModifiedTime,
                    LastSyncTime = DateTime.UtcNow,
                    ModifiedSinceLastSync = false,
                    LocalPath = oneDriveId
                };
                try
                {
                    fileDB.FileItems.InsertOnSubmit(theFile);
                    fileDB.SubmitChanges();
                    await FileIOUtility.CreateFileAndWriteDataAsync(uniqueFileName, content);
                    MessageBox.Show(AppResources.Save_success);
                }
                catch (Exception)
                {
                    fileDB.FileItems.DeleteOnSubmit(theFile);
                    fileDB.SubmitChanges();
                    ToastNotification.ShowSimple(AppResources.Can_not_open);
                }
                SimpleProgressIndicator.Set(false);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // back to normal page
            NavigationService.GoBack();
        }

        private static FileMetaData DropboxMetadataToFileMetaData(Metadata data)
        {
            FileMetaData metadata = new FileMetaData();
            metadata.Path = data.Path;
            metadata.Children.Clear();
            foreach (var file in data.Contents.Where(file => metadata != null))
            {
                if (file.IsDirectory)
                    metadata.Children.Add(new FileMetaData(file.Size, file.Name, file.IsDirectory, file.Path, file.UTCDateModified, file.Revision));
            }
            foreach (var file in data.Contents.Where(file => metadata != null))
            {
                if (!file.IsDirectory)
                    metadata.Children.Add(new FileMetaData(file.Size, file.Name, file.IsDirectory, file.Path, file.UTCDateModified, file.Revision));
            }
            return metadata;
        }

        private static FileMetaData DynamicToFileMetaData(dynamic data, string path, string id)
        {
            FileMetaData metadata = new FileMetaData();
            metadata.Path = path;
            metadata.ID = id;
            metadata.Children.Clear();
            foreach (dynamic file in data)
            {
                try
                {
                    var size = file.size;
                    string sizeStr;
                    if (size <= 1024) sizeStr = "1KB";
                    else if (size <= 1024*1024) sizeStr = size/1024 + "KB";
                    else sizeStr = size/1024/1024 + "MB";
                    var m = new FileMetaData(sizeStr, file.name, (file.type == "folder" || file.type == "album"), path + file.name,
                        Convert.ToDateTime(file.client_updated_time), ((file.type == "folder" || file.type == "album")) ? 0 : 1);
                    m.SetOnedriveId(file.id);
                    metadata.Children.Add(m);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return metadata;
        }
    }
}