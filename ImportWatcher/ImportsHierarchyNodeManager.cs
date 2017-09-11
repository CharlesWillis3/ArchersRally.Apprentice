// <copyright file="ImportsHierarchyNodeManager.cs" company="ArchersRally">
// Copyright (c) ArchersRally. All rights reserved.
// </copyright>

namespace ArchersRally.Apprentice.ImportWatcher
{
    using System;
    using Microsoft;
    using Microsoft.VisualStudio.Shell.Interop;
    using IServiceProvider = System.IServiceProvider;
    using System.Diagnostics;

    /// <summary>
    /// Manages the Watched Imports solution explorer virtual node
    /// </summary>
    internal class ImportsHierarchyNodeManager : IDisposable
    {
        private readonly ImportsMonitor importsMonitor;
        private readonly SolutionMonitor solutionMonitor;
        private readonly ApprenticePackage package;
        private readonly IServiceProvider sp;
        private IVsSolution vsSolution;
        private ImportsHierarchyNode currentHierarchy;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportsHierarchyNodeManager"/> class.
        /// </summary>
        /// <param name="package">The package instance</param>
        /// <param name="importsMonitor">An instance of <see cref="ImportsMonitor"/></param>
        /// <param name="solutionMonitor">An instance of <see cref="SolutionMonitor"/></param>
        public ImportsHierarchyNodeManager(ApprenticePackage package, ImportsMonitor importsMonitor, SolutionMonitor solutionMonitor)
        {
            this.importsMonitor = Requires.NotNull(importsMonitor, nameof(importsMonitor));
            this.solutionMonitor = Requires.NotNull(solutionMonitor, nameof(solutionMonitor));
            this.package = Requires.NotNull(package, nameof(package));
            this.sp = Requires.NotNull((IServiceProvider)package, nameof(package));
            this.importsMonitor.ImportsChanged += this.ImportsMonitor_ImportsChanged;
            this.solutionMonitor.SolutionClosing += this.SolutionMonitor_SolutionClosing;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.importsMonitor.ImportsChanged -= this.ImportsMonitor_ImportsChanged;
            this.solutionMonitor.SolutionClosing -= this.SolutionMonitor_SolutionClosing;
            this.RemoveCurrentNode();

            this.isDisposed = true;
        }

        private void ImportsMonitor_ImportsChanged(object sender, ImportsMonitor.ImportsChangedEventArgs e)
        {
            Trace.TraceInformation($"{nameof(ImportsHierarchyNodeManager)}.{nameof(ImportsHierarchyNodeManager.ImportsMonitor_ImportsChanged)}");

            if (this.vsSolution == null)
            {
                lock (this)
                {
                    if (this.vsSolution == null)
                    {
                        this.vsSolution = (IVsSolution)this.sp.GetService(typeof(SVsSolution));
                        this.currentHierarchy = new ImportsHierarchyNode(this.package);
                        this.vsSolution.AddVirtualProject(
                            this.currentHierarchy,
                            (uint)(__VSADDVPFLAGS.ADDVP_AddToProjectWindow
                            | __VSADDVPFLAGS.ADDVP_ExcludeFromBuild
                            | __VSADDVPFLAGS.ADDVP_ExcludeFromCfgUI
                            | __VSADDVPFLAGS.ADDVP_ExcludeFromDebugLaunch
                            | __VSADDVPFLAGS.ADDVP_ExcludeFromDeploy
                            | __VSADDVPFLAGS.ADDVP_ExcludeFromEnumOutputs
                            | __VSADDVPFLAGS.ADDVP_ExcludeFromSCC));
                    }
                }
            }

            this.currentHierarchy.UpdateItems(e.ImportPaths);
        }

        private void SolutionMonitor_SolutionClosing(object sender, EventArgs e)
        {
            Trace.TraceInformation($"{nameof(ImportsHierarchyNodeManager)}.{nameof(ImportsHierarchyNodeManager.SolutionMonitor_SolutionClosing)}");

            lock (this)
            {
                this.RemoveCurrentNode();
                this.vsSolution = null;
                this.currentHierarchy = null;
            }
        }

        private void RemoveCurrentNode()
        {
            Trace.TraceInformation($"{nameof(ImportsHierarchyNodeManager)}.{nameof(ImportsHierarchyNodeManager.RemoveCurrentNode)}");

            if (this.vsSolution != null && this.currentHierarchy != null)
            {
                this.vsSolution.RemoveVirtualProject(this.currentHierarchy, (uint)__VSREMOVEVPFLAGS.REMOVEVP_DontSaveHierarchy);
            }
        }
    }
}
