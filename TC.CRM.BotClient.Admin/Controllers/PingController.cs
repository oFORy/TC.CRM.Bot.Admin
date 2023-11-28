using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;

namespace ApiService.Controllers
{
    /// <summary>
    /// Проверка доступности сервиса. 
    /// </summary>
    [EnableCors("usercorspolicy")]
    public class PingController : ControllerBase
    {
        private readonly ILogger<PingController> _logger;

        public PingController(ILogger<PingController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Проверка доступности сервиса.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("Api/Ping/Check")]
        public IActionResult Check()
        {
            return new JsonResult(new { Successfully = true });
        }
    }
}
