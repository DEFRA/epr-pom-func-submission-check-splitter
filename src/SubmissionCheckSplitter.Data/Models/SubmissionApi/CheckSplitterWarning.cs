namespace SubmissionCheckSplitter.Data.Models.SubmissionApi;

using SubmissionCheckSplitter.Data.Enums;
public record CheckSplitterWarning(
    EventType ValidationWarningType,
    int RowNumber,
    string? BlobName,
    List<string> ErrorCodes,
    string? ProducerId,
    string? ProducerType,
    string? ProducerSize,
    string? WasteType,
    string? SubsidiaryId,
    string? DataSubmissionPeriod,
    string? PackagingCategory,
    string? MaterialType,
    string? MaterialSubType,
    string? FromHomeNation,
    string? ToHomeNation,
    string? QuantityKg,
    string? QuantityUnits);