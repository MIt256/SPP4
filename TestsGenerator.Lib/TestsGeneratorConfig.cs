using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using TestsGenerator.Lib.Template;
using TestsGenerator.Lib.TreeStructure.SyntaxTree;

namespace TestsGenerator.Lib
{
    public class TestsGeneratorConfig
    {
        public int ReadThreadCount { get; }
        public int WriteThreadCount { get; }
        public int ProcessThreadCount { get; }
        public List<string> ReadPaths { get; } = new();
        public ITemplateGenerator TemplateGenerator { get; } = new TemplateGenerator(new SyntaxTreeGenerator());

        private readonly Func<string, Task<string>> _read;
        public Task<string> Read(string path) => _read.Invoke(path);
        
        private readonly Func<string, string, Task> _write;
        public Task Write(string path, string content) => _write.Invoke(path, content);
        
        public TestsGeneratorConfig([NotNull]Func<string, Task<string>> read, [NotNull]Func<string, string, Task> write, int readThreadCount = 1, int writeThreadCount = 1, int processThreadCount = 1)
        {
            _read = read;
            _write = write;
            ReadThreadCount = Math.Max(readThreadCount, 1);
            WriteThreadCount = Math.Max(writeThreadCount, 1);
            ProcessThreadCount = Math.Max(processThreadCount, Environment.ProcessorCount);
        }
    }
}