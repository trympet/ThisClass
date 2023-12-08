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
            var callingAssembly = Assembly.GetCallingAssembly();
            var resourceName = relativePath
                .TrimStart('.')
                .Replace(Path.DirectorySeparatorChar, '.')
                .Replace(Path.AltDirectorySeparatorChar, '.');

            var manifestResourceName = callingAssembly.GetManifestResourceNames()
                .FirstOrDefault(x => x.EndsWith(resourceName));

            if (string.IsNullOrEmpty(manifestResourceName))
                throw new InvalidOperationException($"Did not find required resource ending in '{resourceName}' in assembly '{callingAssembly}'.");

            using var stream = callingAssembly.GetManifestResourceStream(manifestResourceName);

            if (stream == null)
                throw new InvalidOperationException($"Did not find required resource '{manifestResourceName}' in assembly '{callingAssembly}'.");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
