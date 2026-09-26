using System;
using System.Threading.Tasks;

namespace Celeste.Mod.Vidcutter.Utils;

public interface IProgress {
    public string Message { set; }
    public Task Task { set; }
    public float Progress { set; }
    public string MessageOnComplete { set; }
    public event Action OnComplete; 

    public void Start();
    public void StartAfterDelay(float delay);
    public void AddLine(string message);
}