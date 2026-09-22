using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Abc.Unity.Editor
{
    internal static class ActorFeatureWriter
    {
        private static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while", "__arglist", "__makeref", "__reftype", "__refvalue"
        };

        public static string Validate(string projectRoot, string folder, string feature, string namespaceName,
            IReadOnlyList<ActorFeatureFile> files)
        {
            if (files == null || files.Count == 0)
                return "Select at least one feature module.";
            if (!IsIdentifier(feature))
                return "Feature Name must be a valid C# identifier.";
            var defaultsError = ValidateDefaults(folder, namespaceName);
            if (defaultsError != null)
                return defaultsError;

            try
            {
                var output = GetOutputPath(projectRoot, folder);
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < files.Count; i++)
                {
                    var name = files[i].Name;
                    if (name != Path.GetFileName(name) || !name.EndsWith(".cs", StringComparison.Ordinal) || !names.Add(name))
                        return "Generated file names must be unique C# source names.";

                    var path = Path.Combine(output, name);
                    if (File.Exists(path) || Directory.Exists(path) || File.Exists(path + ".meta"))
                        return $"{folder}/{name} already exists. Existing assets are never overwritten.";
                }
            }
            catch (Exception exception) when (exception is ArgumentException || exception is IOException || exception is UnauthorizedAccessException)
            {
                return exception.Message;
            }

            return null;
        }

        internal static string ValidateDefaults(string folder, string namespaceName)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                return "Namespace is required.";

            foreach (var part in namespaceName.Split('.'))
            {
                if (!IsIdentifier(part))
                    return "Namespace must contain valid dot-separated C# identifiers.";
            }

            try
            {
                ValidateFolder(folder);
                return null;
            }
            catch (ArgumentException exception)
            {
                return exception.Message;
            }
        }

        public static void Write(string projectRoot, string folder, string feature, string namespaceName,
            IReadOnlyList<ActorFeatureFile> files)
        {
            var error = Validate(projectRoot, folder, feature, namespaceName, files);
            if (error != null)
                throw new InvalidOperationException(error);

            var output = GetOutputPath(projectRoot, folder);
            Directory.CreateDirectory(output);
            var created = new List<string>(files.Count);
            try
            {
                for (var i = 0; i < files.Count; i++)
                {
                    var path = Path.Combine(output, files[i].Name);
                    using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    created.Add(path);
                    using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                    writer.Write(files[i].Content);
                }
            }
            catch
            {
                for (var i = created.Count - 1; i >= 0; i--)
                    File.Delete(created[i]);
                throw;
            }
        }

        private static string GetOutputPath(string projectRoot, string folder)
        {
            var normalized = ValidateFolder(folder);
            var output = Path.GetFullPath(Path.Combine(projectRoot, normalized));
            for (var directory = new DirectoryInfo(output); directory != null; directory = directory.Parent)
            {
                if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Output Folder cannot pass through a symbolic link or junction.");
            }

            return output;
        }

        private static string ValidateFolder(string folder)
        {
            var normalized = (folder ?? string.Empty).Replace('\\', '/').Trim().TrimEnd('/');
            if (normalized != "Assets" && !normalized.StartsWith("Assets/", StringComparison.Ordinal))
                throw new ArgumentException("Output Folder must be inside Assets.");

            foreach (var part in normalized.Split('/'))
            {
                if (part == ".." || part == "." || part.Length == 0 || part.IndexOf(':') >= 0)
                    throw new ArgumentException("Output Folder cannot contain traversal or empty path segments.");
            }

            return normalized;
        }

        private static bool IsIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || Keywords.Contains(value))
                return false;
            if (value[0] != '_' && !char.IsLetter(value[0]))
                return false;

            for (var i = 1; i < value.Length; i++)
            {
                if (value[i] != '_' && !char.IsLetterOrDigit(value[i]))
                    return false;
            }

            return true;
        }
    }
}
