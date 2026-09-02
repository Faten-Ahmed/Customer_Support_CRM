namespace CRM.Infrastructure.Storage;

public class MinIOSettings
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string BucketName { get; set; } = "crm-attachments";
    public bool UseSSL { get; set; } = false;
    public int PresignedUrlExpirySeconds { get; set; } = 3600;
}
