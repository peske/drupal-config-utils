using System;
using System.IO;
using JetBrains.Annotations;

namespace FD.Drupal.ConfigUtils
{
    /// <summary>
    /// Represents a single-value (key-value) node.
    /// </summary>
    public class SingleValueNode : IConfigNode
    {
        /// <inheritdoc />
        [NotNull]
        public ConfigurationNode Parent { get; }

        /// <inheritdoc />
        public ushort IndentLevel { get; }

        /// <inheritdoc />
        public string Name { get; private set; }

        /// <summary>
        /// The value.
        /// </summary>
        public string Value { get; internal set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent">To be assigned to <see cref="Parent"/> property.</param>
        /// <param name="name">To be assigned to <see cref="Name"/> property.</param>
        /// <param name="value">To be assigned to <see cref="Value"/> property.</param>
        internal SingleValueNode([NotNull] ConfigurationNode parent, [NotNull] string name, [NotNull] string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException($"{nameof(name)} is null or empty.");

            Parent = parent;
            IndentLevel = parent is ConfigurationFile ? (ushort) 0 : (ushort) (parent.IndentLevel + 1);
            Name = name;
            Value = value;
        }

        /// <inheritdoc />
        public bool EquivalentTo(IConfigNode other) => other is SingleValueNode singleValueOther &&
                                                       string.Equals(Name, other.Name, StringComparison.Ordinal) &&
                                                       string.Equals(Value, singleValueOther.Value,
                                                           StringComparison.Ordinal);

        /// <inheritdoc />
        public IConfigNode Clone(ConfigurationNode parent)
        {
            this.VerifyParent(parent);

            return new SingleValueNode(parent, Name, Value);
        }

        /// <inheritdoc />
        public void Write(StreamWriter writer)
        {
            string indent = new String(' ', IndentLevel * ConfigurationFile.NumberOfSpacesPerIndentLevel);

            string line = string.Equals(Name, "-", StringComparison.Ordinal)
                ? $"{indent}- {Value}"
                : $"{indent}{Name}: {Value}";

            writer.WriteLine(line);
        }

        /// <inheritdoc />
        public void ChangeName(string newName)
        {
            // ReSharper disable once ConstantConditionalAccessQualifier
            newName = newName?.Trim();

            if (string.IsNullOrEmpty(newName))
                throw new ArgumentNullException($"{nameof(newName)} is null or empty.", nameof(newName));

            if (string.Equals(Name, "-", StringComparison.Ordinal))
                throw new InvalidOperationException("If the name is '-', it cannot be changed.");

            Name = newName;
        }
    }
}