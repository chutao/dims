using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMS.Helpers
{
    public interface ILogger
    {
        void Info(string msg);
        void Info(Exception ex, string msg);

        void Warn(string msg);
        void Warn(Exception ex, string msg);

        void Error(string msg);
        void Error(Exception ex, string msg);

        void Fatal(string msg);
        void Fatal(Exception ex, string msg);

        void Debug(string msg);
        void Debug(Exception ex, string msg);
    }
}
