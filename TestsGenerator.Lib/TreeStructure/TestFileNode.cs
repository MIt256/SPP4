using System.Collections.Generic;

namespace TestsGenerator.Lib.TreeStructure
{
    public class TestFileNode
    {
        public List<string> Usings { get; } = new();
        public List<ClassInfoNode> Classes { get; } = new();
    }
}