using CommandLine;

namespace FD.Drupal.ConfigUtils
{
    public class ArgsOptions
    {
        #region Option constants

        private const char SourceDirOptionShort = 's';

        private const string SourceDirOptionLong = "source-dir";

        internal const string SourceDirHelpText =
            "Source directory. The directory that contains source configuration files. If not specified, the user will be prompted for one.";

        private const char DestinationDirOptionShort = 'd';

        private const string DestinationDirOptionLong = "dest-dir";

        internal const string DestDirHelpText =
            "Destination directory. The directory where the files will be copied. Can contain exported target site configuration files, or can be empty. If not specified, the user will be prompted for one.";

        private const char MachineNameFilterOptionShort = 'm';

        private const string MachineNameFilterOptionLong = "machine-name-filter";

        internal const string MachineNameFilterHelpText =
            "Machine name filter. Can contain a single machine name, or comma-separated list of machine names.";

        private const char FileNameFilterOptionShort = 'f';

        private const string FileNameFilterOptionLong = "file-name-filter";

        internal const string FileNameFilterHelpText =
            "File name filter. Can contain a single file name, or comma-separated list of file names. File name can contain '*' wildcard (i.e. 'node.type.*').";

        private const char SiteUuidOptionShort = 'u';

        private const string SiteUuidOptionLong = "dest-uuid";

        internal const string SiteUuidHelpText =
            "Target site UUID. Specified value will be used ONLY if the actual value cannot be inferred from the content of the destination directory. If not specified, and the value cannot be inferred, files will be copied without 'uuid'.";

        private const char OverrideDifferentOptionShort = 'o';

        private const string OverrideDifferentOptionLong = "override";

        internal const string OverrideDifferentHelpText =
            "Indicates what to do if the destination directory already contains a file with the same name as one selected for copying. If the value is 'true', the file in the destination directory will be overridden; otherwise duplicate file will be skipped. Defaults to 'false'.";

        #endregion Option constants

        #region Option properties

        /// <summary>
        /// Source directory path, specified through a CLI argument.
        /// </summary>
        [Option(SourceDirOptionShort, SourceDirOptionLong,
            Required = false,
            HelpText = SourceDirHelpText)]
        public string SourceDirectory { get; set; }

        /// <summary>
        /// Destination directory path, specified through a CLI argument.
        /// </summary>
        [Option(DestinationDirOptionShort, DestinationDirOptionLong,
            Required = false,
            HelpText = DestDirHelpText)]
        public string DestDirectory { get; set; }

        /// <summary>
        /// Configuration file filter based on machine names, specified through a CLI argument.
        /// </summary>
        [Option(MachineNameFilterOptionShort, MachineNameFilterOptionLong,
            Required = false,
            HelpText = MachineNameFilterHelpText)]
        public string MachineNameFilter { get; set; }

        /// <summary>
        /// Configuration file filter based on file names, specified through a CLI argument.
        /// </summary>
        [Option(FileNameFilterOptionShort, FileNameFilterOptionLong,
            Required = false,
            HelpText = FileNameFilterHelpText)]
        public string FileNameFilter { get; set; }

        /// <summary>
        /// Site UUID of the target site, specified through a CLI argument.
        /// </summary>
        [Option(SiteUuidOptionShort, SiteUuidOptionLong,
            Required = false,
            HelpText = SiteUuidHelpText)]
        public string SiteUuid { get; set; }

        /// <summary>
        /// Site UUID of the target site, specified through a CLI argument.
        /// </summary>
        [Option(OverrideDifferentOptionShort, OverrideDifferentOptionLong,
            Required = false,
            Default = false, 
            HelpText = OverrideDifferentHelpText)]
        public bool Override { get; set; }

        #endregion Option properties
    }
}