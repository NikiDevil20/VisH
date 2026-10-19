using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using VisH.Model;
using VisH.Model.GeneralUtils;
using VisH.Model.WPFDisplayObjects;
using VisH.ViewModel;
using ScottPlot;

namespace VisH.View.UserControls.MainWindow;

public partial class Overview : UserControl
{
    private WebView2? _moleculeViewer;
    private long _moleculeViewerRequestId;
    private long _pendingMoleculeViewerRequestId;
    private string? _pendingMoleculeViewerPayload;
    
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
        MoleculeViewerOrbitalMode.SelectedValue = viewModel.MoleculeViewerOrbitalMode;
        UpdateSpectrumPlot();
        var requestId = ++_moleculeViewerRequestId;
        if (viewModel.IsMoleculeViewerVisible &&
            !string.IsNullOrWhiteSpace(viewModel.MoleculeViewerPayloadJson))
        {
            _ = UpdateMoleculeViewerAsync(
                viewModel,
                requestId,
                viewModel.MoleculeViewerPayloadJson);
        }
        else
        {
            _pendingMoleculeViewerPayload = null;
            _ = ClearMoleculeViewerAsync();
        }
    }

    private async Task UpdateMoleculeViewerAsync(
        OverviewViewModel viewModel,
        long requestId,
        string payload)
    {
        if (!IsCurrentMoleculeViewerRequest(viewModel, requestId, payload))
        {
            return;
        }

        try
        {
            _pendingMoleculeViewerPayload = payload;
            _pendingMoleculeViewerRequestId = requestId;
            _moleculeViewer ??= CreateMoleculeViewer();
            await _moleculeViewer.EnsureCoreWebView2Async();
            if (!IsCurrentMoleculeViewerRequest(viewModel, requestId, payload))
            {
                return;
            }

            _moleculeViewer.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.local",
                AppContext.BaseDirectory,
                CoreWebView2HostResourceAccessKind.Allow);
            _moleculeViewer.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "molecule.local",
                Config.Load().LocalRechnungenPath,
                CoreWebView2HostResourceAccessKind.Allow);
            _moleculeViewer.NavigationCompleted -= MoleculeViewerOnNavigationCompleted;
            _moleculeViewer.NavigationCompleted += MoleculeViewerOnNavigationCompleted;
            _moleculeViewer.CoreWebView2.Navigate("https://app.local/WebView/MoleculeViewer.html");
        }
        catch (Exception exception)
        {
            if (IsCurrentMoleculeViewerRequest(viewModel, requestId, payload))
            {
                viewModel.SetMoleculeViewerError(
                    $"Molekülansicht konnte nicht initialisiert werden: {exception.Message}");
            }
        }
    }

    private async void MoleculeViewerOnNavigationCompleted(
        object? sender,
        CoreWebView2NavigationCompletedEventArgs e)
    {
        if (DataContext is not OverviewViewModel viewModel ||
            !viewModel.IsMoleculeViewerVisible ||
            string.IsNullOrWhiteSpace(_pendingMoleculeViewerPayload) ||
            _pendingMoleculeViewerRequestId != _moleculeViewerRequestId)
        {
            return;
        }

        if (!e.IsSuccess)
        {
            viewModel.SetMoleculeViewerError(
                $"Molekülansicht konnte nicht geladen werden: {e.WebErrorStatus}.");
            return;
        }

        var payload = JsonSerializer.Serialize(_pendingMoleculeViewerPayload);
        if (_moleculeViewer is null)
        {
            return;
        }

        try
        {
            if (!string.Equals(
                    viewModel.MoleculeViewerPayloadJson,
                    _pendingMoleculeViewerPayload,
                    StringComparison.Ordinal))
            {
                return;
            }

            var result = await _moleculeViewer.ExecuteScriptAsync(
                $"(() => {{ try {{ setMoleculeData(JSON.parse({payload})); return ''; }} " +
                $"catch (error) {{ return String(error); }} }})()");
            if (!string.IsNullOrWhiteSpace(result) && result != "\"\"")
            {
                viewModel.SetMoleculeViewerError(
                    $"Molekülansicht konnte nicht initialisiert werden: {result.Trim('"')}.");
            }
        }
        catch (Exception exception)
        {
            viewModel.SetMoleculeViewerError(
                $"Molekülansicht konnte nicht aktualisiert werden: {exception.Message}");
        }
    }

    private async Task ClearMoleculeViewerAsync()
    {
        if (_moleculeViewer?.CoreWebView2 is null)
        {
            return;
        }

        try
        {
            await _moleculeViewer.ExecuteScriptAsync("window.clearMoleculeData?.();");
        }
        catch
        {
            // A hidden viewer must not affect the active TimeDependent view.
        }
    }

    private async void MoleculeViewerOrbitalMode_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (DataContext is not OverviewViewModel viewModel ||
            !viewModel.IsMoleculeViewerVisible ||
            MoleculeViewerOrbitalMode.SelectedValue is not string mode ||
            _moleculeViewer?.CoreWebView2 is null)
        {
            return;
        }

        try
        {
            await _moleculeViewer.ExecuteScriptAsync(
                $"setOrbitalMode({JsonSerializer.Serialize(mode)});");
        }
        catch (Exception exception)
        {
            viewModel.SetMoleculeViewerError(
                $"Orbitalauswahl konnte nicht angewendet werden: {exception.Message}");
        }
    }

    private bool IsCurrentMoleculeViewerRequest(
        OverviewViewModel viewModel,
        long requestId,
        string payload) =>
        requestId == _moleculeViewerRequestId &&
        ReferenceEquals(DataContext, viewModel) &&
        viewModel.IsMoleculeViewerVisible &&
        string.Equals(viewModel.MoleculeViewerPayloadJson, payload, StringComparison.Ordinal);

    private WebView2 CreateMoleculeViewer()
    {
        var viewer = new WebView2();
        MoleculeViewerHost.Content = viewer;
        return viewer;
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
