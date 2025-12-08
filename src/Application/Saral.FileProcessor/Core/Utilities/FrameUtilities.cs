namespace Saral.FileProcessor.Core.Utilities;

public static class FrameUtilities
{
    public static Frame<int, string> NormalizeCells(this Frame<int, string> frame)
    {
        string[] columnKeys = frame.ColumnKeys.ToArray();
        int[] rowKeys = frame.RowKeys.ToArray();
        var columns = new List<KeyValuePair<string, Series<int, object?>>>(columnKeys.Length);

        foreach (string columnKey in columnKeys)
        {
            Series<int, object?> column = frame.GetColumn<object?>(columnKey);
            var builder = new SeriesBuilder<int, object?>();

            foreach (int rowKey in rowKeys)
            {
                builder.Add(rowKey, NormalizeValue(GetValue(column, rowKey)));
            }

            columns.Add(KeyValuePair.Create(columnKey, builder.Series));
        }

        return Frame.FromColumns(columns);
    }

    public static Frame<int, string> FromRows(IEnumerable<IReadOnlyDictionary<string, object?>> rows, IReadOnlyList<string> headers)
    {
        IEnumerable<KeyValuePair<int, Series<string, object?>>> rowData = rows.Select((row, index) =>
        {
            var builder = new SeriesBuilder<string, object?>();

            foreach (string header in headers)
            {
                row.TryGetValue(header, out object? value);
                builder.Add(header, NormalizeValue(value));
            }

            return KeyValuePair.Create(index, builder.Series);
        });

        return Frame.FromRows(rowData);
    }

    public static IReadOnlyDictionary<string, object?> CaptureRowValues(Frame<int, string> frame, int rowKey)
    {
        var result = new Dictionary<string, object?>(frame.ColumnCount, StringComparer.OrdinalIgnoreCase);

        foreach (string columnKey in frame.ColumnKeys)
        {
            Series<int, object?> column = frame.GetColumn<object?>(columnKey);
            result[columnKey] = NormalizeValue(GetValue(column, rowKey));
        }

        return result;
    }

    public static object? NormalizeValue(object? value)
        => value switch
        {
            null => null,
            DBNull => null,
            string s when string.IsNullOrWhiteSpace(s) => null,
            string s => s.Trim(),
            _ => value
        };

    private static object? GetValue(Series<int, object?> series, int key)
    {
        OptionalValue<object?> optional = series.TryGet(key);
        return optional.HasValue ? optional.Value : null;
    }
}
