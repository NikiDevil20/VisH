using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VisH.Model;
using VisH.Model.WPFDisplayObjects;
using VisH.ViewModel;
using ScottPlot;

namespace VisH.View.UserControls.MainWindow;

public partial class Overview : UserControl
{
    
    public Overview()
    {
        InitializeComponent();
        Loaded += (_, _) => Dispatcher.BeginInvoke(
            UpdateSpectrumPlot,
            DispatcherPriority.Loaded);
        DataContextChanged += (_, _) => Dispatcher.BeginInvoke(
            UpdateSpectrumPlot,
            DispatcherPriority.DataBind);

        // PythonBridge pythonBridge = new PythonBridge();
        // LogFileAnalyzer logFileAnalyzer = new LogFileAnalyzer(pythonBridge);
        //
        // DataContext = new OverviewViewModel(logFileAnalyzer);
        
    }
    
    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var viewModel = DataContext as OverviewViewModel;
        if (viewModel is null)
        {
            return;
        }

        viewModel.SelectedNode = e.NewValue as TreeNode;
        UpdateSpectrumPlot();
    }

    private void UpdateSpectrumPlot()
    {
        var viewModel = DataContext as OverviewViewModel;
        SpectrumPlot.Plot.Clear();
        if (viewModel is null || !viewModel.IsSpectrumVisible)
        {
            SpectrumPlot.Refresh();
            return;
        }

        var scatter = SpectrumPlot.Plot.Add.Scatter(viewModel.SpectrumWavelengths, viewModel.SpectrumIntensities);
        scatter.MarkerSize = 0;
        scatter.LineWidth = 2;
        
        SpectrumPlot.Plot.Title(viewModel.SpectrumType);
        SpectrumPlot.Plot.XLabel("Wavelength / nm");
        SpectrumPlot.Plot.YLabel("Intensity / L mol⁻¹ cm⁻¹");
        var maximumIntensity = viewModel.SpectrumIntensities.Max();
        if (double.IsNaN(maximumIntensity) || double.IsInfinity(maximumIntensity) || maximumIntensity <= 0)
        {
            maximumIntensity = 1;
        }

        SpectrumPlot.Plot.Axes.SetLimits(
            viewModel.SpectrumWavelengths.Min(),
            viewModel.SpectrumWavelengths.Max(),
            0,
            maximumIntensity * 1.05);
        SpectrumPlot.Refresh();
    }
    
}
