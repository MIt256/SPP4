using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace SPP4_TestsGenerator.IO
{
    public static class AsyncFileStream
    {
        public static async Task<string> ReadFromFile([DisallowNull] string path)
        {
            using var reader = new StreamReader(path);
            return await reader.ReadToEndAsync();
        }
        
        public static async Task WriteToFile([DisallowNull]string relativePath, [DisallowNull]string content)
        {
            const string directory = "../../../../GeneratedClasses";
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var fullPath = Path.Combine(directory, relativePath);
            await using var writer = new StreamWriter(fullPath);
            await writer.WriteAsync(content);
        }
    }
}