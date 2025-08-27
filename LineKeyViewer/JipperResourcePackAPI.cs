using System;
using System.Threading.Tasks;
using JALib.Core;
using JALib.Core.Setting;
using JALib.Tools;
using UnityEngine;

namespace LineKeyViewer;

public class JipperResourcePackAPI {
    private static JipperResourcePackAPI Instance;
    private Type settingType;
    private JAMod Mod;
    private JASetting Setting;
    private Action UpdateKeyLimitAction;

    private JipperResourcePackAPI(JAMod mod) {
        Mod = mod;
        Type keyViewerType = settingType = mod.GetType().Assembly.GetType("JipperResourcePack.Keyviewer.KeyViewer");
        UpdateKeyLimitAction = (Action) keyViewerType.Method("UpdateKeyLimit").CreateDelegate(typeof(Action));
        SetupSetting();
    }

    private void SetupSetting() {
        Setting = settingType.GetValue<JASetting>("Settings");
        if(Setting == null) Task.Yield().GetAwaiter().OnCompleted(SetupSetting);
    }

    public static JipperResourcePackAPI GetAPI() {
        if(Instance != null) return Instance;
        try {
            JAMod mod = JAMod.GetMods("JipperResourcePack");
            if(mod != null) Instance = new JipperResourcePackAPI(mod);
        } catch (Exception) {
            // ignored
        }
        return Instance;
    }

    public static bool CheckJipperResourcePack() => GetAPI() != null && Instance.Setting != null;

    public static KeyCode[] GetKey16() => Instance?.Setting?.GetValue<KeyCode[]>("key16");

    public static void UpdateKeyLimit() => Instance?.UpdateKeyLimitAction();

    public static void SaveSetting() => Instance?.Mod.SaveSetting();
}