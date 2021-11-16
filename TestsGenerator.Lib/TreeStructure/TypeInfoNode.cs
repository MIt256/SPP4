using System;

namespace TestsGenerator.Lib.TreeStructure
{
    public class TypeInfoNode
    {
        public string Typename { get; }
        public bool IsInterface { get; }

        public TypeInfoNode(string typeName)
        {
            Typename = typeName;
            IsInterface = typeName.StartsWith("I");
        }
    }
}