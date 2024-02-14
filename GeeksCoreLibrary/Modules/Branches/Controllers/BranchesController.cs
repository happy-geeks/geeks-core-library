using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GeeksCoreLibrary.Modules.Branches.Controllers
{
    [Area("Branches")]
    [Route("branches")]
    public class BranchesController : Controller
    {
        private readonly IBranchesService branchesService;
        private readonly IDatabaseHelpersService databaseHelpersService;

        public BranchesController(IBranchesService branchesService, IDatabaseHelpersService databaseHelpersService)
        {
            this.branchesService = branchesService;
            this.databaseHelpersService = databaseHelpersService;
        }

        [HttpGet("{dataBaseName}")]
        public async Task<IActionResult> SwitchToBranchAsync(string databaseName)
        {
            if (!await databaseHelpersService.DatabaseExistsAsync(databaseName))
            {
                return NotFound($"Database / branch with name '{databaseName}' does not exist.");
            }

            branchesService.SaveDatabaseNameToCookie(databaseName);
            return Redirect("/");
        }
    }
}