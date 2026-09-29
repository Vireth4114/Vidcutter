using Celeste.Mod.Vidcutter.Exceptions;
using Celeste.Mod.Vidcutter.Models;
using Celeste.Mod.Vidcutter.Progress.Tooltips;
using Celeste.Mod.Vidcutter.Services;
using Celeste.Mod.Vidcutter.Services.GameplayClips;
using Celeste.Mod.Vidcutter.Tasks;
using Celeste.Mod.Vidcutter.Tasks.FFmpegInstallation;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Vidcutter.Hooks;

public class HotkeyHooks(VidcutterModuleSettings settings, LoggingState state) : IHook {
    private static LoggingState _state;
    private static VidcutterModuleSettings _settings;

    private static void OnUpdate(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime) {
        if (_settings.CutFromLastSaveState.Pressed) {
            CutClipFromLastSaveState();
        }
        orig(self, gameTime);
    }

    private static void CutClipFromLastSaveState() {
        FFmpegInstaller ffmpegInstaller = FFmpegInstallerFactory.Create(TooltipWithProgress.Get());
        if (!ffmpegInstaller.IsFFmpegInstalled()) {
            ffmpegInstaller.InstallFFmpegAsynchronously(onComplete: CutClipFromLastSaveState);
            return;
        }

        try {
            if (_state.LastState == null)
                throw new VideoProcessingException("VIDCUTTER_TOOLTIP_STATE_NOT_FOUND");
                
            GameplayClipFactory clipFactory = new(ClipDelays.FromSettings(_settings));
            GameplayClip clip = clipFactory.Create(_state.LastState, _state.LastEvent);
                
            FFmpegService ffmpegService = new(ffmpegInstaller.FFmpegDirectory, _settings.Crf);

            TooltipWithProgress progress = TooltipWithProgress.Get();
            CutClipFromLastVideo cutClipFromLastVideo = new(progress, ffmpegService, _settings.VideoFolder, clip);
            
            progress.OnComplete += () => SimpleTooltip.Show($"{cutClipFromLastVideo.GetOutputFileName()} {Dialog.Clean("VIDCUTTER_TOOLTIP_PROCESSED_VIDEO")}");
            
            cutClipFromLastVideo.Execute();
        } catch (VideoProcessingException exception) {
            SimpleTooltip.Show(Dialog.Clean(exception.DialogId));
        }
    }

    public void Load() {
        _state = state;
        _settings = settings;
        On.Monocle.Engine.Update += OnUpdate;
    }

    public void Unload() {
        On.Monocle.Engine.Update -= OnUpdate;
    }
}