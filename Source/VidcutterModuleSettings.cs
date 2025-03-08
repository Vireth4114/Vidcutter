using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using On.Celeste;
using static Celeste.TextMenuExt;

namespace Celeste.Mod.Vidcutter;

[SettingName("MODOPTIONS_VIDCUTTER_TITLE")]
public class VidcutterModuleSettings : EverestModuleSettings {
    [SettingName("MODOPTIONS_VIDCUTTER_VIDEOFOLDER")]
    [SettingSubText("MODOPTIONS_VIDCUTTER_VIDEOFOLDER_SUB")]
    [SettingMaxLength(200)]
    public string VideoFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Videos");

    [SettingIgnore]
    public float DelayStart { get; set; } = -0.8f;
    public void CreateDelayStartEntry(TextMenu menu, bool inGame) {
        menu.Add(new TextMenu.Slider(Dialog.Clean("MODOPTIONS_VIDCUTTER_DELAYRESPAWN"), i => $"{i/5.0}s", -50, 0, (int)(DelayStart*5.0)) {
            OnValueChange = (value) => {
                DelayStart = value/5.0f;
            }
        });
    }

    [SettingIgnore]
    public float DelayEnd { get; set; } = 1.2f;
    public void CreateDelayEndEntry(TextMenu menu, bool inGame) {
        menu.Add(new TextMenu.Slider(Dialog.Clean("MODOPTIONS_VIDCUTTER_DELAYPASS"), i => $"{i/5.0}s", 0, 100, (int)(DelayEnd*5.0)) {
            OnValueChange = (value) => {
                DelayEnd = value/5.0f;
            }
        });
    }

    [SettingName("MODOPTIONS_VIDCUTTER_CRF")]
    [SettingSubText("MODOPTIONS_VIDCUTTER_CRF_SUB")]
    [SettingRange(0, 51)]
    public int CRF { get; set; } = 27;

    [SettingIgnore]
    public string FFmpegPath { get; set; } = "";

    [SettingIgnore]
    [SettingInGame(false)]
    public bool VideoProcess { get; set; } = false;
    public void CreateVideoProcessEntry(TextMenu menu, bool inGame) {
        if (inGame) {
            return;
        }
        menu.Add(new TextMenu.Button(Dialog.Clean("MODOPTIONS_VIDCUTTER_CUTVIDEOS")) {
            OnPressed = () => {
            OuiLoggedProgress progress = OuiModOptions.Instance.Overworld.Goto<OuiLoggedProgress>();
                if (!Directory.Exists("./VidCutter/ffmpeg/ffmpeg")) {
                    try {
                        // Check for FFmpeg in PATH
                        Process process = VideoCreation.createProcess("ffmpeg", "-version");
                        process.Start();
                        process.WaitForExit();
                        FFmpegPath = "";
                        OuiModOptions.Instance.Overworld.Goto<OuiVideoList>();
                    } catch (Win32Exception) {
                        progress.Init<OuiModOptions>(Dialog.Clean("VIDCUTTER_FFMPEG_TITLE"), new Task(() => {
                            VidcutterModule.InstallFFmpeg(progress);
                        }), 0);
                        FFmpegPath = Path.Combine("./VidCutter/", "ffmpeg", "ffmpeg", "ffmpeg-7.1-essentials_build", "bin") + "/";
                    }
                } else {
                    FFmpegPath = Path.Combine("./VidCutter/", "ffmpeg", "ffmpeg", "ffmpeg-7.1-essentials_build", "bin") + "/";
                    OuiModOptions.Instance.Overworld.Goto<OuiVideoList>();
                }
            }
        });
    }
}