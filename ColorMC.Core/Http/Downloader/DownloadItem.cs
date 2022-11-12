﻿namespace ColorMC.Core.Http.Downloader;

public enum DownloadItemState
{
    Wait, Download, Init, Action, Done,
    Error,
}

public record DownloadItem
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string Local { get; set; }
    public string? SHA1 { get; set; }
    public bool Overwrite { get; set; } = false;
    public long AllSize { get; set; }
    public long NowSize { get; set; }
    public DownloadItemState State { get; set; } = DownloadItemState.Init;
    public Action Later { get; set; }

    public Action Update { get; set; }
}
