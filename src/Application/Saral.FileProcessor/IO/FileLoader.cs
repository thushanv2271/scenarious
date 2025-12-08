namespace Saral.FileProcessor.IO;

public sealed class FileLoader : IFileLoader
{
    static FileLoader()
        => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static readonly Lazy<Encoding[]> CandidateEncodings = new(() =>
        [
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
            Encoding.UTF8,
            Encoding.GetEncoding("ISO-8859-1"),
            Encoding.GetEncoding(1252)
        ]);

    private static readonly string[] MissingMarkers =
    [
        string.Empty,
        "NA",
        "N/A",
        "NULL",
        "null"
    ];

    public FileLoadContext Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}", filePath);
        }

        string extension = Path.GetExtension(filePath).ToUpperInvariant();

        return extension switch
        {
            ".CSV" => LoadCsv(filePath),
            ".XLSX" or ".XLS" => LoadExcel(filePath),
            _ => throw new NotSupportedException($"Unsupported file type: {extension}")
        };
    }

    public FileLoadContext Load(Stream stream, string fileName)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable.", nameof(stream));
        }

        string extension = Path.GetExtension(fileName).ToUpperInvariant();

        return extension switch
        {
            ".CSV" => LoadCsvFromStream(stream, fileName),
            ".XLSX" or ".XLS" => LoadExcelFromStream(stream, fileName),
            _ => throw new NotSupportedException($"Unsupported file type: {extension}")
        };
    }

    private FileLoadContext LoadCsv(string filePath)
    {
        Exception? lastError = null;

        foreach (Encoding encoding in CandidateEncodings.Value)
        {
            try
            {
                string content = File.ReadAllText(filePath, encoding);
                using var buffer = new MemoryStream(encoding.GetBytes(content));
                var frame = Frame.ReadCsv(
                    buffer,
                    hasHeaders: true,
                    culture: CultureInfo.InvariantCulture.Name,
                    missingValues: MissingMarkers,
                    preferOptions: true);
                Frame<int, string> sanitized = frame.NormalizeCells();

                return new FileLoadContext(
                    filePath,
                    ".csv",
                    new FileInfo(filePath).Length,
                    encoding.WebName,
                    sanitized);
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw new InvalidDataException($"Unable to read CSV file '{filePath}' using the supported encodings.", lastError);
    }

    private FileLoadContext LoadExcel(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        IXLWorksheet? worksheet = workbook.Worksheets.FirstOrDefault(ws => !ws.IsEmpty()) ?? throw new InvalidDataException("The Excel workbook does not contain any data.");

        IXLRow headerRow = worksheet.FirstRowUsed() ?? throw new InvalidDataException("Unable to locate header row in the worksheet.");
        string[] headers = headerRow.CellsUsed().Select(cell => cell.GetString().Trim()).ToArray();

        if (headers.Length == 0)
        {
            throw new InvalidDataException("The worksheet must contain at least one column header.");
        }

        var rows = new List<IReadOnlyDictionary<string, object?>>();

        foreach (IXLRow? row in worksheet.RowsUsed().Skip(1))
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            for (int columnIndex = 0; columnIndex < headers.Length; columnIndex++)
            {
                IXLCell cell = row.Cell(columnIndex + 1);
                values[headers[columnIndex]] = cell.Value;
            }

            rows.Add(values);
        }

        Frame<int, string> frame = FrameUtilities.FromRows(rows, headers);
        Frame<int, string> sanitized = frame.NormalizeCells();

        return new FileLoadContext(
            filePath,
            Path.GetExtension(filePath).ToUpperInvariant(),
            new FileInfo(filePath).Length,
            EncodingName: null,
            sanitized);
    }

    private FileLoadContext LoadCsvFromStream(Stream stream, string fileName)
    {
        Exception? lastError = null;

        foreach (Encoding encoding in CandidateEncodings.Value)
        {
            try
            {
                stream.Position = 0; // Reset stream position
                
                var frame = Frame.ReadCsv(
                    stream,
                    hasHeaders: true,
                    culture: CultureInfo.InvariantCulture.Name,
                    missingValues: MissingMarkers,
                    preferOptions: true);
                Frame<int, string> sanitized = frame.NormalizeCells();

                return new FileLoadContext(
                    fileName,
                    ".csv",
                    stream.Length,
                    encoding.WebName,
                    sanitized);
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw new InvalidDataException($"Unable to read CSV from stream for file '{fileName}' using the supported encodings.", lastError);
    }

    private FileLoadContext LoadExcelFromStream(Stream stream, string fileName)
    {
        stream.Position = 0; // Reset stream position
        
        using var workbook = new XLWorkbook(stream);
        IXLWorksheet? worksheet = workbook.Worksheets.FirstOrDefault(ws => !ws.IsEmpty()) ?? throw new InvalidDataException("The Excel workbook does not contain any data.");

        IXLRow headerRow = worksheet.FirstRowUsed() ?? throw new InvalidDataException("Unable to locate header row in the worksheet.");
        string[] headers = headerRow.CellsUsed().Select(cell => cell.GetString().Trim()).ToArray();

        if (headers.Length == 0)
        {
            throw new InvalidDataException("The worksheet must contain at least one column header.");
        }

        var rows = new List<IReadOnlyDictionary<string, object?>>();

        foreach (IXLRow? row in worksheet.RowsUsed().Skip(1))
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            for (int columnIndex = 0; columnIndex < headers.Length; columnIndex++)
            {
                IXLCell cell = row.Cell(columnIndex + 1);
                values[headers[columnIndex]] = cell.Value;
            }

            rows.Add(values);
        }

        Frame<int, string> frame = FrameUtilities.FromRows(rows, headers);
        Frame<int, string> sanitized = frame.NormalizeCells();

        return new FileLoadContext(
            fileName,
            Path.GetExtension(fileName).ToUpperInvariant(),
            stream.Length,
            EncodingName: null,
            sanitized);
    }
}
