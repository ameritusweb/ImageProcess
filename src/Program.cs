namespace ImageProcess
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var processor = new BatchProcessor(
                catPath: @"E:\PetImages\Cat",
                dogPath: @"E:\PetImages\Dog",
                outputPath: @"E:\PetImages\Processed",
                desiredBatchSize: 100,
                randomSeed: 5
            );

            processor.ProcessAllImages();
        }
    }
}
