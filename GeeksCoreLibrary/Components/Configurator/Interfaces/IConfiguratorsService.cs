using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Configurator.Models;

namespace GeeksCoreLibrary.Components.Configurator.Interfaces
{
    public interface IConfiguratorsService
    {
        Task<DataTable> GetConfiguratorDataAsync(string name, int componentId);
    }
}
