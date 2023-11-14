﻿using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Friflo.Fliox.Editor.UI.Explorer;

// ReSharper disable MergeIntoPattern
// ReSharper disable UnusedParameter.Local
namespace Friflo.Fliox.Editor.UI.Panels;

public partial class ExplorerPanel : UserControl, IEditorControl
{
    public  Editor          Editor      { get; private set; }
    public  ExplorerItem    RootItem    => GetRootItem();
    
    public ExplorerPanel()
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel();
        DataContext = viewModel;
        DockPanel.ContextFlyout = new ExplorerFlyout(this);
    }
    
    private ExplorerItem GetRootItem() {
        var source = (HierarchicalTreeDataGridSource<ExplorerItem>)DragDrop.Source!;
        return source.Items.First();
    }

    protected override void OnLoaded(RoutedEventArgs e) {
        base.OnLoaded(e);
        Editor = this.GetEditor(SetupExplorer);
    }
    
    /// <summary>
    /// Set <see cref="HierarchicalTreeDataGridSource{TModel}.Items"/> of <see cref="ExplorerViewModel.ExplorerItemSource"/>
    /// </summary>
    private void SetupExplorer()
    {
        if (Editor.Store == null) throw new InvalidOperationException("expect Store is present");
        // return;
        var source      = (HierarchicalTreeDataGridSource<ExplorerItem>)DragDrop.Source!;
        var rootEntity  = Editor.Store.StoreRoot;
        var tree        = new ExplorerTree(rootEntity);
        source.Items    = new []{ tree.rootItem };
    }

    private void DragDrop_OnRowDragStarted(object sender, TreeDataGridRowDragStartedEventArgs e)
    {
        foreach (ExplorerItem item in e.Models)
        {
            if (!item.AllowDrag) {
                e.AllowedEffects = DragDropEffects.None;
            }
        }
    }

    private void DragDrop_OnRowDragOver(object sender, TreeDataGridRowDragEventArgs e)
    {
        // Console.WriteLine($"OnRowDragOver: {e.Position} {e.TargetRow.Model}");
        if (e.TargetRow.Model is ExplorerItem explorerItem)
        {
            if (!explorerItem.IsRoot) {
                return;
            }
            if (e.Position == TreeDataGridRowDropPosition.Inside) {
                return;
            }
        }
        e.Inner.DragEffects = DragDropEffects.None;
    }
}
