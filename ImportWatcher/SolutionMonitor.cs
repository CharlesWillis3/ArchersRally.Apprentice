// <copyright file="SolutionMonitor.cs" company="ArchersRally">
// Copyright (c) ArchersRally. All rights reserved.
// </copyright>

namespace ArchersRally.Apprentice.ImportWatcher
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using EnvDTE;
    using EnvDTE80;
    using Microsoft;
    using Microsoft.VisualStudio.Shell.Interop;
    using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Monitors the environment for changes to the loaded solution.
    /// </summary>
    internal sealed class SolutionMonitor : IDisposable
    {
        private DTE2 dte;
        private SolutionEvents solutionEvents;
        private readonly ApprenticePackage package;
        private readonly IAsyncServiceProvider asp;

        public SolutionMonitor(ApprenticePackage package)
        {
            this.package = Requires.NotNull(package, nameof(package));
            this.asp = Requires.NotNull((IAsyncServiceProvider)package, nameof(package));
        }

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
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        public async Task InitializeAsync()
        {
            Trace.TraceInformation($"{nameof(SolutionMonitor)}.{nameof(SolutionMonitor.InitializeAsync)}");

            this.dte = (DTE2)await this.asp.GetServiceAsync(typeof(SDTE));

            // SolutionEvents can be garbage-collected when you least want it to happen. Pin it here with a member variable.
            this.solutionEvents = this.dte.Events.SolutionEvents;
            this.solutionEvents.Opened += this.SolutionEvents_Opened;
            this.solutionEvents.ProjectAdded += this.SolutionEvents_ProjectAdded;
            this.solutionEvents.ProjectRemoved += this.SolutionEvents_ProjectRemoved;
            this.solutionEvents.BeforeClosing += this.SolutionEvents_BeforeClosing;
        }

        /// <summary>
        /// Start the Solution Monitor.
        /// </summary>
        public void Start()
        {
            Trace.TraceInformation($"{nameof(SolutionMonitor)}.{nameof(SolutionMonitor.Start)}");
            this.RaiseSolutionChanged();
        }

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
            Trace.TraceInformation($"{nameof(SolutionMonitor)}.{nameof(this.SolutionEvents_BeforeClosing)}");
            this.SolutionClosing.Raise(this, EventArgs.Empty);
        }

        private void SolutionEvents_ProjectRemoved(EnvDTE.Project project)
        {
            Trace.TraceInformation($"{nameof(SolutionMonitor)}.{nameof(this.SolutionEvents_ProjectRemoved)}");
            this.RaiseSolutionChanged();
        }

        private void SolutionEvents_ProjectAdded(EnvDTE.Project project)
        {
            Trace.TraceInformation($"{nameof(SolutionMonitor)}.{nameof(this.SolutionEvents_ProjectAdded)}");
            this.RaiseSolutionChanged();
        }

        private void SolutionEvents_Opened()
        {
            Trace.TraceInformation($"{nameof(SolutionMonitor)}.{nameof(this.SolutionEvents_Opened)}");
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
