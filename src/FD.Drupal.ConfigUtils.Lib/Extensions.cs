using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CSharpx;
using JetBrains.Annotations;

namespace FD.Drupal.ConfigUtils
{
    /// <summary>
    /// Contains extensions helper methods.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Verifies that <paramref name="parent"/> can be used as parameter of 
        /// <see cref="IConfigNode.Clone"/> method of <paramref name="thisInstance"/>. If the method throws 
        /// an exception, it means that <paramref name="parent"/> cannot be used. Otherwise it can.
        /// </summary>
        internal static void VerifyParent([NotNull] this IConfigNode thisInstance, IConfigNode parent)
        {
            if (parent == null)
            {
                if (thisInstance.Parent == null)
                    return;

                throw new ArgumentException(
                    $"{nameof(parent)} cannot be null when {nameof(IConfigNode.Parent)} property of this instance isn't null.",
                    nameof(parent));
            }

            if (thisInstance.Parent == null)
                throw new ArgumentException(
                    $"{nameof(parent)} has to be null when {nameof(IConfigNode.Parent)} property of this instance is null.",
                    nameof(parent));

            if (!string.Equals(parent.Name, thisInstance.Parent.Name, StringComparison.Ordinal))
                throw new ArgumentException(
                    $"{nameof(IConfigNode.Name)} of {nameof(parent)} instance ({parent.Name}) isn't equal to {nameof(IConfigNode.Name)} of the parent of this instance ({thisInstance.Parent.Name}).",
                    nameof(parent));

            if (parent.IndentLevel != thisInstance.Parent.IndentLevel)
                throw new ArgumentException(
                    $"{nameof(IConfigNode.IndentLevel)} of {nameof(parent)} instance ({parent.IndentLevel}) isn't equal to {nameof(IConfigNode.IndentLevel)} of the parent of this instance ({thisInstance.Parent.IndentLevel}).",
                    nameof(parent));
        }

