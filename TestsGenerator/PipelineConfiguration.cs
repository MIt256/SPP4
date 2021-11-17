namespace TestsGenerator
{
    public class PipelineConfiguration
    {
        public int MaxReadingTasks { get; } // Ограничение количества файлов, загружаемых за раз

        public int MaxProcessingTasks { get; } // Ограничение максимального количества одновременно обрабатываемых задач

        public int MaxWritingTasks { get; } // Ограничение количества одновременно записываемых файлов

        public PipelineConfiguration(int maxReadingTasks, int maxProcessingTasks, int maxWritingTasks)
        {
            MaxReadingTasks = maxReadingTasks;
            MaxProcessingTasks = maxProcessingTasks;
            MaxWritingTasks = maxWritingTasks;
        }
    }
}
