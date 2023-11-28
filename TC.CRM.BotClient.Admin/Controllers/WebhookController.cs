using Microsoft.AspNetCore.Mvc;
using TC.CRM.BotClient.Admin.Bot;
using Telegram.Bot.Types;

namespace GptBot.Controllers
{
    public class WebhookController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromServices] HandleUpdateService handleUpdateService,
                                      [FromBody] Update update)
        {
            await handleUpdateService.EchoAsync(update);
            return Ok();
        }
    }
}
