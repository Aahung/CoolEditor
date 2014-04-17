using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace CoolEditor.Class
{
    class FileIOUtility
    {
        static public async Task WriteDataToFileAsync(string fileName, string content)
        {
            byte[] data = Encoding.UTF8.GetBytes(content);

            var folder = ApplicationData.Current.LocalFolder;
            
            var file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            using (var s = await file.OpenStreamForWriteAsync())
            {
                await s.WriteAsync(data, 0, data.Length);
            }
        }

        static public async Task<string> ReadFileContentsAsync(string fileName)
        {
            var folder = ApplicationData.Current.LocalFolder;

            try
            {
                var file = await folder.OpenStreamForReadAsync(fileName);

                using (var streamReader = new StreamReader(file))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        static public async Task<Boolean> DeleteFileAsync(string fileName)
        {
            var folder = ApplicationData.Current.LocalFolder;
            try
            {
                var file = await folder.GetFileAsync(fileName);
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
