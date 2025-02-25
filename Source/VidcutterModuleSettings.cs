using Celeste.Mod.UI;
using On.Celeste;

namespace Celeste.Mod.Vidcutter;

[SettingName("Video Cutter")]
public class VidcutterModuleSettings : EverestModuleSettings {
    [SettingRange(0, 51)] 
    public static int CRF { get; set; } = 27;

    [SettingIgnore]
    public bool Process { get; set; } = false;
    
    public void CreateProcessEntry(TextMenu menu, bool inGame) {
        menu.Add(new TextMenu.Button("Create Video") {
            OnPressed = () => {
                OuiVideoList test = OuiModOptions.Instance.Overworld.Goto<OuiVideoList>();
            }
        });
    }
}