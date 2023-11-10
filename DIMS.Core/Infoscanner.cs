using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DIMS.Hardware
{
    public class Infoscanner : IDisposable
    {
        #region Dispose Implements
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Infoscanner()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        private string _Address;
        private int _Port;

        public Infoscanner()
        {
        }

        public Infoscanner(string address, int port)
        { 
            _Address = address;
            _Port = port;
        }

        public bool IsConnected { get; private set; }

        public string Message { get; private set; }

        public string Address
        {
            get => _Address;
            set => _Address = value;
        }

        public int Port
        {
            get => _Port;
            set => _Port = value;
        }

        public async Task<string> GetBarcodeAsync(CancellationToken cancellation = default)
        {
            string result = null;

            try
            {
                var client = new TcpClient() { SendTimeout = 3000, ReceiveTimeout = 3000 };

                var cancelTask = Task.Delay(1000);
                var connectTask = client.ConnectAsync(_Address ?? "127.0.0.1", _Port);

                await await Task.WhenAny(cancelTask, connectTask);
                if(cancelTask.IsCompleted)
                {
                    client.Dispose();
                    return string.Empty;
                }

                IsConnected = client.Connected;
                if (!IsConnected)
                    return string.Empty;

                var stream = client.GetStream();

                byte[] bytes;
                bytes = Encoding.UTF8.GetBytes("TON");
                await stream.WriteAsync(bytes, 0, bytes.Length);
                byte[] buffer = new byte[2048];
                int count = await stream.ReadAsync(buffer, 0, buffer.Length, cancellation);
                if (count > 0)
                {
                    result = Encoding.UTF8.GetString(buffer, 0, count);
                    if (!string.IsNullOrEmpty(result))
                        result = result.TrimStart().TrimEnd(new char[] { ' ', '\r', '\n' });
                }

                stream.Dispose();
                client.Dispose();
            }
            catch (Exception ex)
            {
                Message = "条码获取失败!\n" + ex.Message;
            }

            return result;
        }
    }
}
