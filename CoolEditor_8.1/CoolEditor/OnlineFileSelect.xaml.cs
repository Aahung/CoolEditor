using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CoolEditor.Class;
using CoolEditor.Resources;
using DropNetRT.Models;
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

        public string Path;

        public bool IsDirectory;

        public int Revision;
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
    }
    public partial class OnlineFileSelect : PhoneApplicationPage
    {
        private ObservableCollection<FileMetaData> _source;
        private Stack<DropNetRT.Models.Metadata> _folderDataStack;
        private DropNetRT.Models.Metadata _currentFolderData;
        public OnlineFileSelect()
        {
            InitializeComponent();
            _source = new ObservableCollection<FileMetaData>();
            _folderDataStack = new Stack<Metadata>();
            ListSelector.ItemsSource = _source;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ListFilesInFolder("/");
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

        private async void ListFilesInFolder(string path)
        {
            ListSelector.SelectedItem = null;
            SimpleProgressIndicator.Set(true);
            var result = await LoadFilesInFolder(path);
            if (result == null)
            {
                ToastNotification.ShowSimple(AppResources.Network_error);
            }
            else
            {
                _currentFolderData = result;
                UpdateListAndTitle();
            }
            SimpleProgressIndicator.Set(false);
        }

        private void UpdateListAndTitle()
        {
            FolderPathBlock.Text = _currentFolderData.Path;
            if (_source != null && _source.Any())
            {
                _source.Clear();
            }
            foreach (var file in _currentFolderData.Contents.Where(file => _source != null))
            {
                if (file.IsDirectory)
                    _source.Add(new FileMetaData(file.Size, file.Name, file.IsDirectory, file.Path, file.UTCDateModified, file.Revision));
            }
            foreach (var file in _currentFolderData.Contents.Where(file => _source != null))
            {
                if (!file.IsDirectory)
                    _source.Add(new FileMetaData(file.Size, file.Name, file.IsDirectory, file.Path, file.UTCDateModified, file.Revision));
            }
        }

        private static async Task<DropNetRT.Models.Metadata> LoadFilesInFolder(string path)
        {
            try
            {
                return await (App.Current as App).DropboxClient.GetMetaData(path);
            }
            catch (Exception ex)
            {
                return null;
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
                ListFilesInFolder(selectedItem.Path);
            }
            else
            {
                // open the file
                SimpleProgressIndicator.Set(true);
                var fileBytes = await (App.Current as App).DropboxClient.GetFile(selectedItem.Path);
                var uniqueFileName = Guid.NewGuid().ToString();
                if (fileBytes != null)
                {
                    var fileDB = new FileItemDataContext(FileItemDataContext.DBConnectionString);
                    var theFile = new FileItem()
                    {
                        Id = Guid.NewGuid().GetHashCode(),
                        FileName = selectedItem.FileName,
                        Revision = selectedItem.Revision,
                        ActualFileName = uniqueFileName,
                        OnlinePath = selectedItem.Path,
                        OnlineProvider = "dropbox",
                        LastModifiedTime = selectedItem.LastModifiedTime,
                        LastSyncTime = DateTime.UtcNow,
                        ModifiedSinceLastSync = false
                    };
                    try
                    {
                        fileDB.FileItems.InsertOnSubmit(theFile);
                        fileDB.SubmitChanges();
                        var content = Encoding.UTF8.GetString(fileBytes, 0, fileBytes.Length);
                        await FileIOUtility.CreateFileAndWriteDataAsync(uniqueFileName, content);
                        MessageBox.Show(AppResources.Save_success);
                        SimpleProgressIndicator.Set(false);
                    }
                    catch (Exception)
                    {
                        fileDB.FileItems.DeleteOnSubmit(theFile);
                        fileDB.SubmitChanges();
                        SimpleProgressIndicator.Set(false);
                        ToastNotification.ShowSimple(AppResources.Can_not_open);
                    }
                }
            }
        }
    }
}