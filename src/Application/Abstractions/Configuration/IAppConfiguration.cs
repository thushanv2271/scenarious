namespace Application.Abstractions.Configuration;

public interface IAppConfiguration
{
	string FrontEndUrl { get; }

	string UserExportPath { get; }

	string HostingType { get; }
}