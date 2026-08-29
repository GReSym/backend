using System;
using System.Collections.Generic;
using GReSym.Parser.Interfaces;
using Gtk;

namespace GReSym.Parser.UI;

public class ParserWindow : Window
{
    private readonly Builder _builder;
    private readonly List<IParserTab> _tabs = new();

    public ParserWindow(IServiceProvider sp) : base("GReSym Steam Parser")
    {
        _builder = new Builder();
        _builder.AddFromFile("ParserWindow.glade");

        var mainWindow = (Window)_builder.GetObject("ParserWindow");
        Title = mainWindow.Title;
        DefaultSize = mainWindow.DefaultSize;

        var content = mainWindow.Child;
        mainWindow.Remove(content);
        Add(content);

        var notebook = (Notebook)_builder.GetObject("main_notebook");

        // Создаем вкладки
        var gamesTab = new SteamGamesTab(sp, sp.GetService(typeof(IParserEngine)) as IParserEngine, _builder);
        var reviewsTab = new SteamReviewsTab(sp, sp.GetService(typeof(IParserEngine)) as IParserEngine, _builder);
        var settingsTab = new SettingsTab(_builder);
        var checkpointsTab = new CheckpointsTab(_builder);

        _tabs.AddRange(gamesTab, reviewsTab, settingsTab, checkpointsTab);

        foreach (var tab in _tabs)
        {
            tab.Initialize();
        }

        ShowAll();
        DeleteEvent += (s, e) => Application.Quit();
    }
}