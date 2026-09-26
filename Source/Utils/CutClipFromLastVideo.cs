using System.IO;
using System.Threading.Tasks;
using Celeste.Mod.Vidcutter.Exceptions;
using Celeste.Mod.Vidcutter.Models;

namespace Celeste.Mod.Vidcutter.Utils;

public class CutClipFromLastVideo {
    private readonly string _videoFolder;
    private readonly VideoFile _lastVideoFile;
    private readonly GameplayClip _clip;
    private readonly FFmpegService _ffmpegService;
    
    public CutClipFromLastVideo(FFmpegService ffmpegService, string videoFolder, GameplayClip clip) {
        _ffmpegService = ffmpegService;
        if (!Directory.Exists(videoFolder))
            throw new VideoProcessingException("VIDCUTTER_TOOLTIP_VIDEO_FOLDER_NOT_FOUND");

        string lastVideo = VideoUtils.GetLastVideoFile(videoFolder);
        if (lastVideo == null)
            throw new VideoProcessingException("VIDCUTTER_TOOLTIP_VIDEO_NOT_FOUND");
        
        _videoFolder = videoFolder;
        VideoFileRepository videoFileRepository = new(ffmpegService);
        _lastVideoFile = videoFileRepository.Get(Path.Combine(videoFolder, lastVideo));
        _clip = clip;
        Validate();
    }
    
    private void Validate() {
        if (!_lastVideoFile.CanBeProcessed)
            throw new VideoProcessingException("VIDCUTTER_TOOLTIP_INVALID_FORMAT_FOR_CLIPPING");
        
        if (!_clip.IsInVideo(_lastVideoFile))
            throw new VideoProcessingException("VIDCUTTER_TOOLTIP_STATE_NOT_FOUND");
    }

    public void Execute(IProgress progress) {
        string output = VideoUtils.GetOutputVideoName(_videoFolder, _clip.Level);
        
        progress.Message = Dialog.Clean("VIDCUTTER_TOOLTIP_PROCESSING_VIDEO");
        progress.Task = new Task(() =>
            _ffmpegService.CutClip(
                _lastVideoFile.FilePath,
                _clip.StartTimeWithDelay - _lastVideoFile.CreationTime,
                _clip.EndTimeWithDelay - _lastVideoFile.CreationTime,
                output,
                progress
            )
        );
        progress.MessageOnComplete = output + " " + Dialog.Clean("VIDCUTTER_TOOLTIP_PROCESSED_VIDEO");
        
        progress.StartAfterDelay(_lastVideoFile.IsStillWriting() ? 5f : 0f);
    }
}