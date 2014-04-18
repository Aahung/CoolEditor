using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Coding4Fun.Toolkit.Controls;

namespace CoolEditor.Class
{
    class ToastNotification
    {
        static public void ShowSimple(string body)
        {
            ShowSimple("Cool Editor", body, 2000);
        }
        static public void ShowSimple(string title, string body)
        {
            ShowSimple(title, body, 2000);
        }
        static public void ShowSimple(string title, string body, int misec)
        {
            var toast = new ToastPrompt
            {
                Title = title,
                Message = body,
                MillisecondsUntilHidden = misec
            };
            //toast.TextOrientation = Orientation.Horizontal;
            toast.Show();
        }
    }
}
