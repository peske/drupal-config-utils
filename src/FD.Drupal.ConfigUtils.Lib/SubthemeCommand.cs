using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FD.Drupal.ConfigUtils
{
    internal static class SubthemeCommand
    {
        private const string ConfigSubdirectoryName = "config";

        private static readonly string[] IgnoreRootNodes = {"_core", "uuid"};

        internal static int Run(SubthemeArgsOptions options)
        {
            ExitCode result = LoadSubthemeConfig(options, out SubthemeOptions config);

            if (result != ExitCode.Success)
                return (int)result;

            DirectoryInfo themeConfigDir =
                new DirectoryInfo(Path.Combine(config.Theme.FullName, ConfigSubdirectoryName));

            if (!themeConfigDir.Exists)
            {
                $"The base theme doesn't contain '{ConfigSubdirectoryName}' directory, so there's nothing to copy."
                    .WriteLineYellow();

                return (int) ExitCode.Success;
            }

            List<ConfigurationFile> themeFiles =
                themeConfigDir.GetConfigurationFiles(c => !IgnoreRootNodes.Contains(c.Name)).ToList();

            if (themeFiles.Count < 1)
            {
                $"The base theme doesn't contain any configuration files in '{ConfigSubdirectoryName}' directory, so there's nothing to copy."
                    .WriteLineYellow();

                return (int) ExitCode.Success;
            }

            string subthemeConfigDir = Path.Combine(config.Subtheme.FullName, ConfigSubdirectoryName);

            int themeConfigDirLength = themeConfigDir.FullName.Length;

            foreach (ConfigurationFile themeFile in themeFiles)
            {
                if (!themeFile.Replace(config.Theme.Name, config.Subtheme.Name))
                {
                    $"'{themeFile.File.FullName}' isn't a theme-specific, so it'll be ignored.".WriteLineYellow();

                    continue;
                }

                // ReSharper disable once PossibleNullReferenceException
                string pathWithin = themeFile.File.Directory.FullName.Substring(themeConfigDirLength);

                if (pathWithin.StartsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                    pathWithin = pathWithin.Length > 1 ? pathWithin.Substring(1) : string.Empty;

                string newDir = Path.Combine(subthemeConfigDir, pathWithin);

                if (!Directory.Exists(newDir))
                {
                    try
                    {
                        Directory.CreateDirectory(newDir);
                    }
                    catch (Exception ex)
                    {
                        $"Exception while trying to create '{newDir}' directory.{Environment.NewLine}{ex}"
                            .WriteLineRed();

                        return (int) ExitCode.IoError;
                    }
                }

                FileInfo destFile = new FileInfo(Path.Combine(newDir, string.Concat(themeFile.Name, ".yml")));

                if (destFile.Exists)
                {
                    $"Destination file '{destFile.FullName}' already exists.".WriteLineRed();

                    return (int) ExitCode.InvalidDestinationDirectory;
                }

                try
                {
                    using (StreamWriter writer = new StreamWriter(destFile.FullName, false))
                        themeFile.Write(writer);
                }
                catch (Exception ex)
                {
                    $"Exception while trying to write file '{destFile.FullName}'.{Environment.NewLine}{ex}"
                        .WriteLineRed();

                    return (int) ExitCode.IoError;
                }
            }

            return (int)ExitCode.Success;
        }

        private static ExitCode LoadSubthemeConfig(SubthemeArgsOptions options, out SubthemeOptions config)
        {
            config = new SubthemeOptions();

            string directory = options?.ThemePath;

            config.Theme = string.IsNullOrEmpty(directory)
                ? InputHelpers.AskForDirectory("Enter the full path of the base theme:")
                : InputHelpers.GetDirectory(directory);

            if (config.Theme == null)
                return ExitCode.InvalidSourceDirectory;

            directory = options?.SubthemePath;

            config.Subtheme = string.IsNullOrEmpty(directory)
                ? InputHelpers.AskForDirectory("Enter the full path of the subtheme:", false)
                : InputHelpers.GetDirectory(directory, false);

            if (config.Subtheme == null)
                return ExitCode.InvalidDestinationDirectory;

            if (string.Equals(config.Theme.FullName, config.Subtheme.FullName, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine();

                "Subtheme directory cannot be the same as the theme directory.".WriteLineRed();

                return ExitCode.InvalidDestinationDirectory;
            }

            return ExitCode.Success;
        }
    }
}