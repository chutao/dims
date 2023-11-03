using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DIMS.Models
{
    public class ProductionDataModel
    {
        /// <summary>
        /// 编号
        /// </summary>
        [Display(Name = "编号")]
        public ulong Id { get; set; }

        /// <summary>
        /// 托盘码
        /// </summary>
        [Display(Name = "托盘码")]
        public string? TrayCode { get; set; }

        /// <summary>
        /// 产品码
        /// </summary>
        [Display(Name = "产品码")]
        public string? ProductCode { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [Display(Name = "时间戳")]
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// 设备过站状态
        /// </summary>
        [Display(Name = "状态码")]
        public ulong State { get; set; }
    }
}