        /// <summary>
        /// Filters input (calling) enumeration of <see cref="ConfigurationFile"/> instances (<paramref name="files"/>) by
        /// machine names specified in coma-separated list in <paramref name="machineNames"/>, and returns the result.
        /// </summary>
        /// <param name="files">Enumeration of <see cref="ConfigurationFile"/> instances to filter.</param>
        /// <param name="machineNames">Coma-separated list of machine names we want to filter by.</param>
        /// <returns>Enumeration of <see cref="ConfigurationFile"/> instances from the input enumeration, whose machine name
        /// is enlisted in <paramref name="machineNames"/>.</returns>
        /// <seealso cref="FilterByMachineNames(IEnumerable{ConfigurationFile},IEnumerable{string})"/>
        /// <seealso cref="FilterByFileNames(IEnumerable{ConfigurationFile},string)"/>
        /// <seealso cref="FilterByFileNames(IEnumerable{ConfigurationFile},IEnumerable{string})"/>
        internal static IEnumerable<ConfigurationFile> FilterByMachineNames(
            [NotNull] this IEnumerable<ConfigurationFile> files, [NotNull] string machineNames) =>
            files.FilterByMachineNames(machineNames.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries));

        /// <summary>
        /// Equivalent to <see cref="FilterByMachineNames(IEnumerable{ConfigurationFile},string)"/>, but this time acceptable
        /// machine names are provided as an enumeration of <see cref="string"/> values (<paramref name="machineNames"/>.
        /// </summary>
        /// <param name="files">Enumeration of <see cref="ConfigurationFile"/> instances to filter.</param>
        /// <param name="machineNames">Enumeration of machine names we want to filter by.</param>
        /// <inheritdoc cref="FilterByMachineNames(IEnumerable{ConfigurationFile},string)" select="returns" />
        /// <seealso cref="FilterByMachineNames(IEnumerable{ConfigurationFile},string)"/>
        /// <seealso cref="FilterByFileNames(IEnumerable{ConfigurationFile},string)"/>
        /// <seealso cref="FilterByFileNames(IEnumerable{ConfigurationFile},IEnumerable{string})"/>
        internal static IEnumerable<ConfigurationFile> FilterByMachineNames(
            [NotNull] this IEnumerable<ConfigurationFile> files, [NotNull] IEnumerable<string> machineNames)
        {
            IList<string> machineNamesList = machineNames as IList<string> ?? machineNames.ToList();

            return files.Where(f =>
            {
                string machineName = f.MachineName;

                return machineNamesList.Any(mn => string.Equals(machineName, mn, StringComparison.Ordinal));
            });
        }

        /// <summary>
        /// Filters input (calling) enumeration of <see cref="ConfigurationFile"/> instances (<paramref name="files"/>) by
        /// file names specified in coma-separated list in <paramref name="fileNames"/>, and returns the result.
        /// </summary>
        /// <param name="files">Enumeration of <see cref="ConfigurationFile"/> instances to filter.</param>
        /// <param name="fileNames">Coma-separated list of file names we want to filter by. The file names can be specified 
        /// with or without the extension - it'll be ignored anyway. We can use <c>*</c> wildcard in the file names, thus
        /// specifying a range of files. If particular name doesn't contain a wildcard, only exact file name matches will
        /// satisfy. If a wildcard is used, then many actual file names can correspond to a single item from 
        /// <paramref name="fileNames"/>.</param>
        /// <returns>Enumeration of <see cref="ConfigurationFile"/> instances from the input enumeration, whose file name
        /// matches the criteria specified in <paramref name="fileNames"/>.</returns>
        /// <seealso cref="FilterByFileNames(IEnumerable{ConfigurationFile},IEnumerable{string})"/>
        /// <seealso cref="FilterByMachineNames(IEnumerable{ConfigurationFile},string)"/>
        /// <seealso cref="FilterByMachineNames(IEnumerable{ConfigurationFile},IEnumerable{string})"/>
        internal static IEnumerable<ConfigurationFile>
            FilterByFileNames([NotNull] this IEnumerable<ConfigurationFile> files, string fileNames) =>
            files.FilterByFileNames(fileNames.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries));

        /// <summary>
        /// Equivalent to <see cref="FilterByFileNames(IEnumerable{ConfigurationFile},string)"/>, but this time file names
        /// filter is provided as an enumeration of <see cref="string"/> values (<paramref name="fileNames"/>.
        /// </summary>
        /// <param name="files">Enumeration of <see cref="ConfigurationFile"/> instances to filter.</param>
        /// <param name="fileNames">Enumeration of file names we want to filter by. The file names can be specified 
        /// with or without the extension - it'll be ignored anyway. We can use <c>*</c> wildcard in the file names, thus
        /// specifying a range of files. If particular name doesn't contain a wildcard, only exact file name matches will
        /// satisfy. If a wildcard is used, then many actual file names can correspond to a single item from 
        /// <paramref name="fileNames"/>.</param>
        /// <inheritdoc cref="FilterByFileNames(IEnumerable{ConfigurationFile},string)" select="returns" />
        /// <seealso cref="FilterByFileNames(IEnumerable{ConfigurationFile},string)"/>
        /// <seealso cref="FilterByMachineNames(IEnumerable{ConfigurationFile},string)"/>
        /// <seealso cref="FilterByMachineNames(IEnumerable{ConfigurationFile},IEnumerable{string})"/>
        internal static IEnumerable<ConfigurationFile> FilterByFileNames(
            [NotNull] this IEnumerable<ConfigurationFile> files, [NotNull] IEnumerable<string> fileNames)
        {
            List<string> fileNamesList = fileNames.Select(fn =>
                fn.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ? fn.Substring(0, fn.Length - 4) : fn).ToList();

            List<Regex> fileNameRx = fileNamesList.Where(fn => fn.Contains('*')).Select(fn =>
                    new Regex(string.Concat("^", fn.Replace(".", @"\.").Replace("*", ".*"), "$"),
                        RegexOptions.Compiled))
                .ToList();

            return files.Where(cf => fileNamesList.Contains(cf.Name) || fileNameRx.Any(rx => rx.IsMatch(cf.Name)));
        }

        private static readonly string[] ForbiddenDirectories = {".", ".."};

        /// <summary>
        /// Loads and returns all Drupal configuration files from the calling directory (<paramref name="directory"/>).
        /// </summary>
        /// <param name="directory">The directory.</param>
        /// <param name="nodeValid">Predicate function for determining which configuration nodes will be included in the
        /// loaded files (if <paramref name="nodeValid"/> returns <c>true</c>), and which will be ignored. Note that this
        /// function will be used <strong>only for root config nodes</strong> in all the files.</param>
        /// <param name="recursively">Determines if subdirectories of <paramref name="directory"/> should also be searched
        /// for config files or not.</param>
        /// <returns>Enumeration of <see cref="ConfigurationFile"/> instances, each representing one Drupal configuration
        /// file from <paramref name="directory"/>.</returns>
        internal static IEnumerable<ConfigurationFile> GetConfigurationFiles([NotNull] this DirectoryInfo directory,
            Predicate<IConfigNode> nodeValid = null, bool recursively = true)
        {
            IEnumerable<ConfigurationFile> localFiles = directory.GetFiles("*.yml").Select(f =>
                ConfigurationFile.TryLoadFromFile(f, out ConfigurationFile configFile, nodeValid)
                    ? configFile
                    : null).Where(cf => cf != null);

            if (recursively)
                localFiles = localFiles.Union(directory.GetDirectories()
                    .Where(d => !ForbiddenDirectories.Contains(d.Name))
                    // ReSharper disable once RedundantArgumentDefaultValue
                    .SelectMany(d => GetConfigurationFiles(d, nodeValid, true)));

            return localFiles;
        }

        /// <summary>
        /// Returns a full path to configuration settings represented by the calling <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Configuration setting whose path we want to get.</param>
        /// <returns>The path of the configuration setting represented by <paramref name="node"/>. It is in format:
        /// <c>[file.name].setting.path</c>. For example: <c>[views.view.comment].display.default.display_plugin</c>.
        /// Note that full path also supports arrays. One example with an array is: 
        /// <c>[views.view.comment].dependencies.module[0]</c>.</returns>
        internal static string GetPathName([NotNull] this IConfigNode node)
        {
            if (node is ConfigurationFile)
                return $"[{node.Name}]";

            ConfigurationNode parent = node.Parent;

            string pathName = node.Name;

            if (string.Equals(pathName, "-", StringComparison.Ordinal))
                pathName =
                    $"[{parent?.Children?.Index().FirstOrDefault(n => ReferenceEquals(n.Value, node)).Key.ToString("##########") ?? string.Empty}]";

            return parent == null ? pathName : $"{parent.GetPathName()}.{pathName}";
        }

        /// <summary>
        /// Replaces <paramref name="pattern"/> with <paramref name="replacement"/> in all names and values contained
        /// by the calling <paramref name="node"/>, and returns <c>true</c> if anything is replaced, and <c>false</c>
        /// otherwise.
        /// </summary>
        /// <param name="node">Node to modify.</param>
        /// <param name="pattern">Pattern to replace.</param>
        /// <param name="replacement">The replacement.</param>
        /// <returns><c>true</c> if anything is replaced; <c>false</c> otherwise.</returns>
        internal static bool Replace([NotNull] this IConfigNode node, [NotNull] string pattern,
            [NotNull] string replacement)
        {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentNullException($"{nameof(pattern)} is null or empty.", nameof(pattern));

            bool replaced = false;

            switch (node)
            {
                case ConfigurationNode configNode:

                    foreach (IConfigNode child in configNode.Children)
                        if (child.Replace(pattern, replacement))
                            replaced = true;

                    break;

                case SingleValueNode singleValueNode:

                    string value = singleValueNode.Value;

                    if (!string.IsNullOrEmpty(value))
                    {
                        value = value.Replace(pattern, replacement);

                        if (!string.Equals(singleValueNode.Value, value, StringComparison.Ordinal))
                        {
                            singleValueNode.Value = value;

                            replaced = true;
                        }
                    }

                    break;
            }

            string name = node.Name;

            if (!string.IsNullOrEmpty(name))
            {
                name = name.Replace(pattern, replacement);

                if (!string.Equals(name, node.Name, StringComparison.Ordinal))
                {
                    $"{node.GetPathName()} - Node name will be changed to: {name}".WriteLineYellow();

                    node.ChangeName(name);

                    replaced = true;
                }
            }

            return replaced;
        }
    }
}