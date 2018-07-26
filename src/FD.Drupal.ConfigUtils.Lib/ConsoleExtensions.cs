using System;

namespace FD.Drupal.ConfigUtils
{
    public static class ConsoleExtensions
    {
        public static void Write(this string message) => Console.Write(message);

        public static void WriteLine(this string message) => Console.WriteLine(message);

        public static void WriteRed(this string message) => message.Write(ConsoleColor.Red);

        public static void WriteLineRed(this string message) => message.WriteLine(ConsoleColor.Red);

        public static void WriteYellow(this string message) => message.Write(ConsoleColor.Yellow);

        public static void WriteLineYellow(this string message) => message.WriteLine(ConsoleColor.Yellow);

        public static void WriteGreen(this string message) => message.Write(ConsoleColor.Green);

        public static void WriteLineGreen(this string message) => message.WriteLine(ConsoleColor.Green);

        public static void WriteCyan(this string message) => message.Write(ConsoleColor.Cyan);

        public static void WriteLineCyan(this string message) => message.WriteLine(ConsoleColor.Cyan);

        public static void WriteWhite(this string message) => message.Write(ConsoleColor.White);

        public static void WriteLineWhite(this string message) => message.WriteLine(ConsoleColor.White);

        public static void Write(this string message, ConsoleColor color) => message.Write(false, color);

        public static void WriteLine(this string message, ConsoleColor color) => message.Write(true, color);

        private static void Write(this string message, bool line, ConsoleColor color)
        {
            ConsoleColor original = Console.ForegroundColor;

            Console.ForegroundColor = color;

            if (line)
                Console.WriteLine(message);
            else
                Console.Write(message);

            Console.ForegroundColor = original;
        }
    }
}