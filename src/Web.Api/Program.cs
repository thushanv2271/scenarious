using System.Reflection;
using Application;
using HealthChecks.UI.Client;
using Infrastructure;
using Infrastructure.Services;
using Application.Abstractions.Emailing;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Web.Api;
using Web.Api.Extensions;
using Application.Abstractions.Authentication;
using Infrastructure.Authentication;
using Application.Files.UploadFile;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;
using System.IO;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddSwaggerGenWithAuth();
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));

builder.Services
    .AddApplication()
    .AddPresentation()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

// Add CORS policy for development
builder.Services.AddCors(options => options.AddPolicy("DevCorsPolicy",
    builder => builder.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader()));

builder.Services.AddAuthorization();
builder.Services.AddAntiforgery();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCorsPolicy");
}

app.UseSwaggerWithUi();
app.ApplyMigrations();

// Only seed database if not in testing environment
//if (!app.Environment.IsEnvironment("Testing"))
//{
//    await app.SeedDatabaseAsync();
//}

app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseRequestContextLogging();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Serve uploaded files as static files based on configuration
FileStorageOptions fileStorage = app.Services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
if (!string.IsNullOrWhiteSpace(fileStorage.RootPath))
{
    // Ensure path is usable on this host. Attempt to create when missing, otherwise skip registering static files.
    string rootPath = fileStorage.RootPath!;
    if (!Path.IsPathRooted(rootPath) && app.Environment.IsDevelopment())
    {
        // Optionally allow relative paths during development by converting to absolute relative to content root
        rootPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, rootPath));
    }

    bool rootReady = false;
    try
    {
        if (Directory.Exists(rootPath))
        {
            rootReady = true;
        }
        else
        {
            // Try to create the directory. If creation fails (permissions, readonly FS, etc.) we'll log and skip static file registration.
            app.Logger.LogWarning("Static files root '{RootPath}' does not exist. Attempting to create it.", rootPath);
            Directory.CreateDirectory(rootPath);
            rootReady = Directory.Exists(rootPath);
            if (rootReady)
            {
                app.Logger.LogInformation("Created static files root directory at '{RootPath}'.", rootPath);
            }
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to ensure static files root directory exists at '{RootPath}'. Static file serving will be disabled.", rootPath);
        rootReady = false;
    }

    if (rootReady)
    {
        // Prefer explicit RequestPath from config; else infer from PublicBaseUrl; else default
        string requestPath = string.Empty;
        if (!string.IsNullOrWhiteSpace(fileStorage.RequestPath))
        {
            requestPath = fileStorage.RequestPath!;
            if (!requestPath.StartsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                requestPath = Path.DirectorySeparatorChar + requestPath;
            }
        }
        else if (!string.IsNullOrWhiteSpace(fileStorage.PublicBaseUrl) &&
                 Uri.TryCreate(fileStorage.PublicBaseUrl, UriKind.Absolute, out Uri? publicUri))
        {
            requestPath = publicUri.AbsolutePath;
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(rootPath),
            RequestPath = requestPath,
            ContentTypeProvider = new FileExtensionContentTypeProvider(),
            ServeUnknownFileTypes = true
        });

        app.Logger.LogInformation("Static file serving enabled for '{RootPath}' at request path '{RequestPath}'", rootPath, string.IsNullOrEmpty(requestPath) ? "/" : requestPath);
    }
    else
    {
        app.Logger.LogWarning("Static file serving disabled because root '{RootPath}' was not available and could not be created.", rootPath);
    }
}

app.MapEndpoints();
app.MapControllers();

await app.RunAsync();

namespace Web.Api
{
    public partial class Program;
}
