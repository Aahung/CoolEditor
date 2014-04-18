using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
            listPicker.Items.Add("Email");
            listPicker.Items.Add("Copy to Clipboard");

            Caption = "Share this file via: ";
            Message = "There are several methods for you to share code files.";
            Content = listPicker;
            LeftButtonContent = "Share";
            RightButtonContent = "Cancel";
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
                                Subject = "[" + _fileName + "] Share by Cool Editor",
                                Body = content
                            };
                            email.Show();
                            break;
                        case 1:
                            //share by clipboard
                            Clipboard.SetText(content);
                            ToastNotification.ShowSimple("Copied to clipboard!");
                            break;
                    }
            }
        }
    }
}
