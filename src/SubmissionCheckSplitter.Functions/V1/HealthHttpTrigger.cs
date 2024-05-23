namespace SubmissionCheckSplitter.Functions.V1
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;

    /// <summary>
    /// Health check endpoint "api/health{code}".
    /// "/api/health" will return 200 OK, "/api/health/500" will return 500 Internal Serer Error, etc.
    /// </summary>
    public static class HealthHttpTrigger
    {
        [FunctionName(nameof(HealthHttpTrigger))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/{code:int}")] HttpRequest req, int? code)
        {
            if (code != null)
            {
                return new ObjectResult($"Specific code response for {code}") { StatusCode = code };
            }

            return new OkObjectResult("Healthy");
        }
    }
}
