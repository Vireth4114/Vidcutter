using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Celeste.Mod.UI;
using Celeste.Mod.Vidcutter.Entities;
using Celeste.Mod.Vidcutter.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using static Celeste.TextMenu;

namespace Celeste.Mod.Vidcutter.UI;

class OuiVideoList : Oui, OuiModOptions.ISubmenu {
    private const float OnScreenX = 960f;
    private const float OffScreenX = 2880f;
    private float _alpha;
    private readonly ObservableCollection<CustomButton> _toProcess = [];
    private readonly List<CustomButton> _buttons = [];
    private readonly Dictionary<LevelInAVideo, LoggedString> _lastLogRow = [];
    
    private TextMenu _menu;

    private void ReloadMenu() {
        TextMenu oldMenu = _menu;
        if (oldMenu != null) {
            Scene.Remove(oldMenu);
        }
        _menu = new TextMenu();

        _buttons.Clear();
        _toProcess.Clear();

        foreach (VideoFile video in VideoCreation.GetAllVideos()) {
            HashSet<LevelInAVideo> rowsForVideo = [];
            List<GameplayClip> clips = VideoCreation.ProcessLogs(video);
            foreach (GameplayClip clip in clips) {
                LevelInAVideo row = new LevelInAVideo(video.FileName, clip.Level);
                rowsForVideo.Add(row);
                _lastLogRow[row] = clip.End;
            }
            Logger.Info("Vidcutter", $"Video {video.FileName} has {rowsForVideo.Count} levels and {clips.Count} clips");
            foreach (var button in rowsForVideo.Select(GetButtonForRow)) {
                CustomEaseIn videoLabel = new CustomEaseIn(video.FileName, false, _menu) {
                    TextColor = Color.Gray,
                    HeightExtra = 0f
                };

                button.OnEnter += () => videoLabel.FadeVisible = true;
                button.OnLeave += () => videoLabel.FadeVisible = false;
                
                _buttons.Add(button);
                _menu.Add(button);
                _menu.Add(videoLabel);
            }
        }

        if (_buttons.Count == 0) {
            _menu.Add(new SubHeader(Dialog.Clean("VIDCUTTER_NOVIDEO")));
        } else {
            _menu.Add(GetProcessButton());
            _menu.Add(GetProcessAndDeleteButton());
            _menu.Add(GetDeleteButton());
        }

        if (oldMenu != null) {
            _menu.Selection = oldMenu.Selection;
            _menu.Position = oldMenu.Position;
        }

        Scene.Add(_menu);
    }

    private CustomButton GetButtonForRow(LevelInAVideo levelInAVideo) {
        string completion;
        LoggedString lastLog = _lastLogRow[levelInAVideo];
        if (lastLog.Event == "LEVEL COMPLETE") {
            completion = Dialog.Clean("VIDCUTTER_LEVEL_CLEARED");
        } else {
            completion = Dialog.Clean("VIDCUTTER_LEVEL_UNTIL") + $" {lastLog.Room}";
        }
        string rowName = $"{levelInAVideo.Level} ({completion})";
                
        CustomButton button = new CustomButton(rowName, levelInAVideo);
        button.OnPressed += () => {
            if (!_toProcess.Remove(button)) {
                _toProcess.Add(button);
            }

            UpdateEveryButtonsState();
        };
        return button;
    }

    private void UpdateEveryButtonsState() {
        foreach (CustomButton b in _buttons) {
            int index = _toProcess.IndexOf(b);
            b.LabelIndex = index >= 0 ? $"{index + 1}." : "";
            b.Colored = index >= 0;
        }
    }

    private Button GetButton(string label, Action onPressed) {
        Button button = new Button(label) {
            OnPressed = onPressed,
            Disabled = true
        };
        _toProcess.CollectionChanged += (_, _) => {
            button.Disabled = _toProcess.Count == 0;
        };
        return button;
    }
    

    private Button GetProcessButton() {
        return GetButton(Dialog.Clean("VIDCUTTER_PROCESS"), () => {
            OuiModOptions.Instance.Overworld.Goto<OuiProcessVideosProgress>().Configure(
                rowsToProcess: _toProcess.Select(button => button.Data).ToList(),
                deleteAfterProcess: false
            );
        });
    }

    private Button GetProcessAndDeleteButton() {
        return GetButton(Dialog.Clean("VIDCUTTER_PROCESS_AND_DELETE"), () => {
            OuiModOptions.Instance.Overworld.Goto<OuiProcessVideosProgress>().Configure(
                rowsToProcess: _toProcess.Select(button => button.Data).ToList(),
                deleteAfterProcess: true
            );
        });
    }

    private Button GetDeleteButton() {
        return GetButton(Dialog.Clean("VIDCUTTER_DELETE"), () => {
            LogManager.DeleteLogs(_toProcess.Select(button => button.Data).ToList());
            OuiModOptions.Instance.Overworld.Goto<OuiVideoList>();
        });
    }

    public override IEnumerator Enter(Oui from) {
        Visible = true;

        ReloadMenu();

        _menu.Visible = true;
        _menu.Focused = false;

        for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
            _menu.X = OffScreenX + -1920f * Ease.CubeOut(p);
            _alpha = Ease.CubeOut(p);
            yield return null;
        }

        _menu.Focused = true;
    }

    public override IEnumerator Leave(Oui next) {
        if (_menu != null) {
            Audio.Play(SFX.ui_main_whoosh_large_out);
            _menu.Focused = false;
        }

        for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
            if (_menu != null)
                _menu.X = OnScreenX + 1920f * Ease.CubeIn(p);
            _alpha = 1f - Ease.CubeIn(p);
            yield return null;
        }

        if (_menu != null) {
            _menu.Visible = Visible = false;
            _menu.RemoveSelf();
            _menu = null;
        }
    }

    public override void Update() {
        if (_menu is { Focused: true } && Selected && Input.MenuCancel.Pressed) {
            Audio.Play(SFX.ui_main_button_back);
            Overworld.Goto<OuiModOptions>();
        }

        base.Update();
    }

    public override void Render() {
        if (_alpha > 0f)
            Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * _alpha * 0.4f);

        base.Render();
    }
}