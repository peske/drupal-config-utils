using System.Collections.Generic;

namespace FD.Drupal.ConfigUtils
{
    /// <summary>
    /// Represents YAML collections.
    /// </summary>
    public interface IConfigCollection : IConfigNode
    {
        /// <summary>
        /// List of items contained by the collection.
        /// </summary>
        List<IConfigNode> Items { get; }
    }
}