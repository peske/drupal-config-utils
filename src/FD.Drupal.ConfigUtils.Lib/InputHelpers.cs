using System;
using System.IO;

namespace FD.Drupal.ConfigUtils
{
    internal static class InputHelpers
    {
        internal static Guid? AskForSiteUuid(string question)
        {
            string answer = Prompt(question, val => string.IsNullOrEmpty(val) || Guid.TryParse(val, out _));

            return string.IsNullOrEmpty(answer) ? (Guid?)null : Guid.Parse(answer);
        }

        internal static string AskAnyAnswer(string question) => Prompt(question, answer => true);

        internal static bool AskYesNo(string question)
        {
            question = string.Concat(question, " [yes/no]:");

            string answer = Prompt(question, val => TryYesNoToBoolean(val, out _));

            if (TryYesNoToBoolean(answer, out bool yes))
                return yes;

            throw new Exception("Won't happen ever.");
        }

        internal static bool TryYesNoToBoolean(string yesNo, out bool isYes)
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

        internal static DirectoryInfo AskForDirectory(string question, bool onlyExisting = true)
        {
            string directory = Prompt(question, answer => GetDirectory(answer, onlyExisting) != null);

            return new DirectoryInfo(directory);
        }

        internal static DirectoryInfo GetDirectory(string directory, bool onlyExisting = true)
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

            if (!onlyExisting)
                return dir;

            if (dir.Exists)
                return dir;

            $"Directory '{directory}' doesn't exist.".WriteLineRed();

            return null;
        }

        internal static string Prompt(string question, Predicate<string> valid)
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