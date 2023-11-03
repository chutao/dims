using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;

namespace DIMS.Models
{
    [Serializable]
    internal class Settings : IEnableLogger
    {
        private static Settings _Instance = new Settings();
        private static readonly string _SettingFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.toml");

        public static Settings Instance => _Instance;


        #region Application Settings
        [Serializable]
        internal class ApplicationSettings
        {
            /// <summary>
            /// 数据库连接字
            /// </summary>
            [DataMember(Name = "mysql")]
            public string? MysqlConnectionString { get; set; } = "server=127.0.0.1;uid=root;pwd=administrator;database=panasonic";

            /// <summary>
            /// 线体PLC地址
            /// </summary>
            [DataMember(Name = "plc_address")]
            public string? LinePlcAddress { get; set; } = "192.168.100.2";

            /// <summary>
            /// 线体PLC端口
            /// </summary>
            [DataMember(Name = "plc_port")]
            public int LinePlcPort { get; set; } = 3000;

            /// <summary>
            /// 线体工段数量
            /// </summary>
            [DataMember(Name = "workstation")]
            public int WorkstationCount { get; set; } = 10;

            [DataMember(Name = "rpc_port")]
            public int JsonRpcPort { get; set; } = 12000;

            /// <summary>
            /// 托盘扫码枪IP地址
            /// </summary>
            [DataMember(Name = "tray_scanner_address")]
            public string? TrayScannerIpAddress { get; set; } = "192.168.100.120";

            /// <summary>
            /// 托盘扫码枪端口号
            /// </summary>
            [DataMember(Name = "tray_scanner_port")]
            public int TrayScannerPort { get; set; } = 4096;

            /// <summary>
            /// RFID通讯地址
            /// </summary>
            [DataMember(Name = "rfid_address")]
            public string? RfidAddress { get; set; } = "192.168.100.121";

            /// <summary>
            /// RFID通讯端口号
            /// </summary>
            [DataMember(Name = "rfid_port")]
            public int RfidPort { get; set; } = 3000;
        }

        [DataMember(Name = "application")]
        public ApplicationSettings Application { get; set; } = new ApplicationSettings();
        #endregion

        #region RFID Settings
        [Serializable]
        public class RfidSettings
        {
            /// <summary>
            /// RFID天线功率(0 - 30)dbm
            /// </summary>
            [DataMember(Name = "power")]
            public int Power { get; set; } = 30;

            /// <summary>
            /// 启用天线1
            /// </summary>
            [DataMember(Name = "enable_antenna_1")]
            public bool EnableAntenna1 { get; set; } = true;

            /// <summary>
            /// 启用天线2
            /// </summary>
            [DataMember(Name = "enable_antenna_2")]
            public bool EnableAntenna2 { get; set; } = true;

            /// <summary>
            /// 启用天线3
            /// </summary>
            [DataMember(Name = "enable_antenna_3")]
            public bool EnableAntenna3 { get; set; } = true;

            /// <summary>
            /// 启用天线4
            /// </summary>
            [DataMember(Name = "enable_antenna_4")]
            public bool EnableAntenna4 { get; set; } = true;
        }

        [DataMember(Name = "rfid")]
        public RfidSettings Rfid { get; set; } = new RfidSettings();
        #endregion

        public bool Save()
        {
            bool result = false;

            try
            {
                string content = Toml.FromModel(this);
                using StreamWriter sw = new StreamWriter(_SettingFileName);
                sw.Write(content);
                sw.Close();
                result = true;
            }
            catch (Exception ex)
            {
                this.Log().Debug(ex, "Save settings failed");
            }

            return result;
        }

        public bool Load()
        {
            bool result = false;

            try
            {
                if (File.Exists(_SettingFileName))
                {
                    string content = "";
                    using StreamReader sr = new StreamReader(_SettingFileName);
                    content = sr.ReadToEnd();
                    sr.Close();

                    var instance = Toml.ToModel<Settings>(content);
                    if (instance != null)
                    {   
                        MemoryStream ms = new MemoryStream();
                        BinaryFormatter formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                        formatter.Serialize(ms, instance);
                        ms.Seek(0, SeekOrigin.Begin);
                        _Instance = (Settings)formatter.Deserialize(ms);
#pragma warning restore SYSLIB0011 // Type or member is obsolete

                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                this.Log().Debug(ex, "Load settings failed");
            }

            return result;
        }
    }
}
