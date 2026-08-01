using System;
using System.Collections;
using System.Threading.Tasks;
using Celeste.Mod.UI;
using Celeste.Mod.Vidcutter.Utils;

namespace Celeste.Mod.Vidcutter.UI;

public class OuiFFmpegInstallProgress : OuiLoggedProgress {
    public override IEnumerator Enter(Oui from) {
        Init<OuiVideoList>(Dialog.Clean("VIDCUTTER_FFMPEG_TITLE"), new Task(() => {
            FFmpegUtils.InstallFFmpeg((position, length, speed) => {
                if (length > 0) {
                    Lines[^1] = Dialog.Clean("VIDCUTTER_DOWNLOADINGFFMPEG") + $" {(int) Math.Floor(100D * (position / (double) length))}% @ {speed} KiB/s";
                    Progress = position;
                } else {
                    Lines[^1] = Dialog.Clean("VIDCUTTER_DOWNLOADINGFFMPEG") + $" {(int) Math.Floor(position / 1000D)}KiB @ {speed} KiB/s";
                }

                ProgressMax = (int) length;
                return true;
            });
        }), 0);
        
        LogLine(Dialog.Clean("VIDCUTTER_DOWNLOADINGFFMPEG"));
        
        yield return base.Enter(from);
    }
}