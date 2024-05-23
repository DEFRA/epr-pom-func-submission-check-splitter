namespace SubmissionCheckSplitter.Functions.V1
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;

    public static class ErrorHttpTrigger
    {
        [FunctionName(nameof(ErrorHttpTrigger))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/error")] HttpRequest req, int? code)
        {
            return new OkObjectResult("Healthy");
        }
    }
}
