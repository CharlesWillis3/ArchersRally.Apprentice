// <copyright file="Feature.cs" company="ArchersRally">
// Copyright (c) ArchersRally. All rights reserved.
// </copyright>

namespace ArchersRally.Apprentice.ImportWatcher
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using ArchersRally.Apprentice.Common;
    using ArchersRally.Apprentice.Contract;
    using Microsoft;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Threading;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Monitors all the files imported by the solution's projects, and forces a reload of the solution if any of the imports are touched.
    /// </summary>
    internal sealed class Feature : IFeature
    {
        private readonly ApprenticePackage package;
        private readonly OptionsDialogPage optionsPage;

        private SolutionMonitor solutionMonitor;
        private ImportsMonitor importMonitor;
        private ImportsHierarchyNodeManager hierarchyNodeManager;
        private LastWriteFileChangeMonitor changeMonitor;
        private IAsyncServiceProvider sp;

        /// <summary>
        /// Initializes a new instance of the <see cref="Feature"/> class.
        /// </summary>
        /// <param name="package">The package instance</param>
        /// <param name="optionsPage">The common options page</param>
        public Feature(ApprenticePackage package, Common.OptionsDialogPage optionsPage)
        {
            this.package = Requires.NotNull(package, nameof(package));
            this.optionsPage = Requires.NotNull(optionsPage, nameof(optionsPage));
            this.optionsPage.PropertyChanged += this.OptionsPage_PropertyChanged;
        }

        /// <inheritdoc />
        public async Task InitializeAsync(IAsyncServiceProvider sp)
        {
            this.sp = Requires.NotNull(sp, nameof(sp));

            if (this.optionsPage.IsEnabled)
            {
                await this.StartAsync();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Stop();
        }

        private async Task StartAsync()
        {
            this.solutionMonitor = new SolutionMonitor();
            this.importMonitor = new ImportsMonitor(this.solutionMonitor);
            this.hierarchyNodeManager = new ImportsHierarchyNodeManager(this.package, this.importMonitor);
            this.changeMonitor = new LastWriteFileChangeMonitor(this.importMonitor);

            this.changeMonitor.FileChanged += this.ChangeMonitor_FileChanged;

            await this.changeMonitor.InitializeAsync(this.sp);
            await this.hierarchyNodeManager.InitializeAsync(this.sp);
            await this.solutionMonitor.InitializeAsync(this.sp);
            this.solutionMonitor.Start();

            Trace.TraceInformation("ImportWatcher Started");
        }

        private void Stop()
        {
            if (this.changeMonitor != null)
            {
                this.changeMonitor.FileChanged -= this.ChangeMonitor_FileChanged;
            }

            this.changeMonitor?.Dispose();
            this.hierarchyNodeManager?.Dispose();
            this.importMonitor?.Dispose();
            this.solutionMonitor?.Dispose();

            this.changeMonitor = null;
            this.hierarchyNodeManager = null;
            this.importMonitor = null;
            this.solutionMonitor = null;

            Trace.TraceInformation("ImportWatcher Stopped");
        }

        private async Task OptionsPage_PropertyChangedImplAsync(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OptionsDialogPage.IsEnabled))
            {
                if (this.optionsPage.IsEnabled)
                {
                    await this.StartAsync();
                }
                else
                {
                    this.Stop();
                }
            }
        }

        private void OptionsPage_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.OptionsPage_PropertyChangedImplAsync(sender, e).ContinueWith(t => Trace.Fail(t.Exception.Message), TaskContinuationOptions.OnlyOnFaulted).Forget();
        }

        private void ChangeMonitor_FileChanged(object sender, LastWriteFileChangeMonitor.FileChangedEventArgs e)
        {
            File.SetLastWriteTimeUtc(e.SolutionPath, DateTime.UtcNow);
        }
    }
}
