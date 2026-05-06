using System.Collections.Generic;

namespace OmniDown.Models;

public sealed record TorrentMetadata(
    string Name,
    IReadOnlyList<TorrentFileEntry> Files);
