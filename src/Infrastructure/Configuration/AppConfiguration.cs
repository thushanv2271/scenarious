using Application.Abstractions.Configuration;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Configuration;

public class AppConfiguration : IAppConfiguration
{
	private readonly IConfiguration _configuration;

	public AppConfiguration(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public string FrontEndUrl => _configuration["FrontEndUrl"]!;
	public string UserExportPath => _configuration["UserExportPath"]!;
	public string HostingType => _configuration["HostingType"]!;


}
