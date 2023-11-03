using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMS.Models
{
    public class DeviceDataModel
    {
        /// <summary>
        /// 设备编号
        /// </summary>
        [Display(Name = "ID")]
        public int Id { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        [Display(Name = "设备名称")]
        public string? DeviceName { get; set; }

        /// <summary>
        /// 设备描述
        /// </summary>
        [Display(Name = "设备描述")]
        public string? Description { get; set; }
    }
}
