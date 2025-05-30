﻿namespace SubmissionCheckSplitter.Application.Helpers;

using SubmissionCheckSplitter.Data.Config;

public interface ICsvStreamParser
{
   // IList<T> GetItemsFromCsvStream<T>(MemoryStream memoryStream, bool isLatest = false);
    IList<T> GetItemsFromCsvStream<T>(MemoryStream memoryStream, CsvDataFileConfig csvDataFileConfigOptions);
}