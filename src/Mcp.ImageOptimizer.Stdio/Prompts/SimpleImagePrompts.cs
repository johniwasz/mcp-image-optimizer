using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Mcp.ImageOptimizer.Stdio.Prompts;

[McpServerPromptType]
public class SimpleImagePrompts
{
    [McpServerPrompt(Name = "list_image_metadata"), Description("List all image metadata")]
    public static string ListImageMetadataPrompt() => "Use the get_image_metadata tool to list all image metadata.";

    [McpServerPrompt(Name = "convert_all_images"), Description("Convert all images")]
    public static string ConvertAllImagesPrompt() => "Use the convert_image_to_webp tool to optimize all images.";


}