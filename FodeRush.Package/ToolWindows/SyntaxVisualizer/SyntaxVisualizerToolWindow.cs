namespace FodeRush.Package.ToolWindows.SyntaxVisualizer {
    using System;
    using System.Runtime.InteropServices;
    using FodeRush.SyntaxVisualizer.Interfaces;
    using Microsoft.VisualStudio.Shell;

    [Guid("7f1f9d6c-8d28-4c46-b568-e74ae1f53a64")]
    public class SyntaxVisualizerToolWindow : ToolWindowPane {
        readonly ViewModels.SyntaxVisualizerViewModel viewModel;
        public SyntaxVisualizerToolWindow() : base(null) {
            Caption = "F# SyntaxVisualizer";
            viewModel = new ViewModels.SyntaxVisualizerViewModel();
            var view = new ViewModels.SyntaxVisualizerView();
            view.DataContext = viewModel;
            Content = view;
        }
        public void Initialize(ISyntaxVisualizerService service) {
            viewModel.Initialize(service);
        }
    }
}
