using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess
{
    public class BatchProcessor
    {
        private readonly string catPath;
        private readonly string dogPath;
        private readonly string outputPath;
        private readonly int desiredBatchSize;
        private readonly Random random;

        public BatchProcessor(string catPath, string dogPath, string outputPath, int desiredBatchSize, int randomSeed = 5)
        {
            this.catPath = catPath;
            this.dogPath = dogPath;
            this.outputPath = outputPath;
            this.desiredBatchSize = desiredBatchSize;
            this.random = new Random(randomSeed);
        }

        public void ProcessAllImages()
        {
            // Get total count of images in each directory
            int totalCatImages = Directory.GetFiles(catPath).Length;
            int totalDogImages = Directory.GetFiles(dogPath).Length;

            // Calculate N by dividing total images by desired batch size
            int N = (totalCatImages + totalDogImages) / (2 * desiredBatchSize);

            // Calculate how many images to take in each batch
            int catsPerBatch = totalCatImages / N;
            int dogsPerBatch = totalDogImages / N;

            LogBatchInfo(totalCatImages, totalDogImages, N, catsPerBatch, dogsPerBatch);

            // Process in batches
            for (int batchStart = 0; batchStart < N; batchStart++)
            {
                ProcessBatch(batchStart, N, catsPerBatch, dogsPerBatch);
            }
        }

        private void ProcessBatch(int batchStart, int totalBatches, int catsPerBatch, int dogsPerBatch)
        {
            var catImages = ImageLoader.LoadImagesFromFolder(catPath, batchStart * catsPerBatch, catsPerBatch);
            var dogImages = ImageLoader.LoadImagesFromFolder(dogPath, batchStart * dogsPerBatch, dogsPerBatch);

            var images = catImages.Concat(dogImages).OrderBy(x => random.NextDouble()).ToList();

            try
            {
                ProcessImages(images);
            }
            finally
            {
                ImageLoader.DisposeImages(images.ToArray());
            }

            Console.WriteLine($"Processed batch {batchStart + 1} of {totalBatches}");
        }

        private void ProcessImages(List<Image> images)
        {
            for (int i = 0; i < images.Count; i += 4)
            {
                var (processedImage, json) = ImageTiler.CreateTiledImageWithJson(
                    images.Skip(i).Take(4).ToArray(),
                    200,
                    200,
                    0.01d);

                var guid = Guid.NewGuid();

                var bmp = processedImage.ToBitmap();
                bmp.Save(Path.Combine(outputPath, $"{guid}.bmp"));

                var jsonData = json.ToJsonString();
                File.WriteAllText(
                    Path.Combine(outputPath, $"{guid}.json"),
                    jsonData);

                processedImage.Dispose();
            }
        }

        private void LogBatchInfo(int totalCatImages, int totalDogImages, int N, int catsPerBatch, int dogsPerBatch)
        {
            Console.WriteLine($"Total images: Cats={totalCatImages}, Dogs={totalDogImages}");
            Console.WriteLine($"Calculated N={N}, will take {catsPerBatch} cats and {dogsPerBatch} dogs per batch");
            Console.WriteLine($"Average batch size will be {(catsPerBatch + dogsPerBatch) / 2}");
        }
    }
}