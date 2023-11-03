using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DIMS.Core
{
    public class JsonRpcClient : IDIMS
    {
        private string _Address = "";
        private int _Port;

        public JsonRpcClient(string address, int port)
        {
            _Address = address;
            _Port = port;
        }


        public async Task<Response> QueryAsync(string tray, CancellationToken cancellation = default)
        {
            try
            {
                var client = new TcpClient(_Address, _Port) {  ReceiveTimeout = 3000, SendTimeout = 3000 };
                var stream = client.GetStream();
                var rpc = JsonRpc.Attach(stream);
                var response = await rpc.InvokeAsync<Response>("Query", tray);
                rpc.Dispose();
                stream.Dispose();
                client.Close();
                client.Dispose();

                return response;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> UpdateAsync(string tray, int state, CancellationToken cancellation = default)
        {
            try
            {
                var client = new TcpClient(_Address, _Port) { ReceiveTimeout = 3000, SendTimeout = 3000 };
                var stream = client.GetStream();
                var rpc = JsonRpc.Attach(stream);
                var response = await rpc.InvokeAsync<bool>("Update", new object[] { tray, state });
                rpc.Dispose();
                stream.Dispose();
                client.Close();
                client.Dispose();

                return response;
            }
            catch (Exception)
            { 
                
            }

            return false;
        }
    }
}
