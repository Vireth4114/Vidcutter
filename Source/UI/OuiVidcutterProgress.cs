using System;
using System.Threading.Tasks;
using Celeste.Mod.UI;
using Celeste.Mod.Vidcutter.Utils;

namespace Celeste.Mod.Vidcutter.UI;

public class OuiVidcutterProgress : OuiLoggedProgress {
    public static void GotoInstallFFmpeg<T>() where T : Oui {
        OuiVidcutterProgress progress = OuiModOptions.Instance.Overworld.Goto<OuiVidcutterProgress>();
        progress.Init<T>(Dialog.Clean("VIDCUTTER_FFMPEG_TITLE"), new Task(() => {
            FFmpegUtils.InstallFFmpeg((position, length, speed) => {
                if (length > 0) {
                    progress.Lines[^1] = Dialog.Clean("VIDCUTTER_DOWNLOADINGFFMPEG") + $" {(int) Math.Floor(100D * (position / (double) length))}% @ {speed} KiB/s";
                    progress.Progress = position;
                } else {
                    progress.Lines[^1] = Dialog.Clean("VIDCUTTER_DOWNLOADINGFFMPEG") + $" {(int) Math.Floor(position / 1000D)}KiB @ {speed} KiB/s";
                }

                progress.ProgressMax = (int) length;
                return true;
            });
        }), 0);
        progress.LogLine(Dialog.Clean("VIDCUTTER_DOWNLOADINGFFMPEG"));
    }
}