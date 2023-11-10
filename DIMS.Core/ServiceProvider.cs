using DIMS.Hardware;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DIMS.Core
{
    public class ServiceProvider
    {
        private JsonRpcClient _Client;
        private Infoscanner _Scanner;

        /// <summary>
        /// 服务初始化
        /// </summary>
        public ServiceProvider()
        {
            
        }

        public ServiceProvider(string serviceAddr, int servicePort)
        { 
            ServiceAddress = serviceAddr;
            ServicePort = servicePort;
        }

        public ServiceProvider(string serviceAddr, int servicePort, string scannerAddr, int scannerPort)
        { 
            ServiceAddress = serviceAddr;
            ServicePort = servicePort;
            ScannerAddress = scannerAddr;
            ScannerPort = scannerPort;
        }

        private string _ServiceAddress = "192.168.255.3";
        public string ServiceAddress
        {
            get => _ServiceAddress;
            set => _ServiceAddress = value;
        }

        private int _ServicePort = 12000;
        public int ServicePort
        {
            get => _ServicePort;
            set => _ServicePort = value;
        }

        private string _ScannerAddress = "192.168.0.100";
        public string ScannerAddress
        {
            get => _ScannerAddress;
            set => _ScannerAddress = value;
        }

        private int _ScannerPort = 4096;
        public int ScannerPort
        { 
            get => _ScannerPort; 
            set => _ScannerPort = value;
        }

        public async Task<string> GetTrayCodeAsync(CancellationToken cancellation = default)
        {
            try
            {
                if (_Scanner == null)
                    _Scanner = new Infoscanner(_ScannerAddress, _ScannerPort);

                return await _Scanner.GetBarcodeAsync(cancellation);
            }
            catch (Exception)
            { 
                return string.Empty;
            }
        }

        public async Task<Tuple<int, bool>> GetProductModelAsync(string traycode, CancellationToken cancellation = default)
        {
            try
            {
                if (_Client == null)
                    _Client = new JsonRpcClient(_ServiceAddress, _ServicePort);

                var response = await _Client.QueryAsync(traycode, cancellation);
                if (response == null || !response.IsSuccess)
                    return new Tuple<int, bool>(-1, false);

                return new Tuple<int, bool>(response.ProductModelIndex, response.IsExist);
            }
            catch (Exception)
            {
                return new Tuple<int, bool>(-1, false);
            }
        }
    }
}
