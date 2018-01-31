//------------------------------------------------------------------------------
// <copyright file="FodeRushPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using FodeRush.Platform.Interfaces;
using FodeRush.SyntaxVisualizer.Interfaces;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace FodeRush.Package {
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(FodeRushPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ToolWindows.SyntaxVisualizer.SyntaxVisualizerToolWindow))]
    public sealed class FodeRushPackage : Microsoft.VisualStudio.Shell.Package, IVsPackage {
        public const string PackageGuidString = "9f06dc0d-8a3e-4173-b32d-edebe6ccf825";

        [ImportMany]
        internal List<IStartable> Startables { get; set; }

        [Import]
        internal ISyntaxVisualizerService SyntaxVisualizer { get; set; }

        public FodeRushPackage() {
        }

        ServiceProvider provider;
        ServiceProvider GetVsServiceProvider() {
            if(provider == null)
                provider = new ServiceProvider(GetGlobalService(typeof(SDTE)) as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            return provider;
        }
        IComponentModel GetComponentModel() {
            ServiceProvider provider = GetVsServiceProvider();
            if(provider == null)
                return null;
            return provider.GetService(typeof(SComponentModel)) as IComponentModel;
        }
        void StartEngine() {
            if(Startables == null)
                return;
            var startables = new List<IStartable>(Startables);
            // direct order
            startables.Sort((s1, s2) => s1.Order - s2.Order);
            startables.ForEach(s => s.Start());
        }
        void StopEngine() {
            if(Startables == null)
                return;
            var startables = new List<IStartable>(Startables);
            // reversive order
            startables.Sort((s1, s2) => s2.Order - s1.Order);
            startables.ForEach(s => s.Stop());
        }
        void SatisfyImports() {
            var componentModel = GetComponentModel();
            if(componentModel != null && componentModel.DefaultCompositionService != null)
                componentModel.DefaultCompositionService.SatisfyImportsOnce(this);
        }
        void LoadAssemblies() {
            // HACK: Force load assemblies
            typeof(System.Windows.Interactivity.Behavior).ToString();
        }

        protected override void Initialize() {
            base.Initialize();

            LoadAssemblies();
            SatisfyImports();
            StartEngine();

            ToolWindows.SyntaxVisualizer.SyntaxVisualizerToolWindowCommand.Initialize(this, SyntaxVisualizer);
        }

        int IVsPackage.Close() {
            StopEngine();
            return 0;
        }
    }
}
