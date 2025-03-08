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

[SettingName("Video Cutter")]
public class VidcutterModuleSettings : EverestModuleSettings {
    [SettingName("Video Folder")]
    [SettingSubText("Folder where the videos are stored.")]
    [SettingMaxLength(200)]
    public string VideoFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Videos");

    [SettingIgnore]
    public float DelayStart { get; set; } = -0.8f;
    public void CreateDelayStartEntry(TextMenu menu, bool inGame) {
        menu.Add(new TextMenu.Slider("Delay before respawn", i => $"{i/5.0}s", -50, 0, (int)(DelayStart*5.0)) {
            OnValueChange = (value) => {
                DelayStart = value/5.0f;
            }
        });
    }

    [SettingIgnore]
    public float DelayEnd { get; set; } = 1.2f;
    public void CreateDelayEndEntry(TextMenu menu, bool inGame) {
        menu.Add(new TextMenu.Slider("Delay after room pass", i => $"{i/5.0}s", 0, 100, (int)(DelayEnd*5.0)) {
            OnValueChange = (value) => {
                DelayEnd = value/5.0f;
            }
        });
    }

    [SettingName("CRF")]
    [SettingSubText("CRF is a quality setting for the video. Lower is better quality but bigger file size.")]
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
        menu.Add(new TextMenu.Button("Cut Video(s)") {
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
                        progress.Init<OuiModOptions>("Installing FFmpeg...", new Task(() => {
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