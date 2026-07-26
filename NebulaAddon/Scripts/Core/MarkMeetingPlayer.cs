using Amongus.GameModes.HideAndSeek;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Reflection;
using Il2CppSystem.Runtime.CompilerServices;
using InnerNet;
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
public class InMeetingMarkPlayerSystemRegister
{
    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        PDebug.Log("Add Mark player Modules");
        preprocessor.DIManager.RegisterModule<Game>(() => new InMeetingMarkPlayerSystem().RegisterPermanently());
    }
    public class InMeetingMarkPlayerSystem : AbstractModule<Game>, IGameOperator
    {
        Image[] AllMarkIcons = new Image[9]
        {
            NebulaAPI.AddonAsset.GetResource("MarkSystem/0star.png")!.AsImage(100)!,
            NebulaAPI.AddonAsset.GetResource("MarkSystem/1none.png")!.AsImage(100)!,
            NebulaAPI.AddonAsset.GetResource("MarkSystem/2yes.png")!.AsImage(100)!,
            NebulaAPI.AddonAsset.GetResource("MarkSystem/3no.png")!.AsImage(100)!,
            NebulaAPI.AddonAsset.GetResource("MarkSystem/4cyan.png")!.AsImage(100)!,
            NebulaAPI.AddonAsset.GetResource("MarkSystem/5red.png")!.AsImage(100)!,
            NebulaAPI.AddonAsset.GetResource("MarkSystem/6orange.png")!.AsImage(100)!,
            NebulaAPI.AddonAsset.GetResource("MarkSystem/7blue.png")!.AsImage(100)!,
            NebulaAPI.AddonAsset.GetResource("MarkSystem/8gray.png")!.AsImage(100)!
        };
        Dictionary<PlayerVoteArea, int> allMarkIndexs = new();
        void OnMeetingStart(MeetingStartEvent ev)
        {
            allMarkIndexs.Clear();
            MeetingHud.Instance.playerStates.Do(v =>
            {
                try
                {
                    var renderer = GameObject.Instantiate(v.Overlay, v.Overlay.transform.parent);
                    renderer.transform.localScale = UnityEngine.Vector2.one;
                    renderer.transform.SetLocalZ(-0.2f);
                    renderer.gameObject.SetActive(false);
                    var button=v.PlayerButton.Cast<PassiveButton>();
                    button.gameObject.AddComponent<ExtraPassiveBehaviour>().OnRightClicked = () =>
                    {
                        var local = GamePlayer.LocalPlayer;
                        if (local == null || local.IsDead || NebulaGameManager.Instance.CanSeeAllInfo)
                        {
                            return;
                        }
                        if (v.TargetPlayerId == local.PlayerId)
                        {
                            return;
                        }
                        var index = allMarkIndexs[v];
                        if (index >= 9)
                        {
                            renderer.gameObject.SetActive(false);
                            allMarkIndexs[v] = 0;
                            return;
                        }
                        renderer.sprite = AllMarkIcons[index].GetSprite();
                        index++;
                        renderer.gameObject.SetActive(true);
                        allMarkIndexs[v] = index;
                    };
                    button.SetOverlay(delegate
                    {
                        PlayerModInfo modPlayer = GamePlayer.LocalPlayer.Unbox();
                        if (modPlayer == null||modPlayer.IsDead||NebulaGameManager.Instance.CanSeeAllInfo)
                        {
                            return null;
                        }
                        GUIWidget content =  NebulaAPI.GUI.RawText(GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(AttributeAsset.DocumentBold), Language.Translate("general.marksystemTips"));
                        if (content == null)
                        {
                            return null;
                        }
                        return NebulaGUIWidgetEngine.API.VerticalHolder(GUIAlignment.Left, content);
                    });
                    allMarkIndexs.Add(v,0);
                }
                catch (Exception e)
                {
                    PDebug.Log($"Create Player {v.TargetPlayerId} Mark Error:{e}");
                }
            });

        }
    }
}