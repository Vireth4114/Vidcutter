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
    public static string VideoFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Videos");

    [SettingIgnore]
    public static double DelayStart { get; set; } = -0.8;
    public void CreateDelayStartEntry(TextMenu menu, bool inGame) {
        menu.Add(new TextMenu.Slider("Delay before respawn", i => $"{i/5.0}s", -50, 0, (int)(DelayStart*5.0)) {
            OnValueChange = (value) => {
                DelayStart = value/5.0;
            }
        });
    }

    [SettingIgnore]
    public static double DelayEnd { get; set; } = 1.2;
    public void CreateDelayEndEntry(TextMenu menu, bool inGame) {
        menu.Add(new TextMenu.Slider("Delay after room pass", i => $"{i/5.0}s", 0, 100, (int)(DelayEnd*5.0)) {
            OnValueChange = (value) => {
                DelayEnd = value/5.0;
            }
        });
    }

    [SettingName("CRF")]
    [SettingSubText("CRF is a quality setting for the video. Lower is better quality but bigger file size.")]
    [SettingRange(0, 51)]
    public static int CRF { get; set; } = 27;

    [SettingIgnore]
    public static string FFmpegPath { get; set; } = "";

    [SettingIgnore]
    public bool VideoProcess { get; set; } = false;
    public void CreateVideoProcessEntry(TextMenu menu, bool inGame) {
        menu.Add(new TextMenu.Button("Cut Video(s)") {
            OnPressed = () => {
            OuiLoggedProgress progress = OuiModOptions.Instance.Overworld.Goto<OuiLoggedProgress>();
                if (!Directory.Exists("./VidCutter/ffmpeg/ffmpeg")) {
                    try {
                        Process process = Process.Start("ffmpeg", "-version");
                        process.WaitForExit();
                        FFmpegPath = "";
                        OuiModOptions.Instance.Overworld.Goto<OuiVideoList>();
                    } catch (Win32Exception ex) {
                        VidcutterModule.Log(ex.Message, true);
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