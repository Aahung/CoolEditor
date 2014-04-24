using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CoolEditor.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;

namespace CoolEditor.Class
{
    class ShareBox : CustomMessageBox
    {
        private string _fileName;
        public ShareBox(string fileName) : base()
        {
            _fileName = fileName;
            var listPicker = new ListPicker();
            listPicker.Items.Add(AppResources.Email);
            listPicker.Items.Add(AppResources.Copy_to_clipboard);

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
                var content = await FileIOUtility.ReadFileContentsAsync(_fileName);
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
                    }
            }
        }
    }
}
