using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMS.ViewModels
{
    public class LogMessage : ReactiveObject
    {
        private Splat.LogLevel _LogLevel;
        public Splat.LogLevel LogLevel
        {
            get => _LogLevel;
            set => this.RaiseAndSetIfChanged(ref _LogLevel, value);
        }

        private DateTime _TimeStamp;
        public DateTime TimeStamp
        {
            get => _TimeStamp;
            set => this.RaiseAndSetIfChanged(ref _TimeStamp, value);
        }

        private string? _Message;
        public string? Message
        {
            get => _Message;
            set => this.RaiseAndSetIfChanged(ref _Message, value);
        }
    }
}
