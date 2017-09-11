// <copyright file="IFeature.cs" company="ArchersRally">
// Copyright (c) ArchersRally. All rights reserved.
// </copyright>

namespace ArchersRally.Apprentice.Contract
{
    using System;
    using Microsoft.VisualStudio.Shell;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Represents the root of a single feature.
    /// </summary>
    public interface IFeature : IDisposable
    {
        /// <summary>
        /// Initialize the feature.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        Task InitializeAsync();
    }
}
