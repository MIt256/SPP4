using System;
using System.Collections.Generic;
using SPP4_TestsGenerator.IO;
using TestsGenerator.Lib;

namespace SPP4_TestsGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var readPaths = new List<string>()
            {
                "../../../../TestClasses/BasicTest.cs",
                "../../../../TestClasses/SmartTest.cs"
            };
            
            var config = new TestsGeneratorConfig(AsyncFileStream.ReadFromFile, AsyncFileStream.WriteToFile, 2, 2, 2);
            config.ReadPaths.AddRange(readPaths);
            
            new NUnitTestsGenerator(config).GenerateClasses().Wait();
            Console.WriteLine("Generated");
        }
    }
}