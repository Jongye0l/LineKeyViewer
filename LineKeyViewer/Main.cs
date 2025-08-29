using System;
using System.Threading;
using JALib.Core;
using JALib.Tools;
using LineKeyViewer.Component;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LineKeyViewer;

public class Main() : JAMod(typeof(Setting)) {
    public static Main Instance;
    public static SettingGUI SettingGUI;
    public static new Setting Setting;
    public static scrLineKeyViewer KeyViewer;
    private static string SizeString;
    private static string LocationXString;
    private static string LocationYString;
    private static int SelectedKey;
    private static int WinAPICool;
    private static bool[] KeyPressed;
    private static Thread[] threads;

    protected override void OnSetup() {
        Setting = (Setting) base.Setting;
        SettingGUI = new SettingGUI(this);
        SelectedKey = -1;
        Patcher.AddPatch(typeof(ResultHandler));
    }

    protected override void OnEnable() {
        ModEntry.Info.DisplayName = ModEntry.Info.Id;
        BundleManager bundleManager = BundleManager.Instance = new BundleManager();
        Setting setting = Setting;
        GameObject gameObject = Object.Instantiate(bundleManager.KeyViewerObject);
        Object.DontDestroyOnLoad(gameObject);
        scrLineKeyViewer keyViewer = KeyViewer = gameObject.GetComponent<scrLineKeyViewer>();
        keyViewer.sizeTransform.localScale = new Vector3(setting.Size, setting.Size);
        if(setting.FlipHorizontal) keyViewer.sizeTransform.eulerAngles = new Vector3(0, 180, 0);
        if(setting.HideDesk) keyViewer.mainImage.sprite = bundleManager.Line;
        UpdateLocation();
        
        threads = new Thread[2];
        (threads[0] = new Thread(KeyInputManager.ListenKey)).Start();
        (threads[1] = new Thread(Winking)).Start();
        Application.quitting += OnQuitting;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private static void OnSceneUnloaded(Scene _) => ResultHandler.Reset();

    private static void OnQuitting() {
        foreach(Thread thread in threads) thread.Abort();
    }

    protected override void OnGUI() {
        Setting setting = Setting;
        SettingGUI settingGUI = SettingGUI;
        JALocalization localization = Localization;
        settingGUI.AddSettingSliderFloat(ref setting.Size, 1, ref SizeString, localization["size"], 0, 2, () => {
            KeyViewer.sizeTransform.localScale = new Vector3(setting.Size, setting.Size);
            UpdateLocation();
        });
        settingGUI.AddSettingSliderFloat(ref setting.LocationX, 0, ref LocationXString, localization["locationX"], 0, 1, UpdateLocation);
        settingGUI.AddSettingSliderFloat(ref setting.LocationY, 0, ref LocationYString, localization["locationY"], 0, 1, UpdateLocation);
        settingGUI.AddSettingToggle(ref setting.FlipHorizontal, localization["flipHorizontal"], () => {
            KeyViewer.sizeTransform.eulerAngles = new Vector3(0, setting.FlipHorizontal ? 180 : 0, 0);
            KeyInputManager.Reset = true;
            UpdateLocation();
        });
        settingGUI.AddSettingToggle(ref setting.HideDesk, localization["hideDesk"], () => {
            scrLineKeyViewer keyViewer = KeyViewer;
            if(keyViewer.headOn) {
                if(setting.HideDesk) keyViewer.mainImage.enable = 0;
                else {
                    keyViewer.mainImage.enable = 1;
                    keyViewer.mainImage.sprite = BundleManager.Instance.Table;
                }
            } else keyViewer.mainImage.sprite = keyViewer.winkOn ?
                                                    setting.HideDesk ? BundleManager.Instance.LineWink : BundleManager.Instance.LineWinkTable :
                                                    setting.HideDesk ? BundleManager.Instance.Line : BundleManager.Instance.LineTable;
            
        });
        if(JipperResourcePackAPI.CheckJipperResourcePack()) settingGUI.AddSettingToggle(ref setting.ShareJipperResourcePack, localization["shareJipperResourcePack"], () => {
            setting.ShareJipperKeyCode(setting.ShareJipperResourcePack);
        });
        GUILayout.Space(18f);
        GUILayout.BeginHorizontal();
        for(int i = 0; i < 8; i++) CreateButton(i);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        for(int i = 8; i < 16; i++) CreateButton(KeyInputManager.Location[i]);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        if(SelectedKey == -1) return;
        GUILayout.Label($"<b>{localization["inputKey"]}</b>");
        if(Input.anyKeyDown) {
            foreach(KeyCode keyCode in Enum.GetValues(typeof(KeyCode))) {
                if(!Input.GetKeyDown(keyCode)) continue;
                SetupKey(keyCode);
                break;
            }
        } else {
            for(int i = 0; i < 256; i++) {
                if((KeyInputManager.GetAsyncKeyState(i) & 0x8000) != 0 == KeyPressed[i]) continue;
                if(KeyPressed[i]) {
                    KeyPressed[i] = false;
                    WinAPICool = 0;
                    continue;
                }
                if(WinAPICool++ < 6) break;
                KeyCode keyCode = (KeyCode) i + 0x1000;
                SetupKey(keyCode);
                break;
            }
        }
    }
    
    private void CreateButton(int i) {
        if(!GUILayout.Button(Bold(KeyToString(Setting.KeyCodes[i]), i == SelectedKey))) return;
        SelectedKey = i;
        WinAPICool = 0;
        KeyPressed = new bool[256];
        for(int i2 = 0; i2 < 256; i2++) KeyPressed[i2] = (KeyInputManager.GetAsyncKeyState(i2) & 0x8000) != 0;
    }
    
    private void SetupKey(KeyCode keyCode) {
        Setting.KeyCodes[SelectedKey] = keyCode;
        SelectedKey = -1;
        WinAPICool = 0;
        KeyPressed = null;
        SaveSetting();
        if(!Setting.KeyCodeJipperResourcePack) return;
        JipperResourcePackAPI.UpdateKeyLimit();
        JipperResourcePackAPI.SaveSetting();
    }
    
    private static string Bold(string text, bool bold) => !bold ? text : $"<b>{text}</b>";

    private static string KeyToString(KeyCode keyCode) => (int) keyCode < 0x1000 ? keyCode.ToString() : ((System.Windows.Forms.Keys) (keyCode - 0x1000)).ToString();
    
    private static void UpdateLocation() {
        Setting setting = Setting;
        float y = 1 - setting.LocationY;
        RectTransform rectTransform = KeyViewer.locationTransform;
        rectTransform.sizeDelta = new Vector2(540 * setting.Size, 420 * setting.Size);
        rectTransform.pivot = new Vector2(setting.LocationX, y);
        rectTransform.anchoredPosition = new Vector2(setting.LocationX * 1920, y * 1080);
    }

    private void Winking() {
        try {
            scrLineKeyViewer keyViewer = KeyViewer;
            while(Enabled) {
                do {
                    Thread.Sleep(JARandom.Instance.Next(3000, 7000));
                } while(keyViewer.headOn || keyViewer.gameResult);
                keyViewer.winkOn = true;
                keyViewer.mainImage.sprite = Setting.HideDesk ? BundleManager.Instance.LineWink : BundleManager.Instance.LineWinkTable;
                Thread.Sleep(JARandom.Instance.Next(100, 250));
                if(!keyViewer.winkOn) continue;
                keyViewer.winkOn = false;
                keyViewer.mainImage.sprite = Setting.HideDesk ? BundleManager.Instance.Line : BundleManager.Instance.LineTable;
            }
        } catch (ThreadAbortException) {
        } catch (Exception e) {
            if(!Enabled) return;
            LogReportException("Failed to work wink", e);
        }
    }

    protected override void OnDisable() {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        if(KeyViewer) {
            Object.Destroy(KeyViewer.gameObject);
            KeyViewer = null;
        }
        foreach(Thread thread in threads) thread.Abort();
        threads = null;
        BundleManager.Instance.Dispose();
    }
}