// <copyright file="ImportItemProperties.cs" company="ArchersRally">
// Copyright (c) ArchersRally. All rights reserved.
// </copyright>

namespace ArchersRally.Apprentice.ImportWatcher
{
    using System;

    /// <summary>
    /// Display properties for items in the Watched Imports virtual node
    /// </summary>
    [System.ComponentModel.Description("Watched Import")]
    internal sealed class ImportItemProperties
    {
        /// <summary>
        /// Gets or sets a value that is the full path to the item on disk
        /// </summary>
        public string FullPath { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating the last write time of the item
        /// </summary>
        public DateTime LastWriteTime { get; internal set; }
    }
}
