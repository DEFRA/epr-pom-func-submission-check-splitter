namespace SubmissionCheckSplitter.Data.Config;
public class ValidationDataApiConfig
{
    public const string Section = "ValidationDataApi";

    public string BaseUrl { get; set; }

    public bool IsEnabled { get; set; }

    public int Timeout { get; set; }
}