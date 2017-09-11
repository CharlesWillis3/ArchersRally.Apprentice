// <copyright file="ImportsMonitor.cs" company="ArchersRally">
// Copyright (c) ArchersRally. All rights reserved.
// </copyright>

namespace ArchersRally.Apprentice.ImportWatcher
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft;
    using Microsoft.Build.Evaluation;

    /// <summary>
    /// Monitors the imports of the current solution.
    /// </summary>
    internal sealed class ImportsMonitor : IDisposable
    {
        private readonly SolutionMonitor solutionMonitor;
        private IEnumerable<string> previousImportPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportsMonitor"/> class.
        /// </summary>
        /// <param name="solutionMonitor">The solution monitor</param>
        public ImportsMonitor(SolutionMonitor solutionMonitor)
        {
            this.solutionMonitor = Requires.NotNull(solutionMonitor, nameof(solutionMonitor));

            this.solutionMonitor.SolutionChanged += this.SolutionMonitor_SolutionChanged;
            this.solutionMonitor.SolutionClosing += this.SolutionMonitor_SolutionClosing;

            this.previousImportPaths = Enumerable.Empty<string>();
        }

        /// <summary>
        /// Raised when the files imported by the current solution change.
        /// </summary>
        public event EventHandler<ImportsChangedEventArgs> ImportsChanged;

        /// <inheritdoc />
        public void Dispose()
        {
            this.solutionMonitor.SolutionClosing -= this.SolutionMonitor_SolutionClosing;
            this.solutionMonitor.SolutionChanged -= this.SolutionMonitor_SolutionChanged;
        }

        private void SolutionMonitor_SolutionChanged(object sender, SolutionMonitor.SolutionChangedEventArgs e)
        {
            Trace.TraceInformation($"{nameof(ImportsMonitor)}.{nameof(ImportsMonitor.SolutionMonitor_SolutionChanged)}");
            ProjectCollection.GlobalProjectCollection.UnloadAllProjects();

            if (e.SolutionPath == null)
            {
                return;
            }

            var nextImportsPaths = e.ProjectPaths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .SelectMany(path => new Project(path).GetImportPaths())
                .Distinct()
                .OrderBy(i => i)
                .ToArray();

            if (!this.previousImportPaths.SequenceEqual(nextImportsPaths))
            {
                this.previousImportPaths = nextImportsPaths;
                this.ImportsChanged.Raise(this, new ImportsChangedEventArgs(e.SolutionPath, nextImportsPaths));
            }
        }

        private void SolutionMonitor_SolutionClosing(object sender, EventArgs e)
        {
            Trace.TraceInformation($"{nameof(ImportsMonitor)}.{nameof(ImportsMonitor.SolutionMonitor_SolutionClosing)}");
            ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
            this.previousImportPaths = Enumerable.Empty<string>();
            this.ImportsChanged.Raise(this, new ImportsChangedEventArgs());
        }

        /// <summary>
        /// Data about the  <see cref="ImportsChanged"/> event.
        /// </summary>
        public class ImportsChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ImportsChangedEventArgs"/> class.
            /// </summary>
            /// <param name="solutionPath">The full path to the solution file</param>
            /// <param name="importPaths">A collection of the paths of all the files imported by the solution</param>
            public ImportsChangedEventArgs(string solutionPath = null, IEnumerable<string> importPaths = null)
            {
                this.SolutionPath = solutionPath;
                this.ImportPaths = importPaths ?? Enumerable.Empty<string>();
            }

            /// <summary>
            /// Gets the paths of all the files imported by the solution. Empty if there is no open solution.
            /// </summary>
            public IEnumerable<string> ImportPaths { get; }

            /// <summary>
            /// Gets the full path to the solution file.
            /// </summary>
            public string SolutionPath { get; }
        }
    }
}
