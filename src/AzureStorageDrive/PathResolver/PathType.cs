namespace AzureStorageDrive
{
    public enum PathType
    {
        Root,

        AzureFileRoot,
        AzureFileDirectory,
        AzureFile,

        AzureBlobRoot,
        AzureBlobDirectory,
        AzureBlobBlock,
        AzureBlobPage,

        AwsS3Root,
        AwsS3Directory,
        AwsS3File,

        Invalid,
        Unknown
    }
}