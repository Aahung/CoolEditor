using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.Shell;

namespace CoolEditor.Class
{
    class SimpleProgressIndicator
    {
        public static void Set(bool value)
        {
            try
            {
                if (SystemTray.ProgressIndicator == null)
                {
                    SystemTray.ProgressIndicator = new ProgressIndicator();
                }
                SystemTray.ProgressIndicator.IsIndeterminate = value;
                SystemTray.ProgressIndicator.IsVisible = value;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
