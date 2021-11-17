using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TestsGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string FolderPath = "D:\\Test generator\\Results";

            List<string> FilesPath = new List<string>() 
            {
                "D:\\Test generator\\NUnitTests\\TestClasses2.cs",
                "D:\\Test generator\\NUnitTests\\TestClasses.cs"
            };

            Pipeline p = new Pipeline(new PipelineConfiguration(1, 1, 1));
            await p.Execute(FilesPath, FolderPath);
        }
    }
}
