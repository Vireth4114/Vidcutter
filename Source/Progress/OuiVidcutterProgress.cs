using System;
using System.Threading.Tasks;
using Celeste.Mod.UI;

namespace Celeste.Mod.Vidcutter.Progress;

class OuiLoggedProgressFromVidcutter : OuiLoggedProgress;

public class OuiVidcutterProgress(string title): IProgress {
    private OuiLoggedProgress _loggedProgress;

    private static Overworld Overworld => OuiModOptions.Instance.Overworld;

    public string Message {
        get;
        set {
            field = value;
            _loggedProgress?.Lines[^1] = Message;
        }
    }

    public Task Task { get; set; }

    public float Progress {
        get;
        set {
            field = value;
            _loggedProgress?.Progress = (int) (Progress * 100);
        }
    }
    public event Action OnComplete;

    public void Start() {
        _loggedProgress = Overworld.Goto<OuiLoggedProgressFromVidcutter>();
        _loggedProgress.Title = title;
        _loggedProgress.Task = Task;
        _loggedProgress.Progress = 0;
        _loggedProgress.ProgressMax = 100;
        _loggedProgress.OnFinish += OnComplete;
        _loggedProgress.Lines = [Message];
        Task.Start();
    }

    public void StartAfterDelay(float delay) {
        Task taskToRun = Task;
        Task = new Task(() => {
            string oldMessage = Message ?? "";
            Message = Dialog.Clean("VIDCUTTER_OUIPROGRESS_LOADING");
            Task.Delay((int) (delay * 1000)).Wait();
            Message = oldMessage;
            taskToRun.RunSynchronously();
        });
        Start();
    }

    public void AddLine(string message) {
        _loggedProgress?.Lines.Add(message);
        Message = message;
    }
}