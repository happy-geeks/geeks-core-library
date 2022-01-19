using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Configurator.Interfaces;
using GeeksCoreLibrary.Components.Configurator.Models;
using Microsoft.AspNetCore.Mvc;

namespace GeeksCoreLibrary.Components.Configurator.Controllers
{
    [Area("Configurators")]
    [Route("configurators")]
    public class ConfiguratorsController : Controller
    {
        private readonly IConfiguratorsService configuratorsService;

        public ConfiguratorsController(IConfiguratorsService configuratorsService)
        {
            this.configuratorsService = configuratorsService;
        }

        /// <summary>
        /// save configurator input to wiser tables
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost, Route("save")]
        public async Task<ulong> Save([FromBody] ConfigurationsModel input)
        {
            if (input == null || input.Items == null || input.Items.Count == 0)
            {
                return 0;
            }
            return await this.configuratorsService.SaveConfigurationAsync(input);
        }
    }
}
