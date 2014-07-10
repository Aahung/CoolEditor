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
        static public async Task CreateFileAndWriteDataAsync(string fileName, string content)
        {
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
                    //try
                    //{
                    //    var currentPage = ((PhoneApplicationFrame) Application.Current.RootVisual).Content as MainPage;
                    //    if (currentPage != null) //await currentPage.ListFiles();
                    //        currentPage.OpenFile(actualFileName);
                    //}
                    //catch (Exception ex)
                    //{
                    //    Console.WriteLine(ex.Message);
                    //}
                };
                customMessageBox.Show();
            }
            else
            {
                actualFileName = await WriteDataToFileAsync(fileName, content);
                //try
                //{
                //    var currentPage = ((PhoneApplicationFrame)Application.Current.RootVisual).Content as MainPage;
                //    if (currentPage != null) //await currentPage.ListFiles();
                //        currentPage.OpenFile(actualFileName);
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.Message);
                //}
            }
        }

        static private async Task<Boolean> FileNameExists(string fileName)
        {
            try
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
            catch (Exception)
            {
                return false;
            }
        }

        static public async Task<Boolean> RenameFileAsync(string fileName, string newFileName)
        {
            if (await FileNameExists(newFileName))
            {
                return false;
            }
            var file = await Folder.GetFileAsync(fileName);
            try
            {
                file.RenameAsync(newFileName, NameCollisionOption.FailIfExists);
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        } 

        static public async Task<string> WriteDataToFileAsync(string actualFileName, string content)
        {
            return await WriteDataToFileAsync(actualFileName, content, true);
        }

        static public async Task<string> WriteDataToFileAsync(string actualFileName, string content, Boolean overwrite)
        {
            byte[] data = Encoding.UTF8.GetBytes(content);

            var file = await Folder.CreateFileAsync(actualFileName, 
                (overwrite)
                ? CreationCollisionOption.ReplaceExisting
                : CreationCollisionOption.GenerateUniqueName);

            using (var s = await file.OpenStreamForWriteAsync())
            {
                await s.WriteAsync(data, 0, data.Length);
            }

            return file.Name;
        }

        static public async Task<string> ReadFileContentsAsync(string actualFileName)
        {
            try
            {
                var file = await Folder.OpenStreamForReadAsync(actualFileName);

                using (var streamReader = new StreamReader(file, true))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        static public async Task<Boolean> DeleteFileAsync(string actualFileName)
        {
            if (await FileNameExists(actualFileName))
                return true;
            try
            {
                var file = await Folder.GetFileAsync(actualFileName);
                await file.DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        } 

        
    }
}
