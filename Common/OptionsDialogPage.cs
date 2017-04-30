// <copyright file="OptionsDialogPage.cs" company="ArchersRally">
// Copyright (c) ArchersRally. All rights reserved.
// </copyright>

namespace ArchersRally.Apprentice.Common
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using Microsoft;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Base of the common Apprentice Options page.
    /// </summary>
    internal partial class OptionsDialogPage : DialogPage, INotifyPropertyChanged
    {
        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T backingField, T value, [CallerMemberName] string caller = null)
        {
            if (!backingField.Equals(value))
            {
                backingField = value;
                this.PropertyChanged.Raise(this, new PropertyChangedEventArgs(caller));
            }
        }
    }
}
