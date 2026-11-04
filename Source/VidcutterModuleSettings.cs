using System;
using System.IO;
using Celeste.Mod.UI;
using Celeste.Mod.Vidcutter.Models;
using Celeste.Mod.Vidcutter.Progress;
using Celeste.Mod.Vidcutter.Services.FFmpeg;
using Celeste.Mod.Vidcutter.Tasks.FFmpegInstallation;
using Celeste.Mod.Vidcutter.UI;

namespace Celeste.Mod.Vidcutter;

[SettingName("MODOPTIONS_VIDCUTTER_TITLE")]
public class VidcutterModuleSettings : EverestModuleSettings, ISettings {
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
            OnValueChange = value => {
                DelayEnd = value/5.0f;
            }
        });
    }

    [SettingIgnore]
    public int DelayComplete { get; set; } = 20;
    public void CreateDelayCompleteEntry(TextMenu menu, bool inGame) {
        menu.Add(new TextMenu.Slider(Dialog.Clean("MODOPTIONS_VIDCUTTER_DELAYCOMPLETE"), i => $"{i}s", 0, 300, DelayComplete) {
            OnValueChange = value => {
                DelayComplete = value;
            }
        });
    }

    [SettingName("MODOPTIONS_VIDCUTTER_CRF")]
    [SettingSubText("MODOPTIONS_VIDCUTTER_CRF_SUB")]
    [SettingRange(0, 51)]
    public int Crf { get; set; } = 27;

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
                FFmpegContext ffmpegContext = new(Crf);

                if (ffmpegContext.IsFFmpegInstalled) {
                    GotoVideoList(ffmpegContext);
                } else {
                    FFmpegInstallerBase ffmpegInstaller = FFmpegInstallerFactory.Create(
                        ffmpegContext,
                        new OuiVidcutterProgress(Dialog.Clean("VIDCUTTER_FFMPEG_TITLE"))
                    );
                    ffmpegInstaller.InstallFFmpegAsynchronously(onComplete: () => {
                        GotoVideoList(ffmpegContext);
                    });
                }
            }
        });
    }

    private void GotoVideoList(FFmpegContext ffmpegContext) {
        OuiModOptions.Instance.Overworld.Goto<OuiVideoList>().Configure(new FFmpegService(ffmpegContext), this);
    }

    [SettingName("MODOPTIONS_VIDCUTTER_CUTFROMLASTSAVESTATE")]
    public ButtonBinding CutFromLastSaveState { get; set; }
    
    
}