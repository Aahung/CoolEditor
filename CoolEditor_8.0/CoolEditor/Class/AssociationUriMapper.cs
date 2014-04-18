using System;
using System.IO;
using System.Windows.Navigation;
using Windows.Phone.Storage.SharedAccess;

namespace CoolEditor.Class
{
    class AssociationUriMapper : UriMapperBase
    {
        private string tempUri;

        public override Uri MapUri(Uri uri)
        {
            tempUri = uri.ToString();

            // File association launch
            if (tempUri.Contains("/FileTypeAssociation"))
            {
                // Get the file ID (after "fileToken=").
                int fileIDIndex = tempUri.IndexOf("fileToken=") + 10;
                string fileID = tempUri.Substring(fileIDIndex);

                // Get the file name.
                string incomingFileName =
                    SharedStorageAccessManager.GetSharedFileName(fileID);

                // Get the file extension.
                string incomingFileType = Path.GetExtension(incomingFileName);

                // Map the .sdkTest1 and .sdkTest2 files to different pages.
                switch (incomingFileType)
                {
                    default:
                        return new Uri("/Editor.xaml?fileToken=" + fileID, UriKind.Relative);
                }
            }
            // Otherwise perform normal launch.
            return uri;
        }
    }
}