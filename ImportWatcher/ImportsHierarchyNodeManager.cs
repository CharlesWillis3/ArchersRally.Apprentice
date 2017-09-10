// <copyright file="ImportsHierarchyNodeManager.cs" company="ArchersRally">
// Copyright (c) ArchersRally. All rights reserved.
// </copyright>

namespace ArchersRally.Apprentice.ImportWatcher
{
    using System;
    using Microsoft;
    using Microsoft.VisualStudio.Shell.Interop;
    using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Manages the Watched Imports solution explorer virtual node
    /// </summary>
    internal class ImportsHierarchyNodeManager : IDisposable
    {
        private readonly ImportsMonitor importsMonitor;
        private readonly ApprenticePackage package;
        private IVsSolution vsSolution;
        private ImportsHierarchyNode currentHierarchy;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportsHierarchyNodeManager"/> class.
        /// </summary>
        /// <param name="package">The package instance</param>
        /// <param name="importsMonitor">An instance of <see cref="importsMonitor"/></param>
        public ImportsHierarchyNodeManager(ApprenticePackage package, ImportsMonitor importsMonitor)
        {
            this.importsMonitor = Requires.NotNull(importsMonitor, nameof(importsMonitor));
            this.package = Requires.NotNull(package, nameof(package));
            this.importsMonitor.ImportsChanged += this.ImportsMonitor_ImportsChanged;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.importsMonitor.ImportsChanged -= this.ImportsMonitor_ImportsChanged;
            if (this.currentHierarchy != null)
            {
                this.vsSolution.RemoveVirtualProject(this.currentHierarchy, (uint)__VSREMOVEVPFLAGS.REMOVEVP_DontSaveHierarchy);
            }

            this.isDisposed = true;
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="sp">The service provider</param>
        /// <returns>Nothing</returns>
        public async Task InitializeAsync(IAsyncServiceProvider sp)
        {
            this.vsSolution = (IVsSolution)await sp.GetServiceAsync(typeof(SVsSolution));
        }

        private void ImportsMonitor_ImportsChanged(object sender, ImportsMonitor.ImportsChangedEventArgs e)
        {
            if (this.currentHierarchy != null)
            {
                this.vsSolution.RemoveVirtualProject(this.currentHierarchy, (uint)__VSREMOVEVPFLAGS.REMOVEVP_DontSaveHierarchy);
            }

            this.currentHierarchy = new ImportsHierarchyNode(this.package, e.ImportPaths);

            this.vsSolution.AddVirtualProject(
                this.currentHierarchy,
                (uint)(__VSADDVPFLAGS.ADDVP_AddToProjectWindow | __VSADDVPFLAGS.ADDVP_ExcludeFromBuild | __VSADDVPFLAGS.ADDVP_ExcludeFromCfgUI | __VSADDVPFLAGS.ADDVP_ExcludeFromDebugLaunch | __VSADDVPFLAGS.ADDVP_ExcludeFromDeploy | __VSADDVPFLAGS.ADDVP_ExcludeFromEnumOutputs | __VSADDVPFLAGS.ADDVP_ExcludeFromSCC));
        }
    }
}
