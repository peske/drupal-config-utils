using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;

namespace FD.Drupal.ConfigUtils
{
    public static class FakeProgram
    {
        public static int FakeMain(string[] args)
        {
            bool optsSpecified = args?.Length > 1;

            int exitCode = Parser.Default.ParseArguments<CopyArgsOptions, SubthemeArgsOptions>(args).MapResult(
                (CopyArgsOptions opts) => CopyCommand.Run(optsSpecified ? opts : null),
                (SubthemeArgsOptions opts) => SubthemeCommand.Run(optsSpecified ? opts : null), ArgsErrors);

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
    }
}