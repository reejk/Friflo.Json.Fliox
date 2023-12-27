﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Friflo.Editor.UI.Panels;
using Friflo.Engine.ECS;

namespace Friflo.Editor;

public abstract class AppEvents
{
#region public properties
    public              EntityStore             Store    => store;
    #endregion
    
    protected           EntityStore             store;
    protected readonly  List<EditorObserver>    observers   = new List<EditorObserver>();
    protected           bool                    isReady;

    public static Func<Window>  createMainWindowFunc;
    public static Window        window;
    public static AppEvents     appEvents;
    
    public static Window CreateMainWindow() {
        window = createMainWindowFunc();
        return window;
    }
    
    
    public void AddObserver(EditorObserver observer)
    {
        observers.Add(observer);
        if (isReady) {
            observer.SendEditorReady();  // could be deferred to event loop
        }
    }
    
    public void SelectionChanged(EditorSelection selection) {
        StoreDispatcher.Post(() => {
            EditorObserver.CastSelectionChanged(observers, selection);    
        });
    }
    
    // -------------------------------------- panel / commands --------------------------------------
    protected PanelControl activePanel;
    
    internal void SetActivePanel(PanelControl panel)
    {
        if (activePanel != null) {
            activePanel.Header.PanelActive = false;
        }
        activePanel = panel;
        if (panel != null) {
            panel.Header.PanelActive = true;
        }
    }
}