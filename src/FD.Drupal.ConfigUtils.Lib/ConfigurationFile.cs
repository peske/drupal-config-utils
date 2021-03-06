﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FD.Drupal.ConfigUtils
{
    public class ConfigurationFile : ConfigurationNode
    {
        #region Constants

        private const string UuidNodeName = "uuid";

        private const string MachineNameNodeName = "id";

        internal const byte NumberOfSpacesPerIndentLevel = 2;

        #endregion Constants

        #region Static

        public static bool TryLoadFromFile(FileInfo file, out ConfigurationFile configuration,
            Predicate<IConfigNode> validNode = null)
        {
            configuration = null;

            try
            {
                using (ConfigFileReader reader = new ConfigFileReader(file))
                    configuration = new ConfigurationFile(reader, validNode);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                $"Exception while loading configuration from '{file}':{Environment.NewLine}{ex}".WriteLineRed();
            }

            return false;
        }

        private static bool DefaultValidNode(IConfigNode node)
        {
            switch (node.Name)
            {
                case "_core":

                    if (!(node is ConfigurationNode configurationNode) || configurationNode.Children.Count != 1 ||
                        !string.Equals(configurationNode.Children[0].Name, "default_config_hash",
                            StringComparison.Ordinal))
                        "Some file contains '_core' node which doesn't have the expected structure. It will be ignored anyway."
                            .WriteLineYellow();

                    return false;

                default:

                    return true;
            }
        }

        #endregion Static

        /// <summary>
        /// The file.
        /// </summary>
        public FileInfo File { get; }

        /// <summary>
        /// UUID.
        /// </summary>
        public Guid? Uuid
        {
            get
            {
                SingleValueNode uuidNode =
                    (SingleValueNode) Children.FirstOrDefault(c =>
                        string.Equals(c.Name, UuidNodeName, StringComparison.Ordinal));

                return uuidNode == null ? (Guid?) null : Guid.Parse(uuidNode.Value);
            }
            set
            {
                SingleValueNode uuidNode =
                    (SingleValueNode)Children.FirstOrDefault(c =>
                        string.Equals(c.Name, UuidNodeName, StringComparison.Ordinal));

                if (!value.HasValue)
                {
                    if (uuidNode != null)
                        RemoveChild(uuidNode);

                    return;
                }

                if (uuidNode == null)
                {
                    uuidNode = new SingleValueNode(this, UuidNodeName, value.Value.ToString().ToLowerInvariant());

                    InsertChild(0, uuidNode);
                }
                else
                    uuidNode.Value = value.Value.ToString().ToLowerInvariant();
            }
        }

        /// <summary>
        /// Returns machine name (if contained).
        /// </summary>
        public string MachineName =>
            (Children.FirstOrDefault(c => string.Equals(c.Name, MachineNameNodeName, StringComparison.Ordinal)) as
                SingleValueNode)?.Value;

        /// <summary>
        /// Returns <c>"dependencies"</c> node.
        /// </summary>
        public ConfigurationNode DependenciesNode =>
            (ConfigurationNode) Children.FirstOrDefault(c =>
                string.Equals(c.Name, "dependencies", StringComparison.Ordinal));

        /// <summary>
        /// Constructor used in reading configuration file.
        /// </summary>
        /// <param name="reader">Configuration file reader used.</param>
        /// <param name="validNode">Predicate that determines if a particular node is <em>valid</em>, thus should be added to 
        /// <see cref="ConfigurationNode.Children"/> collection, or not. If <c>null</c> all the nodes are considered 
        /// <em>valid</em>. Defaults to a function which returns <c>false</c> for nodes with <see cref="IConfigNode.Name"/>
        /// <c>"_core"</c>.</param>
        private ConfigurationFile(ConfigFileReader reader, Predicate<IConfigNode> validNode = null) : base(null,
            reader.File.Name.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                ? reader.File.Name.Substring(0, reader.File.Name.Length - 4)
                : reader.File.Name, 0, reader, validNode ?? DefaultValidNode)
        {
            if (!reader.EoF)
                throw new Exception($"Not all lines are loaded from file '{reader.File.Name}'.");

            File = reader.File;
        }

        /// <summary>
        /// Constructor used in cloning.
        /// </summary>
        /// <param name="name">To be assigned to <see cref="IConfigNode.Name"/> property.</param>
        /// <param name="children">Enumeration of child nodes. Nodes from this enumeration won't be added to the 
        /// <see cref="ConfigurationNode.Children"/> list of the newly created instance, but their clones.</param>
        private ConfigurationFile(string name, IEnumerable<IConfigNode> children) : base(null, name, children, false)
        {
        }

        /// <inheritdoc cref="ConfigurationNode.Clone" select="summary" />
        /// <param name="parent">Has to be <c>null</c>.</param>
        /// <returns>Cloned instance.</returns>
        /// <exception cref="ArgumentException">If <paramref name="parent"/> isn't <c>null</c>.</exception>
        public override IConfigNode Clone(ConfigurationNode parent) => parent == null
            ? Clone()
            : throw new ArgumentNullException($"{nameof(parent)} has to be null.", nameof(parent));

        /// <summary>
        /// Creates and returns clone of this instance.
        /// </summary>
        public ConfigurationFile Clone() => new ConfigurationFile(Name, Children);
    }
}