using System.Collections.Generic;

namespace Abc.Unity.Editor
{
    internal readonly struct ActorFeatureFile
    {
        public ActorFeatureFile(string name, string content)
        {
            Name = name;
            Content = content;
        }

        public string Name { get; }
        public string Content { get; }
    }

    internal static class ActorFeatureTemplates
    {
        public static List<ActorFeatureFile> Build(
            string featureName,
            string namespaceName,
            bool createData,
            bool createBehaviour,
            bool createCommand,
            bool createQueryAction,
            bool createProviders)
        {
            var files = new List<ActorFeatureFile>(6);

            if (createData)
            {
                files.Add(new ActorFeatureFile(
                    $"{featureName}Data.cs",
                    CreateData(featureName, namespaceName)));

                if (createProviders)
                {
                    files.Add(new ActorFeatureFile(
                        $"{featureName}DataProvider.cs",
                        CreateDataProvider(featureName, namespaceName)));
                }
            }

            if (createBehaviour)
            {
                files.Add(new ActorFeatureFile(
                    $"{featureName}Behaviour.cs",
                    CreateBehaviour(featureName, namespaceName, createData, createCommand)));

                if (createProviders)
                {
                    files.Add(new ActorFeatureFile(
                        $"{featureName}BehaviourProvider.cs",
                        CreateBehaviourProvider(featureName, namespaceName)));
                }
            }

            if (createCommand)
            {
                files.Add(new ActorFeatureFile(
                    $"{featureName}Command.cs",
                    CreateCommand(featureName, namespaceName)));
            }

            if (createQueryAction && createData)
            {
                files.Add(new ActorFeatureFile(
                    $"{featureName}Action.cs",
                    CreateQueryAction(featureName, namespaceName)));
            }

            return files;
        }

        private static string CreateData(string featureName, string namespaceName) =>
            $"using System;\n\n" +
            $"using Abc.Unity;\n\n" +
            $"namespace {namespaceName}\n" +
            "{\n" +
            "    [Serializable]\n" +
            $"    public sealed class {featureName}Data : IActorData\n" +
            "    {\n" +
            "    }\n" +
            "}\n";

        private static string CreateDataProvider(string featureName, string namespaceName) =>
            "using Abc.Unity;\n\n" +
            $"namespace {namespaceName}\n" +
            "{\n" +
            $"    public sealed class {featureName}DataProvider : AbstractActorDataProvider<{featureName}Data>\n" +
            "    {\n" +
            "    }\n" +
            "}\n";

        private static string CreateBehaviour(
            string featureName,
            string namespaceName,
            bool hasData,
            bool handlesCommand)
        {
            var interfaces = handlesCommand
                ? $"IActorBehaviour, IActorCommandListener<{featureName}Command>"
                : "IActorBehaviour";
            var lines = new List<string>(24)
            {
                "using System;",
                string.Empty,
                "using Abc.Unity;",
                string.Empty,
                $"namespace {namespaceName}",
                "{",
                "    [Serializable]",
                $"    public sealed class {featureName}Behaviour : {interfaces}",
                "    {"
            };

            if (hasData)
            {
                lines.Add($"        private {featureName}Data Data {{ get; set; }}");
                lines.Add(string.Empty);
            }

            lines.Add("        public IActor Owner { get; set; }");

            if (hasData)
            {
                lines.Add(string.Empty);
                lines.Add($"        public void Initialize() => Data = Owner.GetData<{featureName}Data>();");
            }

            if (handlesCommand)
            {
                lines.Add(string.Empty);
                lines.Add($"        public void ReactActorCommand({featureName}Command command)");
                lines.Add("        {");
                lines.Add("        }");
            }

            if (hasData)
            {
                lines.Add(string.Empty);
                lines.Add("        public void CleanUp() => Data = null;");
            }

            lines.Add("    }");
            lines.Add("}");
            return string.Join("\n", lines) + "\n";
        }

        private static string CreateBehaviourProvider(string featureName, string namespaceName) =>
            "using Abc.Unity;\n\n" +
            $"namespace {namespaceName}\n" +
            "{\n" +
            $"    public sealed class {featureName}BehaviourProvider : AbstractActorBehaviourProvider<{featureName}Behaviour>\n" +
            "    {\n" +
            "    }\n" +
            "}\n";

        private static string CreateCommand(string featureName, string namespaceName) =>
            "using Abc.Unity;\n\n" +
            $"namespace {namespaceName}\n" +
            "{\n" +
            $"    public readonly struct {featureName}Command : IActorCommand\n" +
            "    {\n" +
            "    }\n" +
            "}\n";

        private static string CreateQueryAction(string featureName, string namespaceName) =>
            "using Abc.Unity;\n\n" +
            $"namespace {namespaceName}\n" +
            "{\n" +
            $"    public struct {featureName}Action : IActorQueryAction<{featureName}Data>\n" +
            "    {\n" +
            $"        public void Execute(ActorModel actor, {featureName}Data data)\n" +
            "        {\n" +
            "        }\n" +
            "    }\n" +
            "}\n";
    }
}
