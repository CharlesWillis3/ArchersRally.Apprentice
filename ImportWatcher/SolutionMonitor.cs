// <copyright file="SolutionMonitor.cs" company="ArchersRally">
// Copyright (c) ArchersRally. All rights reserved.
// </copyright>

namespace ArchersRally.Apprentice.ImportWatcher
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using EnvDTE;
    using EnvDTE80;
    using Microsoft;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;
    using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Monitors the environment for changes to the loaded solution.
    /// </summary>
    internal sealed class SolutionMonitor : IDisposable
    {
        private DTE2 dte;

        /// <summary>
        /// Raised when a solution is opened, or a project is added or removed to the current solution.
        /// </summary>
        public event EventHandler<SolutionChangedEventArgs> SolutionChanged;

        /// <summary>
        /// Raised when a solution is being closed.
        /// </summary>
        public event EventHandler SolutionClosing;

        /// <summary>
        /// Initialize the <see cref="SolutionMonitor"/> class.
        /// </summary>
        /// <param name="sp">The AsyncServiceProvider</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        public async Task InitializeAsync(IAsyncServiceProvider sp)
        {
            this.dte = (DTE2)await sp.GetServiceAsync(typeof(SDTE));

            this.dte.Events.SolutionEvents.Opened += this.SolutionEvents_Opened;
            this.dte.Events.SolutionEvents.ProjectAdded += this.SolutionEvents_ProjectAdded;
            this.dte.Events.SolutionEvents.ProjectRemoved += this.SolutionEvents_ProjectRemoved;
            this.dte.Events.SolutionEvents.BeforeClosing += this.SolutionEvents_BeforeClosing;
        }

        /// <summary>
        /// Start the Solution Monitor.
        /// </summary>
        public void Start() => this.RaiseSolutionChanged();

        /// <inheritdoc />
        public void Dispose()
        {
            this.dte.Events.SolutionEvents.BeforeClosing -= this.SolutionEvents_BeforeClosing;
            this.dte.Events.SolutionEvents.ProjectRemoved -= this.SolutionEvents_ProjectRemoved;
            this.dte.Events.SolutionEvents.ProjectAdded -= this.SolutionEvents_ProjectAdded;
            this.dte.Events.SolutionEvents.Opened -= this.SolutionEvents_Opened;
        }

        private void SolutionEvents_BeforeClosing()
        {
            Trace.TraceInformation(nameof(this.SolutionEvents_BeforeClosing));
            this.SolutionClosing.Raise(this, EventArgs.Empty);
        }

        private void SolutionEvents_ProjectRemoved(EnvDTE.Project project)
        {
            Trace.TraceInformation(nameof(this.SolutionEvents_ProjectRemoved));
            this.RaiseSolutionChanged();
        }

        private void SolutionEvents_ProjectAdded(EnvDTE.Project project)
        {
            Trace.TraceInformation(nameof(this.SolutionEvents_ProjectAdded));
            this.RaiseSolutionChanged();
        }

        private void SolutionEvents_Opened()
        {
            Trace.TraceInformation(nameof(this.SolutionEvents_Opened));
            this.RaiseSolutionChanged();
        }

        private void RaiseSolutionChanged()
        {
            var eventArgs = SolutionChangedEventArgs.FromDTE(this.dte);
            this.SolutionChanged.Raise(this, eventArgs);
        }

        /// <summary>
        /// Data about the <see cref="SolutionChanged"/> event.
        /// </summary>
        public class SolutionChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SolutionChangedEventArgs"/> class.
            /// </summary>
            /// <param name="solutionPath">The path to the solution file</param>
            /// <param name="projectPaths">A collection of the paths to the solution's projects</param>
            public SolutionChangedEventArgs(string solutionPath, IEnumerable<string> projectPaths = null)
            {
                this.SolutionPath = solutionPath;
                this.ProjectPaths = projectPaths ?? Enumerable.Empty<string>();
            }

            /// <summary>
            /// Gets a collection of the paths to the solution's projects.
            /// </summary>
            public IEnumerable<string> ProjectPaths { get; }

            /// <summary>
            /// Gets the full path to the current solution.
            /// </summary>
            public string SolutionPath { get; }

            /// <summary>
            /// Creates <see cref="SolutionChangedEventArgs"/> from an instance of <see cref="DTE2"/>.
            /// </summary>
            /// <param name="dte">An instance of <see cref="DTE2"/> that solution and project properties will be read from</param>
            /// <returns>An instance of <see cref="SolutionChangedEventArgs"/> with <see cref="SolutionPath"/> and <see cref="ProjectPaths"/> set from the currently loaded solution</returns>
            public static SolutionChangedEventArgs FromDTE(DTE2 dte)
            {
                var projectPaths = new List<string>();

                foreach (Project p in dte.Solution.Projects)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(p.FileName))
                        {
                            projectPaths.Add(p.FileName);
                        }
                    }
                    catch (NotImplementedException)
                    {
                        // try next project
                    }
                }

                return new SolutionChangedEventArgs(dte.Solution.FullName, projectPaths);
            }
        }
    }
}
