using System;
using System.Threading.Tasks;

namespace Celeste.Mod.Vidcutter.Progress;

public interface IProgress {
    public string Message { set; }
    public Task Task { set; }
    public float Progress { set; }
    public event Action OnComplete; 

    public void Start();
    public void StartAfterDelay(float delay);
    public void AddLine(string message);
}