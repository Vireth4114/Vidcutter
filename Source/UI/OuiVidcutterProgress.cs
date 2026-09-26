using System;
using System.Threading.Tasks;
using Celeste.Mod.UI;
using Celeste.Mod.Vidcutter.Utils;

namespace Celeste.Mod.Vidcutter.UI;

public class OuiVidcutterProgress(string title): IProgress {
    private OuiLoggedProgress _loggedProgress;

    private static Overworld Overworld => OuiModOptions.Instance.Overworld;

    public string Message {
        get;
        set {
            field = value;
            _loggedProgress?.Lines = [Message];
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

    public string MessageOnComplete { get; set; }
    public event Action OnComplete;

    public void Start() {
        _loggedProgress = Overworld.Goto<OuiLoggedProgressFromVidcutter>();
        _loggedProgress.Title = title;
        _loggedProgress.Task = Task;
        _loggedProgress.Progress = 0;
        _loggedProgress.ProgressMax = 100;
        _loggedProgress.OnFinish += OnComplete;
        _loggedProgress.Lines = [Message];
        if (MessageOnComplete != null) {
            _loggedProgress.WaitForConfirmOnFinish = true;
        }
        Task.Start();
        Task.ContinueWith(t => {
            if (t.IsCompletedSuccessfully && MessageOnComplete != null) {
                _loggedProgress.Lines.Add(MessageOnComplete);
                _loggedProgress.Lines.Add(Dialog.Clean("AUTOUPDATECHECKER_CONTINUE"));
            }
        });
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
}