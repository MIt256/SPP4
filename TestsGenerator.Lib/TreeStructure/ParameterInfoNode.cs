namespace TestsGenerator.Lib.TreeStructure
{
    public class ParameterInfoNode
    {
        public string Name { get; }
        public TypeInfoNode Type { get; }

        public ParameterInfoNode(string name, TypeInfoNode type)
        {
            Name = name;
            Type = type;
        }
    }
}