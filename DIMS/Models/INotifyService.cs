using Avalonia.Controls.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMS.Models
{
    internal interface INotifyService
    {
        void Notify(string? title, string? msg, NotificationType type, TimeSpan? timeout = null);
    }
}
