namespace TestsGenerator.Lib.TreeStructure.SyntaxTree
{
    public interface ISyntaxTreeGenerator
    {
        TestFileNode Generate(string code);
    }
}