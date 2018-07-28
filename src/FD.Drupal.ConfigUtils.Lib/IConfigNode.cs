using System.IO;
using JetBrains.Annotations;

namespace FD.Drupal.ConfigUtils
{
    /// <summary>
    /// Defines config node.
    /// </summary>
    public interface IConfigNode
    {
        /// <summary>
        /// Parent node.
        /// </summary>
        ConfigurationNode Parent { get; }

        /// <summary>
        /// Indentation level. It's always equals to the same property of <see cref="Parent"/>
        /// instance, increased by one.
        /// </summary>
        ushort IndentLevel { get; }

        /// <summary>
        /// Node name.
        /// </summary>
        [NotNull]
        string Name { get; }

        /// <summary>
        /// Checks if this node is <em>equivalent to</em> <paramref name="other"/> node.
        /// </summary>
        /// <remarks>
        /// Two nodes are considered <em>equivalent</em> if they represent the same exact Drupal
        /// settings. It means that order of child nodes / values isn't important.
        /// </remarks>
        /// <param name="other">Node to compare against for equivalence.</param>
        /// <returns><c>true</c> if this node is <em>equivalent</em> to <paramref name="other"/>;
        /// <c>false</c> otherwise.</returns>
        bool EquivalentTo(IConfigNode other);

        /// <summary>
        /// Creates and returns clone of this instance, with its <see cref="Parent"/> property set
        /// to <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">Parent of the newly created instance. If <see cref="Parent"/>
        /// property of this instance is <c>null</c>, then <paramref name="parent"/> also has to be
        /// <c>null</c>. Otherwise it cannot be <c>null</c>, and it's <see cref="Name"/> and 
        /// <see cref="IndentLevel"/> property values has to be equal to the corresponding properties
        /// of this instance.</param>
        /// <returns>Created clone.</returns>
        /// <exception cref="System.ArgumentException">If <paramref name="parent"/> doesn't satisfy
        /// criteria described in "Remarks" section.</exception>
        IConfigNode Clone(ConfigurationNode parent);

        /// <summary>
        /// Writes YAML format output to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">Writer to write to.</param>
        void Write(StreamWriter writer);

        /// <summary>
        /// Sets a new value to <see cref="Name"/> property.
        /// </summary>
        /// <param name="newName">Value to set to <see cref="Name"/> property.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="newName"/> is <c>null</c> or
        /// empty.</exception>
        /// <exception cref="System.InvalidOperationException">If the old name is <c>"-"</c> (meaning that 
        /// this instance is an array item.</exception>
        void ChangeName([NotNull] string newName);
    }
}