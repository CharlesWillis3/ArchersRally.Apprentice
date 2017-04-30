// <copyright file="Extensions.cs" company="ArchersRally">
// Copyright (c) ArchersRally. All rights reserved.
// </copyright>

namespace ArchersRally.Apprentice.ImportWatcher
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Build.Evaluation;

    /// <summary>
    /// Extension methods for the <see cref="ImportWatcher"/> feature.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Get the import paths from a <see cref="Project"/>.
        /// </summary>
        /// <param name="p">The project</param>
        /// <returns>A collection of distict import paths</returns>
        public static IEnumerable<string> GetImportPaths(this Project p)
        {
            return p.Imports.Select(i => i.ImportedProject.FullPath);
        }
    }
}
