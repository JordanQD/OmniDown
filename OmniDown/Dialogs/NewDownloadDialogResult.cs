namespace OmniDown.Dialogs;

using OmniDown.Models;
using System.Collections.Generic;

internal sealed record NewDownloadTorrentSelection(
    string Path,
    string DisplayName,
    byte[] Bytes,
    TorrentMetadata Metadata);

internal sealed record NewDownloadDialogResult(
    IReadOnlyList<string> SourceUris,
    string RequestedName,
    string SaveDirectory,
    int SplitCount,
    NewDownloadTorrentSelection? Torrent,
    IReadOnlyList<int> SelectedTorrentFileIndexes,
    int TorrentFileCount)
{
    public bool IsTorrentTask => Torrent is not null;
}
