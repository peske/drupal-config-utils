using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;

namespace FD.Drupal.ConfigUtils
{
    public static class FakeProgram
    {
        public static int FakeMain(string[] args)
        {
            bool argsSpecified = args?.Any() ?? false;

            int exitCode = argsSpecified
                ? Parser.Default.ParseArguments<ArgsOptions>(args).MapResult(Run, ArgsErrors)
                : Run(null);

            Console.WriteLine();

            if (exitCode == 0)
                "Executed successfully.".WriteLineGreen();
            else
                $"Execution failed with exit code {(ExitCode) exitCode}.".WriteLineRed();

            //if (!argsSpecified)
            {
                Console.WriteLine();

                "Press [Enter] to exit.".WriteLine();

                Console.ReadLine();
            }

            return exitCode;
        }

        private static int ArgsErrors(IEnumerable<Error> errors)
        {
            IList<Error> errorsList = errors as IList<Error> ?? errors.ToList();

            List<Error> notErrors = errorsList.Where(error =>
                error.Tag == ErrorType.HelpRequestedError || error.Tag == ErrorType.HelpVerbRequestedError ||
                error.Tag == ErrorType.VersionRequestedError).ToList();

            List<Error> otherErrors = errorsList.Where(error => !notErrors.Contains(error)).ToList();

            return otherErrors.Any() ? (int) ExitCode.InvalidArguments : (int) ExitCode.Success;
        }

        private static int Run(ArgsOptions options)
        {
            ExitCode code = LoadConfig(options, out ConfigOptions config);

            if (code != ExitCode.Success)
                return (int) code;

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

                return (int) ExitCode.Success;
            }

            $"{files.Count} files are satisfying your filtering criteria. These are:".WriteLineWhite();

            foreach (ConfigurationFile file in files)
                $"  - {file.File.Name}".WriteLineGreen();

            Console.WriteLine();

            if (!AskYesNo("Is this OK?"))
                return (int) ExitCode.UserCancelled;

            List<ConfigurationFile> dependencies = source.GetDependencies(files).ToList();

            if (dependencies.Count > 0)
            {
                Console.WriteLine();

                $"{dependencies.Count} additional dependencies detected, and they also will be copied. These are:"
                    .WriteLineWhite();

                foreach (ConfigurationFile file in dependencies)
                    $"  - {file.File.Name}".WriteLineCyan();

                Console.WriteLine();

                if (!AskYesNo("Is this OK?"))
                    return (int)ExitCode.UserCancelled;

                Console.WriteLine();
            }

            files.AddRange(dependencies);

            if (!dest.ModuleThemeDependenciesSatisfied(files))
                return (int) ExitCode.UnmetDependencies;

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

                            return (int) ExitCode.IoError;
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

            return (int) ExitCode.Success;
        }

        private static ExitCode LoadConfig(ArgsOptions options, out ConfigOptions config)
        {
            config = new ConfigOptions();

            string directory = options?.SourceDirectory;

            config.Source = string.IsNullOrEmpty(directory)
                ? AskForExistingDirectory("Enter the full path of the source directory:")
                : ExistingDirectory(directory);

            if (config.Source == null)
                return ExitCode.InvalidSourceDirectory;

            directory = options?.DestDirectory;

            config.Dest = string.IsNullOrEmpty(directory)
                ? AskForExistingDirectory("Enter the full path of the destination directory:")
                : ExistingDirectory(directory);

            if (config.Dest == null)
                return ExitCode.InvalidDestinationDirectory;

            config.MachineNameFilter = options == null
                ? AskAnyAnswer(
                    "Enter machine name filter, or just empty line if you don't need machine name filtering:")
                : options.MachineNameFilter;

            config.FileNameFilter = options == null
                ? AskAnyAnswer(
                    "Enter file name filter, or just empty line if you don't need file name filtering:")
                : options.FileNameFilter;

            config.Override = options?.Override ??
                              AskYesNo("Should files from the destination directory be overridden?");

            if (options == null)
                config.SiteUuid =
                    AskForSiteUuid(
                        "Enter a backup site UUID that will be used if the UUID cannot be inferred, or just empty line if it isn't needed:");
            else if (!string.IsNullOrEmpty(options.SiteUuid))
            {
                if (Guid.TryParse(options.SiteUuid, out Guid uuid))
                    config.SiteUuid = uuid;
                else
                {
                    $"Value '{options.SiteUuid}' doesn't have a valid UUID format.".WriteLineRed();

                    return ExitCode.InvalidArguments;
                }
            }

            return ExitCode.Success;
        }

        private static Guid? AskForSiteUuid(string question)
        {
            string answer = Prompt(question, val => string.IsNullOrEmpty(val) || Guid.TryParse(val, out _));

            return string.IsNullOrEmpty(answer) ? (Guid?) null : Guid.Parse(answer);
        }

        private static string AskAnyAnswer(string question) => Prompt(question, answer => true);

        private static bool AskYesNo(string question)
        {
            question = string.Concat(question, " [yes/no]:");

            string answer = Prompt(question, val => TryYesNoToBoolean(val, out _));

            if (TryYesNoToBoolean(answer, out bool yes))
                return yes;

            throw new Exception("Won't happen ever.");
        }

        private static bool TryYesNoToBoolean(string yesNo, out bool isYes)
        {
            yesNo = yesNo.Trim().ToLowerInvariant();

            switch (yesNo)
            {
                case "yes":
                case "y":

                    isYes = true;

                    return true;

                case "no":
                case "n":

                    isYes = false;

                    return true;

                default:

                    $"'{yesNo}' isn't a valid answer. Valid answers are: 'yes', 'y', 'no' and 'n'.".WriteLineRed();

                    isYes = false;

                    return false;
            }
        }

        private static DirectoryInfo AskForExistingDirectory(string question)
        {
            string directory = Prompt(question, answer => ExistingDirectory(answer) != null);

            return new DirectoryInfo(directory);
        }

        private static DirectoryInfo ExistingDirectory(string directory)
        {
            DirectoryInfo dir;

            try
            {
                dir = new DirectoryInfo(directory);
            }
            catch
            {
                $"'{directory}' isn't a valid directory path.".WriteLineRed();

                return null;
            }

            if (dir.Exists)
                return dir;

            $"Directory '{directory}' doesn't exist.".WriteLineRed();

            return null;
        }

        private static string Prompt(string question, Predicate<string> valid)
        {
            question.WriteLine();

            while (true)
            {
                string answer = Console.ReadLine();

                if (valid(answer))
                    return answer;

                "Please try again:".WriteLineYellow();
            }
        }
    }
}