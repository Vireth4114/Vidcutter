using System;
using System.Collections.Generic;
using System.IO;
using static Celeste.Mod.Vidcutter.Utils.FileConstants;

namespace Celeste.Mod.Vidcutter.Services;

public class VideoDurationProvider {
    private readonly string _durationCacheFile = Path.Combine(VidcutterWorkingDirectory, "durationCache.txt");
    private readonly Dictionary<string, TimeSpan> _durationCache = new();
    private readonly FFmpegService _ffmpegService;

    public VideoDurationProvider(FFmpegService ffmpegService) {
        if (!File.Exists(_durationCacheFile))
            return;

        _durationCache.Clear();
        string[] lines = File.ReadAllLines(_durationCacheFile);
        foreach (string line in lines) {
            string[] parts = line.Split(" | ");
            if (parts.Length == 2) {
                _durationCache[parts[0]] = TimeSpan.Parse(parts[1]);
            }
        }
        _ffmpegService = ffmpegService;
    }
    
    public bool TryGetVideoDuration(string filePath, out TimeSpan duration, out bool canVideoBeProcessed) {
        canVideoBeProcessed = true;
        if (_durationCache.TryGetValue(filePath, out duration)) {
            return true;
        }
        
        string strDuration;
        try {
            strDuration = _ffmpegService.GetDurationFromFFprobe(filePath);
        } catch (InvalidOperationException) {
            // If ffprobe throw an exception on the video, ffmpeg can't process it either
            canVideoBeProcessed = false;
            return false;
        }

        if (!double.TryParse(strDuration, out double durationDouble) || durationDouble <= 0) {
            // ffprobe can process the video but doesn't know its duration, may be a running mkv or missing metadata
            return false;
        }
        
        duration = TimeSpan.FromSeconds(durationDouble);
        WriteCacheInFile(filePath, duration);
        return true;
    }


    private void WriteCacheInFile(string video, TimeSpan duration) {
        if (!_durationCache.TryAdd(video, duration))
            return;
        
        using StreamWriter writer = new(_durationCacheFile, false);
        foreach (KeyValuePair<string, TimeSpan> entry in _durationCache)
            writer.WriteLine($"{entry.Key} | {entry.Value}");
    }
}