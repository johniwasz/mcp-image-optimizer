using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Mcp.ImageOptimizer.Stdio.Prompts
{

    [McpServerPromptType]
    public class ImageComplexPrompts
    {

        [McpServerPrompt(Name = "convert_single_image_prompt"), Description("Convert a single image")]
        public static IEnumerable<ChatMessage> SingleImageConversionComplexPrompt(
        [Description("The fully qualified path to an image file.")] string imageFilePath,
        [Description("Quality level for WebP compression (0-100, where 100 is lossless). Default is 90.")] int quality = 90)
        {
            return [
                new ChatMessage(ChatRole.User,$"This is a complex prompt to run the convert_image_to_webp tool with arguments: imageFilePath={imageFilePath}, quality={quality}"),
                new ChatMessage(ChatRole.Assistant, "I understand. You've provided a complex prompt with imageFilePath and quality arguments. How would you like me to proceed?"),
            ];
        }
    }
}