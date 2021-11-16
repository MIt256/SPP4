using System.Collections.Generic;

namespace TestsGenerator.Lib.TreeStructure
{
    public class MethodInfoNode
    {
        public string Name { get; }
        public TypeInfoNode ReturnType { get; }
        public List<ParameterInfoNode> Parameters { get; } = new();

        public MethodInfoNode(string name, TypeInfoNode returnType = null)
        {
            Name = name;
            ReturnType = returnType;
        }
    }
}