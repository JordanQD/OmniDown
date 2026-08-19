namespace OmniDown.ViewModels;

using OmniDown.Dialogs;
using OmniDown.Models;
using OmniDown.Services.Downloads;
using OmniDown.Services.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

public enum NewDownloadTaskType
{
    Link,
    Torrent
}

internal sealed class NewDownloadDialogViewModel : INotifyPropertyChanged
{
    private readonly string _defaultDownloadDirectory;
    private NewDownloadTaskType _taskType;
    private string _urlText;
    private string _taskName;
    private string _downloadDirectory;
    private double _splitCount;
    private string _uriValidationMessage = string.Empty;
    private string _torrentValidationMessage = string.Empty;
    private NewDownloadTorrentSelection? _torrentSelection;
    private bool? _selectAllTorrentFilesState = true;
    private bool _isUpdatingTorrentSelection;

    public NewDownloadDialogViewModel(
        string defaultDownloadDirectory,
        int splitCount,
        string? initialDownloadText,
        string? initialTaskName)
    {
        _defaultDownloadDirectory = defaultDownloadDirectory;
        _downloadDirectory = defaultDownloadDirectory;
        _splitCount = Math.Clamp(splitCount, 1, 256);
        _urlText = initialDownloadText ?? string.Empty;
        _taskName = initialTaskName?.Trim() ?? string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TorrentFileEntry> TorrentFiles { get; } = [];

    public NewDownloadTaskType TaskType
    {
        get => _taskType;
        set
        {
            if (SetProperty(ref _taskType, value))
            {
                ClearValidationMessages();
            }
        }
    }

    public string UrlText
    {
        get => _urlText;
        set
        {
            if (SetProperty(ref _urlText, value))
            {
                UriValidationMessage = string.Empty;
            }
        }
    }

    public string TaskName
    {
        get => _taskName;
        set => SetProperty(ref _taskName, value);
    }

    public string DownloadDirectory
    {
        get => _downloadDirectory;
        set => SetProperty(ref _downloadDirectory, value);
    }

    public double SplitCount
    {
        get => _splitCount;
        set => SetProperty(ref _splitCount, value);
    }

    public string UriValidationMessage
    {
        get => _uriValidationMessage;
        private set => SetProperty(ref _uriValidationMessage, value);
    }

    public string TorrentValidationMessage
    {
        get => _torrentValidationMessage;
        private set => SetProperty(ref _torrentValidationMessage, value);
    }

    public bool HasTorrent => _torrentSelection is not null;

    public string TorrentDisplayName =>
        _torrentSelection?.DisplayName ?? Strings.Get("TorrentNoFileSelectedText");

    public bool? SelectAllTorrentFilesState
    {
        get => _selectAllTorrentFilesState;
        private set => SetProperty(ref _selectAllTorrentFilesState, value);
    }

    public NewDownloadTorrentSelection? TorrentSelection => _torrentSelection;

    public void SetTorrentSelection(NewDownloadTorrentSelection selection)
    {
        UnsubscribeFromTorrentFiles();
        _torrentSelection = selection;
        TorrentFiles.Clear();
        foreach (TorrentFileEntry file in selection.Metadata.Files)
        {
            file.IsSelected = true;
            file.PropertyChanged += TorrentFile_PropertyChanged;
            TorrentFiles.Add(file);
        }

        TaskType = NewDownloadTaskType.Torrent;
        TorrentValidationMessage = string.Empty;
        UpdateTorrentSelectionState();
        OnPropertyChanged(nameof(HasTorrent));
        OnPropertyChanged(nameof(TorrentDisplayName));
        OnPropertyChanged(nameof(TorrentSelection));
    }

    public void ClearTorrentSelection()
    {
        UnsubscribeFromTorrentFiles();
        _torrentSelection = null;
        TorrentFiles.Clear();
        TorrentValidationMessage = string.Empty;
        SelectAllTorrentFilesState = false;
        OnPropertyChanged(nameof(HasTorrent));
        OnPropertyChanged(nameof(TorrentDisplayName));
        OnPropertyChanged(nameof(TorrentSelection));
    }

    public void SetAllTorrentFilesSelected(bool isSelected)
    {
        _isUpdatingTorrentSelection = true;
        try
        {
            foreach (TorrentFileEntry file in TorrentFiles)
            {
                file.IsSelected = isSelected;
            }
        }
        finally
        {
            _isUpdatingTorrentSelection = false;
        }

        TorrentValidationMessage = string.Empty;
        UpdateTorrentSelectionState();
    }

    public bool Validate(IReadOnlyList<string> sourceUris)
    {
        ClearValidationMessages();
        if (TaskType == NewDownloadTaskType.Link)
        {
            if (sourceUris.Count > 0)
            {
                return true;
            }

            UriValidationMessage = Strings.Get("DownloadUrlRequiredMessage");
            return false;
        }

        if (_torrentSelection is null)
        {
            TorrentValidationMessage = Strings.Get("TorrentFileRequiredMessage");
            return false;
        }

        if (!TorrentFiles.Any(file => file.IsSelected))
        {
            TorrentValidationMessage = Strings.Get("TorrentFileSelectionRequiredMessage");
            return false;
        }

        return true;
    }

    public NewDownloadDialogResult CreateResult(IReadOnlyList<string> sourceUris)
    {
        int splitCount = double.IsNaN(SplitCount)
            ? 1
            : Math.Clamp((int)Math.Round(SplitCount), 1, 256);
        string saveDirectory = string.IsNullOrWhiteSpace(DownloadDirectory)
            ? _defaultDownloadDirectory
            : DownloadDirectory.Trim();
        int[] selectedIndexes = TaskType == NewDownloadTaskType.Torrent
            ? TorrentFiles.Where(file => file.IsSelected).Select(file => file.Index).ToArray()
            : [];

        return new NewDownloadDialogResult(
            sourceUris.ToArray(),
            sourceUris.Count == 1 ? TaskName : string.Empty,
            saveDirectory,
            splitCount,
            TaskType == NewDownloadTaskType.Torrent ? _torrentSelection : null,
            selectedIndexes,
            TorrentFiles.Count);
    }

    public List<string> ParseSourceUris()
    {
        List<string> sources = DownloadSourceParser.ParseLines(UrlText);
        foreach (string source in sources)
        {
            if (Ed2kLinkParser.IsEd2kLink(source) && !Ed2kLinkParser.TryParseFileLink(source, out _))
            {
                throw new FormatException("ED2K file link is invalid.");
            }
        }

        return sources;
    }

    public void SetValidationException(Exception exception)
    {
        UriValidationMessage = UserErrorMessages.Create(UserErrorContext.AddTask, exception).Message;
    }

    public void SetTorrentValidationException(Exception exception)
    {
        TorrentValidationMessage = UserErrorMessages.Create(UserErrorContext.AddTask, exception).Message;
    }

    private void TorrentFile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_isUpdatingTorrentSelection && e.PropertyName == nameof(TorrentFileEntry.IsSelected))
        {
            TorrentValidationMessage = string.Empty;
            UpdateTorrentSelectionState();
        }
    }

    private void UpdateTorrentSelectionState()
    {
        SelectAllTorrentFilesState = TorrentFiles.Count == 0
            ? false
            : TorrentFiles.All(file => file.IsSelected)
                ? true
                : TorrentFiles.Any(file => file.IsSelected)
                    ? null
                    : false;
    }

    private void ClearValidationMessages()
    {
        UriValidationMessage = string.Empty;
        TorrentValidationMessage = string.Empty;
    }

    private void UnsubscribeFromTorrentFiles()
    {
        foreach (TorrentFileEntry file in TorrentFiles)
        {
            file.PropertyChanged -= TorrentFile_PropertyChanged;
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
