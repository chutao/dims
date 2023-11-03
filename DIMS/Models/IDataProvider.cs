using DIMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DIMS
{
    public interface IDataProvider
    {
        ReaderWriterLockSlim Locker { get; }

        IEnumerable<TrayViewModel>? GetTrays();

        bool TrayPushIntoQueue(string? rfid, string? traycode);

        bool TrayPopOutQueue();
    }
}
