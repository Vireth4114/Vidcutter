using Celeste.Mod.UI;

namespace Celeste.Mod.Vidcutter;

[SettingName("Video Cutter")]
public class VidcutterModuleSettings : EverestModuleSettings {
    [SettingRange(0, 51)] 
    public int CRF { get; set; } = 27;

    [SettingIgnore]
    public bool Process { get; set; } = false;
    
    public void CreateProcessEntry(TextMenu menu, bool inGame) {
        menu.Add(new TextMenu.Button("Create Video") {
            OnPressed = () => {
                VideoCreation.ProcessVideoInit(OuiModOptions.Instance.Overworld.Goto<OuiLoggedProgress>(), CRF);
            }
        });
    }
}