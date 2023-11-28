using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TC.CRM.BotClient.Admin.Filters
{
    public class ExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<ExceptionFilter> _logger;

        public ExceptionFilter(ILogger<ExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            string actionName = context.ActionDescriptor.DisplayName;
            string exceptionStack = context.Exception.StackTrace;
            string exceptionMessage = context.Exception.Message;

            string errorMessage = $"В методе {actionName} возникло исключение: \n {exceptionMessage} \n {exceptionStack}";

            _logger.LogError(errorMessage);

            var response = new
            {
                Successfully = false,
                Message = exceptionMessage,
                StackTrace = exceptionStack,
                ActionName = actionName
            };

            context.Result = new JsonResult(response);
            context.ExceptionHandled = true;
        }
    }
}
