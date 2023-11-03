using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMS.Models
{
    public class ProductDataModel
    {
        /// <summary>
        /// 机型名称
        /// </summary>
        [Display(Name = "机型名称")]
        public string? ModelName { get; set; }

        /// <summary>
        /// 机型代码
        /// </summary>
        [Display(Name = "机型代码")]
        public string? Model { get; set; }

        /// <summary>
        /// 产品名称
        /// </summary>
        [Display(Name = "产品名称")]
        public string? ProductName { get; set; }

        /// <summary>
        /// POS代码, 主键
        /// </summary>
        [Display(Name = "POS代码")]
        public string? POSCode { get; set; }

        /// <summary>
        /// 自动化生产分类序号
        /// </summary>
        [Display(Name = "类别代码")]
        public int Category { get; set; }

        /// <summary>
        /// 产品条码长度
        /// </summary>
        [Display(Name = "条码长度")]
        public int CodeLength { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "备注")]
        public string? Description { get; set; }
    }
}
