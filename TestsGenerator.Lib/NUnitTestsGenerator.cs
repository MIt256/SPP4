using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TestsGenerator.Lib
{
    public class NUnitTestsGenerator:ITestsGenerator
    {
        private readonly TestsGeneratorConfig _config;

        public NUnitTestsGenerator([NotNull]TestsGeneratorConfig config)
        {
            _config = config;
        }

        public async Task GenerateClasses()
        {
            var linkOptions = new DataflowLinkOptions() { PropagateCompletion = true };
            var readOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = _config.ReadThreadCount };
            var writeOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = _config.WriteThreadCount};
            var processOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = _config.ProcessThreadCount };
            
            var readTransform = new TransformBlock<string, Task<string>>(readPath => _config.Read(readPath), readOptions);
            var sourceToTestFileTransform = new TransformManyBlock<Task<string>, KeyValuePair<string, string>>(readSourceTask => _config.TemplateGenerator.Generate(readSourceTask.Result), processOptions);
            var writeAction = new ActionBlock<KeyValuePair<string, string>>(pathTextPair => _config.Write(pathTextPair.Key, pathTextPair.Value).Wait(), writeOptions);
        
            readTransform.LinkTo(sourceToTestFileTransform, linkOptions);
            sourceToTestFileTransform.LinkTo(writeAction, linkOptions);
        
            foreach (var readPath in _config.ReadPaths)
            {
                await readTransform.SendAsync(readPath);
            }
        
            readTransform.Complete();
            await writeAction.Completion;
        }
        
    }
}