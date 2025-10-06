using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Mcp.ImageOptimizer.StreamingHttp.Prompts;

[McpServerPromptType]
public class ImageBlobPrompts
{
    /*
    [McpServerPrompt(Name = "list_storage_accounts"), Description("List all Azure Storage Accounts in a subscription or region")]
    public static string ListStorageAccountsPrompt() => 
        "Use the list_storage_accounts tool to retrieve all Azure storage accounts. Optionally filter by region and include blob listings.";

    [McpServerPrompt(Name = "list_storage_accounts_with_blobs"), Description("List all Azure Storage Accounts with their blobs")]
    public static string ListStorageAccountsWithBlobsPrompt() => 
        "Use the list_storage_accounts tool with includeBlobs=true to retrieve all Azure storage accounts along with their containers and blobs.";

    [McpServerPrompt(Name = "list_blob_image_metadata"), Description("List metadata for all image blobs in a storage account")]
    public static string ListBlobImageMetadataPrompt() => 
        "Use the list_blob_image_metadata tool to retrieve metadata (dimensions, size, format) for all image blobs in the specified Azure storage account.";

    [McpServerPrompt(Name = "shrink_blob_images"), Description("Convert blob images to WebP format to reduce size")]
    public static string ShrinkBlobImagesPrompt() => 
        "Use the shrink_blob_images tool to convert all images in an Azure storage account to WebP format with quality 90. Do not delete images after conversion.";

    [McpServerPrompt(Name = "shrink_blob_images_high_quality"), Description("Convert blob images to WebP with high quality")]
    public static string ShrinkBlobImagesHighQualityPrompt() => 
        "Use the shrink_blob_images tool with quality=90 to convert all images in an Azure storage account to high-quality WebP format while preserving visual fidelity.";

    [McpServerPrompt(Name = "shrink_blob_images_and_delete_originals"), Description("Convert blob images to WebP and delete originals")]
    public static string ShrinkBlobImagesAndDeleteOriginalsPrompt() => 
        "Use the shrink_blob_images tool with deleteOriginal=true to convert all images to WebP format and remove the original files to save storage space.";

    [McpServerPrompt(Name = "list_deleted_large_images"), Description("List all deleted large image blobs")]
    public static string ListDeletedLargeImagesPrompt() => 
        "Use the list_deleted_large_blob_images tool to retrieve all deleted large image blobs in the specified Azure storage account, organized by container.";

    [McpServerPrompt(Name = "restore_deleted_large_images"), Description("Restore all deleted large image blobs")]
    public static string RestoreDeletedLargeImagesPrompt() => 
        "Use the restore_deleted_large_blob_images tool to undelete and restore all previously deleted large image blobs in the specified Azure storage account.";

    [McpServerPrompt(Name = "optimize_storage_account_images"), Description("Complete workflow to optimize all images in a storage account")]
    public static string OptimizeStorageAccountImagesPrompt() => 
        """
        Complete workflow to optimize images in an Azure storage account:
        1. Use list_storage_accounts to find available storage accounts
        2. Use list_blob_image_metadata to analyze current image sizes and formats
        3. Use shrink_blob_images to convert images to efficient WebP format
        4. Review the energy and storage savings from the conversion
        """;

    [McpServerPrompt(Name = "recover_deleted_images"), Description("Workflow to find and restore deleted images")]
    public static string RecoverDeletedImagesPrompt() => 
        """
        Workflow to recover deleted images from an Azure storage account:
        1. Use list_deleted_large_blob_images to identify deleted image blobs
        2. Review the list of deleted images and their metadata
        3. Use restore_deleted_large_blob_images to undelete and restore the images
        """;
    */
}