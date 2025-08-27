using System;
using JALib.Core;
using JALib.Core.Setting;
using JALib.Tools.ByteTool;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LineKeyViewer;

public class Setting : JASetting {
    public float Size = 1;
    public float LocationX = 0;
    public float LocationY = 1;
    public bool FlipHorizontal = false;
    public bool HideDesk;
    public bool ShareJipperResourcePack = true;
    [DataExclude] public bool KeyCodeJipperResourcePack;
    public KeyCode[] KeyCodes = [
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Semicolon,
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.N, KeyCode.M, KeyCode.Comma, KeyCode.Period
    ];
    
    public Setting(JAMod mod, JObject jsonObject = null) : base(mod, jsonObject) {
        JipperResourcePackAPI.CheckJipperResourcePack();
    }

    public override void PutFieldData() {
        base.PutFieldData();
        if(KeyCodeJipperResourcePack) JipperResourcePackAPI.SaveSetting();
    }

    public void ShareJipperKeyCode(bool enable) {
        if(enable) {
            KeyCodes = JipperResourcePackAPI.GetKey16() ?? throw new NullReferenceException("Jipper Resource Pack KeyCodes is null");
            KeyCodeJipperResourcePack = true;
        } else {
            KeyCode[] keyCodes = new KeyCode[16];
            Array.Copy(KeyCodes, keyCodes, 16);
            KeyCodes = keyCodes;
            KeyCodeJipperResourcePack = false;
        }
    }
}