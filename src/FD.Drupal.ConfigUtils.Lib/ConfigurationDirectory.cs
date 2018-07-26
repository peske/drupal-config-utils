using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace FD.Drupal.ConfigUtils
{
    /// <summary>
    /// Represents a directory which contains site configuration files.
    /// </summary>
    public class ConfigurationDirectory
    {
        private const string CoreExtensionFileName = "core.extension";

        /// <summary>
        /// The directory.
        /// </summary>
        public DirectoryInfo Directory { get; }

        /// <summary>
        /// List of <see cref="ConfigurationFile"/> instances each representing one configuration file
        /// from the directory.
        /// </summary>
        public IReadOnlyList<ConfigurationFile> Files { get; }

        /// <summary>
        /// Item from <see cref="Files"/> which represents <c>core.extension.yml</c> file.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public ConfigurationFile Core_Extension { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="directory">The directory.</param>
        public ConfigurationDirectory(DirectoryInfo directory)
        {
            Directory = directory;

            if (!Directory.Exists)
                throw new DirectoryNotFoundException(directory.FullName);

            List<ConfigurationFile> files = new List<ConfigurationFile>();

            foreach (FileInfo file in Directory.GetFiles("*.yml"))
                if (ConfigurationFile.TryLoadFromFile(file, out ConfigurationFile configuration))
                    files.Add(configuration);

            Files = files.AsReadOnly();

            Core_Extension = Files.FirstOrDefault(f =>
                string.Equals(f.Name, CoreExtensionFileName, StringComparison.Ordinal));
        }

        public IEnumerable<ConfigurationFile> GetDependencies(IEnumerable<ConfigurationFile> files)
        {
            IList<ConfigurationFile> fileList = files as IList<ConfigurationFile> ?? files.ToList();

            List<ConfigurationNode> dependenciesNodes =
                fileList.Select(f => f.DependenciesNode).Where(n => n != null).ToList();

            List<ConfigurationNode> configDependencies = dependenciesNodes
                .Select(n => n.Children.FirstOrDefault(c => string.Equals(c.Name, "config", StringComparison.Ordinal)))
                .Where(n => n != null).Cast<ConfigurationNode>().ToList();

            List<string> dependencies = configDependencies.SelectMany(n => n.Children).Cast<SingleValueNode>()
                .Select(n => n.Value).Distinct()
                .Where(d => fileList.All(f => !string.Equals(d, f.Name, StringComparison.Ordinal))).ToList();

            while (dependencies.Count > 0)
            {
                string dependency = dependencies[0];

                ConfigurationFile dependencyFile =
                    Files.FirstOrDefault(f => string.Equals(f.Name, dependency, StringComparison.Ordinal));

                if (dependencyFile == null)
                    throw new FileNotFoundException($"Dependency file '{dependency}.yml' not found.");

                dependencies.RemoveAt(0);

                List<string> newDependencies = dependencyFile.DependenciesNode?.Children.OfType<ConfigurationNode>()
                    .FirstOrDefault(c => string.Equals(c.Name, "config", StringComparison.Ordinal))?.Children
                    .Cast<SingleValueNode>().Select(n => n.Value).Distinct().Where(d => !dependencies.Contains(d))
                    .ToList();

                if (newDependencies?.Any() ?? false)
                    dependencies.AddRange(newDependencies);

                yield return dependencyFile;
            }
        }

        public bool ModuleThemeDependenciesSatisfied([NotNull] IEnumerable<ConfigurationFile> configurationFiles)
        {
            List<ConfigurationNode> dependencies =
                configurationFiles.Select(f => f.DependenciesNode).Where(n => n != null).ToList();

            List<string> requiredModules = dependencies
                .Select(d =>
                    d.Children.OfType<ConfigurationNode>()
                        .FirstOrDefault(c => string.Equals(c.Name, "module", StringComparison.Ordinal)))
                .Where(n => n != null).SelectMany(en => en.Children).Cast<SingleValueNode>().Select(n => n.Value)
                .Distinct().ToList();

            List<string> requiredThemes = dependencies
                .Select(d =>
                    d.Children.OfType<ConfigurationNode>()
                        .FirstOrDefault(c => string.Equals(c.Name, "theme", StringComparison.Ordinal)))
                .Where(n => n != null).SelectMany(en => en.Children).Cast<SingleValueNode>().Select(n => n.Value)
                .Distinct().ToList();

            Console.WriteLine();
            "Required modules:".WriteYellow();
            $" {string.Join(", ", requiredModules)}".WriteLine();
            Console.WriteLine();
            "Required themes:".WriteYellow();
            $" {string.Join(", ", requiredThemes)}".WriteLine();

            if (Core_Extension == null)
            {
                Console.WriteLine();
                $"'{Directory.FullName}' doesn't contain '{CoreExtensionFileName}.yml' file, so module/theme requirements cannot be checked."
                    .WriteLineYellow();

                return true;
            }

            bool modulesOk = true;

            if (requiredModules.Any())
            {
                List<string> availableModules =
                    ((ConfigurationNode) Core_Extension.Children.FirstOrDefault(c =>
                        string.Equals(c.Name, "module", StringComparison.Ordinal)))?.Children.Select(c => c.Name)
                    .ToList();

                List<string> missingModules = availableModules == null
                    ? requiredModules
                    : requiredModules.Where(rm => !availableModules.Contains(rm)).ToList();

                if (missingModules.Any())
                {
                    modulesOk = false;

                    Console.WriteLine();
                    $"The following modules aren't available: {string.Join(", ", missingModules)}".WriteLineRed();
                }
            }

            if (modulesOk)
            {
                Console.WriteLine();
                "All module requirements satisfied.".WriteLineGreen();
            }

            bool themesOk = true;

            if (requiredThemes.Any())
            {
                List<string> availableThemes =
                    ((ConfigurationNode)Core_Extension.Children.FirstOrDefault(c =>
                        string.Equals(c.Name, "theme", StringComparison.Ordinal)))?.Children.Select(c => c.Name).ToList();

                List<string> missingThemes = availableThemes == null
                    ? requiredThemes
                    : requiredThemes.Where(rm => !availableThemes.Contains(rm)).ToList();

                if (missingThemes.Any())
                {
                    themesOk = false;

                    Console.WriteLine();
                    $"The following themes aren't available: {string.Join(", ", missingThemes)}".WriteLineRed();
                }
            }

            if (themesOk)
            {
                Console.WriteLine();
                "All theme requirements satisfied.".WriteLineGreen();
            }

            return modulesOk && themesOk;
        }
    }
}