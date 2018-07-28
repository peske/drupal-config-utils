using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FD.Drupal.ConfigUtils
{
    internal static class CopyCommand
    {
        internal static int Run(CopyArgsOptions options)
        {
            ExitCode code = LoadCopyConfig(options, out CopyOptions config);

            if (code != ExitCode.Success)
                return (int)code;

            ConfigurationDirectory source;

            try
            {
                source = new ConfigurationDirectory(config.Source);
            }
            catch (Exception ex)
            {
                $"Exception while trying to load content of the source directory.{Environment.NewLine}{ex}"
                    .WriteLineRed();

                return (int)ExitCode.Unknown;
            }

            $"Content of the source directory loaded successfully. {source.Files.Count} config files loaded."
                .WriteLineGreen();

            ConfigurationDirectory dest;

            try
            {
                dest = new ConfigurationDirectory(config.Dest);
            }
            catch (Exception ex)
            {
                $"Exception while trying to load content of the destination directory.{Environment.NewLine}{ex}"
                    .WriteLineRed();

                return (int)ExitCode.Unknown;
            }

            $"Content of the destination directory loaded successfully. {dest.Files.Count} config files loaded."
                .WriteLineGreen();

            IEnumerable<ConfigurationFile> filesEnum = source.Files;

            if (!string.IsNullOrEmpty(config.FileNameFilter))
                filesEnum = filesEnum.FilterByFileNames(config.FileNameFilter);

            if (!string.IsNullOrEmpty(config.MachineNameFilter))
                filesEnum = filesEnum.FilterByMachineNames(config.MachineNameFilter);

            List<ConfigurationFile> files = filesEnum.ToList();

            if (files.Count < 1)
            {
                "No files are satisfying your filtering criteria.".WriteLineYellow();

                return (int)ExitCode.Success;
            }

            $"{files.Count} files are satisfying your filtering criteria. These are:".WriteLineWhite();

            foreach (ConfigurationFile file in files)
                $"  - {file.File.Name}".WriteLineGreen();

            Console.WriteLine();

            if (!InputHelpers.AskYesNo("Is this OK?"))
                return (int)ExitCode.UserCancelled;

            List<ConfigurationFile> dependencies = source.GetDependencies(files).ToList();

            if (dependencies.Count > 0)
            {
                Console.WriteLine();

                $"{dependencies.Count} additional dependencies detected, and they also will be copied. These are:"
                    .WriteLineWhite();

                foreach (ConfigurationFile file in dependencies)
                    $"  - {file.File.Name}".WriteLineCyan();

                Console.WriteLine();

                if (!InputHelpers.AskYesNo("Is this OK?"))
                    return (int)ExitCode.UserCancelled;

                Console.WriteLine();
            }

            files.AddRange(dependencies);

            if (!dest.ModuleThemeDependenciesSatisfied(files))
                return (int)ExitCode.UnmetDependencies;

            Console.WriteLine();
            "Copying...".WriteLineWhite();
            Console.WriteLine();

            int newFiles = 0;
            int modifiedFiles = 0;
            int unchangedFiles = 0;
            int stillDifferent = 0;

            foreach (ConfigurationFile file in files)
            {
                ConfigurationFile clone = file.Clone();

                FileInfo destFile = new FileInfo(Path.Combine(dest.Directory.FullName, $"{clone.Name}.yml"));

                if (destFile.Exists)
                {
                    ConfigurationFile existingFile =
                        dest.Files.FirstOrDefault(f => string.Equals(f.Name, file.Name, StringComparison.Ordinal));

                    if (clone.EquivalentTo(existingFile))
                    {
                        unchangedFiles++;

                        $"  - '{destFile.Name}' already exists in the destination folder, and it's equivalent. Skipping it."
                            .WriteLine();
                    }
                    else if (config.Override)
                    {
                        try
                        {
                            using (StreamWriter writer = new StreamWriter(destFile.FullName, false))
                                clone.Write(writer);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine();
                            $"Exception while trying to override '{destFile.FullName}'.{Environment.NewLine}{ex}"
                                .WriteLineRed();

                            return (int)ExitCode.IoError;
                        }

                        modifiedFiles++;

                        $"  - '{destFile.Name}' overridden successfully.".WriteLineYellow();
                    }
                    else
                    {
                        stillDifferent++;

                        $"  - '{destFile.Name}' already exists, so skipping it.".WriteLineYellow();
                    }
                }
                else
                {
                    try
                    {
                        using (StreamWriter writer = new StreamWriter(destFile.FullName, false))
                            clone.Write(writer);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();
                        $"Exception while trying to create '{destFile.FullName}'.{Environment.NewLine}{ex}"
                            .WriteLineRed();

                        return (int)ExitCode.IoError;
                    }

                    $"  - '{destFile.Name}' created successfully.".WriteLineGreen();

                    newFiles++;
                }
            }

            Console.WriteLine();
            "Copying finished successfully. Stats:".WriteLineGreen();
            $"  - Newly created files:                             {newFiles}".WriteLineGreen();
            $"  - Modified files:                                  {modifiedFiles}".WriteLineYellow();
            $"  - Unchanged, already present and equivalent files: {unchangedFiles}".WriteLine();
            $"  - Still different (override disabled) files:       {stillDifferent}".WriteLineYellow();

            return (int)ExitCode.Success;
        }

        private static ExitCode LoadCopyConfig(CopyArgsOptions options, out CopyOptions config)
        {
            config = new CopyOptions();

            string directory = options?.SourceDirectory;

            config.Source = string.IsNullOrEmpty(directory)
                ? InputHelpers.AskForExistingDirectory("Enter the full path of the source directory:")
                : InputHelpers.ExistingDirectory(directory);

            if (config.Source == null)
                return ExitCode.InvalidSourceDirectory;

            directory = options?.DestDirectory;

            config.Dest = string.IsNullOrEmpty(directory)
                ? InputHelpers.AskForExistingDirectory("Enter the full path of the destination directory:")
                : InputHelpers.ExistingDirectory(directory);

            if (config.Dest == null)
                return ExitCode.InvalidDestinationDirectory;

            if (string.Equals(config.Source.FullName, config.Dest.FullName, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine();

                "Destination directory cannot be the same as the source directory.".WriteLineRed();

                return ExitCode.InvalidDestinationDirectory;
            }

            config.MachineNameFilter = options == null
                ? InputHelpers.AskAnyAnswer(
                    "Enter machine name filter, or just empty line if you don't need machine name filtering:")
                : options.MachineNameFilter;

            config.FileNameFilter = options == null
                ? InputHelpers.AskAnyAnswer(
                    "Enter file name filter, or just empty line if you don't need file name filtering:")
                : options.FileNameFilter;

            config.Override = options?.Override ??
                              InputHelpers.AskYesNo("Should files from the destination directory be overridden?");

            //if (options == null)
            //    config.SiteUuid =
            //        AskForSiteUuid(
            //            "Enter a backup site UUID that will be used if the UUID cannot be inferred, or just empty line if it isn't needed:");
            //else if (!string.IsNullOrEmpty(options.SiteUuid))
            //{
            //    if (Guid.TryParse(options.SiteUuid, out Guid uuid))
            //        config.SiteUuid = uuid;
            //    else
            //    {
            //        $"Value '{options.SiteUuid}' doesn't have a valid UUID format.".WriteLineRed();

            //        return ExitCode.InvalidArguments;
            //    }
            //}

            return ExitCode.Success;
        }
    }
}