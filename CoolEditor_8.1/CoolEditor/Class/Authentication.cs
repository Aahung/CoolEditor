using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoolEditor.Resources;
using DropNetClient = CoolEditor.Class.DropNetRt.DropNetClient;

namespace CoolEditor.Class
{
    class Authentication
    {
        static void DropboxAuthentication()
        {
            
        }

        public static void DropboxLogOff()
        {
            if (DropboxIsLogin())
            {
                var setting = IsolatedStorageSettings.ApplicationSettings;
                setting.Remove("dropbox-key");
                setting.Remove("dropbox-secret");
                setting.Save();
                (App.Current as App).DropboxClient = null;
                ToastNotification.ShowSimple(AppResources.Log_off);
            }
        }

        // check whether dropbox is login and create client if not
        public static bool DropboxIsLogin()
        {
            SimpleProgressIndicator.Set(true);
            var setting = IsolatedStorageSettings.ApplicationSettings;
            if (setting.Contains("dropbox-key") &&
                !String.IsNullOrEmpty((string)setting["dropbox-key"]))
            {
                if ((App.Current as App).DropboxClient == null)
                {
                    (App.Current as App).DropboxClient = new DropNetClient((App.Current as App).DropboxApiKey,
                        (App.Current as App).DropboxApiSecret,
                        (string)setting["dropbox-key"], (string)setting["dropbox-secret"]);
                }
                SimpleProgressIndicator.Set(false);
                return true;
            }
            else
            {
                SimpleProgressIndicator.Set(false);
                return false;
            }
        }
    }
}
