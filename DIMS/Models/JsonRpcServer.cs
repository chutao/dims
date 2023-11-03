using DIMS.Core;
using DIMS.Helpers;
using DIMS.ViewModels;
using Splat;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMS.Models
{
    internal class JsonRpcServer : IEnableLogger
    {
        private readonly IDataProvider _provider;

        public JsonRpcServer(IDataProvider provider)
        { 
            _provider = provider;
        }

        public Response? Query(string? tray)
        {
            this.Log().Debug($"JSON-RPC call method 'Query' with parameter {tray}");

            Response? response = null;
            if (_provider != null)
            {
                TrayViewModel? selected = null;
                _provider.Locker.EnterReadLock();
                try
                {
                    var trays = _provider.GetTrays();
                    if (trays != null)
                    {
                        selected = trays.Where(x => x.TrayCode == tray).FirstOrDefault();
                        if (selected != null)
                        {
                            response = new Response() { 
                                IsExist = selected.IsExist, 
                                IsSuccess = true, 
                                ProductModel = selected.Model, 
                                ProductModelIndex = selected.ModelIndex,
                                ProductCode = selected.Rfid,
                                TrayCode = selected.TrayCode
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Log().Debug(ex);
                }
                finally
                {
                    _provider.Locker.ExitReadLock();
                }
            }

            if (response == null)
                response = new Response() { 
                    IsSuccess = false, 
                    Message = "查无结果",
                    TrayCode = tray
                };

            return response;
        }

        public bool Update(string tray, int state)
        {
            bool result = false;

            this.Log().Debug($"JSON-RPC call method 'Update' with parameters [{tray}, {state}]");

            if (_provider != null)
            {
                TrayViewModel? selected = null;
                _provider.Locker.EnterReadLock();
                try
                {
                    var trays = _provider.GetTrays();
                    if (trays != null)
                    {
                        selected = trays.Where(x => x.TrayCode == tray).FirstOrDefault();
                        if (selected != null)
                        {
                            
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Log().Debug(ex);
                }
                finally
                {
                    _provider.Locker.ExitReadLock();
                }
            }

            return result;
        }
    }
}
