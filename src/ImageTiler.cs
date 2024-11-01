namespace ImageProcess
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Text.Json.Nodes;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Emgu.CV.CvEnum;
    using System.Threading.Tasks;

    public class ImageTiler
    {
        private static Random random = new Random();

        /// <summary>
        /// Creates a tiled image and returns both the image and its JSON representation
        /// </summary>
        public static (Mat processedImage, JsonObject json) CreateTiledImageWithJson(Image[] sourceImages, int outputWidth, int outputHeight, double noiseLevel = 0.0)
        {
            // Create and process the tiled image
            Mat processedImage = CreateTiledMatImage(sourceImages, outputWidth, outputHeight, noiseLevel);

            // Convert to JSON format
            JsonObject json = ConvertGrayscaleMatToJson(processedImage);

            return (processedImage, json);
        }

        /// <summary>
        /// Converts a grayscale Mat to JSON organized by rows
        /// </summary>
        private static JsonObject ConvertGrayscaleMatToJson(Mat grayscaleImage)
        {
            JsonObject jsonObject = new JsonObject();

            // Convert Mat to grayscale Image
            using (var grayImage = grayscaleImage.ToImage<Gray, byte>())
            {
                // Process each row
                for (int y = 0; y < grayImage.Height; y++)
                {
                    JsonArray rowArray = new JsonArray();

                    // Process each pixel in the row
                    for (int x = 0; x < grayImage.Width; x++)
                    {
                        // Get grayscale value and normalize to 0-1
                        double normalizedValue = Math.Round(grayImage[y, x].Intensity / 255.0, 3);
                        rowArray.Add(normalizedValue);
                    }

                    // Add row to JSON object
                    jsonObject.Add(y.ToString(), rowArray);
                }
            }

            return jsonObject;
        }

        /// <summary>
        /// Creates a tiled Mat image from an array of source images
        /// </summary>
        private static Mat CreateTiledMatImage(Image[] sourceImages, int outputWidth, int outputHeight, double noiseLevel = 0.0)
        {
            if (sourceImages == null || sourceImages.Length == 0)
                throw new ArgumentException("Source images array cannot be null or empty");
            if (noiseLevel < 0.0 || noiseLevel > 1.0)
                throw new ArgumentException("Noise level must be between 0.0 and 1.0");

            // Calculate the grid dimensions
            int gridSize = (int)Math.Ceiling(Math.Sqrt(sourceImages.Length));
            int tileWidth = outputWidth / gridSize;
            int tileHeight = outputHeight / gridSize;

            // Create the output image (single channel grayscale)
            Mat outputMat = new Mat(new System.Drawing.Size(outputWidth, outputHeight), DepthType.Cv8U, 1);

            // Process and tile each image
            for (int i = 0; i < sourceImages.Length; i++)
            {
                if (sourceImages[i] == null) continue;

                int currentRow = i / gridSize;
                int currentCol = i % gridSize;
                int x = currentCol * tileWidth;
                int y = currentRow * tileHeight;

                using (Mat processedTile = ProcessImage(sourceImages[i], tileWidth, tileHeight))
                {
                    if (processedTile != null)
                    {
                        if (sourceImages[i].Height > tileHeight && sourceImages[i].Width > tileHeight)
                        {
                            // Define region for the current tile
                            using (Mat roi = new Mat(outputMat,
                                new Rectangle(x, y, tileWidth, tileHeight)))
                            {
                                processedTile.CopyTo(roi);
                            }
                        }
                    }
                }
            }

            // Apply noise if needed
            if (noiseLevel > 0)
            {
                ApplySaltAndPepperNoise(outputMat, noiseLevel);
            }

            return outputMat;
        }

        /// <summary>
        /// Processes a single image using the specified algorithm
        /// </summary>
        private static Mat ProcessImage(Image sourceImage, int targetWidth, int targetHeight)
        {
            try
            {
                // Convert System.Drawing.Image to Mat
                using (Bitmap bmp = new Bitmap(sourceImage))
                {
                    // Create Emgu CV Image directly from Bitmap
                    Image<Bgr, byte> imgColor2 = bmp.ToImage<Bgr, byte>();

                    using (Image<Gray, Byte> newImageColor = new Image<Gray, Byte>(imgColor2.Width, imgColor2.Height, new Gray(255)))
                    {
                        // Process image using parallel for
                        Parallel.For(0, imgColor2.Height, y =>
                        {
                            for (int x = 0; x < imgColor2.Width; x++)
                            {
                                if (imgColor2[y, x].Green != imgColor2[y, x].Blue ||
                                    imgColor2[y, x].Blue != imgColor2[y, x].Red ||
                                    imgColor2[y, x].Green != imgColor2[y, x].Red)
                                {
                                    newImageColor[y, x] = new Gray(Math.Min(
                                        Math.Min(imgColor2[y, x].Green, imgColor2[y, x].Blue),
                                        imgColor2[y, x].Red));
                                }
                                else
                                {
                                    newImageColor[y, x] = new Gray(imgColor2[y, x].Green);
                                }
                            }
                        });

                        // Apply thresholding
                        using (Mat img2 = newImageColor.Mat)
                        {
                            // Resize to target dimensions
                            Mat resized = new Mat();
                            CvInvoke.Resize(img2, resized, new System.Drawing.Size(targetWidth, targetHeight));
                            return resized.Clone();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Applies salt and pepper noise to a Mat image
        /// </summary>
        private static void ApplySaltAndPepperNoise(Mat image, double noiseLevel)
        {
            int height = image.Height;
            int width = image.Width;

            // Calculate number of pixels to affect based on noise level
            int totalPixels = height * width;
            int pixelsToAffect = (int)(totalPixels * noiseLevel);

            using (Image<Gray, byte> grayImage = image.ToImage<Gray, byte>())
            {
                // Apply noise
                for (int i = 0; i < pixelsToAffect; i++)
                {
                    int x = random.Next(width);
                    int y = random.Next(height);

                    // Randomly choose between salt (white) and pepper (black)
                    byte noiseValue = random.Next(2) == 0 ? (byte)0 : (byte)255;

                    grayImage[y, x] = new Gray(noiseValue);
                }

                // Copy back to original Mat
                grayImage.Mat.CopyTo(image);
            }
        }
    }
}
