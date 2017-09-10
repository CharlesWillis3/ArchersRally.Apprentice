// <copyright file="ImportsHierarchyNode.cs" company="ArchersRally">
// Copyright (c) ArchersRally. All rights reserved.
// </copyright>

namespace ArchersRally.Apprentice.ImportWatcher
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.OLE.Interop;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
    using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// A virtual hierarchy of paths
    /// </summary>
    public class ImportsHierarchyNode : IVsProject, IVsHierarchy, IVsUIHierarchy
    {
        private static readonly Guid WatchedImportType = Guid.Parse("D98191F7-EB49-4784-8D3D-F7F61A1BE7FF");

        private readonly Dictionary<__VSHPROPID, object> rootPropertyMap;
        private readonly Dictionary<__VSHPROPID, object> itemPropertyMap;
        private readonly Dictionary<uint, string> idPathMap;
        private readonly IAsyncServiceProvider asp;
        private readonly IServiceProvider sp;

        // Reserve 0 for comparisons to default
        private uint firstItemId = 1;
        private uint lastItemId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportsHierarchyNode"/> class.
        /// </summary>
        /// <param name="package">The root package</param>
        /// <param name="importsPaths">The paths of the imports being monitered</param>
        public ImportsHierarchyNode(ApprenticePackage package, IEnumerable<string> importsPaths)
        {
            Requires.NotNull(package, nameof(package));
            Requires.NotNull(importsPaths, nameof(importsPaths));

            this.lastItemId = this.firstItemId;
            this.asp = (IAsyncServiceProvider)package;
            this.sp = (IServiceProvider)package;
            this.idPathMap = importsPaths.Distinct().ToDictionary(p => this.lastItemId++);

            this.rootPropertyMap = new Dictionary<__VSHPROPID, object>
            {
                { __VSHPROPID.VSHPROPID_Caption, "Watched Imports" },
                { __VSHPROPID.VSHPROPID_Expandable, true },
                { __VSHPROPID.VSHPROPID_ExpandByDefault, true },
                { __VSHPROPID.VSHPROPID_Expanded, true },
                { __VSHPROPID.VSHPROPID_FirstChild, this.firstItemId },
                { __VSHPROPID.VSHPROPID_HandlesOwnReload, true },
                { __VSHPROPID.VSHPROPID_IconIndex, 2 },
                { __VSHPROPID.VSHPROPID_Name, "Watched Imports Name" },
                { __VSHPROPID.VSHPROPID_OpenFolderIconIndex, 3 },
                { __VSHPROPID.VSHPROPID_Parent, VSConstants.VSITEMID.Nil },
                { __VSHPROPID.VSHPROPID_StateIconIndex, VsStateIcon.STATEICON_READONLY },
                { __VSHPROPID.VSHPROPID_TypeGuid, WatchedImportType },
                { __VSHPROPID.VSHPROPID_TypeName, nameof(WatchedImportType) }
            };

            this.itemPropertyMap = new Dictionary<__VSHPROPID, object>
            {
                { __VSHPROPID.VSHPROPID_Expandable, false },
                { __VSHPROPID.VSHPROPID_ExpandByDefault, false },
                { __VSHPROPID.VSHPROPID_Expanded, false },
                { __VSHPROPID.VSHPROPID_HandlesOwnReload, true },
                { __VSHPROPID.VSHPROPID_Parent, VSConstants.VSITEMID.Root },
                { __VSHPROPID.VSHPROPID_StateIconIndex, VsStateIcon.STATEICON_BLANK },
                { __VSHPROPID.VSHPROPID_TypeGuid, WatchedImportType },
                { __VSHPROPID.VSHPROPID_TypeName, nameof(WatchedImportType) }
            };
        }

        /// <inheritdoc/>
        int IVsProject.IsDocumentInProject(string pszMkDocument, out int pfFound, VSDOCUMENTPRIORITY[] pdwPriority, out uint pitemid)
        {
            var idPathPair = this.idPathMap.Where(kvp => kvp.Value.Equals(pszMkDocument, StringComparison.OrdinalIgnoreCase)).SingleOrDefault();
            if (idPathPair.Value != null)
            {
                pfFound = Common.Constants.TRUE;
                pdwPriority = new VSDOCUMENTPRIORITY[] { VSDOCUMENTPRIORITY.DP_Standard };
                pitemid = idPathPair.Key;
            }
            else
            {
                Trace.TraceInformation($"{this.GetType().Name}.{nameof(IVsProject.IsDocumentInProject)}: Document not found {pszMkDocument}");
                pfFound = Common.Constants.FALSE;
                pdwPriority = new VSDOCUMENTPRIORITY[] { VSDOCUMENTPRIORITY.DP_Unsupported };
                pitemid = 0;
            }

            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        int IVsProject.GetMkDocument(uint itemid, out string pbstrMkDocument) => this.idPathMap.TryGetValue(itemid, out pbstrMkDocument) ? VSConstants.S_OK : VSConstants.E_INVALIDARG;

        /// <inheritdoc/>
        int IVsProject.OpenItem(uint itemid, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
        {
            Guid localLogicView = rguidLogicalView;
            IVsWindowFrame localVsWindowFrame = null;

            async Task<int> OpenDocument()
            {
                var vsOpenDocument = (IVsUIShellOpenDocument)await this.asp.GetServiceAsync(typeof(SVsUIShellOpenDocument));
                string itemPath = this.idPathMap[itemid];

                return vsOpenDocument.OpenStandardEditor(
                    (uint)__VSOSEFLAGS.OSE_ChooseBestStdEditor,
                    itemPath,
                    localLogicView,
                    $"Imports Watcher - {Path.GetFileName(itemPath)}",
                    this,
                    itemid,
                    punkDocDataExisting,
                    this.sp,
                    out localVsWindowFrame);
            }

            if (this.idPathMap.ContainsKey(itemid))
            {
                int result = ThreadHelper.JoinableTaskFactory.Run(OpenDocument);
                ppWindowFrame = localVsWindowFrame;
                return result;
            }
            else
            {
                ppWindowFrame = null;
                return VSConstants.E_INVALIDARG;
            }
        }

        /// <inheritdoc/>
        int IVsProject.GetItemContext(uint itemid, out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP)
        {
            if (this.idPathMap.ContainsKey(itemid))
            {
                ppSP = this.sp;
                return VSConstants.S_OK;
            }
            else
            {
                ppSP = null;
                return VSConstants.E_INVALIDARG;
            }
        }

        /// <inheritdoc/>
        int IVsProject.GenerateUniqueItemName(uint itemidLoc, string pszExt, string pszSuggestedRoot, out string pbstrItemName)
        {
            pbstrItemName = null;
            return VSConstants.E_NOTIMPL;
        }

        /// <inheritdoc/>
        int IVsProject.AddItem(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, VSADDRESULT[] pResult)
        {
            return VSConstants.E_NOTIMPL;
        }

        /// <inheritdoc/>
        int IVsHierarchy.SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
        {
            return VSConstants.E_NOTIMPL;
        }

        /// <inheritdoc/>
        int IVsHierarchy.GetSite(out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP)
        {
            ppSP = this.sp;
            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        int IVsHierarchy.QueryClose(out int pfCanClose)
        {
            pfCanClose = Common.Constants.TRUE;
            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        int IVsHierarchy.Close()
        {
            Trace.TraceInformation("IVsHierarchy Close called");
            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        int IVsHierarchy.GetGuidProperty(uint itemid, int propid, out Guid pguid)
        {
            pguid = Guid.Empty;
            return VSConstants.E_NOTIMPL;
        }

        /// <inheritdoc/>
        int IVsHierarchy.SetGuidProperty(uint itemid, int propid, ref Guid rguid)
        {
            return VSConstants.E_NOTIMPL;
        }

        /// <inheritdoc/>
        int IVsHierarchy.GetProperty(uint itemid, int propid, out object pvar)
        {
            switch (itemid)
            {
                case (uint)VSConstants.VSITEMID.Root:
                    return this.rootPropertyMap.TryGetValue((__VSHPROPID)propid, out pvar) ? VSConstants.S_OK : VSConstants.E_INVALIDARG;
                default:
                    switch (propid)
                    {
                        case (int)__VSHPROPID.VSHPROPID_Name:
                            var result = this.idPathMap.TryGetValue(itemid, out string path) ? VSConstants.S_OK : VSConstants.E_INVALIDARG;
                            pvar = path;
                            return result;
                        case (int)__VSHPROPID.VSHPROPID_Caption:
                            var result2 = this.idPathMap.TryGetValue(itemid, out string path2) ? VSConstants.S_OK : VSConstants.E_INVALIDARG;
                            pvar = Path.GetFileName(path2);
                            return result2;
                        case (int)__VSHPROPID.VSHPROPID_NextSibling:
                            uint nextItemId = itemid + 1;
                            pvar = nextItemId < this.lastItemId ? nextItemId : (uint)VSConstants.VSITEMID.Nil;
                            return VSConstants.S_OK;
                        case (int)__VSHPROPID.VSHPROPID_BrowseObject:
                            pvar = new ItemProperties { FullPath = this.idPathMap[itemid] };
                            return VSConstants.S_OK;
                        default:
                            return this.itemPropertyMap.TryGetValue((__VSHPROPID)propid, out pvar) ? VSConstants.S_OK : VSConstants.E_INVALIDARG;
                    }
            }
        }

        /// <inheritdoc/>
        int IVsHierarchy.SetProperty(uint itemid, int propid, object var)
        {
            return VSConstants.E_NOTIMPL;
        }

        /// <inheritdoc/>
        int IVsHierarchy.GetNestedHierarchy(uint itemid, ref Guid iidHierarchyNested, out IntPtr ppHierarchyNested, out uint pitemidNested)
        {
            ppHierarchyNested = IntPtr.Zero;
            pitemidNested = 0;
            return VSConstants.E_NOINTERFACE;
        }

        /// <inheritdoc/>
        int IVsHierarchy.GetCanonicalName(uint itemid, out string pbstrName)
        {
            if (itemid == (uint)VSConstants.VSITEMID.Root)
            {
                pbstrName = "Watched Imports";
                return VSConstants.S_OK;
            }

            return ((IVsProject)this).GetMkDocument(itemid, out pbstrName);
        }

        /// <inheritdoc/>
        int IVsHierarchy.ParseCanonicalName(string pszName, out uint pitemid)
        {
            var idPathPair = this.idPathMap.Where(kvp => kvp.Value.Equals(pszName, StringComparison.OrdinalIgnoreCase)).SingleOrDefault();
            if (idPathPair.Value != null)
            {
                pitemid = idPathPair.Key;
                return VSConstants.S_OK;
            }

            Trace.TraceInformation($"{this.GetType().Name}.{nameof(IVsHierarchy.ParseCanonicalName)}: Document not found {pszName}");
            pitemid = 0;
            return VSConstants.E_INVALIDARG;
        }

        /// <inheritdoc/>
        int IVsHierarchy.AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out uint pdwCookie)
        {
            pdwCookie = 0;
            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        int IVsHierarchy.UnadviseHierarchyEvents(uint dwCookie)
        {
            return VSConstants.S_FALSE;
        }

        /// <inheritdoc/>
        int IVsUIHierarchy.SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
        {
            return ((IVsHierarchy)this).SetSite(psp);
        }

        /// <inheritdoc/>
        int IVsUIHierarchy.GetSite(out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP)
        {
            return ((IVsHierarchy)this).GetSite(out ppSP);
        }

        /// <inheritdoc/>
        int IVsUIHierarchy.QueryClose(out int pfCanClose)
        {
            return ((IVsHierarchy)this).QueryClose(out pfCanClose);
        }

        /// <inheritdoc/>
        int IVsUIHierarchy.Close()
        {
            return ((IVsHierarchy)this).Close();
        }

        /// <inheritdoc/>
        int IVsUIHierarchy.GetGuidProperty(uint itemid, int propid, out Guid pguid)
        {
            return ((IVsHierarchy)this).GetGuidProperty(itemid, propid, out pguid);
        }

        /// <inheritdoc/>
        int IVsUIHierarchy.SetGuidProperty(uint itemid, int propid, ref Guid rguid)
        {
            return ((IVsHierarchy)this).SetGuidProperty(itemid, propid, rguid);
        }

        /// <inheritdoc/>
        int IVsUIHierarchy.GetProperty(uint itemid, int propid, out object pvar)
        {
            return ((IVsHierarchy)this).GetProperty(itemid, propid, out pvar);
        }

        /// <inheritdoc/>
        int IVsUIHierarchy.SetProperty(uint itemid, int propid, object var)
        {
            return ((IVsHierarchy)this).SetProperty(itemid, propid, var);
        }

        /// <inheritdoc/>
        int IVsUIHierarchy.GetNestedHierarchy(uint itemid, ref Guid iidHierarchyNested, out IntPtr ppHierarchyNested, out uint pitemidNested)
        {
            return ((IVsHierarchy)this).GetNestedHierarchy(itemid, iidHierarchyNested, out ppHierarchyNested, out pitemidNested);
        }

        /// <inheritdoc/>
        int IVsUIHierarchy.GetCanonicalName(uint itemid, out string pbstrName)
        {
            return ((IVsHierarchy)this).GetCanonicalName(itemid, out pbstrName);
        }

        /// <inheritdoc/>
        int IVsUIHierarchy.ParseCanonicalName(string pszName, out uint pitemid)
        {
            return ((IVsHierarchy)this).ParseCanonicalName(pszName, out pitemid);
        }

        /// <inheritdoc/>
        int IVsUIHierarchy.AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out uint pdwCookie)
        {
            return ((IVsHierarchy)this).AdviseHierarchyEvents(pEventSink, out pdwCookie);
        }

        /// <inheritdoc/>
        int IVsUIHierarchy.UnadviseHierarchyEvents(uint dwCookie)
        {
            return ((IVsHierarchy)this).UnadviseHierarchyEvents(dwCookie);
        }

        /// <inheritdoc/>
        int IVsUIHierarchy.QueryStatusCommand(uint itemid, ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_UNKNOWNGROUP;
        }

        /// <inheritdoc/>
        int IVsUIHierarchy.ExecCommand(uint itemid, ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_UNKNOWNGROUP;
        }

        /// <inheritdoc/>
        int IVsHierarchy.Unused0() => throw new NotImplementedException();

        /// <inheritdoc/>
        int IVsHierarchy.Unused1() => throw new NotImplementedException();

        /// <inheritdoc/>
        int IVsHierarchy.Unused2() => throw new NotImplementedException();

        /// <inheritdoc/>
        int IVsHierarchy.Unused3() => throw new NotImplementedException();

        /// <inheritdoc/>
        int IVsHierarchy.Unused4() => throw new NotImplementedException();

        /// <inheritdoc/>
        int IVsUIHierarchy.Unused0() => throw new NotImplementedException();

        /// <inheritdoc/>
        int IVsUIHierarchy.Unused1() => throw new NotImplementedException();

        /// <inheritdoc/>
        int IVsUIHierarchy.Unused2() => throw new NotImplementedException();

        /// <inheritdoc/>
        int IVsUIHierarchy.Unused3() => throw new NotImplementedException();

        /// <inheritdoc/>
        int IVsUIHierarchy.Unused4() => throw new NotImplementedException();

        private class ItemProperties
        {
            public string FullPath { get; set; }
        }
    }
}
