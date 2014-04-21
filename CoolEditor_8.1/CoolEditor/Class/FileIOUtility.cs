using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Windows.Storage;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace CoolEditor.Class
{
    class FileIOUtility
    {
        static readonly StorageFolder Folder = ApplicationData.Current.LocalFolder;
        static public async Task<string> CreateFileAndWriteDataAsync(string fileName, string content)
        {
            SimpleProgressIndicator.Set(true);
            string actualFileName = null;
            //check if filename repeated
            var fileNameExists = await FileNameExists(fileName);
            if (fileNameExists)
            {
                var customMessageBox = new CustomMessageBox()
                {
                    Caption = fileName + " already exists",
                    Message = "What do you want to do?",
                    LeftButtonContent = "Replace",
                    RightButtonContent = "Keep both"
                };
                Boolean waiting = true;
                customMessageBox.Dismissed += async (s1, e1) =>
                {
                    switch (e1.Result)
                    {
                        case CustomMessageBoxResult.LeftButton:
                            actualFileName = await WriteDataToFileAsync(fileName, content, true);
                            break;
                        case CustomMessageBoxResult.RightButton:
                            actualFileName = await WriteDataToFileAsync(fileName, content, false);
                            break;
                    }
                    try
                    {
                        var currentPage = ((PhoneApplicationFrame) Application.Current.RootVisual).Content as MainPage;
                        if (currentPage != null) //await currentPage.ListFiles();
                            currentPage.OpenFile(actualFileName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                };
                customMessageBox.Show();
            }
            else
            {
                actualFileName = await WriteDataToFileAsync(fileName, content);
                try
                {
                    var currentPage = ((PhoneApplicationFrame)Application.Current.RootVisual).Content as MainPage;
                    if (currentPage != null) //await currentPage.ListFiles();
                        currentPage.OpenFile(actualFileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            SimpleProgressIndicator.Set(false);
            return actualFileName;
        }

        static private async Task<Boolean> FileNameExists(string fileName)
        {
            var files = await Folder.GetFilesAsync();
            foreach (var file in files)
            {
                var storageFile = file as StorageFile;
                if (storageFile != null && storageFile.Name == fileName)
                {
                    return true;
                }
            }
            return false;
        }

        static public async Task<Boolean> RenameFileAsync(string fileName, string newFileName)
        {
            SimpleProgressIndicator.Set(true);
            if (await FileNameExists(newFileName))
            {
                SimpleProgressIndicator.Set(false);
                return false;
            }
            var file = await Folder.GetFileAsync(fileName);
            try
            {
                file.RenameAsync(newFileName, NameCollisionOption.FailIfExists);
            }
            catch (Exception ex)
            {
                SimpleProgressIndicator.Set(false);
                return false;
            }
            SimpleProgressIndicator.Set(false);
            return true;
        } 

        static public async Task<string> WriteDataToFileAsync(string fileName, string content)
        {
            return await WriteDataToFileAsync(fileName, content, true);
        }

        static public async Task<string> WriteDataToFileAsync(string fileName, string content, Boolean overwrite)
        {
            SimpleProgressIndicator.Set(true);
            byte[] data = Encoding.UTF8.GetBytes(content);

            var file = await Folder.CreateFileAsync(fileName, 
                (overwrite)
                ? CreationCollisionOption.ReplaceExisting
                : CreationCollisionOption.GenerateUniqueName);

            using (var s = await file.OpenStreamForWriteAsync())
            {
                await s.WriteAsync(data, 0, data.Length);
            }

            SimpleProgressIndicator.Set(false);
            return file.Name;
        }

        static public async Task<string> ReadFileContentsAsync(string fileName)
        {
            SimpleProgressIndicator.Set(true);
            try
            {
                var file = await Folder.OpenStreamForReadAsync(fileName);

                using (var streamReader = new StreamReader(file))
                {
                    SimpleProgressIndicator.Set(false);
                    return streamReader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                SimpleProgressIndicator.Set(false);
                return string.Empty;
            }
        }

        static public async Task<Boolean> DeleteFileAsync(string fileName)
        {
            SimpleProgressIndicator.Set(true);
            try
            {
                var file = await Folder.GetFileAsync(fileName);
                await file.DeleteAsync();
                SimpleProgressIndicator.Set(false);
                return true;
            }
            catch (Exception ex)
            {
                SimpleProgressIndicator.Set(false);
                return false;
            }
        } 

        
    }
}
