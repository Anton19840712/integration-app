namespace sftp_dynamic_gate_app.services.sftp;
public interface ISftpUploader
{
	Task UploadAsync(string localFilePath, string remoteFileName = null);
}
