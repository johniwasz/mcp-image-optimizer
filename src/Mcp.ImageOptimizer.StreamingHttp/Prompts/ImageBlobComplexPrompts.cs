

using Microsoft.Identity.Client.Extensions.Msal;
using System.Reflection.Emit;
using static System.Reflection.Metadata.BlobBuilder;

namespace Mcp.ImageOptimizer.StreamingHttp.Prompts;

[McpServerPromptType]
public class ImageBlobComplexPrompts
{
    [McpServerPrompt(Name = "list_blob_image_metadata"), Description("List metadata for all image blobs in a storage account with detailed context")]
    public static IEnumerable<ChatMessage> ListBlobImageMetadataComplexPrompt(
        [Description("Azure Storage Account name")] string? storageAccountName = null,
        [Description("Azure subscription id (optional)")] string? subscriptionId = null)
    {
        return [
            new ChatMessage(ChatRole.User, $"I need to analyze all image blobs in the {(string.IsNullOrEmpty(storageAccountName) ? "all storage accounts" : $"storage account {storageAccountName}")}{(subscriptionId != null ? $" and within subscription '{subscriptionId}'" : "")}. Please retrieve comprehensive metadata including dimensions, size, and format for each image blob organized by container."),
            new ChatMessage(ChatRole.Assistant, "I understand. I'll use the list_blob_image_metadata tool to retrieve detailed metadata for all image blobs in the specified storage account. This will include information such as width, height, file size, format, and EXIF data if available, organized by container. Results will presented in a tabular format."),
        ];
    }

    [McpServerPrompt(Name = "analyze_storage_account_images_complex"), Description("Comprehensive analysis of storage account images with optimization recommendations")]
    public static IEnumerable<ChatMessage> AnalyzeStorageAccountImagesComplexPrompt(
        [Description("Azure Storage Account name")] string? storageAccountName = null,
        [Description("Target quality for potential WebP conversion (0-100)")] int targetQuality = 90)
    {
        StringBuilder userPromptBuilder = new();
        StringBuilder assistantBuilder = new();

        if(string.IsNullOrEmpty(storageAccountName))
        {
            userPromptBuilder.AppendLine("all storage accounts. Please:");
            userPromptBuilder.AppendLine("1. List all storage accounts to confirm the account exists");
            userPromptBuilder.AppendLine("2. Retrieve metadata for all image blobs");
            userPromptBuilder.AppendLine("3. Identify images that could benefit from WebP conversion");
            userPromptBuilder.AppendLine($"4. Provide recommendations for optimization with quality level {targetQuality}");

            assistantBuilder.Append("I'll perform a comprehensive analysis of your Azure storage account images. I'll start by listing the storage accounts, then retrieve detailed metadata for all image blobs, analyze their current formats and sizes, and provide optimization recommendations including potential storage and energy savings from WebP conversion.");
        }
        else
        {
            userPromptBuilder.AppendLine($"storage account {storageAccountName}. Please: ");
            userPromptBuilder.AppendLine($"1. Retrieve metadata for the image blobs in {storageAccountName}");
            userPromptBuilder.AppendLine("2. Identify images that could benefit from WebP conversion");
            userPromptBuilder.AppendLine($"3. Provide recommendations for optimization with quality level {targetQuality}");

            assistantBuilder.Append($"I'll perform a comprehensive analysis of the images in your '{storageAccountName}' Azure storage account. I'll retrieve detailed metadata for all image blobs in the storage account, analyze their current formats and sizes, and provide optimization recommendations including potential storage and energy savings from WebP conversion.");
        }

        return [
            new ChatMessage(ChatRole.User, userPromptBuilder.ToString()),
            new ChatMessage(ChatRole.Assistant, assistantBuilder.ToString()),
        ];
    }

    [McpServerPrompt(Name = "shrink_images_workflow_complex"), Description("Complete workflow to shrink blob images with safety checks")]
    public static IEnumerable<ChatMessage> ShrinkImagesWorkflowComplexPrompt(
        [Description("Azure Storage Account name")] string storageAccountName,
        [Description("Quality level for WebP compression (0-100)")] int quality = 90,
        [Description("Indicate if the original image should be deleted")] bool deleteOriginal = false)
    {
        return [
            new ChatMessage(ChatRole.User, $"I need to optimize images in the '{storageAccountName}' storage account with the following parameters:\n- Target quality: {quality}\n- Delete originals: {deleteOriginal}\n\nBefore proceeding with conversion, please:\n1. Show me the current image inventory and total size\n2. Estimate the storage savings\n3. {(deleteOriginal ? "Warn me that originals will be permanently deleted and ask for confirmation" : "Confirm that originals will be preserved")}\n4. Proceed with the conversion"),
            new ChatMessage(ChatRole.Assistant, $"I'll help you optimize the images in your storage account safely. First, I'll retrieve the current image metadata to show you the inventory and calculate potential savings. {(deleteOriginal ? "Since you've chosen to delete originals, I'll clearly warn you before proceeding and request explicit confirmation." : "The original images will be preserved alongside the new WebP versions.")} Let me start by analyzing your current images."),
        ];
    }

    [McpServerPrompt(Name = "restore_deleted_images_complex"), Description("Workflow to identify and restore deleted image blobs with context")]
    public static IEnumerable<ChatMessage> RestoreDeletedImagesComplexPrompt(
        [Description("Azure Storage Account name")] string storageAccountName)
    {
        return [
            new ChatMessage(ChatRole.User, $"I need to recover deleted images from Azure storage. Please:\n1. First, list all available storage accounts so I can see what's accessible\n2. Then focus on the '{storageAccountName}' storage account\n3. List all deleted large image blobs organized by container\n4. Show me details about when they were deleted, their sizes, and container information\n5. Ask me which containers or specific blobs I want to restore\n6. Proceed with the restoration of confirmed items"),
            new ChatMessage(ChatRole.Assistant, "I'll help you recover deleted images from your Azure storage account. I'll start by using list_storage_accounts to show you all available storage accounts, then use the list_deleted_large_blob_images tool to identify all deleted image blobs in the '{storageAccountName}' account and provide you with detailed information about each one including deletion time and size. Finally, I'll guide you through selecting which images to restore using the restore_deleted_large_blob_images tool."),
        ];
    }

    [McpServerPrompt(Name = "compare_storage_accounts_complex"), Description("Compare image storage across multiple storage accounts")]
    public static IEnumerable<ChatMessage> CompareStorageAccountsComplexPrompt(
        [Description("Azure region to filter by (optional)")] string? region = null,
        [Description("Include blob listings in the results")] bool includeBlobs = true)
    {
        return [
            new ChatMessage(ChatRole.User, $"I want to compare image storage across all my Azure storage accounts{(region != null ? $" in the {region} region" : "")}. Please:\n1. List all storage accounts with their containers{(includeBlobs ? " and blobs" : "")}\n2. For each account with images, retrieve the image metadata\n3. Provide a summary comparing:\n   - Total number of images per account\n   - Total storage used by images per account\n   - Average image size per account\n   - Recommended accounts for optimization"),
            new ChatMessage(ChatRole.Assistant, $"I'll perform a comprehensive comparison of image storage across your Azure storage accounts. I'll start by listing all accounts{(region != null ? $" in the {region} region" : "")}, then analyze the image metadata for each account, and provide you with a detailed comparison report including optimization recommendations."),
        ];
    }
}
