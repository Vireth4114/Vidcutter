using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Celeste.Mod.UI;
using Celeste.Mod.Vidcutter.Entities;
using Celeste.Mod.Vidcutter.Models;
using Celeste.Mod.Vidcutter.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using static Celeste.TextMenu;

namespace Celeste.Mod.Vidcutter.UI;

class OuiVideoList : Oui, OuiModOptions.ISubmenu {
    private const float OnScreenX = 960f;
    private const float OffScreenX = 2880f;
    private float _alpha;
    private readonly ObservableCollection<LevelRow> _selectedRows = [];
    private readonly List<LevelRow> _rows = [];
    
    private TextMenu _menu;

    private void ReloadMenu() {
        TextMenu oldMenu = _menu;
        if (oldMenu != null) {
            Scene.Remove(oldMenu);
        }
        _menu = new TextMenu {
            InnerContent = InnerContentMode.TwoColumn
        };

        _rows.Clear();
        _selectedRows.Clear();

        foreach (VideoFile video in VideoManager.GetAllVideos()) {
            HashSet<LevelInAVideo> rowsForVideo = VideoManager.ProcessLogs(video)
                .GroupBy(clip => clip.Level)
                .Select(g => new LevelInAVideo(video.FileName, g.Key) { LastLog = g.Last().End })
                .ToHashSet();
            Logger.Info("Vidcutter", $"Video {video.FileName} has {rowsForVideo.Count} levels");
            foreach (LevelRow row in rowsForVideo.Select(GetLevelRow)) {
                _rows.Add(row);
                _menu.Add(row);
                _menu.Add(row.SubHeader);
            }
        }

        if (_rows.Count == 0) {
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

    private LevelRow GetLevelRow(LevelInAVideo levelInAVideo) {
        LevelRow row = new(levelInAVideo);
        row.OnPressed += () => ToggleRow(row);
        return row;
    }

    private void ToggleRow(LevelRow row) {
        if (!_selectedRows.Remove(row)) {
            _selectedRows.Add(row);
        }

        SynchronizeRowIndices();
    }

    private void SynchronizeRowIndices() {
        foreach (LevelRow row in _rows) {
            row.Index = _selectedRows.IndexOf(row);
        }
    }

    private Button GetButton(string label, Action onPressed) {
        Button button = new(label) {
            OnPressed = onPressed,
            Disabled = true,
            AlwaysCenter = true
        };
        _selectedRows.CollectionChanged += (_, _) => {
            button.Disabled = _selectedRows.Count == 0;
        };
        return button;
    }
    

    private Button GetProcessButton() {
        return GetButton(Dialog.Clean("VIDCUTTER_PROCESS"), () => {
            OuiModOptions.Instance.Overworld.Goto<OuiProcessVideosProgress>().Configure(
                rowsToProcess: _selectedRows.Select(button => button.Data).ToList(),
                deleteAfterProcess: false
            );
        });
    }

    private Button GetProcessAndDeleteButton() {
        return GetButton(Dialog.Clean("VIDCUTTER_PROCESS_AND_DELETE"), () => {
            OuiModOptions.Instance.Overworld.Goto<OuiProcessVideosProgress>().Configure(
                rowsToProcess: _selectedRows.Select(button => button.Data).ToList(),
                deleteAfterProcess: true
            );
        });
    }

    private Button GetDeleteButton() {
        return GetButton(Dialog.Clean("VIDCUTTER_DELETE"), () => {
            LogManager.DeleteLogs(_selectedRows.Select(button => button.Data).ToList());
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
            _menu?.X = OnScreenX + 1920f * Ease.CubeIn(p);
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