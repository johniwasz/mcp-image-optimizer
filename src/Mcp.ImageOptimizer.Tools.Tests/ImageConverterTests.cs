using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;

namespace Mcp.ImageOptimizer.Tools.Tests
{

    public class ImageConverterTests
    {
        [Fact]
        public void Test1()
        {
            string imagePath = Path.Combine(AppContext.BaseDirectory, "TestImages", "sample.jpg");
            Assert.True(File.Exists(imagePath), $"File not found: {imagePath}");

            // You can now use imagePath to open/read the file
        }

        [Fact]
        public async Task ValidateMedataTool_Dimensions()
        {
            int width = 1920; // HD width
            int height = 1080; // HD height
            // Generate the test image
            byte[] imageBytes = GenerateTestImage(width, height);

            using (var ms = new MemoryStream(imageBytes))
            {
                var image = await Image.LoadAsync<Rgba32>(ms);
                Assert.Equal(width, image.Width);
                Assert.Equal(height, image.Height);

                if (image.Metadata != null)
                {
                    var xmlProfile = image.Metadata.XmpProfile;

                    if (image.Metadata?.ExifProfile?.Values != null)
                    {
                        Console.WriteLine("EXIF Metadata:");

                        foreach (var prop in image.Metadata?.ExifProfile?.Values)
                        {
                            Console.WriteLine($"{prop.Tag}: {prop.GetValue()}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No EXIF Metadata found.");
                    }
                }
            }
        }

        private byte[] GenerateTestImage(int width, int height)
        {
            // Create a new image with the specified dimensions and a blue background
            using var image = GenerateImage(width, height);

            using (var ms = new MemoryStream())
            {
                image.SaveAsJpeg(ms);
                return ms.ToArray();
            }
        }

        private string GenerateTestImage(int width, int height, string fileName)
        {
            // Save the image to a file
            string filePath = Path.Combine(AppContext.BaseDirectory, "TestImages", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)); // Ensure the directory exists

            // Create a new image with the specified dimensions and a blue background
            using (var image = GenerateImage(width, height))
            {
                image.SaveAsJpeg(filePath);               
            }
            return filePath;
        }

        private Image GenerateImage(int width, int height)
        {
            var image = new Image<Rgba32>(width, height);

            image.Mutate(ctx =>
            {
                ctx.Fill(Color.Blue);

                // Load a font (ensure the font file exists or use a system font)
                var fontCollection = new FontCollection();
                fontCollection.AddSystemFonts(); // Load system fonts

                var fontFam = fontCollection.Families.FirstOrDefault(x => x.Name.Equals("Arial")); // Use a common font
                var font = fontFam.CreateFont(48, FontStyle.Bold);

                // Define the text and its style
                string text = "Sample HD Image";
                RichTextOptions textOptions = new(font)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Origin = new PointF(width / 2f, height / 2f)
                };

                // Draw the text in white
                ctx.DrawText(textOptions, text, Color.White);
            });

            return image;
        
        }
    }
}

