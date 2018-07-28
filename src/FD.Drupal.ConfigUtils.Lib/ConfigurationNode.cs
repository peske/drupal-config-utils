using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace FD.Drupal.ConfigUtils
{
    /// <summary>
    /// Represents a configuration node that can contain child nodes.
    /// </summary>
    public class ConfigurationNode : IConfigNode
    {
        #region Static

        private static readonly Regex RxNameNode =
            new Regex(@"^(?<name>[^\s:]+):$", RegexOptions.Compiled);

        private static readonly Regex RxSingleValueNode =
            new Regex(@"^(?<name>[^\s:]+):\s*(?<value>\S(.*\S)?)\s*$", RegexOptions.Compiled);

        private static readonly Regex RxEmptyArrayNode =
            new Regex(@"^(?<name>[^\s:]+):\s*\{\s*\}\s*$", RegexOptions.Compiled);

        private static IConfigNode LoadNode([NotNull] ConfigurationNode parent, ushort indentSpaces, string content,
            [NotNull] ConfigFileReader reader)
        {
            Match match = RxEmptyArrayNode.Match(content);

            if (match.Success)
                return new ConfigurationNode(parent, match.Groups["name"].Value, indentSpaces, reader,
                    isEmptyArray: true);

            match = RxSingleValueNode.Match(content);

            if (match.Success)
                return new SingleValueNode(parent, match.Groups["name"].Value, match.Groups["value"].Value);

            match = RxNameNode.Match(content);

            if (match.Success)
                return new ConfigurationNode(parent, match.Groups["name"].Value, indentSpaces, reader);

            if (string.Equals(content, "-", StringComparison.Ordinal))
                return new ConfigurationNode(parent, "-", indentSpaces, reader);

            if (content.StartsWith("- ", StringComparison.Ordinal) && content.Length > 2)
                return new SingleValueNode(parent, "-", content.Substring(1).Trim());

            throw new Exception($"Line content cannot be parsed: {content}");
        }

        private static IEnumerable<IConfigNode> LoadChildren(ConfigurationNode parent, ushort indentSpaces,
            ConfigFileReader reader, Predicate<IConfigNode> validNode)
        {
            if (reader.EoF)
                yield break;

            ushort requiredIndent = (ushort) reader.CurrentLineIndentSpaces;

            if (requiredIndent < indentSpaces)
                yield break;

            if (requiredIndent == indentSpaces && !(parent is ConfigurationFile))
                yield break;

            while (!reader.EoF)
            {
                if (reader.CurrentLineIndentSpaces != requiredIndent)
                    yield break;

                string content = reader.CurrentLineContent;

                reader.NextLine();

                IConfigNode node = LoadNode(parent, requiredIndent, content, reader);

                if (validNode?.Invoke(node) ?? true)
                    yield return node;
            }
        }

        #endregion Static

        /// <inheritdoc />
        public ConfigurationNode Parent { get; }

        /// <inheritdoc />
        public ushort IndentLevel { get; }

        /// <inheritdoc />
        public string Name { get; private set; }

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly List<IConfigNode> _children;

        /// <summary>
        /// List of child nodes.
        /// </summary>
        public IReadOnlyList<IConfigNode> Children { get; }

        /// <summary>
        /// Indicates if this instance represents an array.
        /// </summary>
        public bool IsArray { get; }

        /// <summary>
        /// Constructor used in cloning.
        /// </summary>
        /// <param name="parent">To be assigned to <see cref="Parent"/> property.</param>
        /// <param name="name">To be assigned to <see cref="Name"/> property.</param>
        /// <param name="children">Enumeration of child nodes. Nodes from this enumeration won't be added
        /// to the <see cref="Children"/> list of the newly created instance, but their clones.</param>
        /// <param name="isArray">Indicates if this instance represents an array.</param>
        protected ConfigurationNode(ConfigurationNode parent, [NotNull] string name, IEnumerable<IConfigNode> children,
            bool isArray)
        {
            Parent = parent;

            IndentLevel = parent == null || parent is ConfigurationFile
                ? (ushort) 0
                : (ushort) (parent.IndentLevel + 1);

            Name = name;

            IsArray = isArray;

            _children = children.Select(c => c.Clone(this)).ToList();

            Children = _children.AsReadOnly();
        }

        /// <summary>
        /// Constructor used in reading from configuration file.
        /// </summary>
        /// <param name="parent">To be assigned to <see cref="Parent"/> property.</param>
        /// <param name="name">To be assigned to <see cref="Name"/> property.</param>
        /// <param name="indentSpaces">Number of space characters in left indent.</param>
        /// <param name="reader">Reader used for reading the configuration file.</param>
        /// <param name="validNode">Predicate that determines if a particular node is <em>valid</em>, thus should be added to 
        /// <see cref="Children"/> collection, or not. If <c>null</c> all the nodes are considered <em>valid</em>. Defaults to 
        /// <c>null</c>.</param>
        /// <param name="isEmptyArray">Indicates if this instance represents an empty array. Defaults to <c>false</c>.</param>
        protected ConfigurationNode(ConfigurationNode parent, [NotNull] string name, ushort indentSpaces,
            ConfigFileReader reader, Predicate<IConfigNode> validNode = null, bool isEmptyArray = false)
        {
            Parent = parent;

            IndentLevel = parent == null || parent is ConfigurationFile
                ? (ushort) 0
                : (ushort) (parent.IndentLevel + 1);

            Name = name;

            if (isEmptyArray)
            {
                _children = new List<IConfigNode>();

                IsArray = true;
            }
            else
            {
                _children = LoadChildren(this, indentSpaces, reader, validNode).ToList();

                if (_children.Any(c => string.Equals(c.Name, "-", StringComparison.Ordinal)))
                {
                    if (_children.Any(c => !string.Equals(c.Name, "-", StringComparison.Ordinal)))
                        throw new Exception("Node contains both array item nodes, and regular nodes.");

                    IsArray = true;
                }
            }

            Children = _children.AsReadOnly();
        }

        /// <summary>
        /// Adds <paramref name="node"/> to <see cref="Children"/> list.
        /// </summary>
        /// <param name="node">Node to add.</param>
        internal void AddChild(IConfigNode node)
        {
            if (!ValidChild(node))
                throw new ArgumentException(
                    $"Array item nodes and regular nodes cannot be mixed in the same {nameof(ConfigurationNode)} instance.");

            _children.Add(node);
        }

        /// <summary>
        /// Inserts <paramref name="node"/> at specified <paramref name="index"/> in <see cref="Children"/> list.
        /// </summary>
        /// <param name="index">Index at which <paramref name="node"/> should be inserted.</param>
        /// <param name="node">Node to insert.</param>
        internal void InsertChild(int index, IConfigNode node)
        {
            if (!ValidChild(node))
                throw new ArgumentException(
                    $"Array item nodes and regular nodes cannot be mixed in the same {nameof(ConfigurationNode)} instance.");

            _children.Insert(index, node);
        }

        private bool ValidChild(IConfigNode node) => IsArray == string.Equals(node.Name, "-", StringComparison.Ordinal);

        /// <summary>
        /// Removes <paramref name="node"/> from <see cref="Children"/>.
        /// </summary>
        /// <param name="node">Node to remove.</param>
        /// <returns><c>true</c> if the node is successfully removed; <c>false</c> if it wasn't in <see cref="Children"/>
        /// list in the first place.</returns>
        internal bool RemoveChild(IConfigNode node) => _children.Remove(node);

        /// <inheritdoc />
        public bool EquivalentTo(IConfigNode other)
        {
            if (!(other is ConfigurationNode otherConfigurationNode) ||
                !string.Equals(Name, otherConfigurationNode.Name, StringComparison.Ordinal) ||
                Children.Count != otherConfigurationNode.Children.Count)
                return false;

            if (IsArray)
                return !Children.Where((t, i) => !t.EquivalentTo(otherConfigurationNode.Children[i])).Any();

            List<IConfigNode> otherChildren = new List<IConfigNode>(otherConfigurationNode.Children);

            foreach (IConfigNode child in Children)
            {
                IConfigNode otherChild =
                    otherChildren.FirstOrDefault(c => string.Equals(c.Name, child.Name, StringComparison.Ordinal));

                if (otherChild == null)
                    return false;

                otherChildren.Remove(otherChild);

                if (!child.EquivalentTo(otherChild))
                    return false;
            }

            return true;
        }

        /// <inheritdoc />
        public virtual IConfigNode Clone(ConfigurationNode parent)
        {
            this.VerifyParent(parent);

            return new ConfigurationNode(parent, Name, Children, IsArray);
        }

        /// <inheritdoc />
        public void Write(StreamWriter writer)
        {
            if (!(this is ConfigurationFile))
            {
                string indent = new String(' ', IndentLevel * ConfigurationFile.NumberOfSpacesPerIndentLevel);

                string line = string.Equals(Name, "-", StringComparison.Ordinal) ? $"{indent}-" : $"{indent}{Name}:";

                if (IsArray && Children.Count < 1)
                {
                    writer.WriteLine(string.Concat(line, " {  }"));

                    return;
                }

                writer.WriteLine(line);
            }

            foreach (IConfigNode child in Children)
                child.Write(writer);
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