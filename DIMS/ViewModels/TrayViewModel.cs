using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMS.ViewModels
{
    public class TrayViewModel : ReactiveObject
    {
        private uint _Index;
        /// <summary>
        /// 流水号
        /// </summary>
        public uint Index
        {
            get => _Index;
            set => this.RaiseAndSetIfChanged(ref _Index, value);
        }

        private string? _Rfid;
        /// <summary>
        /// 产品RFID
        /// </summary>
        public string? Rfid
        {
            get => _Rfid;
            set => this.RaiseAndSetIfChanged(ref _Rfid, value);
        }

        private string? _TrayCode;
        /// <summary>
        /// 托盘条码
        /// </summary>
        public string? TrayCode
        {
            get => _TrayCode;
            set => this.RaiseAndSetIfChanged(ref _TrayCode, value);
        }

        private bool _IsExist;
        /// <summary>
        /// 是否已存在记录
        /// </summary>
        public bool IsExist
        {
            get => _IsExist;
            set => this.RaiseAndSetIfChanged(ref _IsExist, value);
        }

        private string? _Model;
        /// <summary>
        /// 产品种类
        /// </summary>
        public string? Model
        {
            get => _Model;
            set => this.RaiseAndSetIfChanged(ref _Model, value);
        }

        private int _ModelIndex;
        /// <summary>
        /// 产品种类编号
        /// </summary>
        public int ModelIndex
        {
            get => _ModelIndex;
            set => this.RaiseAndSetIfChanged(ref _ModelIndex, value);
        }

        private string? _Product;
        /// <summary>
        /// 产品名称
        /// </summary>
        public string? Product
        {
            get => _Product;
            set => this.RaiseAndSetIfChanged(ref _Product, value);
        }

        private string? _CurrentWorkstation;
        /// <summary>
        /// 当前工位
        /// </summary>
        public string? CurrentWorkstation
        {
            get => _CurrentWorkstation;
            set => this.RaiseAndSetIfChanged(ref _CurrentWorkstation, value);
        }

        private ulong _StatusWord;
        /// <summary>
        /// 过站信息
        /// </summary>
        public ulong StatusWord
        {
            get => _StatusWord;
            set => this.RaiseAndSetIfChanged(ref _StatusWord, value);
        }
    }
}
