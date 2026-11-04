using System.IO;
using Celeste.Mod.Vidcutter.Utils;
using static Celeste.Mod.Vidcutter.Utils.FileConstants;

namespace Celeste.Mod.Vidcutter.Services.FFmpeg;

public class FFmpegContext {
    public string FFmpegDirectory {
        get;
        set {
            field = value;
            IsFFmpegInstalled = !string.IsNullOrEmpty(value) && Directory.Exists(value);
        }
    } = "";

    public int Crf { get; }
    public bool IsFFmpegInstalled { get; private set; }
    
    public FFmpegContext(int crf) {
        Crf = crf;

        if (CommandUtils.IsFFmpegInPath()) {
            IsFFmpegInstalled = true;
        } else if (Directory.Exists(FFmpegBinDirectory)) {
            FFmpegDirectory = FFmpegBinDirectory;
        }
    }
}