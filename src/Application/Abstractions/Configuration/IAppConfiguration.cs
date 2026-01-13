namespace Application.Abstractions.Configuration;

public interface IAppConfiguration
{
	string FrontEndUrl { get; }

	string UserExportPath { get; }

	string HostingType { get; }

	string PDFilesPath { get; }

	string LGD_ClosedFacilityFilesPath { get; }

	string LGD_OpenFacilityFilesPath { get; }
}