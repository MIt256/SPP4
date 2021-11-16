using System.Collections.Generic;
using TestsGenerator.Lib.TreeStructure;

namespace TestsGenerator.Lib.Template
{
    public interface ITemplateGenerator
    {
        public IEnumerable<KeyValuePair<string, string>> Generate(string source);
    }
}