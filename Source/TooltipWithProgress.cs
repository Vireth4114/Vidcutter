using System;
using System.Collections;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Vidcutter;

public class TooltipWithProgress(string message) : Tooltip(message, 0) {
    public float progress = 0f;
    public bool IsLoading = false;
    private float startLine;
    private float endLine;

    protected override IEnumerator Dismiss() {
        while (progress < 1f) {
            yield return null;
        }
        yield return base.Dismiss();
    }

    public override void Render() {
        base.Render();
        if (IsLoading)
        {
            startLine = (startLine + Engine.RawDeltaTime) % 1f;
            endLine = (startLine + 0.3f) % 1f;
        } else {
            startLine = 0;
            endLine = progress;
        }
        if (startLine <= endLine)
            Draw.Line(new Vector2(Engine.Width * startLine, Engine.Height), new Vector2(Engine.Width * endLine, Engine.Height), Color.White * alpha, 10f);
        else
        {
            Draw.Line(new Vector2(Engine.Width * startLine, Engine.Height), new Vector2(Engine.Width, Engine.Height), Color.White * alpha, 10f);
            Draw.Line(new Vector2(0, Engine.Height), new Vector2(Engine.Width * endLine, Engine.Height), Color.White * alpha, 10f);
        }
    }

    public void AddLoadingDelay(float delay, Action onComplete = null) {
        if (delay > 0) {
            Add(new Coroutine(LoadingDelay(delay, onComplete)));
        } else {
            onComplete?.Invoke();
        }
    }

    private IEnumerator LoadingDelay(float delay, Action onComplete = null) {
        IsLoading = true;
        yield return delay;
        IsLoading = false;
        onComplete?.Invoke();
    }

    public static TooltipWithProgress Show(string message) {
        if (Engine.Scene is { } scene) {
            if (!scene.Tracker.Entities.TryGetValue(typeof(Tooltip), out var tooltips)) {
                tooltips = [..scene.Entities.FindAll<Tooltip>()
                    .ToList().Cast<Entity>()];
            }
            tooltips.ForEach(entity => entity.RemoveSelf());
            TooltipWithProgress progress = new(message);
            scene.Add(progress);
            return progress;
        }
        return null;
    }
}