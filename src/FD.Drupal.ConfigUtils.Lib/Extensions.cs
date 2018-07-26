using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        internal static IEnumerable<ConfigurationFile> FilterByMachineNames(
            [NotNull] this IEnumerable<ConfigurationFile> files, [NotNull] string machineNames) =>
            files.FilterByMachineNames(machineNames.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries));

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

        internal static IEnumerable<ConfigurationFile>
            FilterByFileNames([NotNull] this IEnumerable<ConfigurationFile> files, string fileNames) =>
            files.FilterByFileNames(fileNames.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries));

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
    }
}