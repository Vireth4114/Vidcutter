using Celeste.Mod.Vidcutter.Entities;
using Celeste.Mod.Vidcutter.Exceptions;
using Celeste.Mod.Vidcutter.Models;
using Celeste.Mod.Vidcutter.Utils;
using Celeste.Mod.Vidcutter.Utils.Installers;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Vidcutter.Hooks;

public static class HotkeyHooks {
    private static VidcutterState State => VidcutterModule.State;
    private static VidcutterModuleSettings Settings => VidcutterModule.Settings;

    private static void OnUpdate(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime) {
        if (Settings.CutFromLastSaveState.Pressed) {
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
            if (State.LastState == null)
                throw new VideoProcessingException("VIDCUTTER_TOOLTIP_STATE_NOT_FOUND");
                
            GameplayClipFactory clipFactory = new(ClipDelays.FromSettings(Settings));
            GameplayClip clip = clipFactory.Create(State.LastState, State.LastEvent);
                
            FFmpegService ffmpegService = new(ffmpegInstaller.FFmpegDirectory, Settings);

            CutClipFromLastVideo cutClipFromLastVideo = new(ffmpegService, Settings.VideoFolder, clip);
                
            cutClipFromLastVideo.Execute(TooltipWithProgress.Get());
        } catch (VideoProcessingException exception) {
            SimpleTooltip.Show(Dialog.Clean(exception.DialogId));
        }
    }

    public static void Load() {
        On.Monocle.Engine.Update += OnUpdate;
    }

    public static void Unload() {
        On.Monocle.Engine.Update -= OnUpdate;
    }
}