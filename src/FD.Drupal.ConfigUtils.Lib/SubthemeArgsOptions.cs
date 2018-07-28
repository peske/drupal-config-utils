using CommandLine;

namespace FD.Drupal.ConfigUtils
{
    [Verb("subtheme", HelpText = "Copies configuration from theme to its subtheme.")]
    public class SubthemeArgsOptions
    {
        private const char ThemePathOptionShort = 't';

        private const string ThemePathOptionLong = "theme";

        internal const string ThemePathHelpText =
            "Path of the theme directory. If not specified, the user will be prompted for one.";

        private const char SubthemePathOptionShort = 's';

        private const string SubthemePathOptionLong = "subtheme";

        internal const string SubThemeHelpText =
            "Path of the subtheme directory. If not specified, the user will be prompted for one.";

        /// <summary>
        /// Base theme path, specified through a CLI argument.
        /// </summary>
        [Option(ThemePathOptionShort, ThemePathOptionLong,
            Required = false,
            HelpText = ThemePathHelpText)]
        public string ThemePath { get; set; }

        /// <summary>
        /// Subtheme path, specified through a CLI argument.
        /// </summary>
        [Option(SubthemePathOptionShort, SubthemePathOptionLong,
            Required = false,
            HelpText = SubThemeHelpText)]
        public string SubthemePath { get; set; }

    }
}