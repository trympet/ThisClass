using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ThisClass.Common
{
    internal static class EmbeddedResource
    {
        public static string GetContent(string relativePath)
        {
            var baseDir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            var filePath = Path.Combine(baseDir, Path.GetFileName(relativePath));
            if (File.Exists(filePath))
                return File.ReadAllText(filePath);

            var baseName = Assembly.GetCallingAssembly().GetName().Name;
            var resourceName = relativePath
                .TrimStart('.')
                .Replace(Path.DirectorySeparatorChar, '.')
                .Replace(Path.AltDirectorySeparatorChar, '.');

            var manifestResourceName = Assembly.GetCallingAssembly()
                .GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(resourceName));

            if (string.IsNullOrEmpty(manifestResourceName))
                throw new InvalidOperationException($"Did not find required resource ending in '{resourceName}' in assembly '{baseName}'.");

            using var stream = Assembly.GetCallingAssembly()
                .GetManifestResourceStream(manifestResourceName);

            if (stream == null)
                throw new InvalidOperationException($"Did not find required resource '{manifestResourceName}' in assembly '{baseName}'.");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
