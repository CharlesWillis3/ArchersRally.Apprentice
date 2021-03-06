﻿// <copyright file="LastWriteFileChangeMonitor.cs" company="ArchersRally">
// Copyright (c) ArchersRally. All rights reserved.
// </copyright>

namespace ArchersRally.Apprentice.ImportWatcher
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;
    using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Monitors files for changes to the last write attribute.
    /// </summary>
    internal class LastWriteFileChangeMonitor : IVsFileChangeEvents, IDisposable
    {
        private readonly ApprenticePackage package;
        private readonly IAsyncServiceProvider asp;
        private readonly ImportsMonitor importsMonitor;
        private readonly List<uint> cookies;

        private IVsFileChangeEx fileChange;
        private string currentSolutionPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="LastWriteFileChangeMonitor"/> class.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="importsMonitor">An instance of <see cref="ImportsMonitor"/> that reports files to monitor</param>
        public LastWriteFileChangeMonitor(ApprenticePackage package, ImportsMonitor importsMonitor)
        {
            this.package = Requires.NotNull(package, nameof(package));
            this.asp = Requires.NotNull((IAsyncServiceProvider)package, nameof(package));
            this.importsMonitor = Requires.NotNull(importsMonitor, nameof(importsMonitor));
            this.cookies = new List<uint>();
            this.importsMonitor.ImportsChanged += this.ImportsMonitor_ImportsChanged;
        }

        /// <summary>
        /// Raised when a monitored file's last write attribute changes.
        /// </summary>
        public event EventHandler<FileChangedEventArgs> FileChanged;

        /// <summary>
        /// Initialize the <see cref="LastWriteFileChangeMonitor"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        public async Task InitializeAsync()
        {
            this.fileChange = (IVsFileChangeEx)await this.asp.GetServiceAsync(typeof(SVsFileChangeEx));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.UnadviseAllAndClear(this.fileChange, this.cookies);
            this.importsMonitor.ImportsChanged -= this.ImportsMonitor_ImportsChanged;
        }

        /// <inheritdoc />
        int IVsFileChangeEvents.FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
        {
            Trace.TraceInformation($"{nameof(LastWriteFileChangeMonitor)}.{nameof(IVsFileChangeEvents.FilesChanged)}");
            this.FileChanged.Raise(this, new FileChangedEventArgs(this.currentSolutionPath));
            return VSConstants.S_OK;
        }

        /// <inheritdoc />
        /// <exception cref="NotImplementedException">This method is not implemented.</exception>
        int IVsFileChangeEvents.DirectoryChanged(string pszDirectory)
        {
            Trace.TraceInformation($"{nameof(LastWriteFileChangeMonitor)}.{nameof(IVsFileChangeEvents.DirectoryChanged)}");
            return VSConstants.S_OK;
        }

        private void ImportsMonitor_ImportsChanged(object sender, ImportsMonitor.ImportsChangedEventArgs e)
        {
            Trace.TraceInformation($"{nameof(LastWriteFileChangeMonitor)}.{nameof(LastWriteFileChangeMonitor.ImportsMonitor_ImportsChanged)}");
            this.UnadviseAllAndClear(this.fileChange, this.cookies);

            if (e.SolutionPath == null)
            {
                return;
            }

            this.currentSolutionPath = e.SolutionPath;

            foreach (var p in e.ImportPaths)
            {
                ErrorHandler.ThrowOnFailure(
                    this.fileChange.AdviseFileChange(p, (uint)_VSFILECHANGEFLAGS.VSFILECHG_Time, this, out uint cookie));
                this.cookies.Add(cookie);
            }
        }

        private void UnadviseAllAndClear(IVsFileChangeEx fileChange, IList<uint> cookies)
        {
            Trace.TraceInformation($"{nameof(LastWriteFileChangeMonitor)}.{nameof(LastWriteFileChangeMonitor.UnadviseAllAndClear)}");

            foreach (var c in cookies)
            {
                ErrorHandler.ThrowOnFailure(
                    fileChange.UnadviseFileChange(c));
            }

            cookies.Clear();
        }

        /// <summary>
        /// Data for the <see cref="FileChanged"/> event.
        /// </summary>
        public class FileChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="FileChangedEventArgs"/> class.
            /// </summary>
            /// <param name="solutionPath">The full path to the solution that owns the file whose change is being reported</param>
            public FileChangedEventArgs(string solutionPath)
            {
                this.SolutionPath = solutionPath;
            }

            /// <summary>
            /// Gets the full path to the solution that owns the file whose change is being reported.
            /// </summary>
            public string SolutionPath { get; }
        }
    }
}
