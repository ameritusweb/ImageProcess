using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Linq;

    public class ImageLoader
    {
        private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

        /// <summary>
        /// Loads all supported images from a specified directory
        /// </summary>
        /// <param name="folderPath">Path to the folder containing images</param>
        /// <param name="skip">The number to skip.</param>
        /// <param name="max">The max count.</param>
        /// <returns>Array of Image objects</returns>
        public static Image[] LoadImagesFromFolder(string folderPath, int skip, int max)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

            var imageFiles = Directory.GetFiles(folderPath)
                .Where(file => SupportedExtensions.Contains(Path.GetExtension(file).ToLower()))
                .Skip(skip)
                .Take(max)
                .ToArray();

            Image[] images = new Image[imageFiles.Length];

            for (int i = 0; i < imageFiles.Length; i++)
            {
                try
                {
                    // Load image from file and create a new instance to avoid file locks
                    using (var originalImage = Image.FromFile(imageFiles[i]))
                    {
                        images[i] = new Bitmap(originalImage);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading image {imageFiles[i]}: {ex.Message}");
                    images[i] = null;
                }
            }

            return images;
        }

        /// <summary>
        /// Loads images from specific files in a directory
        /// </summary>
        /// <param name="filePaths">Array of file paths to load</param>
        /// <returns>Array of Image objects</returns>
        public static Image[] LoadImagesFromFiles(string[] filePaths)
        {
            Image[] images = new Image[filePaths.Length];

            for (int i = 0; i < filePaths.Length; i++)
            {
                if (!File.Exists(filePaths[i]))
                {
                    Console.WriteLine($"File not found: {filePaths[i]}");
                    continue;
                }

                if (!SupportedExtensions.Contains(Path.GetExtension(filePaths[i]).ToLower()))
                {
                    Console.WriteLine($"Unsupported file format: {filePaths[i]}");
                    continue;
                }

                try
                {
                    // Load image from file and create a new instance to avoid file locks
                    using (var originalImage = Image.FromFile(filePaths[i]))
                    {
                        images[i] = new Bitmap(originalImage);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading image {filePaths[i]}: {ex.Message}");
                    images[i] = null;
                }
            }

            return images;
        }

        /// <summary>
        /// Disposes all images in the array
        /// </summary>
        /// <param name="images">Array of images to dispose</param>
        public static void DisposeImages(Image[] images)
        {
            if (images == null) return;

            foreach (var image in images)
            {
                image?.Dispose();
            }
        }
    }
}
