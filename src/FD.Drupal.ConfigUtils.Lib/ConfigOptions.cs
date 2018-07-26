using System;
using System.IO;

namespace FD.Drupal.ConfigUtils
{
    internal class ConfigOptions
    {
        internal DirectoryInfo Source { get; set; }

        internal DirectoryInfo Dest { get; set; }

        internal string MachineNameFilter { get; set; }

        internal string FileNameFilter { get; set; }

        internal Guid? SiteUuid { get; set; }

        internal bool Override { get; set; }
    }
}