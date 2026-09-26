using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Celeste.Mod.Vidcutter.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Vidcutter.Entities;

public class TooltipWithProgress : Tooltip, IProgress {
    private float _startLine;
    private float _endLine;
    private bool _loading;

    public Task Task { get; set; }
    public float Progress { get; set; }
    public string MessageOnComplete { get; set; }
    public event Action OnComplete;

    public void Start() {
        Task.Start();
        Task.ContinueWith(t => {
            if (t.IsFaulted) {
                SimpleTooltip.Show(t.Exception.InnerExceptions.First().Message, 5f);
            } else if (t.IsCompletedSuccessfully) {
                Progress = 1;
                if (OnComplete != null) {
                    OnComplete();
                } else if (MessageOnComplete != null) {
                    SimpleTooltip.Show(MessageOnComplete, 5f);
                }
            }
        });
    }

    public void StartAfterDelay(float delay) {
        if (delay > 0) {
            Add(new Coroutine(StartAfterDelayCoroutine(delay)));
        } else {
            Start();
        }
    }

    private IEnumerator StartAfterDelayCoroutine(float delay) {
        _loading = true;
        yield return delay;
        _loading = false;
        Start();
    }

    protected override IEnumerator Dismiss() {
        while (Task != null && Task.Status != TaskStatus.RanToCompletion && Task.Status != TaskStatus.Faulted) {
            yield return null;
        }
        yield return base.Dismiss();
    }

    public override void Render() {
        base.Render();
        if (_loading) {
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

    public static TooltipWithProgress Get() {
        if (Engine.Scene is not { } scene) return null;
        scene.Entities
            .FindAll<Tooltip>()
            .ToList()
            .ForEach(entity => entity.RemoveSelf());
        TooltipWithProgress progress = new();
        scene.Add(progress);
        return progress;
    }

    public void AddLine(string message) {
        Message = message;
    }
}