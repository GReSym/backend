using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GReSym.Parser.Enums;
using GReSym.Parser.Interfaces;
using GReSym.Parser.Models;
using Gtk;

namespace GReSym.Parser.UI;

public class SteamReviewsTab : IParserTab
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IParserEngine _parserEngine;
    private readonly Builder _builder;

    private TextView _logView;
    private TextBuffer _logBuffer;
    private Button _startButton, _pauseButton, _resumeButton, _stopButton;
    private ProgressBar _progressBar;
    private RadioButton _fullModeRadio, _incrementalModeRadio;
    private Entry _batchSizeEntry, _threadsCountEntry;
    private Entry _steamApiEntry, _minAppIdEntry, _maxAppIdEntry;
    private ComboBoxText _checkpointsComboBox;
    private Label _checkpointInfo;

    private ParserContext _currentContext;
    private CancellationTokenSource _cts;
    private bool _isResuming = false;
    private int _lastWarningCount = 0;

    public SteamReviewsTab(IServiceProvider sp, IParserEngine engine, Builder builder)
    {
        _serviceProvider = sp;
        _parserEngine = engine;
        _builder = builder;
    }

    public Widget GetWidget() => (Widget)_builder.GetObject("steam_reviews_tab");

    public void Initialize()
    {
        // Получаем элементы из Glade
        _logView = (TextView)_builder.GetObject("steam_reviews_parse_log_textview");
        _logBuffer = _logView.Buffer;

        _progressBar = (ProgressBar)_builder.GetObject("steam_reviews_parse_progressbar");

        _startButton = (Button)_builder.GetObject("steam_reviews_parse_start");
        _pauseButton = (Button)_builder.GetObject("steam_reviews_parse_pause");
        _resumeButton = (Button)_builder.GetObject("steam_reviews_parse_continue");
        _stopButton = (Button)_builder.GetObject("steam_reviews_parse_stop");

        _fullModeRadio = (RadioButton)_builder.GetObject("parse_mode_full_radiobutton");
        _incrementalModeRadio = (RadioButton)_builder.GetObject("parse_mode_incremental_radiobutton");

        _batchSizeEntry = (Entry)_builder.GetObject("batch_size_entry");
        _threadsCountEntry = (Entry)_builder.GetObject("threads_count_entry");
        _steamApiEntry = (Entry)_builder.GetObject("steam_api_entry");

        _minAppIdEntry = (Entry)_builder.GetObject("min_game_id");
        _maxAppIdEntry = (Entry)_builder.GetObject("max_game_id");

        _checkpointsComboBox = (ComboBoxText)_builder.GetObject("checkpoints_combobox");
        _checkpointInfo = (Label)_builder.GetObject("checkpoint_info");

        SetButtonsState(start: true, pause: false, resume: false, stop: false);

        _startButton.Clicked += OnStartClicked;
        _pauseButton.Clicked += OnPauseClicked;
        _resumeButton.Clicked += OnResumeClicked;
        _stopButton.Clicked += OnStopClicked;

        _parserEngine.ProgressChanged += OnParsingProgressChanged;

        // Загружаем чекпоинты
        _checkpointsComboBox.RemoveAll();
        foreach (var cp in _parserEngine.GetCheckpoints())
            _checkpointsComboBox.AppendText(cp);

        LogMessage("Вкладка парсинга отзывов инициализирована");
    }


    private async void OnStartClicked(object sender, EventArgs e)
    {
        int minId = int.TryParse(_minAppIdEntry.Text, out var mi) ? mi : 1;
        int maxId = int.TryParse(_maxAppIdEntry.Text, out var ma) ? ma : 500;

        var ids = Enumerable.Range(minId, maxId - minId + 1).Select(x => x.ToString()).ToList();

        string checkpoint = _checkpointsComboBox.ActiveText;
        if (string.IsNullOrEmpty(checkpoint))
            checkpoint = $"steam-reviews-parser-{DateTime.Now:yyyyMMdd-HHmmss}";

        _currentContext = new ParserContext
        {
            Source = "steam reviews",
            Mode = _incrementalModeRadio.Active ? ParserMode.Incremental : ParserMode.Full,
            Identifiers = ids,
            CheckpointName = checkpoint,
            BatchSize = int.TryParse(_batchSizeEntry.Text, out var b) ? b : 10,
            MaxDegreeOfParallelism = int.TryParse(_threadsCountEntry.Text, out var t) ? t : 5,
            AutoSaveCheckpoint = true,
            DelayBetweenBatchesMs = 5000,
            ApiKey = _steamApiEntry.Text?.Trim()
        };

        SetButtonsState(start: false, pause: true, resume: false, stop: true);
        _progressBar.Fraction = 0;

        await StartParsingAsync();
    }

    private async Task StartParsingAsync()
    {
        _isResuming = false;
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        try
        {
            var result = await _parserEngine.ParseAsync(_currentContext, _cts.Token);
            HandleParsingResult(result);
        }
        catch (OperationCanceledException)
        {
            if (_isResuming) LogMessage("Парсинг приостановлен, готов к возобновлению");
            else { LogMessage("Парсинг остановлен"); SetButtonsState(true, false, false, false); }
        }
        catch (Exception ex) { LogError(ex.Message); SetButtonsState(true, false, false, false); }
    }

    private async void OnPauseClicked(object sender, EventArgs e)
    {
        _isResuming = true;
        _cts?.Cancel();
        await _parserEngine.PauseAsync();
        SetButtonsState(false, false, true, true);
        LogMessage("Парсинг приостановлен");
    }

    private async void OnResumeClicked(object sender, EventArgs e)
    {
        if (_currentContext == null) return;
        await _parserEngine.ResumeAsync();
        SetButtonsState(false, true, false, true);
        await StartParsingAsync();
    }

    private async void OnStopClicked(object sender, EventArgs e)
    {
        _isResuming = false;
        _cts?.Cancel();
        await _parserEngine.StopAsync();
        SetButtonsState(true, false, false, false);
        LogMessage("Парсинг остановлен");
    }

    private void OnParsingProgressChanged(object sender, ParsingProgressEventArgs e)
    {
        Application.Invoke((s, a) =>
        {
            double frac = e.TotalItems > 0 ? (double)e.ProcessedItems / e.TotalItems : 0;
            _progressBar.Fraction = frac;
            _progressBar.Text = $"Обработано {e.ProcessedItems}/{e.TotalItems}";

            if (e.ParsingResult?.HasWarnings == true && e.ParsingResult.Warnings.Count > _lastWarningCount)
            {
                foreach (var w in e.ParsingResult.Warnings.Skip(_lastWarningCount))
                    LogMessage($"[ВНИМАНИЕ] {w}");
                _lastWarningCount = e.ParsingResult.Warnings.Count;
            }
        });
    }

    private void HandleParsingResult(ParsingResult result)
    {
        Application.Invoke((s, a) =>
        {
            LogMessage($"=== Парсинг завершен: {result.Status} ===");
            LogMessage($"Отзывов добавлено: {result.ReviewsAdded}");
            SetButtonsState(result.Status == ParserStatus.Paused ? false : true, false, result.Status == ParserStatus.Paused, false);
        });
    }

    private void SetButtonsState(bool start, bool pause, bool resume, bool stop)
    {
        _startButton.Sensitive = start;
        _pauseButton.Sensitive = pause;
        _resumeButton.Sensitive = resume;
        _stopButton.Sensitive = stop;
    }

    private void LogMessage(string msg)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _logBuffer.Insert(_logBuffer.EndIter, $"[{timestamp}] {msg}\n");
        _logView.ScrollToMark(_logBuffer.CreateMark("end", _logBuffer.EndIter, false), 0, false, 0, 0);
    }

    private void LogError(string msg) => LogMessage($"[ERROR] {msg}");
}