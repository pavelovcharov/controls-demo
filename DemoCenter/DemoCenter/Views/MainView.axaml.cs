using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaEdit.Highlighting;
using DemoCenter.Helpers;
using DemoCenter.ProductsData;
using DemoCenter.ViewModels;

using Eremex.AvaloniaUI.Controls.TreeList;
using Eremex.AvaloniaUI.Themes;

namespace DemoCenter.Views;

public partial class MainView : UserControl
{
    string loadedResource;
    public MainView()
    {
        InitializeComponent();
        pageSelector.AddHandler(TextBox.KeyDownEvent, OnPageSelectorKeyDown, RoutingStrategies.Tunnel);
    }

    MainViewModel ViewModel { get; set; }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        UnsubscribeEvents(ViewModel);
        ViewModel = DataContext as MainViewModel;
        SubscribeEvents(ViewModel);
    }

    private void SubscribeEvents(MainViewModel viewModel)
    {
        if (viewModel == null)
            return;
        titleSubscriber = this.Bind(TitleProperty, new Binding() { Source = viewModel, Path = "Title" });
        viewModel.PropertyChanged += OnMainViewModelPropertyChanged;
    }

    private void UnsubscribeEvents(MainViewModel viewModel)
    {
        if (viewModel == null)
            return;
        titleSubscriber?.Dispose();
        titleSubscriber = null;
        viewModel.PropertyChanged -= OnMainViewModelPropertyChanged;
    }

    private void OnMainViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedPalette) && ViewModel?.SelectedPalette != null)
            (Application.Current as App)?.UpdatePalette(ViewModel.SelectedPalette);

        else if ((e.PropertyName == nameof(MainViewModel.ShowCode) || e.PropertyName == nameof(MainViewModel.SourceFile)) && ViewModel.ShowCode)
        {
            UpdateDocument();
            Dispatcher.UIThread.Post(() => CodeViewEditor.Focus());
        }        
    }

    private void UpdateDocument()
    {
        var name = App.EmbeddedResources.FirstOrDefault(x => x.EndsWith(ViewModel.SourceFile, StringComparison.InvariantCultureIgnoreCase));
        if (string.IsNullOrEmpty(name))
        {
            CodeViewEditor.Clear();
            return;
        }           
        if (loadedResource != name)
            using (var stream = System.Reflection.Assembly.GetAssembly(typeof(App))?.GetManifestResourceStream(name))
            {
                if (name.EndsWith(".cs"))
                    CodeViewEditor.SyntaxHighlighting = new ThemedSyntaxHighlighter("CSharp-Highlight").HighlightingDefinition;
                else if(name.EndsWith(".axaml"))
                    CodeViewEditor.SyntaxHighlighting = (new ThemedSyntaxHighlighter("Axaml-Highlight")).HighlightingDefinition;
                else
                    CodeViewEditor.SyntaxHighlighting = null;
                CodeViewEditor.Load(stream);
                CodeViewEditor.ScrollToHome();
                loadedResource = name;
            }       
    }

    private void OnShowSearchPanel(object sender, RoutedEventArgs e)
    {
        CodeViewEditor.Focus();
        CodeViewEditor.SearchPanel?.Open();
    }

    public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<MainView, string>(nameof(Title));
    
    public string Title
    {
        get { return GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }
    IDisposable titleSubscriber;

    private void OnPageSelectorKeyDown(object sender, KeyEventArgs e)
    {
       if (e.Key == Key.Up)
        {
            var currentIndex = ViewModel.flatProducts.IndexOf(ViewModel.CurrentProductItem);
            if (ViewModel.flatProducts[--currentIndex] is GroupInfo)
            {
                if (--currentIndex > 0)
                    ViewModel.CurrentProductItem = ViewModel.flatProducts[currentIndex];
                e.Handled = true;
            }
        }
    }

    private void OnPageSelectorFocusedNodeChanged(object sender, TreeListFocusedNodeChangedEventArgs e)
    {
        if (e.Node.Content is GroupInfo)
            pageSelector.MoveNextNode();
    }
}

internal class PagesChildrenSelector : ITreeListChildrenSelector
{
    bool ITreeListChildrenSelector.HasChildren(object item) => (item as GroupInfo)?.Pages?.Any() == true;

    IEnumerable ITreeListChildrenSelector.SelectChildren(object item)
    {
        return ((GroupInfo)item).Pages;
    }
}

public class PaletteTypeToIconDataConverter : MarkupExtension, IMultiValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] == null || values[0] == AvaloniaProperty.UnsetValue || values.Count != 3)
            return null;
        var type = (PaletteType)values[0]!;
        if(type == PaletteType.White)
            return values[1];
        else if(type == PaletteType.Black)
            return values[2];
        return null;
    }
}
