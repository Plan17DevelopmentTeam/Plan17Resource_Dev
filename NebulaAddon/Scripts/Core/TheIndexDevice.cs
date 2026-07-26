using Amongus.GameModes.HideAndSeek;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Reflection;
using Il2CppSystem.Runtime.CompilerServices;
using InnerNet;
using Nebula.Dev;
using Nebula.Map;
using Nebula.Modules;
using Nebula.Modules.Cosmetics;
using Nebula.Modules.GUIWidget;
using Nebula.Patches;
using Nebula.Roles.Assignment;
using Nebula.Roles.Complex;
using Nebula.Roles.Impostor;
using Nebula.Roles.Neutral;
using Nebula.Roles.Perks;
using Nebula.Utilities;
using Nebula.VoiceChat;
using Plana.Roles.Crewmate;
using Plana.Roles.Impostor;
using Plana.Roles.Modifier;
using Plana.Roles.Neutral;
using Steamworks;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Virial;
using Virial.Common;
using Virial.Configuration;
using Virial.Events.Game.Minimap;
using Virial.Game;
using Virial.Helpers;
using Virial.Runtime;
using BindingFlags = System.Reflection.BindingFlags;
using Color = UnityEngine.Color;

namespace Plana.Core;

[NebulaPreprocess(PreprocessPhase.PostFixStructure)]
[NebulaRPCHolder]
public class TheIndexDevice : AbstractModule<Game>, IGameOperator
{
    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        PDebug.Log("Add Index Device Module");
        preprocessor.DIManager.RegisterModule<Game>(() => new TheIndexDevice().RegisterPermanently());
    }
    ModGameObject? crewObject,impObject;
    TextMeshPro? crewTMP, impTMP;
    const string IndexChars = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public static RemoteProcess<(string command, bool isimp)> SendCommandRpc = new("sendCommandRpcIndexDevice", (m, _) =>
    {
        NebulaAPI.CurrentGame.GetModule<TheIndexDevice>().ReceiveCommand(m.command, m.isimp);
    });
    public void ReceiveCommand(string command,bool isImpostor=false)
    {
        if (isImpostor&&!AmongUsLLImpl.LocalPlayer.Data.Role.IsImpostor)
        {
            return;
        }
        //command = "这是一个测试文本。";
        if (!isImpostor)
        {
            if (crewObject == null || crewTMP == null)
            {
                NoSGUIText noSGUIText = new NoSGUIText(GUIAlignment.Top, NebulaGUIWidgetEngine.API.GetAttribute(AttributeAsset.OverlayTitle), new RawTextComponent(""));
                Size size;
                noSGUIText.PostBuilder = tm =>
                {
                    tm.color = Palette.CrewmateBlue;
                    tm.font=HudManager.Instance.IntroPrefab.ImpostorText.font; //TMP_FontAsset.CreateFontAsset(PatchManager.GetIndexDeviceResource<UnityEngine.Font>("Terminal"));
                    tm.alignment = TextAlignmentOptions.Center;
                    crewTMP = tm;
                    NebulaManager.Instance.StartCoroutine(CoReceiveCommand(crewTMP, command));
                };
                crewObject = noSGUIText.Instantiate(new Anchor(new Virial.Compat.Vector2(1f, 0.5f), new Virial.Compat.Vector3(0f, 0f, 0f)), new Size(100f, 100f), out size).ModGameObject();
                crewObject.transform.SetParent(AmongUsLLImpl.HudManagerInstance.SettingsButton.transform);
                crewObject.transform.localPosition = new(-3.69f,0.36f, 0f);
                //UnityEngine.Object.DontDestroyOnLoad(crewObject.gameObject);
                //var lifespan = new GameObjectLifespan(gameObject);
                return;
            }
            NebulaManager.Instance.StartCoroutine(CoReceiveCommand(crewTMP, command));
        }
        if (isImpostor)
        {
            if (impObject == null || impTMP == null)
            {
                NoSGUIText noSGUIText = new NoSGUIText(GUIAlignment.Top, NebulaGUIWidgetEngine.API.GetAttribute(AttributeAsset.OverlayTitle), new RawTextComponent(""));
                Size size;
                noSGUIText.PostBuilder = tm =>
                {
                    tm.color = Palette.ImpostorRed;
                    tm.font =HudManager.Instance.IntroPrefab.ImpostorText.font; // TMP_FontAsset.CreateFontAsset(PatchManager.GetIndexDeviceResource<UnityEngine.Font>("Terminal"));
                    tm.alignment = TextAlignmentOptions.Center;
                    impTMP = tm;
                    NebulaManager.Instance.StartCoroutine(CoReceiveCommand(impTMP, command));
                };
                impObject = noSGUIText.Instantiate(new Anchor(new Virial.Compat.Vector2(1f, 0.5f), new Virial.Compat.Vector3(0f, 0f, 0f)), new Size(100f, 100f), out size).ModGameObject();
                impObject.transform.SetParent(AmongUsLLImpl.HudManagerInstance.SettingsButton.transform);
                impObject.transform.localPosition = new(-3.69f, 0.23f, 0f);
                //UnityEngine.Object.DontDestroyOnLoad(impObject.gameObject);
                //var lifespan = new GameObjectLifespan(gameObject);
                return;
            }
            NebulaManager.Instance.StartCoroutine(CoReceiveCommand(impTMP, command));
        }
    }
    private string _originalText;
    private char[] _currentText;
    int decodeProgress;
    System.Collections.IEnumerator CoReceiveCommand(TextMeshPro tmp,string command)
    {
        _originalText = command;
        int totalLen = command.Length;
        _currentText = new char[totalLen];
        for (int i = 0; i < totalLen; i++)
            _currentText[i] = IndexChars[System.Random.Shared.Next(IndexChars.Length)];
        IndexCharLength=totalLen;
        tmp.text = new(_currentText);
        var audioclip = PatchManager.GetIndexDeviceResource<AudioClip>("beepstart.wav");
        var charWaitSecond = 0.05f;
        var source = AmongUsLLImpl.SoundManagerInstance.PlaySoundImmediate(audioclip, true,1,1,AmongUsLLImpl.SoundManagerInstance.SfxChannel);
        while (tmp.text!=_originalText)
        {
            decodeProgress++;
            for (int i = 0; i < _currentText.Length; i++)
            {
                _currentText[i] = IndexChars[System.Random.Shared.Next(IndexChars.Length)];
            }
            for (int i = 0; i < decodeProgress && i < _originalText.Length; i++)
            {
                _currentText[i] = _originalText[i];
            }
            tmp.text = new string(_currentText);
            if (decodeProgress >= _currentText.Length)
            {
                decodeProgress = 0;
                tmp.text = _originalText;
            }
            yield return ManagedEffects.Wait(charWaitSecond);
        }
        AmongUsLLImpl.SoundManagerInstance.StopSound(source.clip);
        yield break;
    }
    int IndexCharLength;
    /*public void Update(UpdateEvent ev)
    {
        if (crewTMP == null && impTMP == null)
        {
            return;
        }
        if (!Input.GetKey(KeyCode.C) && !Input.GetKey(KeyCode.I) && Input.GetKey(KeyCode.A))
        {
            crewTMP.transform.localPosition += new UnityEngine.Vector3(-0.01f, 0f, 0f);
            impTMP.transform.localPosition += new UnityEngine.Vector3(-0.01f, 0f, 0f);
        }
        if (!Input.GetKey(KeyCode.C) && !Input.GetKey(KeyCode.I) && Input.GetKey(KeyCode.W))
        {
            crewTMP.transform.localPosition += new UnityEngine.Vector3(0f, 0.01f, 0f);
            impTMP.transform.localPosition += new UnityEngine.Vector3(0f, 0.01f, 0f);
        }
        if (!Input.GetKey(KeyCode.C) && !Input.GetKey(KeyCode.I) && Input.GetKey(KeyCode.S))
        {
            crewTMP.transform.localPosition += new UnityEngine.Vector3(0f, -0.01f, 0f);
            impTMP.transform.localPosition += new UnityEngine.Vector3(0f, -0.01f, 0f);
        }
        if (!Input.GetKey(KeyCode.C) && !Input.GetKey(KeyCode.I) && Input.GetKey(KeyCode.D))
        {
            crewTMP.transform.localPosition += new UnityEngine.Vector3(0.01f, 0f, 0f);
            impTMP.transform.localPosition += new UnityEngine.Vector3(0.01f, 0f, 0f);
        }
        if (Input.GetKey(KeyCode.C) && Input.GetKey(KeyCode.A))
        {
            crewTMP.transform.localPosition += new UnityEngine.Vector3(-0.01f, 0f, 0f);
        }
        if (Input.GetKey(KeyCode.C) && Input.GetKey(KeyCode.W))
        {
            crewTMP.transform.localPosition += new UnityEngine.Vector3(0f, 0.01f, 0f);
        }
        if (Input.GetKey(KeyCode.C) && Input.GetKey(KeyCode.S))
        {
            crewTMP.transform.localPosition += new UnityEngine.Vector3(0f, -0.01f, 0f);
        }
        if (Input.GetKey(KeyCode.C) && Input.GetKey(KeyCode.D))
        {
            crewTMP.transform.localPosition += new UnityEngine.Vector3(0.01f, 0f, 0f);
        }
        if (Input.GetKey(KeyCode.I) && Input.GetKey(KeyCode.A))
        {
            impTMP.transform.localPosition += new UnityEngine.Vector3(-0.01f, 0f, 0f);
        }
        if (Input.GetKey(KeyCode.I) && Input.GetKey(KeyCode.W))
        {
            impTMP.transform.localPosition += new UnityEngine.Vector3(0f, 0.01f, 0f);
        }
        if (Input.GetKey(KeyCode.I) && Input.GetKey(KeyCode.S))
        {
            impTMP.transform.localPosition += new UnityEngine.Vector3(0f, -0.01f, 0f);
        }
        if (Input.GetKey(KeyCode.I) && Input.GetKey(KeyCode.D))
        {
            impTMP.transform.localPosition += new UnityEngine.Vector3(0.01f, 0f, 0f);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            PDebug.Log("Output CrewTMP Position:" + crewTMP.transform.localPosition.ToString());
            PDebug.Log("Output ImpTMP Position:" + impTMP.transform.localPosition.ToString());
        }
    }*/
}