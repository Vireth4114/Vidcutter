using System;
using System.Collections;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Vidcutter.Entities;

public class TooltipWithProgress(string message) : Tooltip(message, 0) {
    public float Progress = 0f;
    private bool _isLoading;
    private float _startLine;
    private float _endLine;

    protected override IEnumerator Dismiss() {
        while (Progress < 1f) {
            yield return null;
        }
        yield return base.Dismiss();
    }

    public override void Render() {
        base.Render();
        if (_isLoading) {
            _startLine = (_startLine + Engine.RawDeltaTime) % 1f;
            _endLine = (_startLine + 0.3f) % 1f;
        } else {
            _startLine = 0;
            _endLine = Progress;
        }
        if (_startLine <= _endLine) {
            Draw.Line(new Vector2(Engine.Width * _startLine, Engine.Height), new Vector2(Engine.Width * _endLine, Engine.Height), Color.White * Alpha, 10f);
        } else {
            Draw.Line(new Vector2(Engine.Width * _startLine, Engine.Height), new Vector2(Engine.Width, Engine.Height), Color.White * Alpha, 10f);
            Draw.Line(new Vector2(0, Engine.Height), new Vector2(Engine.Width * _endLine, Engine.Height), Color.White * Alpha, 10f);
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
        _isLoading = true;
        yield return delay;
        _isLoading = false;
        onComplete?.Invoke();
    }

    public static TooltipWithProgress Show(string message) {
        if (Engine.Scene is not { } scene) return null;
        scene.Entities
            .FindAll<Tooltip>()
            .ToList()
            .ForEach(entity => entity.RemoveSelf());
        TooltipWithProgress progress = new(message);
        scene.Add(progress);
        return progress;
    }
}