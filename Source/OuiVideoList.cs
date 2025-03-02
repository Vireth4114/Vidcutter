using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using Celeste.Mod.Helpers;
using Celeste.Mod.Vidcutter;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using Monocle;
using static Celeste.TextMenu;
using static Celeste.TextMenuExt;

namespace Celeste.Mod.UI;

class OuiVideoList : Oui, OuiModOptions.ISubmenu {
    private const float onScreenX = 960f;
    private const float offScreenX = 2880f;
    private float alpha = 0f;
    private ObservableCollection<int> toProcess = new ObservableCollection<int>();
    private List<string> rowInfos = new List<string>();
    
    private TextMenu menu;

    private void ReloadMenu() {
        Vector2 position = Vector2.Zero;

        int selected = -1;
        if (menu != null) {
            position = menu.Position;
            selected = menu.Selection;
            Scene.Remove(menu);
        }

        menu = new TextMenu();
        rowInfos.Clear();
        toProcess.Clear();

        VideoCreation vc = new VideoCreation(crf: VidcutterModuleSettings.CRF);
        int id = 0;
        foreach (string video in vc.GetAllVideos()) {
            List<string> levels = new List<string>();
            List<LoggedString[]> listLogs = VideoCreation.ProcessLogs(video);
            Dictionary<string, LoggedString> lastLogLevel = new Dictionary<string, LoggedString>();
            foreach (LoggedString[] logs in listLogs) {
                VidcutterModule.Log(logs[1].Level, true);
                if (!levels.Contains(logs[1].Level)) {
                    levels.Add(logs[1].Level);
                }
                lastLogLevel[logs[1].Level] = logs[1];
            }
            foreach (string level in levels) {
                string whatHappened;
                LoggedString lastLog = lastLogLevel[level];
                if (lastLog.Event == "LEVEL COMPLETE") {
                    whatHappened = "Cleared";
                } else {
                    whatHappened = $"Until room {lastLog.Room}";
                }
                string videoName = video.Substring(video.LastIndexOf('\\') + 1);
                string rowName = $"{level} ({whatHappened})";
                rowInfos.Add($"{videoName} | {level}");
                int finalId = id;
                CustomButton button = new CustomButton("", rowName) {
                    OnPressed = () => {
                        if (toProcess.Contains(finalId)) {
                            toProcess.Remove(finalId);
                        } else {
                            toProcess.Add(finalId);
                        }
                        for (int i = 0; i < rowInfos.Count; i++) {
                            int index = toProcess.IndexOf(i);
                            foreach (Item item in menu.Items) {
                                VidcutterModule.Log(item.ToString(), true);
                            }
                            VidcutterModule.Log(menu.Items[i*2].ToString(), true);
                            CustomButton b = (CustomButton) menu.Items[i*2];
                            if (index >= 0) {
                                b.LabelIndex = $"{index + 1}.";
                                b.Colored = true;
                            } else {
                                b.LabelIndex = "";
                                b.Colored = false;
                            }
                        }
                    },
                };
                menu.Add(button);
                CustomEaseIn videoLabel = new CustomEaseIn(videoName, false, menu) {
                    TextColor = Color.Gray,
                    HeightExtra = 0f,
                    FadeVisible = finalId == 0
                };
                menu.Add(videoLabel);

                button.OnEnter += () => videoLabel.FadeVisible = true;
                button.OnLeave += () => videoLabel.FadeVisible = false;
                id++;
            }
        }
        if (id == 0) {
            menu.Add(new SubHeader("No videos to process"));
        } else {
            Button button = new Button("Process") {
                OnPressed = () => {
                    vc.progress = OuiModOptions.Instance.Overworld.Goto<OuiLoggedProgress>();
                    foreach (int i in toProcess) {
                        string[] splitted = rowInfos[i].Split(" | ");
                        vc.videos.Add(new ProcessedVideo(splitted[0], splitted[1]));
                    }
                    vc.ProcessVideosProgress();
                },
                Disabled = true
            };
            menu.Add(button);
            toProcess.CollectionChanged += (sender, args) => {
                button.Disabled = toProcess.Count == 0;
            };
        }

        if (selected >= 0) {
            menu.Selection = selected;
            menu.Position = position;
        }

        Scene.Add(menu);
    }

    public override IEnumerator Enter(Oui from) {
        Visible = true;

        ReloadMenu();

        menu.Visible = true;
        menu.Focused = false;

        for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
            menu.X = offScreenX + -1920f * Ease.CubeOut(p);
            alpha = Ease.CubeOut(p);
            yield return null;
        }

        menu.Focused = true;
    }

    public override IEnumerator Leave(Oui next) {
        if (menu != null) {
            Audio.Play(SFX.ui_main_whoosh_large_out);
            menu.Focused = false;
        }

        for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
            if (menu != null)
                menu.X = onScreenX + 1920f * Ease.CubeIn(p);
            alpha = 1f - Ease.CubeIn(p);
            yield return null;
        }

        if (menu != null) {
            menu.Visible = Visible = false;
            menu.RemoveSelf();
            menu = null;
        }
    }

    public override void Update() {
        if (menu != null && menu.Focused &&
            Selected && Input.MenuCancel.Pressed) {
            Audio.Play(SFX.ui_main_button_back);
            Overworld.Goto<OuiModOptions>();
        }

        base.Update();
    }

    public override void Render() {
        if (alpha > 0f)
            Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * alpha * 0.4f);

        base.Render();
    }
}