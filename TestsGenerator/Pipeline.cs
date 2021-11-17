using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TestsGenerator
{
    public class Pipeline
    {
        private readonly PipelineConfiguration PipelineConfig;

        public Pipeline(PipelineConfiguration pipelineConfig)
        {
            PipelineConfig = pipelineConfig;
        }

        public async Task Execute(List<string> filesPath, string outputPath)
        {
            // Блок чтения из файла
            var readingBlock = new TransformBlock<string, string>(
                async filePath =>
                {
                    Console.WriteLine("File path:" + filePath);
                    using (StreamReader streamReader = new StreamReader(filePath))
                    {
                        return await streamReader.ReadToEndAsync(); // Асинхронно читаем все символы из текущей позиции с возвратом в одну строку
                    }
                },
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = PipelineConfig.MaxReadingTasks } // MaxDegreeOfParallelism - Задает максимальное количество одновременных задач
            );

            // Блок создания теста
            var generateTestClass = new TransformManyBlock<string, TestStructure>(
                async Code =>
                {
                    Console.WriteLine("Generating tests... ");
                    return await TestCreator.Generate(Code);
                },
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = PipelineConfig.MaxProcessingTasks }
            );

            // Блок записи теста в файл
            var writeGeneratedFile = new ActionBlock<TestStructure>(
                async testClass =>
                {
                    string fullpath = Path.Combine(outputPath, testClass.TestName);
                    Console.WriteLine("Fullpath " + fullpath);
                    using (StreamWriter streamWriter = new StreamWriter(fullpath))
                    {
                        await streamWriter.WriteAsync(testClass.TestCode);
                    }

                },
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = PipelineConfig.MaxWritingTasks }
            );

            // Успешное или неудачное завершение одного блока в конвейере приведет к завершению следующего блока в конвейере
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            readingBlock.LinkTo(generateTestClass, linkOptions);
            generateTestClass.LinkTo(writeGeneratedFile, linkOptions);

            foreach (string path in filesPath)
            {
                readingBlock.Post(path);
            }

            // Завершаем pipeline
            readingBlock.Complete();

            await writeGeneratedFile.Completion;
        }
    }
}
