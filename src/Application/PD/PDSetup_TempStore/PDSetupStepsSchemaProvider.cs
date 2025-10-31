using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Application.PD.PDSetup_TempStore;

internal static class PDSetupStepsSchemaProvider
{


    public static async Task<string> GetSchemaAsync(string SchemaResourcePath, CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using Stream? stream = assembly.GetManifestResourceStream(SchemaResourcePath) ?? throw new FileNotFoundException($"Schema resource not found: {SchemaResourcePath}");

        using StreamReader reader = new(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}