using System;
using System.Collections.Generic;

namespace TestsGenerator.Lib.TreeStructure
{
    public class ClassInfoNode
    {
        public string Namespace { get; }
        public string Name { get; }
        public ConstructorInfoNode Constructor { get; }
        public List<MethodInfoNode> Methods { get; } = new();

        public ClassInfoNode(string name, string namespaceValue, ConstructorInfoNode constructorInfo)
        {
            Name = name;
            Namespace = namespaceValue;
            Constructor = constructorInfo;
        }
    }
}