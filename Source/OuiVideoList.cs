using System;
using System.Collections;
using Celeste.Mod.Helpers;
using Celeste.Mod.Vidcutter;
using Microsoft.Xna.Framework;
using Monocle;
using static Celeste.TextMenu;

namespace Celeste.Mod.UI;

class OuiVideoList : Oui, OuiModOptions.ISubmenu {
    private const float onScreenX = 960f;
    private const float offScreenX = 2880f;
    private float alpha = 0f;
    
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

        foreach (string video in VideoCreation.GetAllVideos()) {
            string videoName = video.Substring(video.LastIndexOf('\\') + 1);
            Button button = new Button(videoName) {
                OnPressed = () => {
                    VideoCreation.ProcessVideoInit(
                        OuiModOptions.Instance.Overworld.Goto<OuiLoggedProgress>(), 
                        videoName,
                        VidcutterModuleSettings.CRF
                    );
                }
            };
            menu.Add(button);
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
        VidcutterModule.Log("UwU", true);
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