using Azure.Storage.Blobs;
using SixLabors.ImageSharp.PixelFormats;
using System.ComponentModel;
using System.Globalization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using Mcp.ImageOptimizer.Common;

namespace Mcp.ImageOptimizer.Azure.Tools
{
    public class BlobTools
    {

        public async Task ConvertBlobAsync(string sourceConnectionString, string sourceContainerName, string sourceBlobName, string destinationConnectionString, string destinationContainerName, string destinationBlobName)
        {
            // Placeholder for future implementation
            
            BlobContainerClient sourceContainerClient = new BlobContainerClient(sourceConnectionString, sourceContainerName);

            using MemoryStream blobStream = await DownloadBlobAsync(sourceContainerClient, sourceBlobName);

            // Here you would add the image conversion logic (e.g., using ImageSharp)
            BlobContainerClient destinationContainerClient = new BlobContainerClient(destinationConnectionString, destinationContainerName);
            var destinationBlobClient = destinationContainerClient.GetBlobClient(destinationBlobName);
           
        }


        private async Task<MemoryStream> DownloadBlobAsync(BlobContainerClient sourceContainerClient, string blobName)
        {
            var blobClient = sourceContainerClient.GetBlobClient(blobName);
            var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            memoryStream.Position = 0; // Reset stream position after download
            return memoryStream;
        }

        private async Task<ConvertedImageMetadata> ConvertToWebPAsync(MemoryStream blobStream, string blobName, int quality = 80)
        {
            long originalImageSize = blobStream.Length;

            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(blobName);

            // Load the image and save as WebP
            using (var image = await Image.LoadAsync<Rgba32>(blobStream))
            {
                var encoder = new WebpEncoder()
                {
                    Quality = quality
                };
            }

            // Get metadata for the new WebP file
            ImageMetadata imageData = await ImageUtilities.GetImageMetadataFromFileAsync(blobName) ?? new ImageMetadata();

            ConvertedImageMetadata convertedMetadata = new ConvertedImageMetadata(imageData);
          
            long bytesSaved = originalImageSize - convertedMetadata.Size;
            convertedMetadata.EnergySaved = bytesSaved / ImageMetadata.GIGABYTES * 0.81;

            return convertedMetadata;

        }
    }
}
