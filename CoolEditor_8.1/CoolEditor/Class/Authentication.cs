using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store;
using CoolEditor.Resources;
using Microsoft.Live;
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
                var roamSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                roamSettings.Values.Remove("dropbox-key");
                roamSettings.Values.Remove("dropbox-secret");
                (App.Current as App).DropboxClient = null;
                ToastNotification.ShowSimple(AppResources.Log_off);
            }
        }

        // check whether dropbox is login and create client if not
        public static bool DropboxIsLogin()
        {
            SimpleProgressIndicator.Set(true);
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values.ContainsKey("dropbox-key") &&
                !String.IsNullOrEmpty((string)roamingSettings.Values["dropbox-key"]))
            {
                if ((App.Current as App).DropboxClient == null)
                {
                    (App.Current as App).DropboxClient = new DropNetClient((App.Current as App).DropboxApiKey,
                        (App.Current as App).DropboxApiSecret,
                        (string)roamingSettings.Values["dropbox-key"], (string)roamingSettings.Values["dropbox-secret"]);
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

        public static void SetDropboxInfo(string key, string secret)
        {
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["dropbox-key"] = key;
            roamingSettings.Values["dropbox-secret"] = secret;
        }

        // One drive
        public static bool OnedriveIsLogin()
        {
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values.ContainsKey("onedriveLogin") && (string) roamingSettings.Values["onedriveLogin"] == "true")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async static Task<bool> OnedriveAuthentication()
        {
            if ((App.Current as App).OnedriveClient != null) return true;
            bool connected = false;
            try
            {
                var authClient = new LiveAuthClient();
                LiveLoginResult result = await authClient.LoginAsync(new string[] { "wl.signin", "wl.skydrive", "wl.skydrive_update" });

                var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                if (result.Status == LiveConnectSessionStatus.Connected)
                {
                    connected = true;
                    var connectClient = new LiveConnectClient(result.Session);
                    var meResult = await connectClient.GetAsync("me");
                    dynamic meData = meResult.Result;
                    (App.Current as App).OnedriveClient = connectClient;
                    roamingSettings.Values["onedriveLogin"] = "true";
                    return true;
                }
                else
                {
                    roamingSettings.Values["onedriveLogin"] = "false";
                    return false;
                }
            }
            catch (LiveAuthException ex)
            {
                // Display an error message.
                return false;
            }
            catch (LiveConnectException ex)
            {
                // Display an error message.
                return false;
            }
        }
    }
}
