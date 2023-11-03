using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DIMS.Hardware
{

    public class MR6100 : IEnableLogger
    {
        private string? ipaddr;
        private int port;
        private MR6100Api _api = new MR6100Api();

        public MR6100()
        { 
            
        }

        public MR6100(string ip, int port)
            : this()
        {
            ipaddr = ip;
            this.port = port;
        }

        public string? IpAddress
        {
            get => ipaddr;
            set => ipaddr = value;
        }

        public int Port
        {
            get => port;
            set => port = value;
        }

        public bool EnableAntenna1 { get; set; } = true;
        public bool EnableAntenna2 { get; set; } = true;
        public bool EnableAntenna3 { get; set; } = true;
        public bool EnableAntenna4 { get; set; } = true;

        private int _Power = 30;
        public int Power
        {
            get => _Power;
            set
            {
                if (value < 0)
                    _Power = 0;
                else if (value > 30)
                    _Power = 30;
                else
                    _Power = value;
            }
        }

        public bool IsConnected { get; private set; }

        public string? Message { get; private set; }

        private bool Configure()
        {
            bool result = true;
            byte mask = 0;
            if (EnableAntenna1)
                mask |= 0b0001;
            if (EnableAntenna2)
                mask |= 0b0010;
            if (EnableAntenna3)
                mask |= 0b0100;
            if (EnableAntenna4)
                mask |= 0b1000;

            result |= (_api.SetRf(255, Power, Power, Power, Power) == MR6100Api.SUCCESS_RETURN);
            result |= (_api.SetAnt(255, mask) == MR6100Api.SUCCESS_RETURN);
            result |= (_api.SetFrequency(255, 0, new int[] { 0 }) == MR6100Api.SUCCESS_RETURN);
            result |= (_api.SetFastTagMode(255, 0) == MR6100Api.SUCCESS_RETURN);
            result |= (_api.BuzzerLEDON(255) == MR6100Api.SUCCESS_RETURN);

            return result;
        }

        public bool Connect()
        {
            try
            {
                if (IsConnected)
                    Disconnect();

                if (ipaddr == null)
                {
                    Message = "IP地址无效!";
                    return false;
                }

                if (!_api.isNetWorkConnect(ipaddr))
                {
                    Message = $"无法与IP地址{ipaddr}建立连接, 请检查地址是否有效!";
                    return false;
                }

                int status = _api.TcpConnectReader(ipaddr, port);
                if (status != MR6100Api.SUCCESS_RETURN) {
                    Message = "无法与读卡器建立连接!";
                    return false;
                }

                IsConnected = true;

                if (!Configure())
                {
                    Message = "参数设置失败!";
                }
                else
                {
                    Message = "";
                }
            }
            catch (Exception ex)
            {
                this.Log().Debug(ex, "Failed to call connect to rfid reader");
            }

            return IsConnected;
        }

        public bool Connect(string ip, int port)
        {
            ipaddr = ip;
            this.port = port;
            return Connect();
        }

        public void Disconnect()
        {
            if (!IsConnected)
                return;

            try
            {
                int status = _api.TcpCloseConnect();
                if (status != MR6100Api.SUCCESS_RETURN)
                {
                    Message = "关闭读卡器网络连接失败!";
                }
            }
            catch (Exception ex)
            {
                this.Log().Debug(ex, "Failed to call disconnect to rfid reader");
            }

            IsConnected = false;
        }

        public async Task<List<string>?> ReadAsync(int timeout = 3000)
        {
            if (!IsConnected)
                Connect();

            if (!IsConnected)
            {
                return null;
            }

            if (timeout <= 0)
                timeout = 10000;

            byte tag_flag = 0;
            int tagCount = 0;
            byte[,] tagData = new byte[500, 14];

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            return await Task.Run(() =>
            {
                string strAnteNo = "";
                string strID = "";
                string strTemp = "";
                List<string> idList = new List<string>();

                while (true)
                {
                    int status = _api.EpcMultiTagIdentify(255, ref tagData, ref tagCount, ref tag_flag);
                    if (status == MR6100Api.SUCCESS_RETURN)
                    {
                        for (int i = 0; i < tagCount; i++)
                        {
                            int j = 0;
                            strID = "";
                            strAnteNo = string.Format("{0:X2}", tagData[i, 1]);
                            for (j = 2; j < 14; j++)
                            {
                                strTemp = string.Format("{0:X2}", tagData[i, j]);
                                strID += strTemp;
                            }
                            if (strID == "000000000000000000000000")
                            {
                                continue;
                            }

                            idList.Add(strID);
                        }

                        break;
                    }

                    if (stopwatch.ElapsedMilliseconds >= timeout)
                        break;
                }

                return idList;
            });
        }
    }

    public class MR6100Api
    {
        public struct DCB
        {
            public int DCBlength;

            public int BaudRate;

            public uint flags;

            public ushort wReserved;

            public ushort XonLim;

            public ushort XoffLim;

            public byte ByteSize;

            public byte Parity;

            public byte StopBits;

            public byte XonChar;

            public byte XoffChar;

            public byte ErrorChar;

            public byte EofChar;

            public byte EvtChar;

            public ushort wReserved1;
        }

        private struct COMMTIMEOUTS
        {
            public int ReadIntervalTimeout;

            public int ReadTotalTimeoutMultiplier;

            public int ReadTotalTimeoutConstant;

            public int WriteTotalTimeoutMultiplier;

            public int WriteTotalTimeoutConstant;
        }

        private struct OVERLAPPED
        {
            public int Internal;

            public int InternalHigh;

            public int Offset;

            public int OffsetHigh;

            public int hEvent;
        }

        private const string DLLPATH = "kernel32.dll";

        private const uint GENERIC_READ = 2147483648u;

        private const uint GENERIC_WRITE = 1073741824u;

        private const int OPEN_EXISTING = 3;

        private const int INVALID_HANDLE_VALUE = -1;

        private const int PURGE_RXABORT = 2;

        private const int PURGE_RXCLEAR = 8;

        private const int PURGE_TXABORT = 1;

        private const int PURGE_TXCLEAR = 4;

        public static int PortType = 0;

        public byte NOPARITY;

        public byte ONESTOPBIT;

        public static int SUCCESS_RETURN = 2001;

        public int ERR_NOTAG_RETURN = 2002;

        public int ERR_HANDLE_VALUE = 2003;

        public int ERR_UDATA_LEN = 2004;

        public int ERR_UDATA_ADDRESS = 2006;

        public int ERR_RDATA_LEN = 2007;

        public int ERR_SDATA_FAIL = 2008;

        public int ERR_SCMND_FAIL = 2009;

        public int ERR_READ_FAIL = 2010;

        public int ERR_WRITE_FAIL = 2011;

        public int ERR_LOCK_FAIL = 2012;

        public int ERR_EREASE_FAIL = 2013;

        public int ERR_PORT_OPENED = 2014;

        public int ERR_OPEN_REGSTER = 2015;

        public int ERR_LOAD_INI_LOST = 2016;

        public int ERR_DEV_INIT_FAIL = 2017;

        public int ERR_PORT_OPEN_FAIL = 2018;

        public int ERR_PORT_CLOSE_FAIL = 2019;

        public int ERR_OUT_PARA_LEN = 2020;

        public int ERR_GET_PARA_FAIL = 2021;

        public int ERR_SET_PARA_FAIL = 2022;

        public int ERR_TCP_LOCK_FAIL = 2023;

        public int RDERR_GENERAL_ERR = 2101;

        public int RDERR_PAR_SET_FAILED = 2102;

        public int RDERR_PAR_GET_FAILED = 2103;

        public int RDERR_NO_TAG = 2104;

        public int RDERR_READ_FAILED = 2105;

        public int RDERR_WRITE_FAILED = 2106;

        public int RDERR_LOCK_FAILED = 2107;

        public int RDERR_ERASE_FAILED = 2108;

        public int RDERR_CMD_ERR = 2354;

        public int RDERR_UNDEFINED = 2355;

        public int len;

        public int BaudRate = 115200;

        public byte ByteSize = 8;

        public byte Parity;

        public byte StopBits;

        public int ReadTimeout = 2000;

        public bool Opened;

        private int hComm = -1;

        public Socket? sock;

        //[DllImport("kernel32.dll")]
        //private static extern int CreateFile(string lpFileName, uint dwDesiredAccess, int dwShareMode, int lpSecurityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes, int hTemplateFile);

        //[DllImport("kernel32.dll")]
        //private static extern bool GetCommState(int hFile, ref DCB lpDCB);

        //[DllImport("kernel32.dll")]
        //private static extern bool SetCommState(int hFile, ref DCB lpDCB);

        //[DllImport("kernel32.dll")]
        //private static extern bool GetCommTimeouts(int hFile, ref COMMTIMEOUTS lpCommTimeouts);

        //[DllImport("kernel32.dll")]
        //private static extern bool SetCommTimeouts(int hFile, ref COMMTIMEOUTS lpCommTimeouts);

        //[DllImport("kernel32.dll")]
        //private static extern bool ReadFile(int hFile, byte[] lpBuffer, int nNumberOfBytesToRead, ref int lpNumberOfBytesRead, ref OVERLAPPED lpOverlapped);

        //[DllImport("kernel32.dll")]
        //private static extern bool WriteFile(int hFile, byte[] lpBuffer, int nNumberOfBytesToWrite, ref int lpNumberOfBytesWritten, ref OVERLAPPED lpOverlapped);

        //[DllImport("kernel32.dll", SetLastError = true)]
        //private static extern bool FlushFileBuffers(int hFile);

        //[DllImport("kernel32.dll", SetLastError = true)]
        //private static extern bool PurgeComm(int hFile, uint dwFlags);

        //[DllImport("kernel32.dll")]
        //private static extern bool CloseHandle(int hObject);

        //[DllImport("kernel32.dll")]
        //private static extern uint GetLastError();

        internal void SetDcbFlag(int whichFlag, int setting, DCB dcb)
        {
            setting <<= whichFlag;
            uint num;
            switch (whichFlag)
            {
                case 4:
                case 12:
                    num = 3u;
                    break;
                case 15:
                    num = 131071u;
                    break;
                default:
                    num = 1u;
                    break;
            }
            dcb.flags &= ~(num << whichFlag);
            dcb.flags |= (uint)setting;
        }

        /* public int SetBaudRate(int ReaderAddr, int usBaudRate)
        {
            byte[] array = new byte[6] { 10, 0, 3, 32, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[128];
            DCB lpDCB = default(DCB);
            int baudRate;
            switch (usBaudRate)
            {
                case 0:
                case 9600:
                    baudRate = 9600;
                    array2[4] = 0;
                    break;
                case 1:
                case 19200:
                    baudRate = 19200;
                    array2[4] = 1;
                    break;
                case 2:
                case 38400:
                    baudRate = 38400;
                    array2[4] = 2;
                    break;
                case 3:
                case 57600:
                    baudRate = 57600;
                    array2[4] = 3;
                    break;
                case 4:
                case 115200:
                    baudRate = 115200;
                    array2[4] = 4;
                    break;
                default:
                    return ERR_SET_PARA_FAIL;
            }
            int num = SendAndRcv(array2, 6, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (!GetCommState(hComm, ref lpDCB))
            {
                return ERR_GET_PARA_FAIL;
            }
            lpDCB.BaudRate = baudRate;
            lpDCB.ByteSize = 8;
            lpDCB.Parity = NOPARITY;
            lpDCB.StopBits = ONESTOPBIT;
            if (!SetCommState(hComm, ref lpDCB))
            {
                return ERR_SET_PARA_FAIL;
            }
            return SUCCESS_RETURN;
        } */

        /* public int OpenCommPort(string port, int nBaud)
        {
            DCB lpDCB = default(DCB);
            COMMTIMEOUTS lpCommTimeouts = default(COMMTIMEOUTS);
            hComm = CreateFile(port, 3221225472u, 0, 0, 3, 0, 0);
            if (hComm == -1)
            {
                return ERR_OPEN_REGSTER;
            }
            GetCommTimeouts(hComm, ref lpCommTimeouts);
            lpCommTimeouts.ReadTotalTimeoutConstant = ReadTimeout;
            lpCommTimeouts.ReadTotalTimeoutMultiplier = 2000;
            lpCommTimeouts.WriteTotalTimeoutMultiplier = 2000;
            lpCommTimeouts.WriteTotalTimeoutConstant = 2000;
            SetCommTimeouts(hComm, ref lpCommTimeouts);
            GetCommState(hComm, ref lpDCB);
            lpDCB.DCBlength = Marshal.SizeOf(lpDCB);
            lpDCB.BaudRate = BaudRate;
            lpDCB.flags = 0u;
            lpDCB.ByteSize = ByteSize;
            lpDCB.StopBits = StopBits;
            lpDCB.Parity = Parity;
            if (!SetCommState(hComm, ref lpDCB))
            {
                return ERR_PORT_OPEN_FAIL;
            }
            if (SetBaudRate(255, nBaud) != SUCCESS_RETURN)
            {
                return ERR_PORT_OPEN_FAIL;
            }
            Thread.Sleep(200);
            Opened = true;
            PortType = 0;
            return SUCCESS_RETURN;
        } */

        /* public void CloseCommPort()
        {
            SetBaudRate(255, 115200);
            if (hComm != -1)
            {
                CloseHandle(hComm);
            }
        } */

        /* private int Read(ref byte[] bytData, int NumBytes)
        {
            if (hComm != -1)
            {
                OVERLAPPED lpOverlapped = default(OVERLAPPED);
                int lpNumberOfBytesRead = 0;
                ReadFile(hComm, bytData, NumBytes, ref lpNumberOfBytesRead, ref lpOverlapped);
                return lpNumberOfBytesRead;
            }
            return -1;
        } */

        /* private int Write(byte[] WriteBytes, int intSize)
        {
            if (hComm != -1)
            {
                OVERLAPPED lpOverlapped = default(OVERLAPPED);
                int lpNumberOfBytesWritten = 0;
                WriteFile(hComm, WriteBytes, intSize, ref lpNumberOfBytesWritten, ref lpOverlapped);
                return lpNumberOfBytesWritten;
            }
            return -1;
        } */

        /* private void ClearReceiveBuf()
        {
            if (hComm != -1)
            {
                PurgeComm(hComm, 10u);
            }
        } */

        /* private void ClearSendBuf()
        {
            if (hComm != -1)
            {
                PurgeComm(hComm, 5u);
            }
        } */

        private int BaudRateLower(int BaudRate)
        {
            if (BaudRate <= 19200)
            {

            }
            else if (BaudRate != 38400 && BaudRate != 57600)
            {
                _ = 115200;
            }
            return SUCCESS_RETURN;
        }

        public int TcpConnectReader(string ip, int port)
        {
            IPAddress address = IPAddress.Parse(ip);
            IPEndPoint remoteEP = new IPEndPoint(address, port);
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.ReceiveTimeout = 1000;
            sock.SendTimeout = 1000;
            try
            {
                sock.Connect(remoteEP);
            }
            catch (Exception)
            {
                return 1;
            }
            if (sock.Connected)
            {
                PortType = 1;
                return SUCCESS_RETURN;
            }
            return 1;
        }

        public bool isNetWorkConnect(string ip)
        {
            Ping ping = new Ping();
            PingReply pingReply = ping.Send(ip);
            if (pingReply.Status == IPStatus.Success)
            {
                return true;
            }
            return false;
        }

        public int TcpCloseConnect()
        {
            if (sock != null)
            {
                sock.Close();
            }
            return SUCCESS_RETURN;
        }

        private int TcpSend(byte[] send_buf, int len)
        {
            if (sock == null || !sock.Connected)
            {
                return 1;
            }
           
            try
            {
                return sock.Send(send_buf, len, SocketFlags.None);
            }
            catch (Exception ex)
            {
                _ = ex.Message;
                return 0;
            }
        }

        private int TcpReceive(ref byte[] rcv_buf, int len)
        {
            if (sock == null || !sock.Connected)
            {
                return 1;
            }
            try
            {
                return sock.Receive(rcv_buf, len, SocketFlags.None);
            }
            catch (Exception ex)
            {
                _ = ex.Message;
                return 1;
            }
        }

        private int SendAndRcv(byte[] send_buf, int intSize, ref int len, ref byte[] rcv_buf)
        {
            len = 0;
            byte[] bytData = new byte[3];
            byte b = 0;
            for (int i = 0; i < intSize - 1; i++)
            {
                b = (byte)(b + send_buf[i]);
            }
            b = (byte)(~b + 1);
            send_buf[intSize - 1] = b;
            if (PortType == 0)
            {
                /* ClearSendBuf();
                ClearReceiveBuf();
                if (Write(send_buf, intSize) != intSize)
                {
                    return ERR_SET_PARA_FAIL;
                }
                if (Read(ref bytData, 3) == 3)
                {
                    if (bytData[0] != 11)
                    {
                        return ERR_RDATA_LEN;
                    }
                    len = bytData[2];
                    if (Read(ref rcv_buf, len) != len)
                    {
                        return ERR_RDATA_LEN;
                    }
                    return SUCCESS_RETURN;
                } */
                return ERR_NOTAG_RETURN;
            }
            if (TcpSend(send_buf, intSize) != intSize)
            {
                return ERR_SCMND_FAIL;
            }
            if (TcpReceive(ref bytData, 3) == 3)
            {
                if (bytData[0] != 11)
                {
                    return ERR_RDATA_LEN;
                }
                len = bytData[2];
                if (TcpReceive(ref rcv_buf, len) != len)
                {
                    return ERR_RDATA_LEN;
                }
                return SUCCESS_RETURN;
            }
            return ERR_NOTAG_RETURN;
        }

        public int QueryIDCount(int ReaderAddr, ref byte tagCount)
        {
            tagCount = 0;
            byte[] array = new byte[6] { 10, 0, 3, 67, 8, 172 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[128];
            int num = SendAndRcv(send_buf, 5, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                tagCount = (byte)(rcv_buf[1] * 10 + rcv_buf[2]);
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        private int GetTagEPC(int ReaderAddr, ref byte[,] tag_data, byte tag_cnt)
        {
            byte b = 0;
            byte b2 = 0;
            int num = 0;
            byte[] array = new byte[6] { 10, 0, 3, 64, 8, 172 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[256];
            int num2;
            do
            {
                num++;
                num2 = SendAndRcv(send_buf, 6, ref len, ref rcv_buf);
                if (num2 == SUCCESS_RETURN && rcv_buf[0] == 0)
                {
                    b = rcv_buf[1];
                    for (int i = 0; i < b; i++)
                    {
                        for (int j = 0; j < 14; j++)
                        {
                            tag_data[b2, j] = rcv_buf[i * 14 + j + 2];
                        }
                        b2 = (byte)(b2 + 1);
                    }
                }
                if (num > (int)tag_cnt / 8 + 3)
                {
                    num2 = ERR_UDATA_LEN;
                    break;
                }
            }
            while (b2 < tag_cnt);
            return num2;
        }

        public int GetTagData(int ReaderAddr, ref byte[,] tag_data, int tag_cnt, ref int getCount)
        {
            int num = 0;
            int num2 = 0;
            byte[] array = new byte[6] { 10, 0, 3, 65, 16, 163 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[2048];
            int num3;
            do
            {
                num2++;
                num3 = SendAndRcv(send_buf, 6, ref len, ref rcv_buf);
                if (num3 == SUCCESS_RETURN && rcv_buf[0] == 0)
                {
                    try
                    {
                        getCount = rcv_buf[1];
                        if (getCount <= 0)
                        {
                            break;
                        }
                        int num4 = (len - 3) / getCount;
                        for (int i = 0; i < getCount; i++)
                        {
                            for (int j = 0; j < num4; j++)
                            {
                                tag_data[num, j] = rcv_buf[i * num4 + j + 2];
                            }
                            num++;
                        }
                        continue;
                    }
                    catch
                    {
                        num = 0;
                    }
                    break;
                }
                if (num3 == ERR_SCMND_FAIL)
                {
                    break;
                }
            }
            while (num < tag_cnt);
            getCount = num;
            return num3;
        }

        public int ResetReader(int ReaderAddr)
        {
            byte[] array = new byte[7] { 10, 0, 2, 33, 0, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[128];
            int num = SendAndRcv(send_buf, 5, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                BaudRateLower(4);
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int ResetParameter(int ReaderAddr)
        {
            byte[] array = new byte[7] { 10, 0, 3, 47, 5, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[128];
            int num = SendAndRcv(send_buf, 6, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                BaudRateLower(4);
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int GetTcpParameter(int ReaderAddr, ref string strIP, ref string strMark, ref string strGate, ref int nTcpPort)
        {
            byte[] array = new byte[7] { 10, 0, 2, 43, 0, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[128];
            int num = SendAndRcv(send_buf, 5, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] != 0)
            {
                return 2100 + rcv_buf[0];
            }
            for (int i = 0; i < 4; i++)
            {
                if (i < 3)
                {
                    strIP = strIP + rcv_buf[1 + i] + ".";
                }
                else
                {
                    strIP += rcv_buf[1 + i];
                }
            }
            for (int j = 0; j < 4; j++)
            {
                if (j < 3)
                {
                    strMark = strMark + rcv_buf[5 + j] + ".";
                }
                else
                {
                    strMark += rcv_buf[5 + j];
                }
            }
            for (int k = 0; k < 4; k++)
            {
                if (k < 3)
                {
                    strGate = strGate + rcv_buf[9 + k] + ".";
                }
                else
                {
                    strGate += rcv_buf[9 + k];
                }
            }
            nTcpPort = (rcv_buf[14] << 8) | rcv_buf[13];
            return num;
        }

        public int SetTcpParameter(int ReaderAddr, string strIP, string strMark, string strGate, int nTcpPort)
        {
            byte[] array = new byte[20]
            {
            10, 0, 16, 44, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[128];
            strIP = strIP.Trim();
            strMark = strMark.Trim();
            strGate = strGate.Trim();
            string[] array3 = new string[4];
            string[] array4 = new string[4];
            string[] array5 = new string[4];
            array3 = strIP.Split('.');
            array4 = strMark.Split('.');
            array5 = strGate.Split('.');
            for (int i = 0; i < 4; i++)
            {
                array2[4 + i] = byte.Parse(array3[i]);
                array2[8 + i] = byte.Parse(array4[i]);
                array2[12 + i] = byte.Parse(array5[i]);
            }
            array2[16] = (byte)((uint)nTcpPort & 0xFFu);
            array2[17] = (byte)((uint)(nTcpPort >> 8) & 0xFFu);
            int num = SendAndRcv(array2, 19, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }
 
        public int ClearIdBuf(int ReaderAddr)
        {
            byte[] array = new byte[5] { 10, 0, 2, 68, 177 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[6];
            int num = SendAndRcv(send_buf, 5, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int GetFirmwareVersion(int ReaderAddr, ref byte v1, ref byte v2)
        {
            byte[] array = new byte[5] { 10, 0, 2, 34, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[8];
            int num = SendAndRcv(send_buf, 5, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                v1 = rcv_buf[1];
                v2 = rcv_buf[2];
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int SetRf(int ReaderAddr, int power1, int power2, int power3, int power4)
        {
            byte[] array = new byte[9] { 10, 0, 6, 37, 0, 0, 0, 0, 0 };
            array[1] = (byte)ReaderAddr;
            array[4] = (byte)power1;
            array[5] = (byte)power2;
            array[6] = (byte)power3;
            array[7] = (byte)power4;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[6];
            int num = SendAndRcv(send_buf, 9, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int GetRf(int ReaderAddr, ref int[] power)
        {
            byte[] array = new byte[5] { 10, 0, 2, 38, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[16];
            int num = SendAndRcv(send_buf, 5, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    power[i] = rcv_buf[i + 1];
                }
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int SetAnt(int ReaderAddr, byte ant)
        {
            byte[] array = new byte[6] { 10, 0, 3, 41, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[6];
            array2[4] = ant;
            int num = SendAndRcv(array2, 6, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int GetAnt(int ReaderAddr, ref byte workAnt, ref byte antState)
        {
            byte[] array = new byte[5] { 10, 0, 2, 42, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[8];
            int num = SendAndRcv(send_buf, 5, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                workAnt = rcv_buf[1];
                antState = rcv_buf[2];
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int SetFrequency(int ReaderAddr, int freqNum, int[] points)
        {
            byte[] array;
            if (freqNum != 0)
            {
                array = new byte[freqNum + 6];
                array[0] = 10;
                array[1] = (byte)ReaderAddr;
                array[2] = (byte)(freqNum + 3);
                array[3] = 39;
                array[4] = (byte)freqNum;
                for (int i = 0; i < freqNum; i++)
                {
                    array[i + 5] = (byte)points[i];
                }
            }
            else
            {
                byte[] array2 = new byte[7] { 10, 0, 4, 39, 0, 0, 0 };
                array2[1] = (byte)ReaderAddr;
                array2[5] = (byte)points[0];
                array = array2;
                freqNum = 1;
            }
            byte[] rcv_buf = new byte[8];
            int num = SendAndRcv(array, (byte)(freqNum + 6), ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int GetFrequency(int ReaderAddr, ref int freqNum, ref int[] points)
        {
            byte[] array = new byte[5] { 10, 0, 2, 40, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[256];
            int num = SendAndRcv(send_buf, 5, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] != 0)
            {
                return 2100 + rcv_buf[0];
            }
            freqNum = rcv_buf[1];
            for (int i = 0; i < len - 3; i++)
            {
                points[i] = rcv_buf[i + 2];
            }
            return num;
        }

        public int GetFastTagMode(int ReaderAddr, ref int mode)
        {
            byte[] array = new byte[5] { 10, 0, 2, 22, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[8];
            int num = SendAndRcv(send_buf, 5, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] != 0)
            {
                return 2100 + rcv_buf[0];
            }
            mode = rcv_buf[1];
            return num;
        }

        public int SetFastTagMode(int ReaderAddr, int mode)
        {
            byte[] array = new byte[6] { 10, 0, 3, 21, 0, 0 };
            array[1] = (byte)ReaderAddr;
            array[4] = (byte)mode;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[6];
            int num = SendAndRcv(send_buf, 6, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int SetTestMode(int ReaderAddr, int mode)
        {
            byte[] array = new byte[6] { 10, 0, 3, 47, 0, 0 };
            array[1] = (byte)ReaderAddr;
            array[4] = (byte)mode;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[6];
            int num = SendAndRcv(send_buf, 6, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int IsoMultiTagIdentify(int ReaderAddr, ref byte[,] tag_buf, ref int tag_cnt, ref int getCount)
        {
            getCount = 0;
            byte[] array = new byte[5] { 10, 0, 2, 96, 149 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[64];
            int num = SendAndRcv(send_buf, 5, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                tag_cnt = rcv_buf[1];
                if (tag_cnt > 0)
                {
                    GetTagData(ReaderAddr, ref tag_buf, tag_cnt, ref getCount);
                }
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int IsoMultiTagRead(int ReaderAddr, int startAddr, ref byte[,] tag_buf, ref int tag_cnt, ref int getCount)
        {
            getCount = 0;
            byte[] array = new byte[6] { 10, 0, 3, 97, 0, 149 };
            array[1] = (byte)ReaderAddr;
            array[4] = (byte)startAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[64];
            int num = SendAndRcv(send_buf, 6, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                tag_cnt = rcv_buf[1];
                if (tag_cnt > 0)
                {
                    GetTagData(ReaderAddr, ref tag_buf, tag_cnt, ref getCount);
                }
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int IsoRead(int ReaderAddr, byte addr, ref byte[] value)
        {
            byte[] array = new byte[7] { 10, 0, 3, 104, 0, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[16];
            array2[4] = addr;
            int num = SendAndRcv(array2, 6, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] != 0)
            {
                return 2100 + rcv_buf[0];
            }
            for (int i = 0; i < 8; i++)
            {
                value[i] = rcv_buf[i + 2];
            }
            return num;
        }

        public int IsoWrite(int ReaderAddr, byte addr, byte value)
        {
            byte[] array = new byte[7] { 10, 0, 4, 98, 0, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[6];
            array2[4] = addr;
            array2[5] = value;
            int num = SendAndRcv(array2, 7, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] != 0)
            {
                return 2100 + rcv_buf[0];
            }
            return num;
        }

        public int IsoLock(int ReaderAddr, byte addr)
        {
            byte[] array = new byte[7] { 10, 0, 3, 101, 0, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[6];
            array2[4] = addr;
            int num = SendAndRcv(array2, 6, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] != 0)
            {
                return 2100 + rcv_buf[0];
            }
            return num;
        }

        public int IsoLockWithID(int ReaderAddr, byte[] byTagID, byte byAddress)
        {
            byte[] array = new byte[14]
            {
            10, 0, 11, 105, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0
            };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            for (int i = 0; i < 8; i++)
            {
                array2[4 + i] = byTagID[i];
            }
            array2[12] = byAddress;
            byte[] rcv_buf = new byte[256];
            int num = SendAndRcv(array2, 14, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] != 0)
            {
                return 2100 + rcv_buf[0];
            }
            return num;
        }

        public int IsoReadWithID(int ReaderAddr, byte[] byTagID, byte byAddress, ref byte[] byLabelData, ref byte byAntenna)
        {
            byte[] array = new byte[14]
            {
            10, 0, 11, 99, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0
            };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            for (int i = 0; i < 8; i++)
            {
                array2[4 + i] = byTagID[i];
            }
            array2[12] = byAddress;
            byte[] rcv_buf = new byte[256];
            int num = SendAndRcv(array2, 14, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] != 0)
            {
                return 2100 + rcv_buf[0];
            }
            byAntenna = rcv_buf[4];
            for (int j = 0; j < 8; j++)
            {
                byLabelData[j] = rcv_buf[2 + j];
            }
            byAntenna = rcv_buf[1];
            return num;
        }

        public int IsoWriteWithID(int ReaderAddr, byte[] byTagID, byte byAddress, byte byValue)
        {
            byte[] array = new byte[15]
            {
            10, 0, 12, 100, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0
            };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            for (int i = 0; i < 8; i++)
            {
                array2[4 + i] = byTagID[i];
            }
            array2[12] = byAddress;
            array2[13] = byValue;
            byte[] rcv_buf = new byte[256];
            int num = SendAndRcv(array2, 15, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] != 0)
            {
                return 2100 + rcv_buf[0];
            }
            return num;
        }

        public int IsoQueryLock(int ReaderAddr, byte addr, ref byte lstate)
        {
            byte[] array = new byte[6] { 10, 0, 3, 102, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[6];
            array2[4] = addr;
            lstate = 0;
            int num = SendAndRcv(array2, 6, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] != 0)
            {
                return 2100 + rcv_buf[0];
            }
            lstate = rcv_buf[1];
            return num;
        }

        public int IsoQueryLockWithUID(int ReaderAddr, byte[] byTagID, byte addr, ref byte lstate)
        {
            byte[] array = new byte[14]
            {
            10, 0, 11, 106, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0
            };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[6];
            for (int i = 0; i < 8; i++)
            {
                array2[4 + i] = byTagID[i];
            }
            array2[12] = addr;
            lstate = 0;
            int num = SendAndRcv(array2, 14, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] != 0)
            {
                return 2100 + rcv_buf[0];
            }
            lstate = rcv_buf[1];
            return num;
        }

        public int EpcMultiTagIdentify(int ReaderAddr, ref byte[,] tag_buf, ref int tag_cnt, ref byte tag_flag)
        {
            int getCount = 0;
            byte[] array = new byte[5] { 10, 0, 2, 128, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[128];
            int num = SendAndRcv(send_buf, 5, ref len, ref rcv_buf);
            if (num == SUCCESS_RETURN)
            {
                if (len == 3)
                {
                    tag_cnt = rcv_buf[1];
                }
                else
                {
                    tag_cnt = rcv_buf[1] * 256 + rcv_buf[2];
                }
                tag_flag = rcv_buf[0];
                if (tag_cnt > 0)
                {
                    GetTagData(255, ref tag_buf, tag_cnt, ref getCount);
                }
            }
            return num;
        }

        public int EpcLockTag(int ReaderAddr, byte MemBank)
        {
            byte[] array = new byte[5] { 10, 0, 2, 68, 177 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[6];
            int num = SendAndRcv(send_buf, 6, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] != 0)
            {
                return 2100 + rcv_buf[0];
            }
            return num;
        }

        public int EpcInitEpc(int ReaderAddr, byte bit_cnt)
        {
            byte[] array = new byte[6] { 10, 0, 3, 132, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[6];
            array2[4] = bit_cnt;
            int num = SendAndRcv(array2, 6, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] != 0)
            {
                return 2100 + rcv_buf[0];
            }
            return num;
        }

        public int EpcRead(int ReaderAddr, byte membank, byte wordptr, byte wordcnt, ref byte[] value)
        {
            byte[] array = new byte[10] { 10, 0, 5, 133, 0, 0, 0, 0, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[32];
            array2[4] = membank;
            array2[5] = wordptr;
            array2[6] = wordcnt;
            int num = SendAndRcv(array2, 8, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                for (int i = 0; i < wordcnt * 2; i++)
                {
                    value[i] = rcv_buf[i + 2];
                }
                return SUCCESS_RETURN;
            }
            return ERR_READ_FAIL;
        }

        public int EpcWrite(int ReaderAddr, byte membank, byte wordptr, ushort value)
        {
            byte[] array = new byte[10] { 10, 0, 6, 134, 0, 0, 0, 0, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[32];
            array2[4] = membank;
            array2[5] = wordptr;
            array2[6] = (byte)(value >> 8);
            array2[7] = (byte)value;
            int num = SendAndRcv(array2, 9, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return ERR_WRITE_FAIL;
        }

        public int Gen2MultiTagWrite(int ReaderAddr, int membank, int wordaddr, int wordLen, string strValue, ref int writeCount)
        {
            byte[] array = new byte[100];
            byte[] rcv_buf = new byte[32];
            array[0] = 10;
            array[1] = (byte)ReaderAddr;
            array[2] = (byte)(wordLen * 2 + 5);
            array[3] = 133;
            array[4] = (byte)membank;
            array[5] = (byte)wordaddr;
            array[6] = (byte)wordLen;
            for (int i = 0; i < wordLen * 2; i++)
            {
                array[i + 7] = Convert.ToByte(strValue.Substring(i * 2, 2), 16);
            }
            int num = SendAndRcv(array, wordLen * 2 + 8, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                writeCount = Convert.ToInt32(rcv_buf[1].ToString(), 10);
                return SUCCESS_RETURN;
            }
            return ERR_WRITE_FAIL;
        }

        public int Gen2SecLock(int ReaderAddr, uint AccPassWord, byte Membank, byte Level)
        {
            byte[] array = new byte[20]
            {
            10, 0, 8, 138, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[256];
            array2[4] = (byte)((AccPassWord >> 24) & 0xFFu);
            array2[5] = (byte)((AccPassWord >> 16) & 0xFFu);
            array2[6] = (byte)((AccPassWord >> 8) & 0xFFu);
            array2[7] = (byte)AccPassWord;
            array2[8] = Membank;
            array2[9] = Level;
            int num = SendAndRcv(array2, 11, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return RDERR_LOCK_FAILED;
        }

        public int Gen2SecWrite(int ReaderAddr, uint AccPassWord, byte Membank, byte WordAddr, ushort Value)
        {
            byte[] array = new byte[20]
            {
            10, 0, 10, 137, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[256];
            array2[4] = (byte)((AccPassWord >> 24) & 0xFFu);
            array2[5] = (byte)((AccPassWord >> 16) & 0xFFu);
            array2[6] = (byte)((AccPassWord >> 8) & 0xFFu);
            array2[7] = (byte)AccPassWord;
            array2[8] = Membank;
            array2[9] = WordAddr;
            array2[10] = (byte)((uint)(Value >> 8) & 0xFFu);
            array2[11] = (byte)(Value & 0xFFu);
            int num = SendAndRcv(array2, 13, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] != 0)
            {
                return 2100 + rcv_buf[0];
            }
            return num;
        }

        public int Gen2SecRead(int ReaderAddr, uint AccPassWord, byte Membank, byte WordAddr, byte WordCnt, ref byte[] value)
        {
            byte[] array = new byte[20]
            {
            10, 0, 9, 136, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[256];
            array2[4] = (byte)((AccPassWord >> 24) & 0xFFu);
            array2[5] = (byte)((AccPassWord >> 16) & 0xFFu);
            array2[6] = (byte)((AccPassWord >> 8) & 0xFFu);
            array2[7] = (byte)AccPassWord;
            array2[8] = Membank;
            array2[9] = WordAddr;
            array2[10] = WordCnt;
            int num = SendAndRcv(array2, 12, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                for (int i = 0; i < WordCnt * 2; i++)
                {
                    value[i] = rcv_buf[i + 2];
                }
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        /* public int Gen2SelectConfig(int ReaderAddr, int Action, int Membank, int wordAddr, int wordCnt, string[] words)
        {
            byte[] array = new byte[26]
            {
            10, 0, 9, 143, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0
            };
            array[1] = (byte)ReaderAddr;
            array[4] = (byte)Action;
            array[5] = (byte)Membank;
            array[8] = (byte)(wordCnt * 16);
            byte[] array2 = array;
            byte[] rcv_buf = new byte[256];
            array2[2] = (byte)(7 + 2 * wordCnt);
            array2[6] = (byte)((uint)(16 * wordAddr >> 8) & 0xFFu);
            array2[7] = (byte)(16 * wordAddr);
            for (int i = 0; i < wordCnt; i++)
            {
                array2[9 + i * 2] = Convert.ToByte(words[i].Substring(0, 2), 16);
                array2[10 + i * 2] = Convert.ToByte(words[i].Substring(2, 2), 16);
            }
            int num = SendAndRcv(array2, 10 + 2 * wordCnt, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                ClearIdBuf(255);
                ClearReceiveBuf();
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        } */

        public int Gen2KillTag(int ReaderAddr, uint AccPassWord)
        {
            byte[] array = new byte[20]
            {
            10, 0, 6, 131, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[256];
            array2[4] = (byte)((AccPassWord >> 24) & 0xFFu);
            array2[5] = (byte)((AccPassWord >> 16) & 0xFFu);
            array2[6] = (byte)((AccPassWord >> 8) & 0xFFu);
            array2[7] = (byte)AccPassWord;
            int num = SendAndRcv(array2, 9, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] != 0)
            {
                return 2100 + rcv_buf[0];
            }
            return num;
        }

        /* public void Gen2MultiTagInventory(int ReaderAddr)
        {
            byte[] array = new byte[6] { 10, 0, 3, 128, 1, 115 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            if (PortType == 0)
            {
                ClearSendBuf();
                ClearReceiveBuf();
                Write(array2, 6);
            }
            else
            {
                TcpSend(array2, 6);
            }
            Thread.Sleep(6);
        } */

        /* public void Gen2MultiTagInventoryStop(int ReaderAddr)
        {
            byte[] array = new byte[6] { 10, 0, 2, 129, 116, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            if (PortType == 0)
            {
                ClearSendBuf();
                ClearReceiveBuf();
                Write(array2, 5);
            }
            else
            {
                TcpSend(array2, 5);
            }
            Thread.Sleep(6);
        } */

        public int Gen2MultiTagRead(int ReaderAddr, byte MembankMask, byte ResWordPtr, byte ResWordCnt, byte EpcWordPtr, byte EpcWordCnt, byte TidWordPtr, byte TidWordCnt, byte UserWordPtr, byte UserWordCnt, ref int ReadCnt)
        {
            byte[] send_buf = new byte[20]
            {
            10,
            (byte)ReaderAddr,
            11,
            132,
            MembankMask,
            ResWordPtr,
            ResWordCnt,
            EpcWordPtr,
            EpcWordCnt,
            TidWordPtr,
            TidWordCnt,
            UserWordPtr,
            UserWordCnt,
            0,
            0,
            0,
            0,
            0,
            0,
            0
            };
            byte[] rcv_buf = new byte[256];
            int num = SendAndRcv(send_buf, 14, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                ReadCnt = int.Parse(rcv_buf[1].ToString()) * 256 + int.Parse(rcv_buf[2].ToString());
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int SetOutPort(int ReaderAddr, byte port_num, byte level)
        {
            byte[] array = new byte[7] { 10, 0, 4, 45, 0, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[128];
            array2[4] = port_num;
            array2[5] = level;
            int num = SendAndRcv(array2, 7, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int BuzzerLEDON(int ReaderAddr)
        {
            byte[] array = new byte[7] { 10, 0, 4, 35, 27, 3, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[8];
            int num = SendAndRcv(send_buf, 7, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int BuzzerLEDOFF(int ReaderAddr)
        {
            byte[] array = new byte[7] { 10, 0, 4, 35, 27, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[8];
            int num = SendAndRcv(send_buf, 7, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int GetBuzzerLED(int ReaderAddr, ref byte state)
        {
            byte[] array = new byte[6] { 10, 0, 3, 36, 27, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] send_buf = array;
            byte[] rcv_buf = new byte[8];
            int num = SendAndRcv(send_buf, 6, ref len, ref rcv_buf);
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                state = rcv_buf[1];
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int SetMacAddress(int ReaderAddr, string[] strMacAddr)
        {
            int num = 1;
            byte[] array = new byte[7] { 10, 0, 4, 35, 0, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[8];
            int num2 = 176;
            for (int i = 0; i < 6; i++)
            {
                array2[4] = (byte)(num2 + i);
                array2[5] = Convert.ToByte(strMacAddr[i], 16);
                num = SendAndRcv(array2, 7, ref len, ref rcv_buf);
                if (num != SUCCESS_RETURN)
                {
                    break;
                }
            }
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int GetMacAddress(int ReaderAddr, ref string strMacAddr)
        {
            int num = 1;
            byte[] array = new byte[6] { 10, 0, 3, 36, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[8];
            int num2 = 176;
            for (int i = 0; i < 6; i++)
            {
                array2[4] = (byte)(num2 + i);
                num = SendAndRcv(array2, 6, ref len, ref rcv_buf);
                if (num != SUCCESS_RETURN)
                {
                    break;
                }
                strMacAddr += $"{rcv_buf[1]:X2} ";
            }
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int SetSerialNo(int ReaderAddr, string[] strSerialNo)
        {
            int num = 2;
            byte[] array = new byte[7] { 10, 0, 4, 35, 0, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[8];
            int num2 = 16;
            for (int i = 0; i < 6; i++)
            {
                array2[4] = (byte)(num2 + i);
                array2[5] = Convert.ToByte(strSerialNo[i], 10);
                num = SendAndRcv(array2, 7, ref len, ref rcv_buf);
                if (num != SUCCESS_RETURN)
                {
                    break;
                }
            }
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }

        public int GetSerialNo(int ReaderAddr, ref string strSerialNo)
        {
            int num = 2;
            byte[] array = new byte[6] { 10, 0, 3, 36, 0, 0 };
            array[1] = (byte)ReaderAddr;
            byte[] array2 = array;
            byte[] rcv_buf = new byte[8];
            int num2 = 16;
            for (int i = 0; i < 6; i++)
            {
                array2[4] = (byte)(num2 + i);
                num = SendAndRcv(array2, 6, ref len, ref rcv_buf);
                if (num != SUCCESS_RETURN)
                {
                    break;
                }
                strSerialNo += $"{rcv_buf[1]:D2} ";
            }
            if (num != SUCCESS_RETURN)
            {
                return num;
            }
            if (rcv_buf[0] == 0)
            {
                return SUCCESS_RETURN;
            }
            return 2100 + rcv_buf[0];
        }
    }


}
