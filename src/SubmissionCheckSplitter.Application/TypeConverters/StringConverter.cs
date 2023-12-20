namespace SubmissionCheckSplitter.Application.TypeConverters;

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

public class StringConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}