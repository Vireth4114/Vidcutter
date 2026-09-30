namespace Celeste.Mod.Vidcutter.Models;

public interface ISettings {
    public float DelayStart { get; }
    public float DelayEnd { get; }
    public int DelayComplete { get; }
    public string VideoFolder { get; }
    public int Crf { get; }
    public ButtonBinding CutFromLastSaveState { get; }

    public ClipDelays GetClipDelays() => new(DelayStart, DelayEnd, DelayComplete);
}