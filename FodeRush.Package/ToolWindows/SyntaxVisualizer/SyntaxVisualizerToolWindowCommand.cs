using System;
using System.ComponentModel.Design;
using FodeRush.SyntaxVisualizer.Interfaces;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace FodeRush.Package.ToolWindows.SyntaxVisualizer {
    internal sealed class SyntaxVisualizerToolWindowCommand {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("daeb4ece-67c1-4170-9b0c-04fc07bf88ad");
        readonly Microsoft.VisualStudio.Shell.Package package;
        readonly ISyntaxVisualizerService service;
        SyntaxVisualizerToolWindowCommand(Microsoft.VisualStudio.Shell.Package package, ISyntaxVisualizerService service) {
            if(package == null) {
                throw new ArgumentNullException("package");
            }
            if(service == null) {
                throw new ArgumentNullException("service");
            }
            this.package = package;
            this.service = service;

            var commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if(commandService != null) {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.ShowToolWindow, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        public static SyntaxVisualizerToolWindowCommand Instance { get; private set; }
        private IServiceProvider ServiceProvider => this.package;
        public static void Initialize(Microsoft.VisualStudio.Shell.Package package, ISyntaxVisualizerService service) {
            Instance = new SyntaxVisualizerToolWindowCommand(package, service);
        }

        private void ShowToolWindow(object sender, EventArgs e) {
            var window = this.package.FindToolWindow(typeof(SyntaxVisualizerToolWindow), 0, true) as SyntaxVisualizerToolWindow;
            if((null == window) || (null == window.Frame)) {
                throw new NotSupportedException("Cannot create tool window");
            }
            window.Initialize(service);
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
