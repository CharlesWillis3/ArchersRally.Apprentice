// <copyright file="OptionsDialogPage.cs" company="ArchersRally">
// Copyright (c) ArchersRally. All rights reserved.
// </copyright>

namespace ArchersRally.Apprentice.Common
{
    using System.ComponentModel;

    /// <summary>
    /// Options for the <see cref="ImportWatcher"/> feature.
    /// </summary>
    internal partial class OptionsDialogPage
    {
        private const string ImportsWatcherCategory = "Solution Imports Watcher";

        private bool isEnabled;

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="ImportWatcher"/> is enabled.
        /// </summary>
        [Category(ImportsWatcherCategory)]
        [DisplayName("Enable Solution Imports Watcher")]
        [Description("Solution Imports Watcher will prompt you to reload the solution when any of it's projects' imports are changed.")]
        public bool IsEnabled
        {
            get => this.isEnabled;
            set => this.SetProperty(ref this.isEnabled, value);
        }
    }
}
