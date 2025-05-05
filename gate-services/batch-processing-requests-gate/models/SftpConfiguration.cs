using sftp_dynamic_gate_app.services.sftp;

namespace sftp_dynamic_gate_app.models
{
	public static class SftpConfiguration
	{
		public static IServiceCollection AddSftpServices(this IServiceCollection services, IConfiguration configuration)
		{
			services.Configure<SftpSettings>(configuration.GetSection("SftpSettings"));
			services.AddTransient<FileHashService>();
			services.AddTransient<ISftpUploader, SftpUploader>();

			return services;
		}
	}
}
