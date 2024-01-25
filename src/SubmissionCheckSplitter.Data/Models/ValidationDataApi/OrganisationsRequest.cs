namespace SubmissionCheckSplitter.Data.Models.ValidationDataApi;

using System.Text.Json.Serialization;

public class OrganisationsRequest
{
    [JsonPropertyName(nameof(ReferenceNumbers))]
    public IEnumerable<string> ReferenceNumbers { get; set; }
}