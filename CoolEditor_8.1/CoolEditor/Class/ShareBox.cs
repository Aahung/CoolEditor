using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;
using CoolEditor.Resources;
using Microsoft.Live;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;

namespace CoolEditor.Class
{
    class ShareBox : CustomMessageBox
    {
        private string _actualFileName;
        private string _fileName;
        private FileItemDataContext _fileDB;
        private FileItem _theFile;
        public ShareBox(string actualFileName)
            : base()
        {
            _actualFileName = actualFileName;
            _fileDB = new FileItemDataContext(FileItemDataContext.DBConnectionString);
            _theFile = (new ObservableCollection<FileItem>(
                from FileItem file in _fileDB.FileItems where file.ActualFileName == actualFileName select file))
                .FirstOrDefault();
            if (_theFile != null) _fileName = _theFile.FileName;
            else
            {
                MessageBox.Show(AppResources.Fail);
            }

            var listPicker = new ListPicker();
            listPicker.Items.Add(AppResources.Email);
            listPicker.Items.Add(AppResources.Copy_to_clipboard);
            listPicker.Items.Add(AppResources.upload_dropbox);
            listPicker.Items.Add(AppResources.upload_onedrive);

            Caption = AppResources.Share_caption;
            Message = AppResources.Share_message;
            Content = listPicker;
            LeftButtonContent = AppResources.Share;
            RightButtonContent = AppResources.Cancel;
            Dismissed += ShareBoxDismissed;
        }

        private async void ShareBoxDismissed(object sender, DismissedEventArgs e)
        {
            var shareBox = sender as ShareBox;
            if (shareBox != null && e.Result == CustomMessageBoxResult.LeftButton)
            {
                //share
                var content = await FileIOUtility.ReadFileContentsAsync(_actualFileName);
                var listPicker = shareBox.Content as ListPicker;
                if (listPicker != null)
                    switch (listPicker.SelectedIndex)
                    {
                        case 0:
                            //share by email
                            //connect the feedback
                            var email = new EmailComposeTask
                            {
                                Subject = "[" + _fileName + "] " + AppResources.Share_by_cooleditor,
                                Body = content
                            };
                            email.Show();
                            break;
                        case 1:
                            //share by clipboard
                            Clipboard.SetText(content);
                            ToastNotification.ShowSimple(AppResources.Copy_success);
                            break;
                        case 2:
                            //share by dropbox
                            if (Authentication.DropboxIsLogin())
                            {
                                SimpleProgressIndicator.Set(true);
                                // share to dropbox
                                var onlineFileName = String.Format("{0}_{1}", DateTime.Now.ToString("yyMMddHHmmss"), _fileName);
                                var fileMetaData = await (App.Current as App).DropboxClient.Upload("/CoolEditor", onlineFileName, Encoding.UTF8.GetBytes(content));
                                var shareLink = await (App.Current as App).DropboxClient.GetShare(fileMetaData.Path);
                                Clipboard.SetText(shareLink.Url);
                                // connect file with dropbox
                                if (_theFile != null)
                                {
                                    _theFile.OnlineProvider = "dropbox";
                                    _theFile.OnlinePath = String.Format("/CoolEditor/{0}", onlineFileName);
                                    _theFile.LastSyncTime = DateTime.UtcNow;
                                    _theFile.Revision = fileMetaData.Revision;
                                    _theFile.ModifiedSinceLastSync = false;
                                }
                                _fileDB.SubmitChanges();
                                var currentPage = ((PhoneApplicationFrame)Application.Current.RootVisual).Content;
                                try
                                {
                                    await ((MainPage)currentPage).ListFiles();
                                }
                                catch (Exception)
                                {
                                    ;
                                }
                                ToastNotification.ShowSimple(String.Format("Share link: {0} has already been copied to your clipboard.", shareLink.Url));
                                SimpleProgressIndicator.Set(false);
                            }
                            else
                            {
                                // lead to log in
                                var currentPage = ((PhoneApplicationFrame)Application.Current.RootVisual).Content;
                                try
                                {
                                    ((MainPage) currentPage).DropboxAuthentication();
                                }
                                catch (Exception)
                                {
                                    ;
                                }
                            }
                            break;
                        case 3:
                            // by onedrive\
                            SimpleProgressIndicator.Set(true);
                            await Authentication.OnedriveAuthentication();
                            if (true)
                            {
                                IStorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
                                IStorageFile storageFile = await applicationFolder.GetFileAsync(_actualFileName);
                                
                                var onlineFileName = String.Format("{0}_{1}", DateTime.Now.ToString("yyMMddHHmmss"), _fileName);
                                var shareFolder = await CreateOnedriveDirectoryAsync((App.Current as App).OnedriveClient,
                                    "CoolEditor", "me/skydrive");
                                LiveUploadOperation uploadOperation =
                                    await (App.Current as App).OnedriveClient.CreateBackgroundUploadAsync(
                                        shareFolder, onlineFileName, storageFile, OverwriteOption.Overwrite);
                                LiveOperationResult uploadResult = await uploadOperation.StartAsync();
                                dynamic dyResult = uploadResult.Result;
                                string fileId = dyResult.id;
                                LiveOperationResult fileInfo =
                                    await (App.Current as App).OnedriveClient.GetAsync(fileId + "/shared_read_link");
                                dynamic shareResult = fileInfo.Result;
                                Clipboard.SetText(shareResult.link);
                                // connect file with onedrive
                                if (_theFile != null)
                                {
                                    _theFile.LocalPath = fileId;
                                    _theFile.OnlineProvider = "onedrive";
                                    _theFile.OnlinePath = String.Format("/CoolEditor/{0}", onlineFileName);
                                    _theFile.LastSyncTime = DateTime.UtcNow;
                                    _theFile.ModifiedSinceLastSync = false;
                                }
                                _fileDB.SubmitChanges();
                                var currentPage = ((PhoneApplicationFrame)Application.Current.RootVisual).Content;
                                try
                                {
                                    await ((MainPage)currentPage).ListFiles();
                                }
                                catch (Exception)
                                {
                                    ;
                                }
                                ToastNotification.ShowSimple(
                                    String.Format("Share link: {0} has already been copied to your clipboard.",
                                        shareResult.link));
                            }
                            SimpleProgressIndicator.Set(false);
                            break;
                    }
            }
        }

        public async static Task<string> CreateOnedriveDirectoryAsync(LiveConnectClient client,
            string folderName, string parentFolder)
        {
            string folderId = null;

            // Retrieves all the directories.
            var queryFolder = parentFolder + "/files?filter=folders,albums";
            var opResult = await client.GetAsync(queryFolder);
            dynamic result = opResult.Result;

            foreach (dynamic folder in result.data)
            {
                // Checks if current folder has the passed name.
                if (folder.name.ToLowerInvariant() == folderName.ToLowerInvariant())
                {
                    folderId = folder.id;
                    break;
                }
            }

            if (folderId == null)
            {
                // Directory hasn't been found, so creates it using the PostAsync method.
                var folderData = new Dictionary<string, object>();
                folderData.Add("name", folderName);
                opResult = await client.PostAsync(parentFolder, folderData);
                result = opResult.Result;

                // Retrieves the id of the created folder.
                folderId = result.id;
            }

            return folderId;
        }
    }
}
