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

public class HotkeyHooks : IHook {
    private static HookState State => HookManager.State;
    private static ISettings Settings => HookManager.Settings;

    private static void OnUpdate(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime) {
        if (Settings.CutFromLastSaveState.Pressed) {
            CutClipFromLastSaveState();
        }
        orig(self, gameTime);
    }

    private static void CutClipFromLastSaveState() {
        FFmpegInstallerBase ffmpegInstaller = FFmpegInstallerFactory.Create(TooltipWithProgress.Get());
        if (!ffmpegInstaller.IsFFmpegInstalled()) {
            ffmpegInstaller.InstallFFmpegAsynchronously(onComplete: CutClipFromLastSaveState);
            return;
        }

        try {
            if (State.LastState == null)
                throw new VideoProcessingException("VIDCUTTER_TOOLTIP_STATE_NOT_FOUND");
                
            GameplayClipFactory clipFactory = new(Settings.GetClipDelays());
            GameplayClip clip = clipFactory.Create(State.LastState, State.LastEvent);
                
            FFmpegService ffmpegService = new(ffmpegInstaller.FFmpegDirectory, Settings.Crf);

            TooltipWithProgress progress = TooltipWithProgress.Get();
            CutClipFromLastVideo cutClipFromLastVideo = new(progress, ffmpegService, Settings.VideoFolder, clip);
            
            progress.OnComplete += () => SimpleTooltip.Show($"{cutClipFromLastVideo.GetOutputFileName()} {Dialog.Clean("VIDCUTTER_TOOLTIP_PROCESSED_VIDEO")}");
            
            cutClipFromLastVideo.Execute();
        } catch (VideoProcessingException exception) {
            SimpleTooltip.Show(Dialog.Clean(exception.DialogId));
        }
    }

    public void Load() {
        On.Monocle.Engine.Update += OnUpdate;
    }

    public void Unload() {
        On.Monocle.Engine.Update -= OnUpdate;
    }
}