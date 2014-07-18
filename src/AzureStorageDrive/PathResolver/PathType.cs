namespace AzureStorageDrive
{
    public enum PathType
    {
        Root,
        ProviderRoot,

        AzureFileRoot,
        AzureFileDirectory,
        AzureFile,

        AzureBlobRoot,
        AzureBlobDirectory,
        AzureBlobBlock,
        AzureBlobPage,

        Invalid,
        Unknown
    }
}