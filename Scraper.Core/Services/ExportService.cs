using System.Globalization;
using System.Data;
using System.Text;
using System.Text.Json;
using CsvHelper;

namespace Scraper.Core.Services;

public class ExportService
{
    public void ToJson(DataTable data, string path)
    {
        var rows = data.Rows
            .Cast<DataRow>()
            .Select(row => data.Columns
                .Cast<DataColumn>()
                .ToDictionary(column => column.ColumnName, column => row[column]?.ToString() ?? string.Empty))
            .ToList();

        var json = JsonSerializer.Serialize(rows, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
    }

    public void ToJson<T>(IReadOnlyCollection<T> data, string path)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
    }

    public void ToCsv(DataTable data, string path)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        foreach (DataColumn column in data.Columns)
        {
            csv.WriteField(column.ColumnName);
        }

        csv.NextRecord();

        foreach (DataRow row in data.Rows)
        {
            foreach (DataColumn column in data.Columns)
            {
                csv.WriteField(row[column]?.ToString() ?? string.Empty);
            }

            csv.NextRecord();
        }
    }

    public void ToCsv<T>(IReadOnlyCollection<T> data, string path)
    {
        using var writer = new StreamWriter(path);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(data);
    }

    public void ToExcel(DataTable data, string path)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<html><head><meta charset=\"utf-8\" /></head><body><table border=\"1\">");
        builder.AppendLine("<tr>");

        foreach (DataColumn column in data.Columns)
        {
            builder.Append("<th>")
                .Append(System.Net.WebUtility.HtmlEncode(column.ColumnName))
                .AppendLine("</th>");
        }

        builder.AppendLine("</tr>");

        foreach (DataRow row in data.Rows)
        {
            builder.AppendLine("<tr>");
            foreach (DataColumn column in data.Columns)
            {
                builder.Append("<td>")
                    .Append(System.Net.WebUtility.HtmlEncode(row[column]?.ToString() ?? string.Empty))
                    .AppendLine("</td>");
            }

            builder.AppendLine("</tr>");
        }

        builder.AppendLine("</table></body></html>");
        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
    }

    public void ToExcel<T>(IReadOnlyCollection<T> data, string path)
    {
        var properties = typeof(T).GetProperties();
        var builder = new StringBuilder();
        builder.AppendLine("<html><head><meta charset=\"utf-8\" /></head><body><table border=\"1\">");
        builder.AppendLine("<tr>");

        foreach (var property in properties)
        {
            builder.Append("<th>")
                .Append(System.Net.WebUtility.HtmlEncode(property.Name))
                .AppendLine("</th>");
        }

        builder.AppendLine("</tr>");

        foreach (var item in data)
        {
            builder.AppendLine("<tr>");
            foreach (var property in properties)
            {
                builder.Append("<td>")
                    .Append(System.Net.WebUtility.HtmlEncode(property.GetValue(item)?.ToString() ?? string.Empty))
                    .AppendLine("</td>");
            }

            builder.AppendLine("</tr>");
        }

        builder.AppendLine("</table></body></html>");
        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
    }
}
