using System.Threading;
using System.Threading.Tasks;

namespace DIMS.Core
{
    /// <summary>
    /// 信息查询服务返回得响应对象
    /// </summary>
    public class Response
    {
        /// <summary>
        /// 是否成功响应
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 产品是否已存在记录
        /// </summary>
        public bool IsExist { get; set; }

        /// <summary>
        /// 产品条码
        /// </summary>
        public string ProductCode { get; set; }

        /// <summary>
        /// 托盘条码
        /// </summary>
        public string TrayCode { get; set; }

        /// <summary>
        /// 产品种类
        /// </summary>
        public string ProductModel { get; set; }

        /// <summary>
        /// 产品种类编号
        /// </summary>
        public int ProductModelIndex { get; set; }

        /// <summary>
        /// 返回得消息
        /// </summary>
        public string Message { get; set; }
    }

    public interface IDIMS
    {
        Task<Response> QueryAsync(string tray, CancellationToken cancellation = default);

        Task<bool> UpdateAsync(string tray, int state, CancellationToken cancellation = default);
    }
}