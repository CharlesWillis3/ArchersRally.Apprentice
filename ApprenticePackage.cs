// <copyright file="ApprenticePackage.cs" company="ArchersRally">
// Copyright (c) ArchersRally. All rights reserved.
// </copyright>

namespace ArchersRally.Apprentice
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "0.1", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(ApprenticePackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(Common.OptionsDialogPage), "ArchersRally", "Apprentice", 121, 122, true, 123)]
    public sealed class ApprenticePackage : AsyncPackage
    {
        /// <summary>
        /// Apprentice GUID string.
        /// </summary>
        public const string PackageGuidString = "068613c3-caf6-45f8-9856-2f51abb74ce4";

        private ImportWatcher.Feature importWatcher;

        /// <summary>
        /// Initialize the package.
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <param name="progress">Progress Reporter</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the asynchronous operation</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            var optionsPage = (Common.OptionsDialogPage)this.GetDialogPage(typeof(Common.OptionsDialogPage));

            this.importWatcher = new ImportWatcher.Feature(this, optionsPage);
            await this.importWatcher.InitializeAsync();

            await base.InitializeAsync(cancellationToken, progress);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            this.importWatcher.Dispose();
            base.Dispose(disposing);
        }
    }
}
