using System;

namespace Celeste.Mod.Vidcutter.Exceptions;

public class VideoProcessingException(string dialogId) : Exception {
    public string DialogId { get; } = dialogId;
}