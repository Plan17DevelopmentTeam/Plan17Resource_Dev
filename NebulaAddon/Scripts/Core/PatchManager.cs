global using Plana.Core;
global using Plana.Core.API;
using Amongus.GameModes.HideAndSeek;
using AmongUs.Data;
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using Cpp2IL.Core.Extensions;
using HarmonyLib;
using System.Text.Json;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Reflection;
using Il2CppSystem.Runtime.CompilerServices;
using InnerNet;
using NAudio.CoreAudioApi;
using Nebula.Bridge;
using Nebula.Listeners;
using Nebula.Map;
using Nebula.Modifications.Map;
using Nebula.Modules;
using Nebula.Modules.Cosmetics;
using Nebula.Modules.GUIWidget;
using Nebula.Modules.Online;
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
using Rewired.UI.ControlMapper;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;
using Plana.Core.CloudPlayer;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SocialPlatforms;
using Virial;
using Virial.Common;
using Virial.Configuration;
using Virial.Events.Game.Minimap;
using Virial.Game;
using Virial.Helpers;
using Virial.Runtime;
using static FilterPopUp.FilterInfoUI;
using static Il2CppSystem.Globalization.CultureInfo;
using static UnityEngine.GraphicsBuffer;
using BindingFlags = System.Reflection.BindingFlags;
using Color = Virial.Color;
using DirectoryInfo = System.IO.DirectoryInfo;
using FileInfo = System.IO.FileInfo;
using IEnumerator = System.Collections.IEnumerator;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;
using System.Diagnostics.CodeAnalysis;

namespace Plana.Core;

[NebulaPreprocess(PreprocessPhase.PostFixStructure)]
public class PatchManager
{
    static PatchManager()
    {
        Init();
    }

    static Harmony? harmony;
    static AssetBundle? ab, indexDeviceAb;
    public static AudioClip GetSound(string name)
    {
        try
        {
            if (ab == null)
            {
                ab = AssetBundle.LoadFromMemory(NebulaAPI.AddonAsset.GetResource("sounds.bundle")!.AsStream()!.ReadBytes());
            }
            return ab!.LoadAsset(name + ".wav").Cast<AudioClip>();
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
        return null!;
    }

    static void Preprocess(NebulaPreprocessor preprocessor)
    {
        Nebula.Game.NebulaGameEnd.RegisterWinCondTip(Yandere.Instance.YandereTeamWin, () => ((ISpawnable)Yandere.MyRole).CanSpawnInCurrentGame, "yandere", null);
        Nebula.Game.NebulaGameEnd.RegisterWinCondTip(Doomsayer.Instance.DoomsayerTeamWin, () => ((ISpawnable)Doomsayer.MyRole).CanSpawnInCurrentGame, "doomsayer", null);
        Nebula.Game.NebulaGameEnd.RegisterWinCondTip(Lawyer.Instance.LawyerWin, () => ((ISpawnable)Lawyer.MyRole).CanSpawnInCurrentGame, "lawyer1", null);
        Nebula.Game.NebulaGameEnd.RegisterWinCondTip(Lawyer.Instance.LawyerWin, () => ((ISpawnable)Lawyer.MyRole).CanSpawnInCurrentGame, "lawyer2", null);
        Nebula.Game.NebulaGameEnd.RegisterWinCondTip(Moriarty.Instance.MoriartyTeamWin, () => ((ISpawnable)Moriarty.MyRole).CanSpawnInCurrentGame, "moriarty1", str => str.Replace("%IMPOSTOR%", Language.Translate("document.tip.winCond.teams.impostor").Color(Impostor.MyTeam.Color.ToUnityColor())).Replace("%JACKAL%", Language.Translate("document.tip.winCond.teams.jackal").Color(Jackal.MyTeam.Color.ToUnityColor())));
        Nebula.Game.NebulaGameEnd.RegisterWinCondTip(Moriarty.Instance.MoriartyTeamWin, () => ((ISpawnable)Moriarty.MyRole).CanSpawnInCurrentGame && Moriarty.CanWinByKillHolmes, "moriarty2", null);
        Nebula.Game.NebulaGameEnd.RegisterWinCondTip(Skinner.Instance.SkinnerWin, () => ((ISpawnable)Skinner.MyRole).CanSpawnInCurrentGame, "skinner", null);
        Nebula.Game.NebulaGameEnd.RegisterWinCondTip(Ouroboros.Instance.OuroborosWin, () => ((ISpawnable)Ouroboros.MyRole).CanSpawnInCurrentGame, "ouroboros", null);
    }

    public static void Init()
    {
        try
        {
            ab = AssetBundle.LoadFromMemory(NebulaAPI.AddonAsset.GetResource("sounds.bundle")!.AsStream()!.ReadBytes());
            audioclips = new Dictionary<string, AudioClip>();
            PDebug.Log("AssetBundleLoaded IsNull:" + (ab == null ? "true" : "false"));
            PDebug.Log("RunPatch");
            harmony = new Harmony("PatchManager");
            harmony.Patch(typeof(ChatController).GetMethod("CoOpen"), null,new HarmonyMethod(typeof(PatchManager).GetMethod("OnChatOpen")));
            harmony.Patch(typeof(ChatController).GetMethod("SendChat"), new HarmonyMethod(typeof(PatchManager).GetMethod("SendChat")));
            harmony.Patch(typeof(ChatController).GetMethod("Update", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("ChatUpdate")));
            PDebug.Log(0);
            harmony.Patch(typeof(HudOverrideTask).GetMethod("Initialize"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("CommCamouflagerEffectInitialize")));
            harmony.Patch(typeof(HudOverrideTask).GetMethod("Complete"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("CommCamouflagerEffectComptele")));
            PDebug.Log(1);
            //harmony.Patch(typeof(AmongUsClient).GetMethod("CoStartGame", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("CoStartGame")));
            //harmony.Patch(typeof(ShipStatus).GetMethod("Start"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("ShipStatusStartPos")));
            harmony.Patch(typeof(PlayerVoteArea).GetMethod("SetCosmetics", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SetLevel")));
            try
            {
                harmony.Patch(typeof(CrewmateGameRule)!.GetMethod("CheckExileWin", BindingFlags.Instance | BindingFlags.NonPublic), new HarmonyMethod(typeof(PatchManager).GetMethod("CrewmateCheck")));
                harmony.Patch(typeof(ImpostorGameRule)!.GetMethod("CheckKillWin", BindingFlags.Instance | BindingFlags.NonPublic), new HarmonyMethod(typeof(PatchManager).GetMethod("ImpostorCheck")));
                harmony.Patch(typeof(JackalCriteria)!.GetMethod("OnUpdate", BindingFlags.Instance | BindingFlags.NonPublic), new HarmonyMethod(typeof(PatchManager).GetMethod("JackalCheck")));
            }
            catch (Exception e)
            {
                PDebug.Log("EndCriteriaPatchError Error:" + e.ToString());
            }
            PDebug.Log(2);
            harmony.Patch(typeof(MeetingCalledAnimation).GetMethod("CoShow"), new HarmonyMethod(typeof(PatchManager).GetMethod("CoShow")));
            harmony.Patch(typeof(SabotageButton).GetMethod("DoClick"), new HarmonyMethod(typeof(PatchManager).GetMethod("MadmatePlusSabotageButton")));
            harmony.Patch(typeof(InfectedOverlay).GetMethod("get_CanUseSabotage"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("MadmatePlusCanSabotage")));
            harmony.Patch(typeof(HudManagerExtension).GetMethod("UpdateHudContent"), new HarmonyMethod(typeof(PatchManager).GetMethod("MadmatePlusButtonPatch")));
            harmony.Patch(typeof(PlayerModInfo).GetMethod("get_FeelLikeHaveCrewmateTasks"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("MadmateFeelLikeHaveCrewmateTasks")));
            harmony.Patch(typeof(PlayerModInfo).GetMethod("get_HasAnyTasks"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("MadmateHasAnyTasks")));
            harmony.Patch(typeof(Vent).GetMethod("CanUse"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("VentCanUse")));
            PDebug.Log(3);
            harmony.Patch(typeof(Guesser.Ability).GetPrivateMethodInfoType("OnMeetingStart"), new HarmonyMethod(typeof(PatchManager).GetMethod("GuesserMeetingStart")));
            harmony.Patch(typeof(Guesser.Ability).GetPrivateMethodInfoType("OnDead"), new HarmonyMethod(typeof(PatchManager).GetMethod("GuesserDead")));
            harmony.Patch(typeof(Guesser.Ability).GetConstructor(new Type[] { typeof(GamePlayer), typeof(bool), typeof(int) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("OnCreateGuesserAbility")));
            harmony.Patch(typeof(GuesserModifier.Instance).GetPrivateMethodInfoType("OnMeetingStart"), new HarmonyMethod(typeof(PatchManager).GetMethod("GuesserModifierMeetingStart")));
            harmony.Patch(typeof(GuesserModifier.Instance).GetPrivateMethodInfoType("OnDead"), new HarmonyMethod(typeof(PatchManager).GetMethod("GuesserDead")));
            harmony.Patch(typeof(PlayerModInfo).GetMethod("UpdateRoleText", AccessTools.all), null, new HarmonyMethod(typeof(GuesserSystem).GetMethod("GuessedLockTargets", AccessTools.all)));
            harmony.Patch(typeof(Spectre.Instance).GetPrivateMethodInfoType("CheckAndTrackKiller"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SpectreArrowPatch")), null, null, null);
            harmony.Patch(typeof(Sheriff.Ability).GetPrivateMethodInfoType("CanKill"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SheriffCanKillPatch")), null, null, null);
            harmony.Patch(typeof(Doctor.Ability).GetConstructor(new Type[] { typeof(GamePlayer), typeof(bool), typeof(float) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("DoctorShieldButton")));
            PDebug.Log(4);
            harmony.Patch(typeof(ChatBubble).GetMethod("SetName"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("DecorateNameColor")));
            harmony.Patch(typeof(MeetingHud).GetMethod("Close"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("GenerateMadmate")));
            //harmony.Patch(typeof(PlayerModInfo).GetMethod("UpdateNameText"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("UpdateNameText")));
            harmony.Patch(typeof(ItemSupplierManager).GetPrivateMethodInfoType("OnGameStart"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("GrowALLFlowers")));
            harmony.Patch(typeof(ItemSupplierManager).GetPrivateMethodInfoType("OnGameStartClient"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("ItemSupplierStartClient")));
            PDebug.Log(5);
            harmony.Patch(typeof(PlayerModInfo).GetMethod("OnGameStart"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("PlayerStartGame")));
            harmony.Patch(typeof(PlayerModInfo).GetMethod("Update"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("PlayerUpdate")));
            harmony.Patch(typeof(PlayerControl).GetMethod("CmdReportDeadBody"), new HarmonyMethod(typeof(PatchManager).GetMethod("CmdReportBody")));
            harmony.Patch(typeof(PolusModification).GetMethod("Modify"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("ModifyPolus")));
            harmony.Patch(typeof(ShipModification).GetMethod("PatchEarlierModification"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("ModifyPolusEarly")));
            var interfaceType = typeof(Virial.Game.IPlayerAbility);
            string getterName = $"{interfaceType.FullName}.get_HideKillButton";
            PDebug.Log(6);
            //harmony.Patch(typeof(Sniper.Ability).GetConstructor(new Type[] { typeof(GamePlayer), typeof(float), typeof(bool) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SniperKillPatch")));
            harmony.Patch(typeof(Sniper.Ability).GetMethod(getterName, BindingFlags.NonPublic | BindingFlags.Instance), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SniperKillPatch2")));
            harmony.Patch(typeof(Sniper.SniperRifle).GetMethod("GetTarget"), new HarmonyMethod(typeof(PatchManager).GetMethod("SniperGetTargetPatch")));
            //harmony.Patch(typeof(Raider.Ability).GetConstructor(new Type[] { typeof(GamePlayer), typeof(bool) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("RaiderKillPatch")));
            harmony.Patch(typeof(Raider.Ability).GetMethod(getterName, BindingFlags.NonPublic | BindingFlags.Instance), null, new HarmonyMethod(typeof(PatchManager).GetMethod("RaiderKillPatch2")));
            harmony.Patch(typeof(ProgressTracker).GetMethod("FixedUpdate"), new HarmonyMethod(typeof(PatchManager).GetMethod("ProgressTrackerUpdateFix")));
            harmony.Patch(typeof(NebulaGameEventListeners).GetMethod("NonCrewmateCanSeeTaskTracker", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("NonCrewSeeProgressTrackerPatch", AccessTools.all)));
            harmony.Patch(typeof(Nebula.Listeners.UpdateProgressTrackerPatch).GetMethod("Prefix", AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("BlockNosProgressTrackerUpdate")));
            harmony.Patch(typeof(FreePlayRoleAllocator).GetMethod("Assign"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("AssignExtraModifier")));
            harmony.Patch(typeof(StandardRoleAllocator).GetMethod("Assign"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("AssignExtraModifier")));
            PDebug.Log(7);
            harmony.Patch(typeof(DelayPlayDropshipAmbiencePatch).GetMethod("Postfix"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("LobbyStart")));
            harmony.Patch(typeof(Navvy.Ability).GetConstructor(new Type[] { typeof(GamePlayer), typeof(bool), typeof(int) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("NavvyCamButton")));
            harmony.Patch(typeof(Lover.Instance).GetPrivateMethodInfo("OnMurdered"), new HarmonyMethod(typeof(PatchManager).GetMethod("LoverOnDisconnectedPatch")));
            harmony.Patch(typeof(NumberOption).GetMethod("Initialize"), new HarmonyMethod(typeof(PatchManager).GetMethod("NumberOptionInitializePrefix")));
            harmony.Patch(typeof(NebulaGameManager).GetMethod("RegisterPlayer"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("AssignHostOp")));
            harmony.Patch(typeof(Psychic.Ability).GetMethod("OnReportDeadBody", BindingFlags.NonPublic | BindingFlags.Instance), new HarmonyMethod(typeof(PatchManager).GetMethod("PsychicReportDeadBody")));
            harmony.Patch(typeof(Collator.Ability).GetMethod("RegisterResult", BindingFlags.NonPublic | BindingFlags.Instance), new HarmonyMethod(typeof(PatchManager).GetMethod("CollatorRegisterResult")));
            harmony.Patch(typeof(Jackal.Instance).GetMethod("get_RoleArgumentsForSidekick"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("JackalArgumentsForSidekick")));
            string released = typeof(IGameOperator).FullName + ".OnReleased";
            PDebug.Log(8);
            harmony.Patch(typeof(Trapper.NiceAbility).GetConstructor(new Type[] { typeof(GamePlayer), typeof(bool), typeof(int) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("NiceTrapperActivate")));
            harmony.Patch(typeof(Trapper.EvilAbility).GetConstructor(new Type[] { typeof(GamePlayer), typeof(bool), typeof(int) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("EvilTrapperActivate")));
            harmony.Patch(typeof(Trapper.NiceAbility).GetPrivateMethodInfoType("OnMeetingStart"), new HarmonyMethod(typeof(PatchManager).GetMethod("NiceTrapperMeetingStart")));
            harmony.Patch(typeof(Trapper.EvilAbility).GetPrivateMethodInfoType("OnMeetingStart"), new HarmonyMethod(typeof(PatchManager).GetMethod("EvilTrapperMeetingStart")));
            harmony.Patch(typeof(Trapper.NiceAbility).GetPrivateMethodInfoType(released), new HarmonyMethod(typeof(PatchManager).GetMethod("NiceTrapperReleased")));
            harmony.Patch(typeof(Trapper.EvilAbility).GetPrivateMethodInfoType(released), new HarmonyMethod(typeof(PatchManager).GetMethod("EvilTrapperReleased")));
            harmony.Patch(typeof(Trapper.EvilAbility).GetPrivateMethodInfoType("LocalUpdate"), new HarmonyMethod(typeof(PatchManager).GetMethod("EvilTrapperUpdate")));
            harmony.Patch(typeof(Trapper.Trap).GetPrivateMethodInfoType("Update"), new HarmonyMethod(typeof(PatchManager).GetMethod("TrapUpdate")));
            harmony.Patch(typeof(PolusData).GetMethod("get_SabotageTypes", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SabotageTypesFix")));
            harmony.Patch(typeof(StampHelpers).GetMethod("TryShowStampRingMenu"), new HarmonyMethod(typeof(PatchManager).GetMethod("TryShowStampRingMenu")));
            harmony.Patch(typeof(Jackal.Instance).GetMethod("OnGameEnd", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("JackalGameEnd")));
            string activated = typeof(RuntimeAssignable).FullName + ".OnActivated";
            PDebug.Log(8);
            harmony.Patch(typeof(Sidekick.Instance).GetMethod(activated, AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SidekickMeetingCheckJackal")));
            harmony.Patch(typeof(SidekickModifier.Instance).GetMethod(activated, AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SidekickModifierMeetingCheckJackal")));
            harmony.Patch(typeof(EmergencyMinigame).GetMethod("Update"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("CallMeetingCheck")));
            harmony.Patch(typeof(NebulaGameManager).GetMethod("OnGameStart"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("NebulaGameStart")));
            harmony.Patch(typeof(FreePlayRoleAllocator).GetMethod("Assign"), new HarmonyMethod(typeof(PatchManager).GetMethod("AssignGameMasterPre")));
            harmony.Patch(typeof(StandardRoleAllocator).GetMethod("Assign"), new HarmonyMethod(typeof(PatchManager).GetMethod("AssignGameMasterPre")));
            harmony.Patch(typeof(RoleTable).GetMethod("Determine"), new HarmonyMethod(typeof(PatchManager).GetMethod("DetermineGameMasterPre")));
            PDebug.Log(9);
            harmony.Patch(typeof(MetaAbility).GetConstructor(new Type[0]), new HarmonyMethod(typeof(PatchManager).GetMethod("GameMasterBlockFreePlayAbility")));
            harmony.Patch(typeof(PlayerControl).GetMethod("AdjustLighting"), new HarmonyMethod(typeof(PatchManager).GetMethod("AdjustLightingPatch")));
            harmony.Patch(typeof(GameData).GetMethod("ShowNotification", AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("ShowNotification")));
            harmony.Patch(typeof(HelpScreen).GetMethod("ShowMyRolesSrceen", AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("ShowMyRolesScreen")));
            harmony.Patch(typeof(Destroyer.Ability).GetConstructor([typeof(GamePlayer), typeof(bool)]), null, new HarmonyMethod(typeof(PatchManager).GetMethod("RpcDestroyerPatch")));
            PDebug.Log(10);
            harmony.Patch(typeof(Snatcher.Ability).GetMethod("OnSnatching", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SnatchPatch")));
            harmony.Patch(typeof(OracleSystem).GetMethod("IsNeutralRole", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("GetNeutralRolePatch")));
            harmony.Patch(typeof(Jackal.Instance).GetMethod("OnDead", AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("JackalPromotePatch")));
            harmony.Patch(typeof(TaskPanelBehaviour).GetMethod("Update", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("TaskPanelPosPatch")));
            harmony.Patch(typeof(Language).GetMethod("Translate"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("TranslatePatch")));
            harmony.Patch(typeof(PlayerModInfo).GetMethod(typeof(GamePlayer).FullName + ".MurderPlayer", AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("ModKillPatch")));
            harmony.Patch(typeof(MetaScreen).GetMethod("GenerateWindow", new Type[]
            {
                typeof(Vector2),typeof(Transform),typeof(Vector3),typeof(bool),typeof(bool),typeof(bool),typeof(BackgroundSetting),typeof(bool)
            }), new HarmonyMethod(typeof(PatchManager).GetMethod("ModernBGPatch")));
            PDebug.Log(11);
            harmony.Patch(typeof(ShipStatus).GetMethod("UpdateSystem", new Type[] { typeof(SystemTypes), typeof(PlayerControl), typeof(byte) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("HostRecordInvokeSabotageRpc")));
            harmony.Patch(typeof(ShipStatus).GetMethod("UpdateSystem", new Type[] { typeof(SystemTypes), typeof(PlayerControl), typeof(Hazel.MessageReader) }), new HarmonyMethod(typeof(PatchManager).GetMethod("ClientRecordInvokeSabotageRpc")));
            harmony.Patch(typeof(NebulaGameManager).GetMethod("OnUpdate"), null, new HarmonyMethod(typeof(Engineer.Instance).GetMethod("EngineerVentTextPatch")));
            harmony.Patch(typeof(TimerImpl).GetMethod("SetAsAbilityCoolDown"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("AbilityTimerFix")));
            harmony.Patch(typeof(ShapeshifterMinigame).GetMethod("Shapeshift"), new HarmonyMethod(typeof(PatchManager).GetMethod("OnShapeshiftPanelSelected")));
            harmony.Patch(typeof(LogicOptionsNormal).GetMethod("GetAnonymousVotes"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("AnonymousVotesFix")));
            harmony.Patch(typeof(PopulateResultPatch).GetMethod("Prefix"), new HarmonyMethod(typeof(PatchManager).GetMethod("ResultPrefix")));
            harmony.Patch(typeof(PopulateResultPatch).GetMethod("ModBloopAVoteIcon", AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("ModBloopAVoteIconPatch")));
            harmony.Patch(typeof(CreateGameOptions).GetMethod("Confirm"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("CreateGameOptionsLoadingPatch")));
            harmony.Patch(typeof(AmongUsClient).GetMethod("CoJoinOnlineGameFromListing"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("JoinGameLoadingPatch")));
            harmony.Patch(typeof(DisconnectPopup).GetMethod("SetText"), new HarmonyMethod(typeof(PatchManager).GetMethod("CustomKickReasonPatch")));
            PDebug.Log(12);
            harmony.Patch(typeof(PlayerControl).GetMethod("get_KillSfx"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("FixMeetingKillSound")));
            harmony.Patch(typeof(PlayerControl).GetMethod("SetKillTimer"), new HarmonyMethod(typeof(PatchManager).GetMethod("FixSetKillTimer")));
            harmony.Patch(typeof(Lover.Instance).GetMethod("CheckExtraWins", AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("LoverExtraWinFix")));
            harmony.Patch(typeof(NebulaGameManager).GetMethod("get_CanSeeAllInfo"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("HasGhostRolePlayerCannotSeeRolePatch")));
            harmony.Patch(typeof(KillerTeamCallback).GetMethod("MarkRemaining"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("FixKillerTeamCallbackCrewmate")));
            harmony.Patch(typeof(ModNewsLoader).GetMethod("Preprocess", AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("BlockLoadNews")));
            harmony.Patch(typeof(ChatController).GetMethod("GetPooledBubble"), new HarmonyMethod(typeof(PatchManager).GetMethod("FixChatNoRoleIcon")));
            harmony.Patch(typeof(Synchronizer).GetMethod("OnInjected", AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("AutoKickNoSyncPlayer")));
            harmony.Patch(typeof(EnterCodeManager).GetMethod("OnEnable"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("BetterEnterCodeUI")));
            harmony.Patch(typeof(ServerDropdownPatch).GetMethod("Prefix", AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("BlockNoSFillServer")));
            harmony.Patch(typeof(ServerDropdown).GetMethod("FillServerOptions"), new HarmonyMethod(typeof(PatchManager).GetMethod("BetterFillServer")));
            PDebug.Log(13);
            harmony.Patch(typeof(ItemSupplier).GetMethod("get_CanObtainForRole", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("ImpostorCannotGainCrewmateFlowerPatch")));
            harmony.Patch(typeof(ItemSupplier).GetMethod("TryGainPerk"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("RefreshFlowerPatch")));
            harmony.Patch(typeof(LobbyBehaviour).GetMethod(nameof(LobbyBehaviour.Start)), null, new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PatchManager.OnLobbyStart))));
            harmony.Patch(typeof(PlayerControl).GetMethod(nameof(PlayerControl.HandleRpc)), null, null, null, new HarmonyMethod(typeof(PatchManager).GetMethod("HandleRpcFinalizer")), null);
            harmony.Patch(typeof(MeetingRoleSelectWindow).GetMethod(nameof(MeetingRoleSelectWindow.OpenRoleSelectWindowUsingTabs), AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PatchManager.OpenRoleSelectWindowUsingTabs))));
            harmony.Patch(typeof(Sheriff.Ability).GetConstructor([typeof(GamePlayer), typeof(bool), typeof(int)]), new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PatchManager.OnCreateSheriffAbility))));
            harmony.Patch(typeof(ObjectTrackers).GetMethod("ForPlayer", [typeof(ILifespan), typeof(float?), typeof(GamePlayer), typeof(Predicate<GamePlayer>), typeof(Predicate<GamePlayer>), typeof(UnityEngine.Color?), typeof(bool), typeof(bool)]), new HarmonyMethod(typeof(PatchManager).GetMethod("FixModTrackerDistance")));
            harmony.Patch(typeof(ObjectTrackers).GetMethod("ForPlayerlike", [typeof(ILifespan), typeof(float?), typeof(Func<UnityEngine.Vector2?>), typeof(Predicate<IPlayerlike>), typeof(Predicate<IPlayerlike>), typeof(UnityEngine.Color?), typeof(bool), typeof(bool)]), new HarmonyMethod(typeof(PatchManager).GetMethod("FixModTrackerDistance")));
            harmony.Patch(typeof(Scarlet.Instance).GetMethod("OnActivated"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("EditScarletGuessable")));
            harmony.Patch(typeof(HelpScreen).GetMethod(nameof(HelpScreen.ShowAssignableScreen), AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("ShowAssignableScreen")));
            harmony.Patch(typeof(AmongUsClient).GetMethod(nameof(AmongUsClient.ExitGame)), new HarmonyMethod(typeof(PatchManager).GetMethod("OnExitGame"), Priority.VeryLow));
            harmony.Patch(typeof(PlayerBanData).GetMethod("get_IsBanned"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("IsSpBanned"), Priority.VeryLow));
            //harmony.Patch(typeof(ObjectTrackerUnityImpl<,>).GetMethod(typeof(ObjectTracker<>).FullName+ "set_KeepAsLongAsPossible"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("FixKeepAsLongAsPossible")));
            OptionPatchs.StartPatch(harmony);
            harmony.PatchAll(typeof(DarkThemePatchs));
            harmony.PatchAll(typeof(IntroPatchs));
            harmony.PatchAll(typeof(NebulaOptionPatchs));
            harmony.PatchAll(typeof(DiagnosisModePatchs));
            harmony.PatchAll(typeof(BetterStandardRoleAllocator));
            harmony.PatchAll(typeof(CannotSeeOtherPatch));
            harmony.PatchAll(typeof(BlockStampsPatch));
            harmony.PatchAll(typeof(LoggerPatchs));
            harmony.PatchAll(typeof(ChatLogSystemPatch));
            harmony.PatchAll(typeof(BlackListSupportPatch));
            harmony.PatchAll(typeof(LanguageSupportPatch));
            SpeechEater.LoadPatch(harmony);
            Plana.Roles.Complex.Splicer.LoadPatch(harmony);
            Scrambler.LoadPatch(harmony);
            Crow.LoadPatch(harmony);
            NinjaFix.LoadPatch(harmony);
            CriminalistWriter.LoadPatch(harmony);
            Lapicide.LoadPatch(harmony);
            Disposer.LoadPatch(harmony);
            Pollyanna.LoadPatch(harmony);
            NewsAnchor.LoadPatch(harmony);
            Lover.MyRole.ConfigurationHolder?.AppendConfiguration(ChatOption);
            Lover.MyRole.ConfigurationHolder?.AppendConfiguration(LoverDisConnectedRemoveLover);
            Sheriff.MyRole.ConfigurationHolder?.AppendConfiguration(SheriffCanKillMadmateModifierOption);
            Sheriff.MyRole.ConfigurationHolder?.AppendConfiguration(SheriffCanKillSidekickModifierOption);
            Sheriff.MyRole.ConfigurationHolder?.AppendConfiguration(SheriffSuicideKillTarget);
            Doctor.MyRole.ConfigurationHolder?.AppendConfiguration(DoctorCanUseShieldOption);
            Doctor.MyRole.ConfigurationHolder?.AppendConfiguration(DoctorShieldNoBlockGuessedOption);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(AssignHostOpOption);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(HostIsGameMaster);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(AutoAssignFirstDeadShield);
            Sidekick.MyRole.ConfigurationHolder!.AppendConfiguration(SidekickModifierCanUseVent);
            ExOptions.AppendConfiguration(BeforeStartGameDisconnectKickAll);
            ExOptions.AppendConfiguration(ModTrackerDistance);
            ExOptions.AppendConfiguration(HideLevelOption);
            ExOptions.AppendConfiguration(IntroShowImpostor);
            GeneralConfigurations.PerkOptions.AppendConfigurations([GameStartGrowALLFlower, ImpostorCannotGainCrewmateFlower, RefreshFlower, RefreshFlowerTime]);
            ExOptions.AppendConfiguration(ImpostorCannotMoveTime);
            ExOptions.AppendConfiguration(CrewmateHasEchoSkill);
            ExOptions.AppendConfiguration(EchoSkillUseTime);
            ExOptions.AppendConfiguration(CannotReport);
            ExOptions.AppendConfiguration(HasHNSMusic);
            ExOptions.AppendConfiguration(HasFinal);
            ExOptions.AppendConfiguration(EnterFinalLeftTaskNum);
            ExOptions.AppendConfiguration(FinalImpostorSpeedUpOption);
            ExOptions.AppendConfiguration(FinalImpostorScanCooldown);
            ExOptions.AppendConfiguration(ShowTaskNum);
            ExOptions.AppendConfiguration(HasFreeChat);
            ExOptions.AppendConfiguration(ImpostorMustKillAllToWin);
            ExOptions.AppendConfiguration(CanSendAdminMessage);
            ExOptions.AppendConfiguration(UseFlashLightMode);
            ExOptions.AppendConfiguration(CrewmateFlashSize);
            ExOptions.AppendConfiguration(ImpostorFlashSize);
            ExOptions.AppendConfiguration(ImpostorHasVitals);
            ExOptions.AppendConfiguration(CrewmateHasVitals);
            ExOptions.AppendConfiguration(CanSeePlayerState);
            ExOptions.AppendConfiguration(CanSeeAllPlayerDead);
            ExOptions.AppendConfiguration(JackalMeetingChat);
            ExOptions.AppendConfiguration(LoverMeetingChat);
            ExOptions.AppendConfiguration(MoriartyMeetingChat);
            ExOptions.AppendConfiguration(FantastMeetingChat);
            ExOptions.AppendConfiguration(HasGhostRolePlayerCannotSeeAllInfo);
            ExOptions.AppendConfiguration(ImpostorCannotSeeOther);
            ExOptions.AppendConfiguration(AllPlayerCannotSeeOtherNameColor);
            ExOptions.AppendConfiguration(SoloBlockUseVent);
            ExOptions.AppendConfiguration(CannotUseSabotage);
            ExOptions.AppendConfiguration(MeetingDeadSendNotice);
            GeneralConfigurations.AssignmentOptions.AppendConfigurations([DesperateAssignToImp, DesperateDoNotAssignToLoverImp/*,DiagnosisOptions*/]);
            Sniper.MyRole.ConfigurationHolder?.AppendConfigurations([SniperCanUseNormalKill, SniperCanLockMini]);
            Raider.MyRole.ConfigurationHolder?.AppendConfiguration(RaiderCanUseNormalKill);
            GeneralConfigurations.MapOptions.AppendConfigurations([PolusHasOxygenSabotage,PolusOxygenSabotageTime,CommSabCamouflagerEffectActive,
                CommSabRandomCosmic,TimeLimit,TimeLimitMin]);
            Navvy.MyRole.ConfigurationHolder?.AppendConfiguration(NavvyCanUseCamera);
            Psychic.MyRole.ConfigurationHolder?.AppendConfiguration(PsychicSendMessageMode);
            Collator.MyRole.ConfigurationHolder?.AppendConfiguration(collatorSendMessageMode);
            Sidekick.MyRole.ConfigurationHolder?.AppendConfigurations([SidekickPromoteToJackalNoJackalizedRole, SidekickPromoteToJackalRandomJackalizedRole]);
            Jackal.MyRole.ConfigurationHolder?.AppendConfigurations([JackalCanInvokeSabotage, NoAliveImpostorSabotageJackalWin, JackalUseOldPromoteSidekick]);
            IEnumerable<IConfiguration> options = [AccelTrapCost, AccelTrapOnlyCrewmate, DecelTrapCost, DecelTrapOnlyImpostor, PlaceTrapNoMeetingStart];
            Trapper.MyNiceRole.ConfigurationHolder?.AppendConfigurations(options);
            Trapper.MyEvilRole.ConfigurationHolder?.AppendConfigurations(options);
            Trapper.MyEvilRole.ConfigurationHolder?.AppendConfiguration(EvilTrapperKillTrapPLUS);
            Jester.MyRole.ConfigurationHolder?.AppendConfiguration(JesterCanCallMeeting);
            Guesser.MyNiceRole.ConfigurationHolder?.AppendConfigurations([GuesserCannotGuessNoSpawnableRole, NiceGuesserSafeGuessNum]);
            Guesser.MyEvilRole.ConfigurationHolder?.AppendConfigurations([GuesserCannotGuessNoSpawnableRole, EvilGuesserSafeGuessNum]);
            GuesserModifier.MyRole.ConfigurationHolder?.AppendConfigurations([GuesserCannotGuessNoSpawnableRole, GuesserAutoAssign, GuesserOnlyInGameAutoAssign, GuesserAutoAssignFilter]);
            Destroyer.MyRole.ConfigurationHolder?.AppendConfiguration(InVisibleDestroyCanMove);
            Snatcher.MyRole.ConfigurationHolder?.AppendConfigurations([SnatcherCannotGuessSnatchedPlayer, SnatcherExileUseUsurpRole]);
            Mayor.MyRole.ConfigurationHolder?.AppendConfiguration(MayorExtraVoteAnonymousVote);
            GeneralConfigurations.ExileOptions.AppendConfiguration(ExileShowEvilModifier);
            GeneralConfigurations.ExileOptions.AppendConfiguration(ExileShowAllModifier);
            Scarlet.MyRole.ConfigurationHolder?.AppendConfigurations([ScarletPrivateMeetingChat, ScarletCannotGuessScarletTeam]);
            new ClientOption((ClientOption.ClientOptionType)123, "useairshipSound", ["options.switch.off", "options.switch.on"], 0);
            new ClientOption((ClientOption.ClientOptionType)124, "darktheme", ["options.switch.off", "options.switch.on"], 0);
            new ClientOption((ClientOption.ClientOptionType)125, "ggdGuesserSound", ["options.switch.off", "options.switch.on"], 0);
            new ClientOption((ClientOption.ClientOptionType)126, "mapuseRoleColor", ["options.switch.off", "options.switch.on"], 0);
            new ClientOption((ClientOption.ClientOptionType)127, "alluiUseModern", ["options.switch.off", "options.switch.on"], 1);
            new ClientOption((ClientOption.ClientOptionType)128, "loadingAnim", ["options.switch.off", "options.switch.on"], 1);
            new ClientOption((ClientOption.ClientOptionType)129, "myRolesScreenUseIconSort", ["options.switch.off", "options.switch.on"], 1);
            new ClientOption((ClientOption.ClientOptionType)130, "blockStamps", ["options.switch.off", "options.switch.on"], 0);
            GeneralConfigurations.AssignmentOptions.ScheduleAddRelated(() => [Desperate.MyRole.ConfigurationHolder!]);

            static Hint WithImage(string id)
            {
                return new HintWithImage(NebulaAPI.AddonAsset.GetResource("Hints/" + id.HeadUpper() + ".png")!.AsImage()!, new TranslateTextComponent("hint." + id.HeadLower() + ".title"), new TranslateTextComponent("hint." + id.HeadLower() + ".detail"));
            }
            HintManager.AllHints = new();
            HintManager.RegisterHint(WithImage("AddonDoctor"));
            HintManager.RegisterHint(WithImage("Skinner"));
            HintManager.RegisterHint(WithImage("KindnessJackal"));
            HintManager.RegisterHint(WithImage("Plan17HB"));
            HintManager.RegisterHint(WithImage("Rolenumbers"));

            PDebug.Log("Done!");
            if (DataManager.Settings.Language.CurrentLanguage == SupportedLangs.SChinese || DataManager.Settings.Language.CurrentLanguage == SupportedLangs.English)
            {
                PDebug.Log("CoLoadSponsers");
                NebulaManager.Instance.StartCoroutine(LoadInfo());
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }

    public static Team DreamweaverAndInsomniacsTeam = new Team("teams.DreamweaverAndInsomniacs", new(216, 164, 246), TeamRevealType.OnlyMe);
    public static Team GameMasterTeam = new Team("teams.gm", new(255, 91, 112), TeamRevealType.OnlyMe);
    public static Team ViolatorTeam = new Team("teams.violator", new(255, 91, 112), TeamRevealType.Everyone);
    public static TranslatableTag Refiner = new TranslatableTag("state.refiner");
    public static TranslatableTag DesperateDead = new TranslatableTag("state.desperate");
    public static TranslatableTag Regret = new TranslatableTag("state.regret");
    public static TranslatableTag baddream = new TranslatableTag("state.baddream");
    public static TranslatableTag CriminologistKill = new TranslatableTag("state.cwend");
    public static TranslatableTag NoHope = new TranslatableTag("state.nohope");
    public static BoolConfiguration ChatOption = NebulaAPI.Configurations.Configuration("options.role.lover.ChatActive", false);
    static BoolConfiguration SheriffCanKillMadmateModifierOption = NebulaAPI.Configurations.Configuration("options.role.sheriff.cankillmadmatemodifier", true);
    static BoolConfiguration SheriffCanKillSidekickModifierOption = NebulaAPI.Configurations.Configuration("options.role.sheriff.cankillsidekickmodifier", true);
    static BoolConfiguration DoctorCanUseShieldOption = NebulaAPI.Configurations.Configuration("options.role.doctor.canuseshield", true);
    static BoolConfiguration DoctorShieldNoBlockGuessedOption = NebulaAPI.Configurations.Configuration("options.role.doctor.shieldNoblockGuessed", false, () => DoctorCanUseShieldOption);
    public static BoolConfiguration CanGenerateInfected = NebulaAPI.Configurations.Configuration("options.assignment.canGenerateInfected", false);
    public static IntegerConfiguration GenerateIfPlayer = NebulaAPI.Configurations.Configuration("options.assignment.Genifplayer", (1, 16), 10);
    public static BoolConfiguration CanGenerateLastImpostor = NebulaAPI.Configurations.Configuration("options.assignment.canGenerateLastImpostor", false);
    public static IntegerConfiguration GenerateLastImpostorIfPlayer = NebulaAPI.Configurations.Configuration("options.assignment.GenLastImpostorifPlayer", (1, 16), 6);
    public static BoolConfiguration CannotGenerateHasInfected = NebulaAPI.Configurations.Configuration("options.assignment.cannotGenerateLastImpostorHasInfected", false);
    static BoolConfiguration SidekickModifierCanUseVent = NebulaAPI.Configurations.Configuration("options.role.sidekick.canusevent", false, () => Sidekick.IsModifierOption);
    static BoolConfiguration IntroShowImpostor = NebulaAPI.Configurations.Configuration("options.general.showImpostor", false);
    static BoolConfiguration GameStartGrowALLFlower = NebulaAPI.Configurations.Configuration("options.perk.glowallflower", false);
    static BoolConfiguration ImpostorCannotGainCrewmateFlower = NebulaAPI.Configurations.Configuration("options.perk.impostorCannotGainCrewmateFlower", false);
    static BoolConfiguration RefreshFlower = NebulaAPI.Configurations.Configuration("options.perk.refreshflower", false);
    static FloatConfiguration RefreshFlowerTime = NebulaAPI.Configurations.Configuration("options.perk.refreshTime", (10f, 600f, 2.5f), 120f, FloatConfigurationDecorator.Second, () => RefreshFlower);
    public static IConfigurationHolder ExOptions = NebulaAPI.Configurations.Holder("options.extra", [ConfigurationTab.Settings], [Virial.Game.GameModes.FreePlay, Virial.Game.GameModes.Standard]);
    static BoolConfiguration CrewmateHasEchoSkill = NebulaAPI.Configurations.Configuration("options.general.CrewmateHasEcho", false, () => IntroShowImpostor);
    static FloatConfiguration EchoSkillUseTime = NebulaAPI.Configurations.Configuration("options.general.echoskillusetime", (2.5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second, () => IntroShowImpostor);
    static BoolConfiguration HasHNSMusic = NebulaAPI.Configurations.Configuration("options.general.HasHNSMusic", false, () => IntroShowImpostor);
    static BoolConfiguration CannotReport = NebulaAPI.Configurations.Configuration("options.general.CannotReport", false, () => IntroShowImpostor);
    static BoolConfiguration HasFinal = NebulaAPI.Configurations.Configuration("options.general.hasfinal", false, () => IntroShowImpostor);
    static IntegerConfiguration EnterFinalLeftTaskNum = NebulaAPI.Configurations.Configuration("options.general.enterfinalLefttasknm", (1, 99), 12, () => IntroShowImpostor && HasFinal);
    static BoolConfiguration ShowTaskNum = NebulaAPI.Configurations.Configuration("options.general.ShowTaskNum", false);
    public static BoolConfiguration HasFreeChat = NebulaAPI.Configurations.Configuration("options.general.hasfreechat", false);
    static BoolConfiguration ImpostorMustKillAllToWin = NebulaAPI.Configurations.Configuration("options.general.impostorMustKillAllToWin", false);
    static FloatConfiguration FinalImpostorScanCooldown = NebulaAPI.Configurations.Configuration("options.general.FinalImpostorScanCooldown", (0f, 60f, 2.5f), 5f, FloatConfigurationDecorator.Second, () => IntroShowImpostor);
    static FloatConfiguration ImpostorCannotMoveTime = NebulaAPI.Configurations.Configuration("options.general.ImpostorCannotMoveTime", (0f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second, () => IntroShowImpostor);
    static FloatConfiguration FinalImpostorSpeedUpOption = NebulaAPI.Configurations.Configuration("options.general.FinalImpotorSpeedUp", (0f, 10f, 0.125f), 0.125f, FloatConfigurationDecorator.Ratio, () => IntroShowImpostor && HasFinal);
    static BoolConfiguration ImpostorHasVitals = NebulaAPI.Configurations.Configuration("options.general.ImpostorHasVitals", false);
    static BoolConfiguration CrewmateHasVitals = NebulaAPI.Configurations.Configuration("options.general.CrewmateHasVitals", false);
    static BoolConfiguration CanSeePlayerState = NebulaAPI.Configurations.Configuration("options.general.CanSeePlayerState", false);
    public static BoolConfiguration CanSeeAllPlayerDead = NebulaAPI.Configurations.Configuration("options.general.CanSeeAllPlayerDead", false);
    static BoolConfiguration UseFlashLightMode = NebulaAPI.Configurations.Configuration("options.general.UseFlashLight", false);
    static FloatConfiguration CrewmateFlashSize = NebulaAPI.Configurations.Configuration("options.general.crewmateFlashSize", (0f, 5f, 0.05f), 0.35f, (val) => val.ToString("F2") + Language.Translate("options.cross"), () => UseFlashLightMode);
    static FloatConfiguration ImpostorFlashSize = NebulaAPI.Configurations.Configuration("options.general.impostorFlashSize", (0f, 5f, 0.05f), 0.25f, (val) => val.ToString("F2") + Language.Translate("options.cross"), () => UseFlashLightMode);
    static BoolConfiguration RaiderCanUseNormalKill = NebulaAPI.Configurations.Configuration("options.raider.CanUseNormalKill", false);
    static BoolConfiguration SniperCanUseNormalKill = NebulaAPI.Configurations.Configuration("options.sniper.CanUseNormalKill", false);
    public static BoolConfiguration SniperCanLockMini = NebulaAPI.Configurations.Configuration("options.sniper.CanLockMini", false);
    static BoolConfiguration PolusHasOxygenSabotage = NebulaAPI.Configurations.Configuration("options.map.PolusHasOxygenSab", true);
    static FloatConfiguration PolusOxygenSabotageTime = NebulaAPI.Configurations.Configuration("options.map.PolusOxygenSabTime", (0f, 120f, 2.5f), 45f, FloatConfigurationDecorator.Second, () => PolusHasOxygenSabotage);
    static BoolConfiguration CanSendAdminMessage = NebulaAPI.Configurations.Configuration("options.general.cansendAdminMessage", true);
    static BoolConfiguration NavvyCanUseCamera = NebulaAPI.Configurations.Configuration("options.navvy.canusecamera", true);
    static BoolConfiguration LoverDisConnectedRemoveLover = NebulaAPI.Configurations.Configuration("options.lover.DCRemoveLover", true);
    public static BoolConfiguration DesperateAssignToImp = NebulaAPI.Configurations.Configuration("options.desperate.AssignToAllImpostor", false);
    static BoolConfiguration DesperateDoNotAssignToLoverImp = NebulaAPI.Configurations.Configuration("options.desperate.DoNotAssignToLoverImpostor", false, () => DesperateAssignToImp);
    static BoolConfiguration AssignHostOpOption = NebulaAPI.Configurations.Configuration("options.general.AssignHostOp", true, () => !GeneralConfigurations.AssignOpToHostOption);
    static BoolConfiguration SidekickPromoteToJackalRandomJackalizedRole = NebulaAPI.Configurations.Configuration("options.role.sidekick.promoteRandomJackalizedRole", false, () => Jackal.JackalizedImpostorOption && SidekickPromoteToJackalNoJackalizedRole != null && !SidekickPromoteToJackalNoJackalizedRole);
    static BoolConfiguration SidekickPromoteToJackalNoJackalizedRole = NebulaAPI.Configurations.Configuration("options.role.sidekick.promoteNoJackalizedRole", false, () => Jackal.JackalizedImpostorOption && !SidekickPromoteToJackalRandomJackalizedRole);
    public static BoolConfiguration EvilTrapperKillTrapPLUS = NebulaAPI.Configurations.Configuration("options.role.trapper.EvilTrapperKillTrapPLUS", false);
    public static BoolConfiguration AccelTrapOnlyCrewmate = NebulaAPI.Configurations.Configuration("options.role.trapper.AccelTrapOnlyCrewmate", false);
    public static BoolConfiguration DecelTrapOnlyImpostor = NebulaAPI.Configurations.Configuration("options.role.trapper.DecelTrapOnlyImpostor", false);
    public static BoolConfiguration PlaceTrapNoMeetingStart = NebulaAPI.Configurations.Configuration("options.role.trapper.placeTrapNoMeetingStart", false);
    public static IntegerConfiguration AccelTrapCost = NebulaAPI.Configurations.Configuration("options.role.trapper.AccelTrapCost", (1, 5), 1);
    public static IntegerConfiguration DecelTrapCost = NebulaAPI.Configurations.Configuration("options.role.trapper.DecelTrapCost", (1, 5), 1);
    static BoolConfiguration JesterCanCallMeeting = NebulaAPI.Configurations.Configuration("options.role.jester.cancallmeeting", true);
    static BoolConfiguration TimeLimit = NebulaAPI.Configurations.Configuration("options.map.general.timelimit", false);
    static IntegerConfiguration TimeLimitMin = NebulaAPI.Configurations.Configuration("options.map.general.timelimit.min", (1, 120), 35, () => TimeLimit);
    static BoolConfiguration HostIsGameMaster = NebulaAPI.Configurations.Configuration("options.assignments.hostisGM", false);
    static BoolConfiguration InVisibleDestroyCanMove = NebulaAPI.Configurations.Configuration("options.role.destroyer.invisibledestroycanmove", false);
    static BoolConfiguration SnatcherCannotGuessSnatchedPlayer = NebulaAPI.Configurations.Configuration("options.role.snatcher.cannotguessSnatchedPlayer", false);
    static BoolConfiguration JackalUseOldPromoteSidekick = NebulaAPI.Configurations.Configuration("options.role.jackal.useoldpromotesidekick", false);
    static BoolConfiguration PsychicSendMessageMode = NebulaAPI.Configurations.Configuration("options.role.psychic.sendmessagemode", false);
    static BoolConfiguration collatorSendMessageMode = NebulaAPI.Configurations.Configuration("options.role.collator.sendmessagemode", false);
    public static BoolConfiguration GuesserCannotGuessNoSpawnableRole = NebulaAPI.Configurations.Configuration("options.role.guesser.cannotguessnospwanableRole", true);
    static BoolConfiguration JackalCanInvokeSabotage = NebulaAPI.Configurations.Configuration("options.role.jackal.canInvokeSabotage", false);
    static BoolConfiguration NoAliveImpostorSabotageJackalWin = NebulaAPI.Configurations.Configuration("options.role.jackal.noAliveImpostorSabotageWin", false, () => JackalCanInvokeSabotage);
    public static BoolConfiguration ImpostorMeetingChat = NebulaAPI.Configurations.Configuration("options.general.impMeetingChat", false);
    public static BoolConfiguration JackalMeetingChat = NebulaAPI.Configurations.Configuration("options.general.jackalMeetingChat", false);
    public static BoolConfiguration LoverMeetingChat = NebulaAPI.Configurations.Configuration("options.general.LoverMeetingChat", false);
    public static BoolConfiguration MoriartyMeetingChat = NebulaAPI.Configurations.Configuration("options.general.moriartyMeetingChat", false);
    public static BoolConfiguration FantastMeetingChat = NebulaAPI.Configurations.Configuration("options.general.fantastMeetingChat", false);
    static bool CanInvokeSpecialMeeting = false;//NebulaAPI.Configurations.Configuration("options.diagnosis.caninvokespecialMeeting", false, () => AmongUsLLImpl.AmongUsClientInstance?.AmHost ?? false);
    static bool DiagnosisIsActive = false;//NebulaAPI.Configurations.Configuration("options.diagnosis.IsActive", false, () => (AmongUsLLImpl.AmongUsClientInstance?.AmHost ?? false) && CanInvokeSpecialMeeting);
    //static int DiagnosisChance = 0;
    // NebulaAPI.Configurations.Configuration("options.diagnosis.chance", new ValueTuple<int, int, int>(0, 100, 10), 0, () => (AmongUsLLImpl.AmongUsClientInstance?.AmHost ?? false) && CanInvokeSpecialMeeting && DiagnosisIsActive, val => val.ToString() + "%");
    static FloatConfiguration StartKillTime = NebulaAPI.Configurations.Configuration("options.diagnosis.startkilltime", (0f, 60f, 2.5f), 17.5f, FloatConfigurationDecorator.Second, () => (AmongUsLLImpl.AmongUsClientInstance?.AmHost ?? false) && CanInvokeSpecialMeeting && DiagnosisIsActive);
    static IntegerConfiguration KillChance = NebulaAPI.Configurations.Configuration("options.diagnosis.killchance", new ValueTuple<int, int, int>(0, 100, 10), 40, () => (AmongUsLLImpl.AmongUsClientInstance?.AmHost ?? false) && CanInvokeSpecialMeeting && DiagnosisIsActive, val => val.ToString() + "%");
    //static GroupConfiguration DiagnosisOptions = new GroupConfiguration("options.diagnosis.group", [CanInvokeSpecialMeeting, DiagnosisIsActive, DiagnosisChance, StartKillTime, KillChance], ViolatorTeam.Color.ToUnityColor(), () => AmongUsLLImpl.AmongUsClientInstance?.AmHost ?? false);
    static BoolConfiguration ExileShowEvilModifier = NebulaAPI.Configurations.Configuration("options.exile.showevilmodifier", false, () => ExileShowAllModifier != null && !ExileShowAllModifier);
    static BoolConfiguration ExileShowAllModifier = NebulaAPI.Configurations.Configuration("options.exile.showallmodifier", false, () => !ExileShowEvilModifier);
    static BoolConfiguration MayorExtraVoteAnonymousVote = NebulaAPI.Configurations.Configuration("options.role.mayor.extravoteAnonymous", false);
    static BoolConfiguration AutoAssignFirstDeadShield = NebulaAPI.Configurations.Configuration("options.assignment.autoassignfirstdeadshield", true);
    static IntegerConfiguration NiceGuesserSafeGuessNum = NebulaAPI.Configurations.Configuration("options.role.guesser.niceGuesserSafeGuessNum", (0, 15), 0);
    static IntegerConfiguration EvilGuesserSafeGuessNum = NebulaAPI.Configurations.Configuration("options.role.guesser.evilGuesserSafeGuessNum", (0, 15), 0);
    static BoolConfiguration HasGhostRolePlayerCannotSeeAllInfo = NebulaAPI.Configurations.Configuration("options.general.hasGhostRoleCannotSeeAllInfo", true);
    public static BoolConfiguration ImpostorCannotSeeOther = NebulaAPI.Configurations.Configuration("options.general.impostorCannotSeeOther", false);
    public static BoolConfiguration AllPlayerCannotSeeOtherNameColor = NebulaAPI.Configurations.Configuration("options.general.allplayerCannotSeeOtherNameColor", false);
    static BoolConfiguration SoloBlockUseVent = NebulaAPI.Configurations.Configuration("options.general.soloblockUseVent", false);
    static BoolConfiguration CannotUseSabotage = NebulaAPI.Configurations.Configuration("options.general.cannotUseSabotage", false);
    static BoolConfiguration MeetingDeadSendNotice = NebulaAPI.Configurations.Configuration("options.general.meetingDeadSendNotice", false);
    static BoolConfiguration GuesserAutoAssign = NebulaAPI.Configurations.Configuration("options.role.guesser.autoassign", false);
    static BoolConfiguration GuesserOnlyInGameAutoAssign = NebulaAPI.Configurations.Configuration("options.role.guesser.onlyInGameAutoAssign", true);
    static SimpleRoleFilterConfiguration GuesserAutoAssignFilter = new SimpleRoleFilterConfiguration("options.role.guesser.autoassignFilter")
    {
        ScrollerTag = "guesserAutoAssign",
        RolePredicate = (r) => !r.IsSystemRole && r.CanLoad(GuesserModifier.MyRole)
    };
    static ValueConfiguration<int> ModTrackerDistance = NebulaAPI.Configurations.Configuration("options.general.modtrackerDistance", ["options.general.modtrackerDistance.short", "options.general.modtrackerDistance.mid", "options.general.modtrackerDistance.long"], 1);
    static BoolConfiguration SheriffSuicideKillTarget = NebulaAPI.Configurations.Configuration("options.role.sheriff.suicidekilltarget", false);
    public static BoolConfiguration ScarletPrivateMeetingChat = NebulaAPI.Configurations.Configuration("options.role.scarlet.privatechat", false);
    static BoolConfiguration ScarletCannotGuessScarletTeam = NebulaAPI.Configurations.Configuration("options.role.scarlet.cannotguessScarletTeam", false);
    static BoolConfiguration SnatcherExileUseUsurpRole = NebulaAPI.Configurations.Configuration("options.role.snatcher.exileUseUsurpRole", true);
    static BoolConfiguration HideLevelOption = NebulaAPI.Configurations.Configuration("options.general.hidelevel", false);
    static BoolConfiguration BeforeStartGameDisconnectKickAll = NebulaAPI.Configurations.Configuration("options.general.beforeStartGameDisconnectKickAll", true);
    static Virial.Media.Image doctorshieldimage = NebulaAPI.AddonAsset.GetResource("DoctorShield.png")!.AsImage(115f)!;
    static Virial.Media.Image globalscanimage = NebulaAPI.AddonAsset.GetResource("GlobalScan.png")!.AsImage(115f)!;
    static ModAbilityButton? scanbutton;
    static SpriteRenderer? redFullS;
    static bool isfinal;
    static Minigame? navvyMiniGame;
    public static GameEnd SkeldEnd = NebulaAPI.Preprocessor!.CreateEnd("timeout.skeld", new(0.28235295f, 0.30588236f, 0.32941177f), 255, false);
    public static GameEnd MiraEnd = NebulaAPI.Preprocessor!.CreateEnd("timeout.mira", new(0.28235295f, 0.30588236f, 0.32941177f), 255, false);
    public static GameEnd PolusEnd = NebulaAPI.Preprocessor!.CreateEnd("timeout.polus", new(0.28235295f, 0.30588236f, 0.32941177f), 255, false);
    public static GameEnd AirshipEnd = NebulaAPI.Preprocessor!.CreateEnd("timeout.airship", new(0.28235295f, 0.30588236f, 0.32941177f), 255, false);
    public static GameEnd FungleEnd = NebulaAPI.Preprocessor!.CreateEnd("timeout.fungle", new(0.28235295f, 0.30588236f, 0.32941177f), 255, false);
    public static GameEnd NobodyAliveEnd = NebulaAPI.Preprocessor!.CreateEnd("nobodyalive", new(0.28235295f, 0.30588236f, 0.32941177f), 255, false);
    static bool FlashlightEnabled = false;
    static RemoteProcess<int> SetUseBubbleNum = new("SetUseBubblenum", (m, _) =>
    {
        usebubblenum = m;
    });
    public static int usebubblenum;
    static List<byte> votedPlayerId = new List<byte>();
    public static GameObject EnterCodeSelectServer;
    private static PlayerControl? FindPlayerById(byte playerId)
    {
        try
        {
            return PlayerControl.AllPlayerControls.ToArray()
                .FirstOrDefault(player => player && player.PlayerId == playerId);
        }
        catch
        {
            return null;
        }
    }
    public class BlackListReadWriter
    {
        static string blackListPath => Application.persistentDataPath + "/p17hostblacklist.dat";
        public static void WritePlayer(PlayerControl pc)
        {
            if (!File.Exists(blackListPath))
            {
                File.Create(blackListPath);
            }
            File.AppendAllText(blackListPath, pc.FriendCode + Environment.NewLine);
        }
        public static void WritePlayer(string code)
        {
            if (!File.Exists(blackListPath))
            {
                File.Create(blackListPath);
            }
            File.AppendAllText(blackListPath, code + Environment.NewLine);
        }
        public static void WriteList(List<string> list)
        {
            var sb = new StringBuilder();
            list.Do(s => sb.Append(s).Append(Environment.NewLine));
            File.WriteAllText(blackListPath, sb.ToString());
        }
        public static void RemovePlayer(string code)
        {
            if (!File.Exists(blackListPath))
            {
                PDebug.Log("Not Found BlackListData");
                return;
            }
            var list = ReadBlacklist();
            if (!list.Remove(code))
            {
                PDebug.Log("Not Found " + code + " In BlackListData");
            }
            else
            {
                WriteList(list);
            }
        }
        public static List<string> ReadBlacklist()
        {
            return File.ReadAllLines(blackListPath).ToList();
        }
    }
    [HarmonyPatch]
    public class BlackListSupportPatch
    {
        public static void SendFriendCode(PlayerControl myPlayer)
        {
            try
            {
                if (AmongUsLLImpl.AmongUsClientInstance.AmHost) return;
                if (myPlayer == null)
                {
                    return;
                }
                var myFriendCode = EOSManager.Instance.FriendCode;
                if (myFriendCode != null)
                {
                    ReceiveFriendCode(myPlayer.PlayerId, myFriendCode);
                }
                else
                {
                    PDebug.Log("Fail to Find Friendcode");
                    return;
                }
            }
            catch (Exception e)
            {
                PDebug.Log("SendFriendCode Error: " + e.Message);
            }
        }
        [NebulaRPC]
        public static void ReceiveFriendCode(byte senderPlayerId, string friendCode)
        {
            try
            {
                if (!AmongUsLLImpl.AmongUsClientInstance.AmHost || string.IsNullOrWhiteSpace(friendCode))
                {
                    return;
                }
                var playerControl = FindPlayerById(senderPlayerId);
                if (playerControl == null)
                {
                    return;
                }
                var list = BlackListReadWriter.ReadBlacklist();
                if (list.Contains(friendCode))
                {
                    var client = GetClient(playerControl);
                    SetSendKickPlayer.Invoke((AmongUsLLImpl.LocalPlayer.PlayerId, senderPlayerId, Language.Translate("chatcommand.blackmaillist.kickreason")));
                    NebulaManager.Instance.StartDelayAction(1f, () =>
                    {
                        KickPlayer.Invoke((client.Id, false));
                    });
                }
            }
            catch (Exception e)
            {
                PDebug.Log("ReceiveFriendCode Error: " + e.Message);
            }
        }
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start)), HarmonyPostfix]
        public static void OnPlayerJoined(PlayerControl __instance)
        {
            if (__instance == null || !__instance.AmOwner) return;
            NebulaManager.Instance.StartDelayAction(2f, () =>
            {
                SendFriendCode(__instance);
            });
        }
    }
    public static List<(IEnumerable<DefinedAssignable> roles, TextComponent label)> GetCustomAssignableLabel()
    {
        var madmateRoles = Nebula.Roles.Roles.AllRoles.Where(r => r.IsMadmate && r.ShowOnHelpScreen);
        var madmateModifier = Nebula.Roles.Roles.AllModifiers.Where(m => m.IsMadmate && m.ShowOnHelpScreen);
        List<(IEnumerable<DefinedAssignable> roles, TextComponent label)> list = [
            (Nebula.Roles.Roles.AllRoles.Where(r => r.Category == RoleCategory.ImpostorRole &&!r.IsMadmate&& (r as DefinedAssignable).ShowOnHelpScreen), NebulaGUIWidgetEngine.API.TextComponent(Virial.Color.ImpostorColor, "role.category.impostor")),
            (Nebula.Roles.Roles.AllRoles.Where(r => r.Category == RoleCategory.NeutralRole &&!r.IsMadmate&& (r as DefinedAssignable).ShowOnHelpScreen), NebulaGUIWidgetEngine.API.TextComponent(new(1f, 0.7f, 0f), "role.category.neutral")),
            (Nebula.Roles.Roles.AllRoles.Where(r => r.Category == RoleCategory.CrewmateRole &&!r.IsMadmate&& (r as DefinedAssignable).ShowOnHelpScreen), NebulaGUIWidgetEngine.API.TextComponent(Virial.Color.CrewmateColor, "role.category.crewmate")),
            (Nebula.Roles.Roles.AllGhostRoles.Where(r => (r as DefinedAssignable).ShowOnHelpScreen), NebulaGUIWidgetEngine.API.TextComponent(Virial.Color.Gray, "role.category.ghost")),
            (Nebula.Roles.Roles.AllModifiers.Where(r => (r as DefinedAssignable).ShowOnHelpScreen), NebulaGUIWidgetEngine.API.TextComponent(Virial.Color.White, "role.category.modifier")),
            (madmateRoles, NebulaGUIWidgetEngine.API.TextComponent(Virial.Color.ImpostorColor, "role.category.madmate")),
            (madmateModifier, NebulaGUIWidgetEngine.API.TextComponent(Virial.Color.ImpostorColor, "role.category.madmatemodifier"))
            ];
        return list;
    }
    public static bool ShowAssignableScreen(ref IMetaWidgetOld __result)
    {
        var assignables = GetCustomAssignableLabel();
        (IEnumerable<PerkFunctionalDefinition> perks, TextComponent label)[] perks = [
            (Nebula.Roles.Roles.AllPerks.Where(p => p.PerkCategory == PerkFunctionalDefinition.Category.Standard), NebulaGUIWidgetEngine.API.TextComponent(Virial.Color.White, "game.metaAbility.perks.standard")),
            (Nebula.Roles.Roles.AllPerks.Where(p => p.PerkCategory == PerkFunctionalDefinition.Category.NoncrewmateOnly), NebulaGUIWidgetEngine.API.TextComponent(Virial.Color.White, "game.metaAbility.perks.noncrewmateOnly")),
            ];

        MetaWidgetOld inner = new();

        void AddContent(TextComponent label, Action routine)
        {
            if (inner.Count > 0) inner.Append(new MetaWidgetOld.VerticalMargin(0.2f));

            inner.Append(new MetaWidgetOld.WrappedWidget(new NoSGUIText(GUIAlignment.Left, NebulaGUIWidgetEngine.API.GetAttribute(Virial.Text.AttributeAsset.DocumentTitle), label)));
            inner.Append(new MetaWidgetOld.VerticalMargin(0.1f));
            routine.Invoke();
        }

        List<DefinedAssignable> listedAssignables = [];

        foreach (var content in assignables)
        {
            AddContent(content.label, () =>
            {
                inner.Append(content.roles, (role) =>
                {
                    int myIndex = listedAssignables.Count;
                    listedAssignables.Add(role);

                    return new CombinedWidgetOld(new MetaWidgetOld.HorizonalMargin(0.12f), new MetaWidgetOld.Button(() =>
                    {
                        int currentIndex = myIndex;
                        var window = HelpScreen.OpenAssignableHelp(listedAssignables[myIndex]);

                        void ReopenWindow()
                        {
                            if (window != null) window.CloseScreen();
                            window = HelpScreen.OpenAssignableHelp(listedAssignables[currentIndex]);
                            if (window != null)
                            {
                                MetaScreen.SetUpNavButton(window, increment =>
                                {
                                    currentIndex = (listedAssignables.Count + currentIndex + (increment ? 1 : -1)) % listedAssignables.Count;
                                    ReopenWindow();
                                });
                            }
                        }
                        ReopenWindow();
                    }, HelpScreen.RoleTitleAttr)
                    {
                        RawText = role.DisplayColoredName,
                        PostBuilder = (PassiveButton button, SpriteRenderer renderer, TMPro.TextMeshPro text) =>
                        {
                            renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                            button.OnMouseOver.AddListener(() =>
                            {
                                NebulaManager.Instance.SetHelpWidget(button, HelpScreen.GetAssignableOverlay(role));
                            });
                            button.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpWidgetIf(button));
                            text.transform.localPosition += new UnityEngine.Vector3(0.07f, 0f, 0f);
                            button.transform.localPosition -= new UnityEngine.Vector3(0.15f, 0f, 0f);

                            var roleIcon = role.GetRoleIcon()?.GetSprite();
                            if (roleIcon)
                            {
                                var icon = UnityHelper.CreateObject<SpriteRenderer>("Icon", button.transform, new(-0.73f, 0f, -0.01f));
                                icon.sprite = roleIcon;
                                icon.material = RoleIcon.GetRoleIconMaterial(role, 0.8f);
                                icon.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                                icon.transform.localScale = new(0.275f, 0.275f, 1f);
                            }
                        },
                        Alignment = IMetaWidgetOld.AlignmentOption.Center,
                        TextHorizonotalExtraMargin = 0.15f,
                    });
                }, 4, -1, 0, 0.6f);
            });
        }

        foreach (var content in perks)
        {
            AddContent(content.label, () =>
            {
                inner.Append(content.perks, (perk) => new MetaWidgetOld.WrappedWidget(perk.PerkDefinition.GetPerkImageWidget(true, overlay: () => perk.PerkDefinition.GetPerkWidget())), 8, -1, 0, 0.7f);
            });
        }

        __result = new MetaWidgetOld.ScrollView(new(7.4f, HelpScreen.HelpHeight), inner) { Alignment = IMetaWidgetOld.AlignmentOption.Center };
        return false;
    }

    public static void EditScarletGuessable(RuntimeRole __instance)
    {
        if (!ScarletCannotGuessScarletTeam)
        {
            return;
        }
        var scarlet = __instance as Scarlet.Instance;
        if (scarlet?.AmOwner ?? false)
        {
            GameOperatorManager.Instance?.Subscribe<PlayerCanGuessPlayerLocalEvent>(ev =>
            {
                if (scarlet.IsMyLover(ev.Player))
                {
                    ev.CanGuess = false;
                }
            }, scarlet);
        }
    }

    public static void FixModTrackerDistance(ref float? distance)
    {
        var local = GamePlayer.LocalPlayer;
        if (local != null && (local.Role is Arsonist.Instance || local.TryGetAbility<Oracle.Ability>(out var a) || local.TryGetAbility<Sommelier.Ability>(out var ab)))
        {
            if (!distance.HasValue)
            {
                var array = AmongUsLLImpl.CurrentGameOptionsInstance.GetFloatArray(FloatArrayOptionNames.KillDistances);
                distance = array[Math.Clamp(ModTrackerDistance.GetValue(), 0, array.Length - 1)];
            }
        }
    }

    //I won't be using this patch for now
    /*public static void FixKeepAsLongAsPossible(object __instance)
    {
        var type = __instance.GetType();
        var maxDistance = type.GetField("maxDistance");
        if ((bool)type.GetField("isSoftLocked", AccessTools.all)?.GetValue(__instance))
        {
            var array = AmongUsLLImpl.CurrentGameOptionsInstance.GetFloatArray(FloatArrayOptionNames.KillDistances);
            maxDistance.SetValue(__instance, array[Math.Clamp(ModTrackerDistance.GetValue(), 0, array.Length - 1)]);
        }
        else
        {
            maxDistance.SetValue(__instance, AmongUsLLImpl.Instance.VanillaKillDistance);
        }
    }*/

    public static void RecordCompleteTask(PlayerControl __instance, uint idx)
    {
        var task = __instance.myTasks.GetFastEnumerator().FirstOrDefault(c => c.Id == idx);
        if (task.AsBoolFast(out var taskObj))
        {
            var sb = new Il2CppSystem.Text.StringBuilder();
            taskObj.AppendTaskText(sb);
            GameMaster.SendMessageToGM.Invoke($"玩家 {__instance.name} 完成了任务 {sb.ToString()}");
        }
    }

    public static bool OnCreateSheriffAbility(Sheriff.Ability __instance, GamePlayer player, bool isUsurped, int shots)
    {
        if (!SheriffSuicideKillTarget)
        {
            return true;
        }
        var myPlayerProperty = typeof(AbstractPlayerUsurpableAbility).GetProperty(
            "MyPlayer",
            AccessTools.all
        );
        myPlayerProperty?.SetValue(__instance, player);
        var isUsurpedProperty = typeof(AbstractPlayerUsurpableAbility).GetProperty(
            "IsUsurped",
            AccessTools.all
        );
        isUsurpedProperty?.SetValue(__instance, isUsurped);
        try
        {
            __instance.leftShots = shots;
            if (__instance.AmOwner)
            {
                PDebug.Log("CreateSuicideKillTargetSheriffAbility");
                var acTokenAnother3 = AbstractAchievement.GenerateSimpleTriggerToken("sheriff.another3");
                GameOperatorManager.Instance?.Subscribe<MeetingEndEvent>(ev => { acTokenAnother3.Value.triggered = false; }, __instance);
                GameOperatorManager.Instance?.Subscribe<PlayerExiledEvent>(ev => { if (ev.Player.AmOwner) acTokenAnother3.Value.isCleared |= acTokenAnother3.Value.triggered; }, __instance);

                __instance.acTokenCommon2 = new("sheriff.common2", (byte.MaxValue, false), (val, _) => val.Item2);
                __instance.acTokenAnother2 = new("sheriff.another2", true, (val, _) => val && NebulaGameManager.Instance?.EndState?.EndCondition == NebulaGameEnd.CrewmateWin && !__instance.MyPlayer.IsDead);

                var killTracker = ObjectTrackers.ForPlayerlike(__instance, null, __instance.MyPlayer, ObjectTrackers.PlayerlikeKillablePredicate(__instance.MyPlayer), null, Sheriff.CanKillHidingPlayerOption);
                __instance.killButton = new ModAbilityButtonImpl(isArrangedAsKillButton: __instance.MyPlayer.IsCrewmate).KeyBind(__instance.MyPlayer.IsCrewmate ? Virial.Compat.VirtualKeyInput.Kill : Virial.Compat.VirtualKeyInput.Ability).Register(__instance);

                var leftText = __instance.killButton.ShowUsesIcon(3);
                leftText.text = __instance.leftShots.ToString();

                __instance.killButton.SetSprite(Sheriff.buttonSprite.GetSprite());

                SpriteRenderer? lockSprite = null;
                if (Sheriff.SealAbilityUntilReportingDeadBodiesOption && !NebulaGameManager.Instance!.AllPlayerInfo.Any(p => p.IsDead))
                {
                    lockSprite = __instance.killButton.VanillaButton.AddLockedOverlay();

                    if (Sheriff.UnsealOnReportButtonTurningOnOption)
                    {
                        GameOperatorManager.Instance?.Subscribe<GameUpdateEvent>(ev =>
                        {
                            if (lockSprite != null && HudManager.InstanceExists && AmongUsLLImpl.HudManagerBridge.ReportButton.canInteract)
                            {
                                __instance.killButton.PlayFlashOnce();
                                GameObject.Destroy(lockSprite!.gameObject);
                                lockSprite = null;
                            }
                        }, __instance);
                    }
                }

                __instance.killButton.Availability = (button) => killTracker.CurrentTarget != null && __instance.MyPlayer.CanMove && !__instance.MyPlayer.WillDie && lockSprite == null;
                __instance.killButton.Visibility = (button) => !__instance.MyPlayer.IsDead && __instance.leftShots > 0;
                __instance.killButton.OnClick = (button) =>
                {
                    var player = killTracker.CurrentTarget;

                    if (player == null) return;
                    __instance.acTokenAnother2.Value = false;
                    Sheriff.StatsShot.Progress();
                    if (!__instance.MyPlayer.IsMadmate && __instance.CanKill(player!.RealPlayer!))
                    {
                        new StaticAchievementToken("sheriff.common1");
                        __instance.acTokenCommon2.Value.Item2 |= __instance.acTokenCommon2.Value.Item1 == player!.RealPlayer.PlayerId;
                        if (__instance.acTokenChallenge != null && player!.RealPlayer.IsImpostor) __instance.acTokenChallenge!.Value--;

                        __instance.MyPlayer.MurderPlayer(player!, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);

                        acTokenAnother3.Value.triggered = true;
                    }
                    else
                    {
                        __instance.MyPlayer.MurderPlayer(player, PlayerState.Misfired, EventDetail.Kill, KillParameter.RemoteKill, KillCondition.TargetAlive);
                        __instance.MyPlayer.Suicide(PlayerState.Misfired, PlayerState.Suicide, Virial.Game.KillParameter.NormalKill);
                        NebulaGameManager.Instance?.GameStatistics.RpcRecordEvent(GameStatistics.EventVariation.Kill, EventDetail.Misfire, __instance.MyPlayer.VanillaPlayer, player!.RealPlayer.VanillaPlayer);
                        Sheriff.RpcShareExtraInfo.Invoke((__instance.MyPlayer, killTracker.CurrentTarget!.RealPlayer));
                        new StaticAchievementToken("sheriff.another1");
                        Sheriff.StatsMisshot.Progress();
                    }
                    button.StartCoolDown();

                    leftText.text = (--__instance.leftShots).ToString();
                };
                __instance.killButton.CoolDownTimer = NebulaAPI.Modules.Timer(__instance, Sheriff.KillCoolDownOption.GetCooldown(__instance.MyPlayer.TeamKillCooldown)).SetAsKillCoolTimer();
                __instance.killButton.CoolDownTimer.Start();
                __instance.killButton.SetLabel(__instance.MyPlayer.IsMadmate ? "madmate.suicide" : "kill");
                __instance.killButton.OnMeeting = button =>
                {
                    if (lockSprite && NebulaGameManager.Instance!.AllPlayerInfo.Any(p => p.IsDead))
                    {
                        GameObject.Destroy(lockSprite!.gameObject);
                        lockSprite = null;
                    }
                };
                __instance.killButton.RelatedAbility = __instance;

                if ((Vanity.MyRole as DefinedRole).CanSpawnInCurrentGame)
                {
                    GamePlayer? lastMyExile = null;
                    GameOperatorManager.Instance?.Subscribe<PlayerVoteDisclosedLocalEvent>(ev =>
                    {
                        if (ev.VoteToWillBeExiled) lastMyExile = ev.VoteFor;
                    }, __instance);
                    GameOperatorManager.Instance?.SubscribeAchievement<GameEndEvent>("combination.2.sheriff.vanity.common2", ev => lastMyExile != null && NebulaGameManager.Instance?.LastDead == lastMyExile && !ev.EndState.Winners.Test(__instance.MyPlayer), __instance);
                }
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
        return false;
    }
    static public bool OpenRoleSelectWindowUsingTabs(IEnumerable<DefinedRole>? roles, (string? tab, Predicate<DefinedRole>? predicate)[] tabs, bool impRolesArrangeAtFirst, string underText, Action<DefinedRole> onSelected, ref MetaScreen __result)
    {
        var window = MetaScreen.GenerateWindow(new(7.6f, 4.2f), AmongUsLLImpl.HudManagerInstance.transform, new Vector3(0, 0, -50f), true, false);

        MetaWidgetOld widget = new();

        MetaWidgetOld inner = new();
        if (roles == null)
        {
            HashSet<DefinedRole> roleSet = [];
            foreach (var r in Nebula.Roles.Roles.AllRoles) foreach (var abilityRole in r.GetGuessableAbilityRoles()) roleSet.Add(abilityRole);
            foreach (var type in AssignmentType.AllTypes)
            {
                if (!type.CanGuessAsAbility) continue;
                foreach (var r in Nebula.Roles.Roles.AllRoles)
                {
                    if (type.Predicate.Invoke(r.AssignmentStatus, r) && r.GetCustomAllocationParameters(type)?.RoleCountSum > 0) roleSet.Add(r);
                }
            }
            roles = roleSet;
        }

        int CategoryToInt(RoleCategory roleCategory) => roleCategory switch
        {
            RoleCategory.ImpostorRole => impRolesArrangeAtFirst ? 0 : 1,
            RoleCategory.CrewmateRole => impRolesArrangeAtFirst ? 1 : 0,
            _ => 2
        };

        bool isFirst = true;
        foreach (var tab in tabs)
        {
            var ary = roles.Where(r => tab.predicate?.Invoke(r) ?? true).ToArray();
            ary.Sort((r1, r2) =>
            {
                if (r1.Category == r2.Category) return r1.InternalName.CompareTo(r2.InternalName);
                return CategoryToInt(r1.Category).CompareTo(CategoryToInt(r2.Category));
            });

            if (isFirst) isFirst = false;
            else inner.Append(new MetaWidgetOld.VerticalMargin(0.1f));

            if (tab.tab != null) inner.Append(new MetaWidgetOld.Text(MeetingRoleSelectWindow.TabAttribute) { MyText = new RawTextComponent(tab.tab), Alignment = IMetaWidgetOld.AlignmentOption.Center });
            inner.Append(ary, r => new CombinedWidgetOld(new MetaWidgetOld.HorizonalMargin(0.1f), new MetaWidgetOld.Button(() => onSelected.Invoke(r), MeetingRoleSelectWindow.ButtonAttribute)
            {
                RawText = r.DisplayColoredName,
                TextHorizonotalExtraMargin = 0.15f,
                PostBuilder = (button, renderer, text) =>
                {
                    renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    button.transform.localPosition += new UnityEngine.Vector3(0.05f, 0f, 0f);
                    text.transform.localPosition += new UnityEngine.Vector3(0.072f, 0f, 0f);

                    var icon = UnityHelper.CreateObject<SpriteRenderer>("Icon", button.transform, new(-0.65f, 0f, -0.1f));
                    icon.sprite = r.GetRoleIcon()?.GetSprite();
                    icon.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    icon.material = RoleIcon.GetRoleIconMaterial(r, 0.8f);
                    icon.transform.localScale = new(0.253f, 0.253f, 1f);
                    icon.SetBothOrder(15);
                }
            }), 4, -1, 0, 0.59f);
        }
        var originalInnerContents = new List<IMetaWidgetOld>(inner.contents);
        GUITextField? textField = null;
        MetaWidgetOld.ScrollView scroller = new(new(6.9f, 3f), inner, true) { Alignment = IMetaWidgetOld.AlignmentOption.Center };
        textField = new GUITextField(GUIAlignment.Center, new(5.8f, 0.38f)) { HintText = Language.Translate("help.search.hint").Color(UnityEngine.Color.gray), WithMaskMaterial = false, IsSharpField = false, MaxLines = 1, EnterAction = text => { OnSearch(); return true; }, GainFocus = true };
        bool HasButtonWithText(IMetaWidgetOld widget, string[] query)
        {
            if (widget is MetaWidgetOld.Button button)
                return query.All(q => button.Text.GetString().Contains(q));
            if (widget is CombinedWidgetOld combined)
            {
                foreach (var child in combined.contents)
                {
                    var childWidget = child as IMetaWidgetOld;
                    if (childWidget == null) return false;

                    if (HasButtonWithText(childWidget, query))
                        return true;
                }
            }
            return false;
        }

        void OnSearch()
        {
            var inputText = textField?.Artifact.Last().Text;
            var queries = inputText?.Split(' ', '　');
            if (string.IsNullOrWhiteSpace(inputText) || (queries?.All(string.IsNullOrEmpty) ?? false))
            {
                (scroller.Inner as MetaWidgetOld)?.contents = originalInnerContents.ToList();
                window.SetWidget(widget);
                return;
            }
            PDebug.Log("MeetingRoleSelectWindow Search: " + string.Join(" ", queries ?? []));
            var matchedRoles = new List<IMetaParallelPlacableOld>();
            foreach (var widget in originalInnerContents)
            {
                if (widget is CombinedWidgetOld outerCombined)
                {
                    foreach (var inner in outerCombined.contents)
                    {
                        if (inner is CombinedWidgetOld roleWidget && HasButtonWithText(roleWidget, queries))
                        {
                            matchedRoles.Add(roleWidget);
                        }
                    }
                }
            }
            PDebug.Log($"Searched Role num: {matchedRoles.Count}");
            var newInner = new MetaWidgetOld();
            int perLine = 4;
            int index = 0;
            while (index < matchedRoles.Count)
            {
                var lineRoles = matchedRoles.Skip(index).Take(perLine).ToArray();
                newInner.Append(new CombinedWidgetOld(0.59f, lineRoles)
                {
                    Alignment = IMetaWidgetOld.AlignmentOption.Center
                });
                index += perLine;
                /*if (index < matchedRoles.Count)
                {
                    newInner.Append(new MetaWidgetOld.VerticalMargin(0.1f));
                }*/
            }
            (scroller.Inner as MetaWidgetOld)?.contents = newInner.contents;
            window.SetWidget(widget);
        }
        widget.Append(new MetaWidgetOld.WrappedWidget(NebulaAPI.GUI.HorizontalHolder(GUIAlignment.Center, textField, NebulaAPI.GUI.LocalizedButton(GUIAlignment.Center, AttributeAsset.MarketplaceTabNonMaskedButton, "help.search.search", _ => OnSearch()))));
        widget.Append(new MetaWidgetOld.VerticalMargin(0.05f));
        widget.Append(scroller);

        widget.Append(new MetaWidgetOld.Text(TextAttributeOld.BoldAttr) { MyText = new RawTextComponent(underText), Alignment = IMetaWidgetOld.AlignmentOption.Center });

        window.SetWidget(widget);

        IEnumerator CoCloseOnResult()
        {
            if (MeetingHud.Instance.AsBoolFast())
            {
                while (MeetingHud.Instance.state != MeetingHud.MeetingStates.Results) yield return null;
            }
            else
            {
                while (!MeetingHud.Instance.AsBoolFast()) yield return null;
            }
            window.CloseScreen();
        }

        window.StartCoroutine(CoCloseOnResult().WrapToIl2Cpp());


        __result = window;
        return false;
    }

    public static Exception HandleRpcFinalizer(byte callId, Hazel.MessageReader reader, Exception __exception)
    {
        try
        {
            if (__exception != null)
            {
                if (callId == 128)
                {
                    PDebug.Log("PlayerControl Handle NebulaRpc Error callid:" + reader.PeekInt32().ToString() + " Error:" + __exception.ToString());
                    return null!;
                }
                PDebug.Log("PlayerControl Handle Rpc Error callid:" + callId.ToString() + " Error:" + __exception.ToString());
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
        return __exception;
    }
    public static void IsSpBanned(PlayerBanData __instance, ref bool __result)
    {
        if (File.Exists(Application.persistentDataPath + "/sp"))
        {
            __result = __instance.BanMinutesLeft > 0;
        }
    }
    public static void OnExitGame(AmongUsClient __instance, DisconnectReasons reason)
    {
        if (!AmongUsLLImpl.LobbyInstance.AsBoolFast() && __instance.NetworkMode == NetworkModes.OnlineGame && GeneralConfigurations.CurrentGameMode == Virial.Game.GameModes.Standard)
        {
            if (AmongUsLLImpl.AmongUsClientInstance.AmHost || AmongUsLLImpl.LocalPlayer.Data.IsDead || IntroCutscene.Instance.AsBoolFast() || reason == DisconnectReasons.Custom)
            {
                return;
            }
            PDebug.RpcLog("Player " + AmongUsLLImpl.LocalPlayer.name + " Exit Game,FriendCode:" + EOSManager.Instance.FriendCode, true);
            DataManager.Player.Ban.PreviousGameStartDate = AmongUsDateTime.UtcNow;
            DataManager.Player.Ban.BanPoints = 3;
            if (!File.Exists(Application.persistentDataPath + "/sp"))
            {
                File.Create(Application.persistentDataPath + "/sp");
            }
        }
    }
    public static void OnLobbyStart()
    {
        if (File.Exists(Application.persistentDataPath + "/sp"))
        {
            File.Delete(Application.persistentDataPath + "/sp");
        }
        if (AmongUsLLImpl.AmongUsClientInstance.AmHost)
        {
            NebulaManager.Instance.StartDelayAction(5f, () =>
            {
                float time = 5f;
                GameOperatorManager.Instance.Subscribe<UpdateEvent>(ev =>
                {
                    time -= Time.deltaTime;
                    if (time <= 0f)
                    {
                        time = 5f;
                        foreach (var p in PlayerControl.AllPlayerControls)
                        {
                            var list = BlackListReadWriter.ReadBlacklist();
                            if (list.Contains(p.FriendCode))
                            {
                                AmongUsLLImpl.AmongUsClientInstance.KickPlayer(p.Data.ClientId, true);
                            }
                        }
                    }
                }, NebulaAPI.CurrentGame);
            });
        }
        var now = DateTime.Now;
        if ((now.Month == 5 && now.Day >= 24) || (now.Month == 6 && now.Day <= 7))
        {
            NebulaManager.Instance.StartDelayAction(1.75f, () =>
            {
                var player = AmongUsLLImpl.LocalPlayer;
                var originName = player.name;
                player.SetName("System".Color(Sheriff.MyRole.UnityColor));
                AmongUsLLImpl.HudManagerBridge.Chat.AddChat(player, Language.Translate("plan17SpecialDay"));
                player.SetName(originName);
            });
        }
    }
    public static void RefreshFlowerPatch(ItemSupplier __instance)
    {
        if (RefreshFlower)
        {
            NebulaManager.Instance.StartDelayAction(RefreshFlowerTime, () =>
            {
                try
                {
                    while (__instance.age < 3)
                    {
                        __instance.TryGrow();
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
            });
        }
    }
    public static void ImpostorCannotGainCrewmateFlowerPatch(ItemSupplier __instance, ref bool __result)
    {
        if (ImpostorCannotGainCrewmateFlower && !__instance.NoncrewmateOnly && (GamePlayer.LocalPlayer?.IsImpostor ?? false))
        {
            __result = false;
        }
    }
    public static bool BetterFillServer(ServerDropdown __instance)
    {
        IEnumerable<IRegionInfo> enumerable = DestroyableSingleton<ServerManager>.Instance.AvailableRegions
            .Where((IRegionInfo r) => r.TranslateName == StringNames.NoTranslation)
            .Concat(CustomServerLoader.CurrentAvailableRegions()).DistinctBy(r => r.Name).ToArray<IRegionInfo>();
        IRegionInfo[] defaultRegions = ServerManager.DefaultRegions.ToArray<IRegionInfo>();
        IRegionInfo[] array = enumerable.OrderBy((IRegionInfo r) => defaultRegions.FindIndex((IRegionInfo d) => d.Name == r.Name)).ToArray<IRegionInfo>();
        int num = 0;
        List<ServerListButton> buttons = new List<ServerListButton>();
        foreach (IRegionInfo regionInfo in array)
        {
            if (DestroyableSingleton<ServerManager>.Instance.CurrentRegion.Name == regionInfo.Name)
            {
                __instance.defaultButtonSelected = __instance.firstOption;
                __instance.firstOption.ChangeButtonText(regionInfo.ModTranslatedName());
            }
            else
            {
                IRegionInfo region = regionInfo;
                ServerListButton serverListButton = __instance.ButtonPool.Get<ServerListButton>();
                serverListButton.transform.localPosition = new(4.2f * (float)(num / 7), __instance.y_posButton + -0.55f * (float)(num % 7), -1f);
                serverListButton.transform.localScale = UnityEngine.Vector3.one;
                serverListButton.Text.text = ServerDropdownPatch.TranslateText(regionInfo.Name);
                serverListButton.Text.ForceMeshUpdate(false, false);
                serverListButton.Button.OnClick.RemoveAllListeners();
                serverListButton.Button.OnClick.AddListener(delegate
                {
                    __instance.ChooseOption(region);
                });
                buttons.Add(serverListButton);
                __instance.controllerSelectable.Add(serverListButton.Button);
                num++;
            }
        }
        int columns = (num - 1) / 7 + 1;
        __instance.background.transform.localPosition = new(0f, __instance.initialYPos + -0.3f * (float)(Math.Min(num, 7) - 1), 0f);
        __instance.background.size = new((float)columns * 4.2f, 0.6f + 0.6f * (float)Math.Min(num, 7));
        if (columns > 1)
        {
            foreach (ServerListButton serverListButton2 in buttons)
            {
                serverListButton2.transform.localPosition -= new UnityEngine.Vector3((float)(columns - 1) * 4.2f * 0.5f, 0f, 0f);
            }
        }
        return false;
    }
    public static bool BlockNoSFillServer(ref bool __result)
    {
        __result = true;
        return false;
    }
    public static void BetterEnterCodeUI(EnterCodeManager __instance)
    {
        try
        {
            var mainmenu = MainMenuManagerInstance.MainMenu;
            if (mainmenu != null)
            {
                ServerDropdown? dropdown = null;
                if (!EnterCodeSelectServer)
                {
                    var obj = UnityEngine.Object.Instantiate(mainmenu.createGameScreen.serverButton.transform.parent.gameObject, __instance.enterCodeField.transform.parent);
                    obj.name = "EnterCodeSelectServer";
                    obj.transform.localPosition = new(2.8596f, 1.234f);
                    dropdown = UnityEngine.Object.Instantiate(mainmenu.createGameScreen.serverDropdown, obj.transform);
                    EnterCodeSelectServer = obj;
                }
                else
                {
                    dropdown = EnterCodeSelectServer.GetComponentInChildren<ServerDropdown>();
                }
                mainmenu.entercodeField.transform.parent.localPosition = new(-1.1106f, 0.12f);
                List<TextMeshPro> tmps = new();
                EnterCodeSelectServer.transform.Find("BlackSquare").transform.localPosition = new(0.554f, 0.658f, -1f);
                GameObject serverButton = EnterCodeSelectServer.transform.Find("ServerBox").gameObject;
                serverButton.transform.localPosition = new(-0.97f, 0f);
                tmps.Add(serverButton.transform.Find("Inactive/ClassicText").GetComponent<TextMeshPro>());
                tmps.Add(serverButton.transform.Find("Highlight/ClassicText").GetComponent<TextMeshPro>());
                void UpdateServer(string text)
                {
                    tmps.Do(t => t.text = text);
                }
                string SetCurrentServer()
                {
                    IRegionInfo currentRegion = DestroyableSingleton<ServerManager>.Instance.CurrentRegion;
                    return DestroyableSingleton<TranslationController>.Instance.GetStringWithDefault(currentRegion.TranslateName, currentRegion.Name, new Il2CppReferenceArray<Il2CppSystem.Object>(Array.Empty<Il2CppSystem.Object>()));
                }
                void OpenServerDropdown()
                {
                    dropdown?.gameObject.SetActive(true);
                    serverButton.SetActive(false);
                }
                void CloseServerDropdown()
                {
                    serverButton.SetActive(true);
                }
                UpdateServer(SetCurrentServer());
                dropdown.transform.localPosition = new(0.5348f, -1.2209f, -15f);
                dropdown.transform.localScale = UnityEngine.Vector2.one;
                dropdown.Initialize(new Action<string>(s => UpdateServer(s)), new Action(() => CloseServerDropdown()));
                var button = EnterCodeSelectServer.transform.Find("ServerBox").GetComponent<PassiveButton>();
                button.OnClick = new();
                button.OnClick.AddListener(() =>
                {
                    OpenServerDropdown();
                });
                dropdown.transform.GetChild(0).GetComponent<PassiveButton>().enabled = false;

            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    public static bool AutoKickNoSyncPlayer(Synchronizer __instance, Game container)
    {
        DebugScreen.Push(new FunctionalDebugTextContent(() => {
            if (__instance.LastSync == null) return null;
            if (!AmongUsLLImpl.LocalPlayer) return null;
            if (__instance.LastSync.Value.tag == SynchronizeTag.PreSpawnMinigame) return null;
            var elapsedTime = Time.time - __instance.LastSync.Value.time;
            if (elapsedTime < 3f) return null;
            if (!__instance.sync.TryGetValue(__instance.LastSync.Value.tag, out var val)) return null;

            var waitFor = PlayerControl.AllPlayerControls.GetFastEnumerator()
                .Where(p => (val & (1 << p.PlayerId)) == 0).ToArray() ?? [];
            if (waitFor.Length == 0 || waitFor.Any(p => p.AmOwner)) return null;
            if (elapsedTime >= 13f && AmongUsLLImpl.AmongUsClientInstance.AmHost)
            {
                foreach (var p in waitFor)
                {
                    if (p != null && !p.AmOwner && !p.isDummy)
                    {
                        AmongUsLLImpl.AmongUsClientInstance.KickPlayer(GetClient(p).Character.Data.ClientId, false);
                        UnityEngine.Object.Destroy(p);
                        var list = PlayerControl.AllPlayerControls.GetFastEnumerator().ToList();
                        if (list.Contains(p))
                        {
                            list.Remove(p);
                        }
                        PlayerControl.AllPlayerControls = list.ToIl2CppList();
                    }
                }
            }
            return Language.Translate("log.awaiting").Replace("%TAG%", __instance.LastSync.Value.tag.ToString()).Bold()
                + waitFor.Join(p => "\n -" + p.name + $"({Mathn.Max(0f, Mathn.Abs(elapsedTime - 13f))})").Bold();
        }, container));
        return false;
    }
    public static void FixChatNoRoleIcon(ChatController __instance, ref ChatBubble __result)
    {
        __instance.chatBubblePool.Prefab.CastFast<ChatBubble>().TextArea.UseRoleIcon();
    }

    //Prefix if language is not jp block loading news.
    public static bool BlockLoadNews()
    {
        return DataManager.Settings.Language.CurrentLanguage == SupportedLangs.Japanese;
    }

    public static void FixKillerTeamCallbackCrewmate(KillerTeamCallback __instance)
    {
        if (__instance.ExcludedTeam == NebulaTeams.CrewmateTeam && __instance.RemainingOtherTeam)
        {
            if (NebulaGameManager.Instance!.AllPlayerInfo.Where(p => !p.IsDead).All(p => !p.IsKiller || p.TryGetModifier<HasLove.Instance>(out var i)))
            {
                __instance.RemainingOtherTeam = false;
            }
        }
    }
    public static void HasGhostRolePlayerCannotSeeRolePatch(ref bool __result)
    {
        if (HasGhostRolePlayerCannotSeeAllInfo && __result)
        {
            var player = GamePlayer.LocalPlayer;
            __result = (player?.GhostRole ?? null) == null;
        }
    }
    public static bool OnShapeshiftPanelSelected(ShapeshifterMinigame __instance, PlayerControl target)
    {
        if ((GamePlayer.LocalPlayer?.Role is not Assistant.Instance) || GamePlayer.LocalPlayer.Role.GetAbility<Almighty.Ability>() != null)
        {
            return true;
        }
        if (GamePlayer.LocalPlayer.Logic.InVent)
        {
            __instance.Close();
            return false;
        }
        if (target != null)
        {
            VanillaRpc.ShapeshiftRpc.Invoke((GamePlayer.LocalPlayer, target.ToNebulaPlayer(), true, GamePlayer.LocalPlayer.Role is Assistant.Instance ? OutlawTeam.AssistantShapeshiftLeaveEvidence : Almighty.ShapeshiftLeaveEvidence));
            GameOperatorManager.Instance?.AllOperators.Where(p => p is ModAbilityButtonImpl).Select(p => p as ModAbilityButtonImpl).Do(impl =>
            {
                if (impl?.VanillaButton.buttonLabelText.text == Language.Translate("button.label.assistant.shapeshift"))
                {
                    impl.ActivateEffect();
                }
            });
        }
        else
        {
            Logger.GlobalInstance.Warning(string.Format("Player attempted to shapeshift into a null player {0}. Maybe due to disconnect?", (target != null) ? new byte?(target.PlayerId) : null), null);
        }
        __instance.Close();
        return false;
    }

    public static bool LoverExtraWinFix(Lover.Instance __instance, AbstractPlayerEvent ev)
    {
        if (ev is not PlayerCheckExtraWinEvent pev)
        {
            return true;
        }
        if (__instance.MyPlayer.IsDead && __instance.MyPlayer.MyKiller == __instance.MyLover)
        {
            if (!Lover.AllowExtraWinOption && pev.GameEnd != NebulaGameEnd.AvengerWin)
            {
                return false;
            }
            if (!Lover.AllowExtraWinViaCrewWinOption && pev.GameEnd == NebulaGameEnd.CrewmateWin)
            {
                return false;
            }
            Player myLover = __instance.MyLover.Get();
            if (myLover == null)
            {
                return false;
            }
            if (!pev.WinnersMask.Test(myLover))
            {
                return false;
            }
            if (pev.WinnersMask.Test(__instance.MyPlayer))
            {
                return false;
            }
            pev.ExtraWinMask.Add(NebulaGameEnd.ExtraLoversWin);
            pev.IsExtraWin = true;
            return false;
        }
        return true;
    }
    public static bool FixSetKillTimer(PlayerControl __instance, float time)
    {
        try
        {
            if (__instance.Data.Role.CanUseKillButton)
            {
                float max = AmongUsLLImpl.CurrentGameOptionsInstance.GetFloat(FloatOptionNames.KillCooldown);
                __instance.killTimer = Mathn.Clamp(time, 0f, max);
                DestroyableSingleton<HudManager>.Instance.KillButton.SetCoolDown(__instance.killTimer, max);
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
            return true;
        }
        return false;
    }
    public static void FixMeetingKillSound(ref AudioClip __result)
    {
        PDebug.Log("FixMeetingKillSound");
        if (MeetingHud.Instance.AsBoolFast())
        {
            if (ClientOption.GetValue((ClientOption.ClientOptionType)125) == 1)
            {
                __result = PatchManager.GetSound("Gunfire");
                PDebug.Log("FixMKSDone");
            }
        }
    }
    public static void JoinGameLoadingPatch(AmongUsClient __instance, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        if (ClientOption.GetValue((ClientOption.ClientOptionType)128) == 0)
        {
            return;
        }
        var foverlay = DestroyableSingleton<TransitionFade>.Instance.overlay;
        var overlay = UnityEngine.Object.Instantiate<SpriteRenderer>(foverlay, null);
        overlay.transform.position = foverlay.transform.position;
        IEnumerator CoFadeInIf()
        {
            if (AmongUsLLImpl.AmongUsClientInstance.ClientId < 0)
            {
                yield return Effects.ColorFade(overlay, UnityEngine.Color.black, UnityEngine.Color.clear, 0.2f);
                UnityEngine.Object.Destroy(overlay);
            }
        }
        Il2CppSystem.Collections.IEnumerator[] array = new Il2CppSystem.Collections.IEnumerator[4];
        array[0] = Effects.ColorFade(overlay, UnityEngine.Color.clear, UnityEngine.Color.black, 0.2f);
        array[1] = ManagedEffects.Action(delegate
        {
            NebulaManager.Instance.StartCoroutine(HintManager.CoShowHint(0.6f).WrapToIl2Cpp());
        }).WrapToIl2Cpp();
        array[2] = __result;
        array[3] = CoFadeInIf().WrapToIl2Cpp();
        __result = Effects.Sequence(array);
    }
    public static void CreateGameOptionsLoadingPatch()
    {
        if (ClientOption.GetValue((ClientOption.ClientOptionType)128) == 0)
        {
            return;
        }
        NebulaManager.Instance.StartCoroutine(HintManager.CoShowHint(0.8f).WrapToIl2Cpp());
    }
    public static void ResultPrefix()
    {
        votedPlayerId = new List<byte>();
    }
    public static bool ModBloopAVoteIconPatch(NetworkedPlayerInfo? voterPlayer, int index, Transform parent, bool isExtra)
    {
        if (!MayorExtraVoteAnonymousVote)
        {
            return true;
        }
        var __instance = MeetingHud.Instance;
        SpriteRenderer spriteRenderer = GameObject.Instantiate<SpriteRenderer>(__instance.PlayerVotePrefab);
        if ((GameManager.Instance.LogicOptions.GetAnonymousVotes() && !(NebulaGameManager.Instance?.CanSeeAllInfo ?? false)) || voterPlayer == null)
            PlayerMaterial.SetColors(Palette.DisabledGrey, spriteRenderer);
        else
        {
            if (voterPlayer.Object.ToNebulaPlayer().Role.GetAbility<Mayor.Ability>() != null && votedPlayerId.Contains(voterPlayer.PlayerId))
            {
                var list = GameData.Instance.AllPlayers.GetFastEnumerator().Where(p => !p.IsDead).ToList();
                PlayerMaterial.SetColors(list[UnityEngine.Random.Range(0, list.Count)].DefaultOutfit.ColorId, spriteRenderer);
            }
            else
            {
                PlayerMaterial.SetColors(voterPlayer.DefaultOutfit.ColorId, spriteRenderer);
            }
        }
        if (voterPlayer != null)
        {
            votedPlayerId.Add(voterPlayer.PlayerId);
        }
        spriteRenderer.transform.SetParent(parent);
        spriteRenderer.transform.localScale = UnityEngine.Vector3.zero;
        __instance.StartCoroutine(Effects.Bloop((float)index * 0.3f + (isExtra ? 0.85f : 0f), spriteRenderer.transform, 1f, isExtra ? 0.5f : 0.7f));

        if (isExtra)
            __instance.StartCoroutine(Effects.Sequence(Effects.Wait((float)index * 0.3f + 0.85f), ManagedEffects.Action(() => parent.GetComponent<VoteSpreader>().AddVote(spriteRenderer)).WrapToIl2Cpp()));
        else
            parent.GetComponent<VoteSpreader>().AddVote(spriteRenderer);
        return false;
    }
    public static void AnonymousVotesFix(ref bool __result)
    {
        if (__result && NebulaGameManager.Instance != null && GamePlayer.LocalPlayer != null)
        {
            if (GamePlayer.LocalPlayer.Role.GetAbility<CrawlerEngineer.Ability>() != null && CrawlerEngineer.CrawlerHasWatching)
            {
                __result = false;
            }
        }
    }
    public static void AbilityTimerFix(TimerImpl __instance)
    {
        __instance.SetPredicate(() => AmongUsLLImpl.LocalPlayer.IsKillTimerEnabled);
    }
    public static void ClientRecordInvokeSabotageRpc(SystemTypes systemType, PlayerControl player, Hazel.MessageReader msgReader)//Prefix
    {
        RpcSabotageLogForAdmin.Invoke(((byte)systemType, msgReader.PeekByte(), player.PlayerId));
    }
    public static void HostRecordInvokeSabotageRpc(SystemTypes systemType, PlayerControl player, byte amount)//Postfix
    {
        RpcSabotageLogForAdmin.Invoke(((byte)systemType, amount, player.PlayerId));
    }
    static RemoteProcess<(byte, byte, byte)> RpcSabotageLogForAdmin = new("RpcLogForAdmin", (message, _) =>
    {
        if (AmongUsLLImpl.AmongUsClientInstance.AmHost)
        {
            if ((SystemTypes)message.Item1 == SystemTypes.Sabotage)
            {
                var system = (SystemTypes)message.Item2;
                var player = PlayerControl.AllPlayerControls.GetFastEnumerator().FirstOrDefault(p => p.PlayerId == message.Item3);
                if (player == null)
                {
                    PDebug.Log("Not Found Invoke player");
                    return;
                }
                PDebug.Log("Invoke Sabotage:" + system + " Invoker:" + player.name);
            }
        }
    });
    public static void ModernBGPatch(ref BackgroundSetting background)
    {
        if (background == BackgroundSetting.Off) return;
        if (ClientOption.GetValue((ClientOption.ClientOptionType)127) == 1)
        {
            background = BackgroundSetting.Modern;
        }
    }
    public static bool ModKillPatch(GamePlayer __instance, IPlayerlike player, CommunicableTextTag playerState, CommunicableTextTag eventDetail, ref KillParameter killParams, KillCondition killCondition, Action<KillResult> callBack)
    {
        var syncs = NebulaSyncObject.GetObjects<Nightmare.NightmareSeed>(Nightmare.disposableNightSeedOption ? Nightmare.NightmareSeed.MyTempTag : Nightmare.NightmareSeed.MyTag);
        if (syncs.Any(obj =>
        {
            if (player.Position.Distance(obj.Position) < Nightmare.darknessSizeOption * 1.54f)
            {
                if (obj.ActualOwner.TryGetAbility<Nightmare.Ability>(out var ability))
                {
                    return ability.EffectIsActive;
                }
            }
            return false;
        }))
        {
            killParams = killParams & ~KillParameter.WithOverlay;
        }
        return true;
        /*if (playerState == PlayerState.Sniped) 
        {
            NebulaGameManager.Instance?.KillRequestHandler.RequestKill(__instance, player, playerState, eventDetail, killParams & ~KillParameter.WithOverlay, killCondition, (result) =>
            {
                callBack(result);
                /*if (result==KillResult.Kill)
                {
                    SniperKillShowAnimRpc.Invoke(player.RealPlayer.PlayerId);
                }*
            });
            return false;
        }
        return true;*/
    }
    /*static RemoteProcess<byte> SniperKillShowAnimRpc = new RemoteProcess<byte>("SniperShowAnimRpc", (message,_)=>
    {
        var player = GamePlayer.GetPlayer(message);
        if (player!=null&&player.AmOwner)
        {
            AmongUsLLImpl.HudManagerBridge.KillOverlay.ShowKillAnimation(player.ToAUPlayer().Data, player.ToAUPlayer().Data);
        }
    });*/
    public static void TranslatePatch(string translationKey, ref string __result)
    {
        if (Doomsayer.CanUseOracle)
        {
            switch (translationKey)
            {
                case "role.doomsayer.name":
                    __result = Language.Translate("role.doomsayer2.name");
                    break;
                case "teams.doomsayer":
                    __result = Language.Translate("teams.doomsayer2");
                    break;
                case "end.doomsayer":
                    __result = Language.Translate("end.doomsayertext2");
                    break;
            }
        }
        /*if (translationKey.StartsWith("options.role.") && translationKey.EndsWith(".detail"))
        {
            __result = Language.Translate(System.Text.RegularExpressions.Regex.Replace(
        translationKey,
        @"options\.role\.(.*?)\.detail",
        "role.$1.doc",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase
    ));
        }*/
    }
    public static bool JackalPromotePatch(Jackal.Instance __instance, PlayerDieOrDisconnectEvent ev)
    {
        if (JackalUseOldPromoteSidekick)
        {
            if (ev.Player.IsDisconnected)
            {
                if (!ev.Player.VanillaPlayer.AsBoolFast())
                {
                    if (ev.Player.PlayerState != PlayerStates.Alive && ev.Player.PlayerState != PlayerStates.Revived)
                    {
                        return false;
                    }
                }
                if (ev.Player.VanillaPlayer.Data.IsDead)
                {
                    return false;
                }
            }
            __instance.PromoteSidekick(ev is PlayerExiledEvent);
            return false;
        }
        return true;
    }
    public static void GetNeutralRolePatch(DefinedRole role, ref bool __result)
    {
        if (role is GameMaster)
        {
            __result = false;
        }
    }
    public static void SnatchPatch(RuntimeRole __instance, GamePlayer target, bool isMatched)
    {
        if (SnatcherCannotGuessSnatchedPlayer)
        {
            if (isMatched)
            {
                GameOperatorManager.Instance!.Subscribe<PlayerCanGuessPlayerLocalEvent>(ev =>
                {
                    if (ev.Guesser.AmOwner)
                    {
                        if (ev.Target.PlayerId == target.PlayerId)
                        {
                            ev.CanGuess = false;
                        }
                    }
                }, __instance, 101);
            }
        }
    }
    static System.Collections.IEnumerator CoDestroyKill(PlayerControl myPlayer, PlayerControl target, Vector3 targetPos, bool moveToLeft)
    {
        myPlayer.moveable = false;
        target.moveable = false;
        if (InVisibleDestroyCanMove && myPlayer.ToNebulaPlayer().IsInvisible)
        {
            target.moveable = true;
        }
        //キルされる相手は今の操作を中断させられる。
        if (target.AmOwner && Minigame.Instance)
        {
            try
            {
                Minigame.Instance.Close();
                Minigame.Instance.Close();
            }
            catch
            {
            }
        }

        //自身が動いている間は相手側もうまいこと動かそうとさせる。
        bool myDone = false;
        var myAnim = Effects.Sequence(myPlayer.MyPhysics.WalkPlayerTo(Destroyer.Ability.GetDestroyKillPosition(targetPos.AsVector2(), moveToLeft), 0.001f, 1f, false), ManagedEffects.Action(() => myDone = true).WrapToIl2Cpp());
        var targetAnim = target.MyPhysics.WalkPlayerTo(targetPos.ToUnityVector(), 0.001f, 1f, false);

        var targetCoroutine = NebulaManager.Instance.StartCoroutine(targetAnim);
        var myCoroutine = NebulaManager.Instance.StartCoroutine(myAnim);

        float leftCount = 1.5f;
        while (leftCount > 0f && !myDone)
        {
            leftCount -= Time.deltaTime;
            yield return null;
        }

        if (!myDone)
        {
            NebulaManager.Instance.StopCoroutine(myCoroutine);
        }
        try
        {
            NebulaManager.Instance.StopCoroutine(targetCoroutine);
        }
        catch { }

        target.NetTransform.SnapTo(targetPos.AsVector2().ToUnityVector() - target.Collider.offset);
        myPlayer.MyPhysics.body.velocity = UnityEngine.Vector2.zero;
        target.MyPhysics.body.velocity = UnityEngine.Vector2.zero;

        myPlayer.MyPhysics.FlipX = !moveToLeft;
        target.MyPhysics.FlipX = !moveToLeft;

        //キルモーション

        System.Collections.IEnumerator CoMonitorMeeting()
        {
            while (true)
            {
                if (MeetingHud.Instance.AsBoolFast())
                {
                    if (!target.Data.IsDead && myPlayer.AmOwner)
                    {
                        myPlayer.GetModInfo()?.MurderPlayer(target.GetModInfo()!, PlayerState.Crushed, null, KillParameter.WithAssigningGhostRole);
                    }
                    yield break;
                }
                yield return null;
            }
        }

        SizeModulator sizeModulator = new(UnityEngine.Vector2.one, 10000f, false, 100, "nebula::destroyer", false, false);
        PlayerModInfo.RpcAttrModulator.LocalInvoke((target.PlayerId, sizeModulator, true));

        Coroutine monitorMeetingCoroutine = NebulaManager.Instance.StartCoroutine(CoMonitorMeeting().WrapToIl2Cpp());

        if (myPlayer.AmOwner)
        {
            NebulaGameManager.Instance?.GameStatistics.RpcRecordEvent(GameStatistics.EventVariation.Kill, EventDetail.DestroyKill, myPlayer, target);
        }

        var handRenderer = UnityHelper.CreateObject<SpriteRenderer>("KillerHand", null, UnityEngine.Vector3.zero, LayerExpansion.GetPlayersLayer());
        handRenderer.gameObject.AddComponent<PlayerColorRenderer>().SetPlayer(myPlayer.GetModInfo());
        handRenderer.sprite = DestroyerAssets.HandSprite[1].GetSprite();
        handRenderer.flipX = !moveToLeft;
        handRenderer.transform.localScale = new(0.5f, 0.5f, 1f);
        handRenderer.transform.localPosition = targetPos + new Vector3(moveToLeft ? -0.2f : 0.2f, 0.9f, -1f);
        System.Collections.IEnumerator FreshHandAlpha()
        {
            while (true)
            {
                var alpha = myPlayer.cosmetics.currentBodySprite.BodySprite.color.a;
                var color = handRenderer.color;
                color.a = alpha;
                handRenderer.color = color;
                yield return null;
            }
        }
        Coroutine FreshHandCoroutine = NebulaManager.Instance.StartCoroutine(FreshHandAlpha());
        yield return new WaitForSeconds(0.15f);

        //死体を生成
        DeadBody deadBody = GameObject.Instantiate<DeadBody>(GameManager.Instance.deadBodyPrefab[0]);
        GameObject deadBodyObj = deadBody.gameObject;
        deadBody.enabled = Destroyer.CanReportKillSceneOption;
        deadBody.Reported = !Destroyer.CanReportKillSceneOption;
        deadBody.ParentId = target.PlayerId;
        deadBody.transform.localPosition = target.GetTruePosition();
        foreach (var r in deadBody.bodyRenderers) r.enabled = false;
        var splatter = deadBody.bloodSplatter;
        target.SetPlayerMaterialColors(deadBody.bloodSplatter);
        splatter.gameObject.SetActive(false);

        var modSplatterRenderer = UnityHelper.CreateObject<SpriteRenderer>("ModSplatter", null, target.transform.position, splatter.gameObject.layer);
        modSplatterRenderer.sharedMaterial = splatter.sharedMaterial;
        var modSplatter = modSplatterRenderer.gameObject.AddComponent<ModAnimator>();

        var targetModInfo = target.GetModInfo()!;
        target.Visible = true;
        target.inVent = false;
        targetModInfo.Unbox().WillDie = true;
        System.Collections.IEnumerator CoScale(float startScale, float goalScale, float duration, NebulaAudioClip audioClip, bool playKillSE = false)
        {
            var alpha = myPlayer.cosmetics.currentBodySprite.BodySprite.color.a;
            handRenderer.sprite = DestroyerAssets.HandSprite[2].GetSprite();
            yield return new WaitForSeconds(0.15f);

            float scale = startScale;
            float p = 0f;
            handRenderer.sprite = DestroyerAssets.HandSprite[3].GetSprite();

            float randomX = 0f;
            float randomTimer = 0f;

            if (!MeetingHud.Instance.AsBoolFast()) NebulaAsset.PlaySE(audioClip, target.transform.position, 0.8f, Destroyer.KillSEStrengthOption, 1f);

            int sePhase = 0;
            float[] seTime = [0.45f, 0.62f, 0.98f];

            while (p < 1f)
            {
                scale = startScale + (goalScale - startScale) * p;
                handRenderer.transform.localPosition = targetPos + new Vector3(randomX + (moveToLeft ? -0.15f : 0.15f), scale * 0.55f + 0.4f, -1f);
                if (!targetModInfo.IsDead) sizeModulator.Size.y = scale;

                randomTimer -= Time.deltaTime;
                if (randomTimer < 0f)
                {
                    randomX = ((float)System.Random.Shared.NextDouble() - 0.5f) * 0.07f;
                    randomTimer = 0.05f;
                }

                if (playKillSE && !MeetingHud.Instance.AsBoolFast())
                {
                    if (sePhase < seTime.Length && p > seTime[sePhase])
                    {
                        if (sePhase < 2)
                        {
                            //血を出す
                            splatter.gameObject.SetActive(false);
                            deadBody.transform.localScale = new(sePhase == 0 ? 0.7f : -0.7f, 0.7f, 0.7f);
                            splatter.gameObject.SetActive(true);
                        }
                        else if (sePhase == 2)
                        {
                            modSplatter.PlayOneShot(Destroyer.Ability.spriteModBlood, 12f, true);
                        }
                        NebulaAsset.PlaySE(target.KillSfx, target.transform.position + new UnityEngine.Vector3(((float)System.Random.Shared.NextDouble() - 0.5f) * 0.05f, ((float)System.Random.Shared.NextDouble() - 0.5f) * 0.05f, 0f), 0.6f, 1.4f, 1f);
                        sePhase++;
                    }
                }

                p += Time.deltaTime / duration;

                yield return null;
            }

            sizeModulator.Size.y = goalScale;
            handRenderer.transform.localPosition = targetPos + new Vector3(moveToLeft ? -0.12f : 0.12f, startScale * 0.55f + 0.21f, -1f);
            handRenderer.sprite = DestroyerAssets.HandSprite[2].GetSprite();

        }

        int phases = Destroyer.PhasesOfDestroyingOption - 1;

        for (int i = 0; i < phases; i++)
        {
            yield return CoScale(
                1f - 0.4f / phases * i,
                1f - 0.4f / phases * (i + 1),
                1.2f, (i % 2 == 0) ? NebulaAudioClip.Destroyer1 : NebulaAudioClip.Destroyer2);
            yield return new WaitForSeconds(0.7f);
        }
        yield return CoScale(phases == 0 ? 1f : 0.6f, 0f, 3.2f, NebulaAudioClip.Destroyer3, true);

        if (myPlayer.AmOwner && !target.Data.IsDead)
        {
            myPlayer.GetModInfo()?.MurderPlayer(target.GetModInfo()!, PlayerState.Crushed, null, KillParameter.WithOverlay | KillParameter.WithAssigningGhostRole, KillCondition.TargetAlive);
        }
        try
        {
            NebulaManager.Instance.StopCoroutine(FreshHandCoroutine);
        }
        catch
        {

        }
        try
        {
            NebulaManager.Instance.StopCoroutine(monitorMeetingCoroutine);
        }
        catch
        {
            //会議に入って停止に失敗しても何もしない
        }

        if (Destroyer.LeaveKillEvidenceOption)
        {
            var bloodRenderer = UnityHelper.CreateObject<SpriteRenderer>("DestroyerBlood", null, (targetPos + new Vector3(0f, 0.1f, 0f)).AsVector2().AsWorldPos(true));
            bloodRenderer.sprite = Destroyer.spriteBloodPuddle.GetSprite();
            bloodRenderer.color = DynamicPalette.PlayerColors[target.CurrentOutfit.ColorId].ToUnityColor();
            bloodRenderer.transform.localScale = new(0.45f, 0.45f, 1f);
        }

        GameObject.Destroy(handRenderer.gameObject);
        if (deadBodyObj) GameObject.Destroy(deadBodyObj);

        myPlayer.moveable = true;

        //こちらの目線で死ぬまで待つ
        while (!targetModInfo.IsDead) yield return null;

        target.moveable = true;
        target.inVent = false;
        target.onLadder = false;
        target.inMovingPlat = false;

        yield return new WaitForSeconds(0.2f);

        PlayerModInfo.RpcRemoveAttrByTag.LocalInvoke((targetModInfo.PlayerId, "nebula::destroyer"));

        //血しぶきを片付ける
        yield return new WaitForSeconds(0.5f);
        GameObject.Destroy(modSplatter.gameObject);
    }
    static internal readonly RemoteProcess<(GamePlayer player, GamePlayer target, Vector2 targetPosition, bool moveToLeft)> RpcCoDestroyKillFix = new(
    "DestroyerKillFix",
    (message, _) =>
    {
        NebulaManager.Instance.StartCoroutine(CoDestroyKill(message.player.VanillaPlayer, message.target.VanillaPlayer, message.targetPosition.AsVector3(), message.moveToLeft).WrapToIl2Cpp());
    }
    );
    public static void RpcDestroyerPatch(Destroyer.Ability __instance, GamePlayer player, bool isUsurped)
    {
        if (player.AmOwner)
        {
            NebulaManager.Instance.StartDelayAction(0.1f, delegate
            {
                GameOperatorManager.Instance?.AllOperators.Do(p =>
                {
                    if (p is ModAbilityButtonImpl button)
                    {
                        if (button.VanillaButton.buttonLabelText.text == Language.Translate("button.label.destroyerKill"))
                        {
                            button.Availability = (button) => false;
                            button.Visibility = (button) => false;
                        }
                    }
                });
                AchievementToken<int> achChallengeToken = new("destroyer.challenge", 0, (val, _) => val >= 3 && (NebulaGameManager.Instance?.EndState?.Winners.Test(__instance.MyPlayer) ?? false));

                var destroyButton = NebulaAPI.Modules.PlayerlikeKillButton(__instance, __instance.MyPlayer, new Virial.Events.Player.PlayerInteractParameter(RealPlayerOnly: true, IsKillInteraction: true), true, Virial.Compat.VirtualKeyInput.Kill, "destroyer.kill",
    Destroyer.KillCoolDownOption.GetCooldown(__instance.MyPlayer.TeamKillCooldown), "destroyerKill", Virial.Components.ModAbilityButton.LabelType.Impostor,
    null, (player, _) =>
    {
        //左右どちらでキルすればよいか考える
        var targetTruePos = player.RealPlayer.TruePosition;
        var targetPos = player.Position;
        var canMoveToLeft = Destroyer.Ability.CheckCanMove(__instance.MyPlayer.VanillaPlayer, __instance.MyPlayer.Position, Destroyer.Ability.GetDestroyKillPosition(targetPos, true), out var leftDis);
        var canMoveToRight = Destroyer.Ability.CheckCanMove(__instance.MyPlayer.VanillaPlayer, __instance.MyPlayer.Position, Destroyer.Ability.GetDestroyKillPosition(targetPos, false), out var rightDis);
        bool moveToLeft = false;
        if (canMoveToLeft && canMoveToRight && leftDis < rightDis) moveToLeft = true;
        else if (!canMoveToRight) moveToLeft = true;

        __instance.SetPrivateField("lastKilling", player.RealPlayer);

        RpcCoDestroyKillFix.Invoke((__instance.MyPlayer, player!.RealPlayer, targetTruePos, moveToLeft));

        new StaticAchievementToken("destroyer.common1");
        new StaticAchievementToken("destroyer.common2");
        achChallengeToken.Value++;
        NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
    }, filterHeavier: p => Destroyer.Ability.CheckDestroyKill(__instance.MyPlayer.VanillaPlayer, __instance.MyPlayer.Position, p.RealPlayer.Position)).SetLabelType(ModAbilityButton.LabelType.Impostor)
     .SetAsUsurpableButton(__instance);
                destroyButton.OnBroken = _ => Snatcher.RewindKillCooldown();
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(destroyButton.GetKillButtonLike());
                __instance.SetPrivateField("destroyButton", destroyButton);
            });
        }
    }
    public static bool ShowMyRolesScreen(MetaScreen outsideScreen, ref Virial.Media.Image? backImage, ref IMetaWidgetOld __result)
    {
        if (ClientOption.GetValue((ClientOption.ClientOptionType)129) == 1)
        {
            return true;
        }
        MetaWidgetOld widget = new();
        Virial.Compat.Artifact<GUIScreen> inner = null!;
        var assigned = GamePlayer.LocalPlayer!.AllAssigned().Where(a => a.CanBeAwareAssignment).ToList();
        widget.Append(assigned.Select(a => a.AssignableOnHelp).Smooth(),
            (role) => new MetaWidgetOld.Button(() =>
            {
                var doc = DocumentManager.GetDocument("role." + role.InternalName);
                if (doc == null) return;

                inner.Do(screen =>
                {
                    screen.SetWidget(doc.Build(inner), out _);
                    outsideScreen.ClearBackImage();
                    outsideScreen.SetBackImage(role.ConfigurationHolder?.Illustration, 0.2f);
                });
            }, HelpScreen.RoleTitleAttrUnmasked)
            {
                RawText = role.DisplayColoredName,
                Alignment = IMetaWidgetOld.AlignmentOption.Center
            }, 5, 4, 0, 0.6f);
        var assignable = GamePlayer.LocalPlayer!.Role.AssignableOnHelp.First();
        var num = Mathn.Clamp((assigned.Count / 5) + (assigned.Count % 5 == 0 ? 0 : 1), 1, 4);
        var scrollView = new GUIScrollView(GUIAlignment.Left, new(7.4f, HelpScreen.HelpHeight - (0.675f * num)), () =>
        {
            var doc = DocumentManager.GetDocument("role." + assignable.InternalName);
            return doc?.Build(inner) ?? GUIEmptyWidget.Default;
        });
        inner = scrollView.Artifact;

        widget.Append(new MetaWidgetOld.WrappedWidget(scrollView));

        backImage = assignable.ConfigurationHolder?.Illustration;

        __result = widget;
        return false;
    }

    public static void MadmateHasAnyTasks(GamePlayer __instance, ref bool __result)
    {
        if (!MadmatePLUS.CanSeeAllImpostorOption && MadmatePLUS.CanIdentifyImpostorsOption > 0 && __instance.TryGetModifier<MadmatePLUS.Instance>(out var m))
        {
            __result = true;
        }
    }
    public static void MadmateFeelLikeHaveCrewmateTasks(GamePlayer __instance, ref bool __result)
    {
        if (!MadmatePLUS.CanSeeAllImpostorOption && __instance.TryGetModifier<MadmatePLUS.Instance>(out var m))
        {
            __result = false;
        }
    }
    public static void SetFlashLight(bool useFlashLight)
    {
        FlashlightEnabled = useFlashLight;
        AmongUsLLImpl.LocalPlayer.AdjustLighting();
    }
    public static bool AdjustLightingPatch(PlayerControl __instance)
    {
        if (AmongUsLLImpl.LocalPlayer != __instance) return false;
        if (GamePlayer.LocalPlayer != null && !(GamePlayer.LocalPlayer.Role.Role is Plana.Roles.Complex.Splicer))
        {
            if (!UseFlashLightMode)
            {
                return true;
            }
        }
        float num = 0f;
        bool flashFlag = false;
        if (FlashlightEnabled) flashFlag = FlashlightEnabled;
        else if (__instance.IsFlashlightEnabled()) flashFlag = true;
        else if (__instance.lightSource.useFlashlight) flashFlag = true;

        if (__instance.IsFlashlightEnabled())
        {
            if (__instance.Data.Role.IsImpostor)
                AmongUsLLImpl.CurrentGameOptionsInstance.TryGetFloat(FloatOptionNames.ImpostorFlashlightSize, out num);
            else
                AmongUsLLImpl.CurrentGameOptionsInstance.TryGetFloat(FloatOptionNames.CrewmateFlashlightSize, out num);
        }
        else if (__instance.lightSource.useFlashlight || FlashlightEnabled)
        {
            num = __instance.lightSource.flashlightSize;
        }
        if (UseFlashLightMode)
        {
            if (__instance.Data.Role.IsImpostor)
            {
                num = ImpostorFlashSize;
            }
            else
            {
                num = CrewmateFlashSize;
            }
        }
        __instance.SetFlashlightInputMethod();
        __instance.lightSource.SetupLightingForGameplay(flashFlag, num, __instance.TargetFlashlight.transform);
        return false;
    }
    public static bool GameMasterBlockFreePlayAbility()
    {
        if (HostIsGameMaster)
        {
            return false;
        }
        return true;
    }
    public static void DetermineGameMasterPre(IRoleTable __instance)
    {
        if (HostIsGameMaster)
        {
            try
            {
                __instance.SetRole(AmongUsLLImpl.LocalPlayer.PlayerId, GameMaster.MyRole);
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
    }
    public static void AssignGameMasterPre(List<byte> impostors, List<byte> others)
    {
        try
        {
            if (HostIsGameMaster)
            {
                var player = AmongUsLLImpl.LocalPlayer;
                if (player == null)
                {
                    PDebug.Log("Host is null");
                    return;
                }
                if (AmongUsLLImpl.AmongUsClientInstance.AmHost)
                {
                    if (impostors.Contains(player.PlayerId))
                    {
                        impostors.Remove(player.PlayerId);
                        var p2 = others.Random();
                        impostors.Add(p2);
                        others.Remove(p2);
                    }
                    else if (others.Contains(player.PlayerId))
                    {
                        others.Remove(player.PlayerId);
                    }
                }
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    public static float currentKillTime;
    public static string firstDeadName;
    public static void NebulaGameStart(Game __instance)
    {
        try
        {
            PDebug.Log("Nebula Game Started");
            ChatLogSystem.ResetForNewGame();
            /*CurrentChatId = 0;
            currentIndex = 0;
            CanUseChatList = new List<int>();*/
            GuesserSystem.playerGuess = new Dictionary<byte, int>();
            usebubblenum = 0;
            currentKillTime = 0f;
            DiagnosisModePatchs.yesId = 1;
            DiagnosisModePatchs.noId = 2;
            DiagnosisModePatchs.ExileMessageType = 0;
            DiagnosisModePatchs.isSpecialMeeting = false;
            DiagnosisModePatchs.usedSpecialMeeting = false;
            DiagnosisModePatchs.startmeeting = false;
            GameOperatorManager.Instance!.Subscribe<PlayerRoleSetEvent>(ev =>
            {
                if (ev.Player.AmOwner)
                {
                    if (GuesserAutoAssign && GuesserAutoAssignFilter.Contains(ev.Role.Role))
                    {
                        if (!ev.Player.Modifiers.Any(r => r is GuesserModifier.Instance) && !ev.Player.TryGetAbility<Guesser.Ability>(out var g) && !ev.Player.TryGetAbility<SpeechEater.Ability>(out var s))
                        {
                            ev.Player.AddModifier(GuesserModifier.MyRole);
                        }
                    }
                }
            }, __instance);
            GameOperatorManager.Instance!.Subscribe<GameUpdateEvent>(ev =>
            {
                if (DiagnosisModePatchs.IsEnabled)
                {
                    if (!MeetingHud.Instance && !ExileController.Instance)
                    {
                        currentKillTime += Time.deltaTime;
                    }
                }
            }, __instance);
            GameOperatorManager.Instance.Subscribe<TaskPhaseStartEvent>(ev =>
            {
                currentKillTime = 0f;
                DiagnosisModePatchs.isSpecialMeeting = false;
                DiagnosisModePatchs.ExileMessageType = 0;
                EvilTrapperKillCD = 15f;
                if (CanGenerateLastImpostor)
                {
                    if (!AmongUsLLImpl.AmongUsClientInstance.AmHost)
                    {
                        return;
                    }
                    if (NebulaGameManager.Instance == null)
                    {
                        return;
                    }
                    var allplayer = NebulaGameManager.Instance.AllPlayerInfo;
                    if (allplayer.Count(p => !p.IsDead && !p.IsDisconnected) >= GenerateLastImpostorIfPlayer && allplayer.Count(p => !p.IsDead && !p.IsDisconnected && p.IsImpostor) == 1)
                    {
                        if (CannotGenerateHasInfected && allplayer.Any(p => p.TryGetModifier<Infected.Instance>(out var i)))
                        {
                            return;
                        }
                        if (allplayer.Any(p => p.TryGetModifier<LastImpostor.Instance>(out var li)))
                        {
                            return;
                        }
                        var players = allplayer.Where(p => !p.IsDead && p.IsImpostor && !p.TryGetModifier<HasLove.Instance>(out var h)).ToList();
                        int index = UnityEngine.Random.Range(0, players.Count);
                        if (players != null && players[index] != null)
                        {
                            players[index].AddModifier(LastImpostor.MyRole);
                        }
                    }
                }
            }, __instance);
            GameOperatorManager.Instance.Subscribe<GameUpdateEvent>(ev =>
            {
                if (DiagnosisModePatchs.isSpecialMeeting)
                {
                    DiagnosisModePatchs.OnSpecialMeetingUpdate();
                }
                /*if (Input.GetKey(KeyCode.RightControl))
                {
                    if (Input.GetKey(KeyCode.A))
                    {
                        spr.transform.localPosition -= new Vector3(0.01f, 0f);
                    }
                    if (Input.GetKey(KeyCode.D))
                    {
                        spr.transform.localPosition += new Vector3(0.01f, 0f);
                    }
                    if (Input.GetKey(KeyCode.W))
                    {
                        spr.transform.localPosition += new Vector3(0f, 0.01f);
                    }
                    if (Input.GetKey(KeyCode.S))
                    {
                        spr.transform.localPosition -= new Vector3(0f, 0.01f);
                    }
                    if (Input.GetKey(KeyCode.U))
                    {
                        spr.transform.localScale += new Vector3(0.01f, 0.01f);
                    }
                    if (Input.GetKey(KeyCode.J))
                    {
                        spr.transform.localScale -= new Vector3(0.01f, 0.01f);
                    }
                }
                if (Input.GetKeyDown(KeyCode.P))
                {
                    PDebug.Log(spr.transform.localScale);
                    PDebug.Log(spr.transform.localPosition);
                }*/
            }, __instance);
            GameOperatorManager.Instance.Subscribe<PlayerDieEvent>(ev =>
            {
                if (ev.Player.AmOwner)
                {
                    var syncs = NebulaSyncObject.GetObjects<Nightmare.NightmareSeed>(Nightmare.disposableNightSeedOption ? Nightmare.NightmareSeed.MyTempTag : Nightmare.NightmareSeed.MyTag);
                    if (syncs.Any(obj =>
                    {
                        if (ev.Player.Position.Distance(obj.Position) < Nightmare.darknessSizeOption * 1.54f)
                        {
                            if (obj.ActualOwner.TryGetAbility<Nightmare.Ability>(out var ability))
                            {
                                return ability.EffectIsActive;
                            }
                        }
                        return false;
                    }))
                    {
                        if (ev.Player.PlayerState == PlayerState.Pseudocide)
                        {
                            NebulaManager.Instance.ScheduleDelayAction(() => GameOperatorManager.Instance.AllOperators.Do(p =>
                            {
                                if (p is ModAbilityButtonImpl button)
                                {
                                    button.InactivateEffect();
                                }
                            }));
                            ev.Player.Unbox().MyState = baddream;
                        }
                        IEnumerator NightmareKillAnim()
                        {
                            if (Constants.ShouldPlaySfx())
                            {
                                var vanillaAnim = AmongUsLLImpl.HudManagerInstance.KillOverlay.KillAnims[0];
                                AmongUsLLImpl.SoundManagerInstance.PlaySound(vanillaAnim.Stinger, false, 1f, null).volume = vanillaAnim.StingerVolume;
                            }
                            SpriteRenderer spr = UnityHelper.CreateSpriteRenderer("NightmareKillOverlay", AmongUsLLImpl.HudManagerInstance.transform, UnityEngine.Vector3.zero, LayerExpansion.GetUILayer());
                            spr.sprite = NebulaAPI.AddonAsset.GetResource("nightmarekill/nightmarekill1.png")!.AsImage(100f)!.GetSprite();
                            spr.transform.localScale = new UnityEngine.Vector2(0.84f, 0.84f);
                            for (int i = 1; i <= 15; i++)
                            {
                                spr.sprite = NebulaAPI.AddonAsset.GetResource($"nightmarekill/nightmarekill{i}.png")!.AsImage(100f)!.GetSprite();
                                yield return Effects.Wait(0.125f).WrapToManaged();
                            }
                            UnityEngine.Object.Destroy(spr);
                        }
                        NebulaManager.Instance.StartCoroutine(NightmareKillAnim().WrapToIl2Cpp());
                    }
                }
                if (MeetingDeadSendNotice && MeetingHud.Instance.AsBoolFast())
                {
                    var targetPlayer = PlayerControl.AllPlayerControls.GetFastEnumerator().FirstOrDefault(p => !p.Data.IsDead);
                    var originName = targetPlayer?.name;
                    targetPlayer?.SetName("System".Color(Palette.CrewmateBlue));
                    AmongUsLLImpl.HudManagerBridge.Chat.AddChat(targetPlayer, Language.Translate("options.general.meetingdeadNotice").Replace("%DEAD%", ev.Player.PlayerName));
                    targetPlayer?.SetName(originName);
                    targetPlayer?.ToNebulaPlayer()?.Unbox()?.playerNameText?.lastText = null;
                }
            }, __instance);
            GameOperatorManager.Instance.Subscribe<EndCriteriaMetEvent>(ev =>
            {
                if (JackalCanInvokeSabotage && NoAliveImpostorSabotageJackalWin && ev.OverwrittenEndReason == GameEndReason.Sabotage &&
                __instance.GetAllPlayers().Count(p => !p.IsDead && p.IsImpostor) <= 0 &&
                __instance.GetAllPlayers().Count(p => !p.IsDead && p.Role is Jackal.Instance) > 0)
                {
                    ev.TryOverwriteEnd(NebulaGameEnd.JackalWin, GameEndReason.Sabotage);
                    return;
                }
                if (__instance.GetAllPlayers().Count(p => !p.IsDead) <= 0)
                {
                    ev.TryOverwriteEnd(NobodyAliveEnd, GameEndReason.Situation);
                }
            }, __instance);
            GameOperatorManager.Instance.Subscribe<PlayerCheckCanKillLocalEvent>(ev =>
            {
                if (ev.Player.IsImpostor && ev.Target.IsImpostor && NebulaGameManager.Instance!.AllPlayerInfo.Any(p => !p.IsDead && p.IsImpostor && p.TryGetModifier<HasLove.Instance>(out var h)))
                {
                    ev.SetAsCanKillForcedly();
                }
            }, __instance, 101);
            GameOperatorManager.Instance.Subscribe<MapOpenSabotageEvent>(ev =>
            {
                if (!AmongUsLLImpl.HudManagerBridge.SabotageButton.gameObject.activeSelf)
                {
                    MapBehaviour.Instance.infectedOverlay.gameObject.SetActive(false);
                    MapBehaviour.Instance.ColorControl.SetColor(new UnityEngine.Color(0.05f, 0.2f, 1f, 1f));
                }
                if (GamePlayer.LocalPlayer!.Role is Jackal.Instance && JackalCanInvokeSabotage && AmongUsLLImpl.HudManagerBridge.SabotageButton.gameObject.activeSelf)
                {
                    MapBehaviour.Instance.ColorControl.SetColor(Jackal.MyRole.RoleColor.ToUnityColor());
                }
                if (ClientOption.GetValue((ClientOption.ClientOptionType)126) == 1)
                {
                    var p = GamePlayer.LocalPlayer;
                    var color = p.Role.Role.Color.ToUnityColor();
                    if (p.Role is Vanity.Instance)
                    {
                        if (p.Role.DisplayName == ((DefinedAssignable)Vanity.MyRole).DisplayName)
                        {
                            color = Vanity.MyRole.UnityColor;
                        }
                        else
                        {
                            color = Sheriff.MyRole.UnityColor;
                        }
                    }
                    else if (p.Role is Dissociation.Instance inst)
                    {
                        if (p.Role.DisplayName.StartsWith(((DefinedAssignable)Dissociation.MyRole).DisplayName))
                        {
                            color = Dissociation.MyRole.UnityColor;
                        }
                        else
                        {
                            color = inst.myExtRole.UnityColor;
                        }
                    }
                    MapBehaviour.Instance.ColorControl.SetColor(color);
                }
            }, __instance, 101);
            GameOperatorManager.Instance.Subscribe<MapOpenNormalEvent>(ev =>
            {
                if (AmongUsLLImpl.HudManagerBridge.SabotageButton.gameObject.activeSelf && !MeetingHud.Instance.AsBoolFast())
                {
                    MapBehaviour.Instance.infectedOverlay.gameObject.SetActive(true);
                    if (GamePlayer.LocalPlayer!.Role is Jackal.Instance && JackalCanInvokeSabotage)
                    {
                        MapBehaviour.Instance.ColorControl.SetColor(Jackal.MyRole.UnityColor);
                    }
                    else
                    {
                        MapBehaviour.Instance.ColorControl.SetColor(Palette.ImpostorRed);
                    }
                    ConsoleJoystick.SetMode_Sabotage();
                }
                if (ClientOption.GetValue((ClientOption.ClientOptionType)126) == 1)
                {
                    var p = GamePlayer.LocalPlayer;
                    var color = p.Role.Role.Color.ToUnityColor();
                    if (p.Role is Vanity.Instance)
                    {
                        if (p.Role.DisplayName == ((DefinedAssignable)Vanity.MyRole).DisplayName)
                        {
                            color = Vanity.MyRole.UnityColor;
                        }
                        else
                        {
                            color = Sheriff.MyRole.UnityColor;
                        }
                    }
                    else if (p.Role is Dissociation.Instance inst)
                    {
                        if (p.Role.DisplayName.StartsWith(((DefinedAssignable)Dissociation.MyRole).DisplayName))
                        {
                            color = Dissociation.MyRole.UnityColor;
                        }
                        else
                        {
                            color = inst.myExtRole.UnityColor;
                        }
                    }
                    MapBehaviour.Instance.ColorControl.SetColor(color);
                }
            }, __instance, 101);
            GameOperatorManager.Instance.Subscribe<MeetingPreStartEvent>(ev =>
            {
                ChatLogSystem.OnMeetingStarted();
            }, __instance);
            /*GameOperatorManager.Instance.Subscribe<PlayerRoleSetEvent>(ev =>
            {
                if (MeetingHud.Instance.AsBoolFast())
                {
                    PrivateChatManager.Instance.ChangeChannel
                }
            }, __instance);
            GameOperatorManager.Instance.Subscribe<ExileScenePreStartEvent>(ev =>
            {
                CurrentChatId = 0;
                currentIndex = 0;
            }, __instance);*/
            GameOperatorManager.Instance.Subscribe<GameEndEvent>(ev =>
            {
                firstDeadName = GamePlayer.AllPlayers.Where(p => p.IsDead)?.MinBy(p => p.DeathTime.HasValue ? p.DeathTime : float.MaxValue)?.DefaultOutfit.outfit.PlayerName ?? "";
                ChatLogSystem.ExportNow();
            }, __instance);
            VitalsMinigame OpenSpecialVitalsMinigame()
            {
                VitalsMinigame? vitalsMinigame = null;
                foreach (RoleBehaviour role in RoleManager.Instance.AllRoles)
                {
                    if (role.Role == RoleTypes.Scientist)
                    {
                        vitalsMinigame = UnityEngine.Object.Instantiate(role.gameObject.GetComponent<ScientistRole>().VitalsPrefab, Camera.main.transform, false);
                        break;
                    }
                }
                if (vitalsMinigame == null) return null!;
                vitalsMinigame.transform.SetParent(Camera.main.transform, false);
                vitalsMinigame.transform.localPosition = new Vector3(0.0f, 0.0f, -50f);
                vitalsMinigame.Begin(null);

                ConsoleTimer.MarkAsNonConsoleMinigame();

                return vitalsMinigame;
            }
            if (CrewmateHasVitals && !GamePlayer.LocalPlayer!.IsImpostor)
            {
                var sprite = AmongUsLLImpl.HudManagerBridge.UseButton.fastUseSettings[ImageNames.VitalsButton].Image;
                var vitalButton = NebulaAPI.Modules.AbilityButton(__instance, GamePlayer.LocalPlayer, Virial.Compat.VirtualKeyInput.Ability, 0f, "vital", new WrapSpriteLoader(() => sprite), null, _ => !GamePlayer.LocalPlayer.IsDead);
                vitalButton.OnClick = (button) =>
                {
                    VitalsMinigame? vitalsMinigame = OpenSpecialVitalsMinigame();
                    ConsoleTimer.MarkAsNonConsoleMinigame();
                };
                vitalButton.SetLabelType(ModAbilityButton.LabelType.Utility);
                vitalButton.ResetKeyBinding();
            }
            if (ImpostorHasVitals && GamePlayer.LocalPlayer!.IsImpostor)
            {
                var vitalButton = NebulaAPI.Modules.AbilityButton(__instance, GamePlayer.LocalPlayer, Virial.Compat.VirtualKeyInput.Ability, 0f, "vital", Quack.buttonSprite, null, _ => !GamePlayer.LocalPlayer.IsDead);
                vitalButton.OnClick = (button) =>
                {
                    VitalsMinigame? vitalsMinigame = OpenSpecialVitalsMinigame();

                    ConsoleTimer.MarkAsNonConsoleMinigame();
                };
                vitalButton.SetLabelType(ModAbilityButton.LabelType.Utility);
                vitalButton.ResetKeyBinding();
            }
            if (CanSeeAllPlayerDead)
            {
                GameOperatorManager.Instance.Subscribe<PlayerDieEvent>(ev =>
                {
                    UnityEngine.Object.Instantiate(GameManagerCreator.Instance.HideAndSeekManagerPrefab.DeathPopupPrefab, AmongUsLLImpl.HudManagerInstance.transform.parent).Show(ev.Player.VanillaPlayer, 0);
                }, __instance, 101);
            }
            string GetFormattedTime(float seconds)
            {
                int minute = (int)Math.Floor((double)(seconds / 60));
                int second = (int)Math.Floor((double)(seconds % 60));
                return $"{minute:D2}:{second:D2}";
            }
            System.Collections.IEnumerator CoAnimTextColor(TextMeshPro tm, UnityEngine.Color color1, UnityEngine.Color color2, float duration)
            {
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    tm.color = UnityEngine.Color.Lerp(color1, color2, t / duration);
                    yield return null;
                }
                tm.color = color2;
            }
            /*GameOperatorManager.Instance.Subscribe<GameEndEvent>(ev =>
            {
                hidelevelPlayers = new List<byte>();
            }, __instance);
            if (ClientOption.GetValue((ClientOption.ClientOptionType)126) == 1)
            {
                SendMyHideInfo.Invoke(AmongUsLLImpl.LocalPlayer.PlayerId);
            }*/
            if (TimeLimit)
            {

                try
                {
                    float second = TimeLimitMin * 60;
                    //float totalsecond = TimeLimitMin * 60;
                    NoSGUIText noSGUIText = new NoSGUIText(GUIAlignment.Top, NebulaGUIWidgetEngine.API.GetAttribute(AttributeAsset.OverlayTitle), new RawTextComponent(GetFormattedTime(second)));
                    Size size;
                    noSGUIText.PostBuilder = tm =>
                    {
                        tm.color = Palette.White;
                        if (second > 300 && second <= 600)
                        {
                            tm.color = new Color32(255, 242, 219, 255);
                        }
                        else if (second <= 300)
                        {
                            tm.color = new Color32(255, 45, 38, 255);
                        }
                    };
                    GameObject? gameObject = noSGUIText.Instantiate(new Anchor(new Virial.Compat.Vector2(1f, 0.5f), new Virial.Compat.Vector3(0f, 0f, 0f)), new Size(100f, 100f), out size);
                    if (!gameObject.AsBoolFast(out var obj)) return;

                    obj.transform.SetParent(AmongUsLLImpl.HudManagerInstance.SettingsButton.transform);
                    obj.transform.localPosition = new Vector3(-3.75f, 0.18f, 0f);
                    UnityEngine.Object.DontDestroyOnLoad(obj);
                    var lifespan = new GameObjectLifespan(obj);
                    TextMeshPro countText = obj.GetComponent<TextMeshPro>();
                    countText.text = GetFormattedTime(second);
                    GameOperatorManager.Instance.Subscribe<GameUpdateEvent>(ev =>
                    {
                        /*if (Input.GetKey(KeyCode.RightControl))
                        {
                            if (Input.GetKey(KeyCode.W))
                            {
                                gameObject.transform.localPosition += new Vector3(0f, 0.01f, 0f);
                            }
                            if (Input.GetKey(KeyCode.A))
                            {
                                gameObject.transform.localPosition += new Vector3(-0.01f, 0f, 0f);
                            }
                            if (Input.GetKey(KeyCode.S))
                            {
                                gameObject.transform.localPosition += new Vector3(0f, -0.01f, 0f);
                            }
                            if (Input.GetKey(KeyCode.D))
                            {
                                gameObject.transform.localPosition += new Vector3(0.01f, 0f, 0f);
                            }
                        }
                        if (Input.GetKeyDown(KeyCode.P))
                        {
                            PDebug.Log(gameObject.transform.localPosition);
                        }*/
                        second -= Time.deltaTime;
                        if (second >= 0)
                        {
                            countText.text = GetFormattedTime(second);
                            if ((int)Math.Floor((double)second) == 630)
                            {
                                NebulaManager.Instance.StartCoroutine(CoAnimTextColor(countText, countText.color, new Color32(255, 242, 219, 255), 30));
                            }
                            else if ((int)Math.Floor((double)second) == 330)
                            {
                                NebulaManager.Instance.StartCoroutine(CoAnimTextColor(countText, countText.color, new Color32(255, 45, 38, 255), 30));
                            }
                        }
                        if (second <= 0)
                        {
                            if (AmongUsLLImpl.AmongUsClientInstance.AmHost)
                            {
                                var game = NebulaAPI.CurrentGame;
                                if (game == null)
                                {
                                    return;
                                }
                                switch (AmongUsLLImpl.Instance.MapId)
                                {
                                    case 0:
                                        game.TriggerGameEnd(SkeldEnd, GameEndReason.SpecialSituation);
                                        break;
                                    case 1:
                                        game.TriggerGameEnd(MiraEnd, GameEndReason.SpecialSituation);
                                        break;
                                    case 2:
                                        game.TriggerGameEnd(PolusEnd, GameEndReason.SpecialSituation);
                                        break;
                                    case 3:
                                        game.TriggerGameEnd(SkeldEnd, GameEndReason.SpecialSituation);
                                        break;
                                    case 4:
                                        game.TriggerGameEnd(AirshipEnd, GameEndReason.SpecialSituation);
                                        return;
                                    case 5:
                                        game.TriggerGameEnd(FungleEnd, GameEndReason.SpecialSituation);
                                        break;
                                    default:
                                        return;
                                }
                            }
                        }
                    }, lifespan, 100);
                    GameOperatorManager.Instance.Subscribe<GameEndEvent>(ev =>
                    {
                        if (gameObject != null)
                        {
                            UnityEngine.Object.Destroy(gameObject);
                        }
                        gameObject = null!;
                    }, lifespan, 100);
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
            }
        }
        catch (Exception e)
        {
            PDebug.Log("ExStartGameError Error:" + e.ToString());
        }
    }
    public static void CallMeetingCheck(EmergencyMinigame __instance)
    {
        if (GamePlayer.LocalPlayer == null)
        {
            return;
        }
        if (GamePlayer.LocalPlayer.Role is Jester.Instance && !JesterCanCallMeeting)
        {
            __instance.StatusText.text = Language.Translate("role.jester.cannotmeeting");
            __instance.NumberText.text = string.Empty;
            __instance.ClosedLid.gameObject.SetActive(true);
            __instance.OpenLid.gameObject.SetActive(false);
            __instance.ButtonActive = false;
        }
    }
    public static void SidekickMeetingCheckJackal(Sidekick.Instance __instance)
    {
        if (__instance.MyPlayer.AmOwner)
        {
            PrivateChatPatches.SidekickOnActivated(__instance);
            GameOperatorManager.Instance?.Subscribe<TaskPhaseRestartEvent>(ev =>
            {
                if (__instance.MyPlayer.IsDead)
                {
                    return;
                }
                if (!NebulaGameManager.Instance!.AllPlayerInfo.Any(p => !p.IsDead && Jackal.IsJackalLeader(p, __instance.JackalTeamId, true)))
                {
                    var jackal = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => Jackal.IsJackalLeader(p, __instance.JackalTeamId, true));
                    if (jackal != null)
                    {
                        __instance.MyPlayer.SetRole(Jackal.MyRole, (jackal as Jackal.Instance)!.RoleArgumentsForSidekick);
                    }
                }
            }, __instance, 100);
        }
    }
    public static void SidekickModifierMeetingCheckJackal(SidekickModifier.Instance __instance)
    {
        if (__instance.MyPlayer.AmOwner)
        {
            PrivateChatPatches.SidekickMOnActivated(__instance);
            GameOperatorManager.Instance?.Subscribe<TaskPhaseRestartEvent>(ev =>
            {
                if (__instance.MyPlayer.IsDead)
                {
                    return;
                }

                if (!NebulaGameManager.Instance!.AllPlayerInfo.Any(p => !p.IsDead && Jackal.IsJackalLeader(p, __instance.JackalTeamId, true)))
                {
                    var jackal = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => Jackal.IsJackalLeader(p, __instance.JackalTeamId, true));
                    if (jackal != null)
                    {
                        __instance.MyPlayer.SetRole(Jackal.MyRole, (jackal as Jackal.Instance)!.RoleArgumentsForSidekick);
                    }
                }
            }, __instance, 100);
        }
    }
    public static void JackalGameEnd(Jackal.Instance __instance, GameEndEvent ev)
    {
        if (ev.EndState.EndCondition != NebulaGameEnd.JackalWin)
        {
            return;
        }
        if (!ev.EndState.Winners.Test(__instance.MyPlayer))
        {
            return;
        }
        if (ev.EndState.EndReason != GameEndReason.Situation)
        {
            return;
        }
        if (NebulaGameManager.Instance!.AllPlayerInfo.Count(p => !p.IsDead) == 2 &&
            NebulaGameManager.Instance!.AllPlayerInfo.Count(p => Jackal.IsJackalTeam(p, __instance.JackalTeamId)) == 2 && __instance.MyPlayer.AmOwner)
        {
            new StaticAchievementToken("jackal.challenge");
            return;
        }
    }
    public static bool TryShowStampRingMenu(Func<bool> showWhile)
    {
        bool gameIsNotStated = !GameManager.Instance || !GameManager.Instance.GameHasStarted;
        bool inMeeting = MeetingHud.Instance || ExileController.Instance || IntroCutscene.Instance || NebulaPreSpawnMinigame.PreSpawnMinigame;
        bool isDead = AmongUsLLImpl.LocalPlayer && AmongUsLLImpl.LocalPlayer.Data != null && AmongUsLLImpl.LocalPlayer.Data.IsDead;
        bool canSeeSomeStamps = inMeeting || (isDead && NebulaGameManager.Instance!.CanSeeAllInfo) || gameIsNotStated;
        bool shouldShowStampMenu = canSeeSomeStamps;
        bool shouldShowEmoteMenu = !inMeeting && !isDead;
        if (shouldShowStampMenu)
        {
            if (ModSingleton<ShowUp>.Instance.CanUseStamps)
            {
                var stamps = StampManager.GetTableStamps().ToArray();
                NebulaManager.Instance.ShowRingMenu(stamps.Select(stamp => new RingMenu.RingMenuElement(stamp.GetStampWidget(null, AmongUsLLImpl.LocalPlayer.PlayerId, GUIAlignment.Center, false, stamps.Length < 7 ? 0.45f : 0.4f), () =>
                {
                    StampManager.SendStamp(stamp);
                })).ToArray(), showWhile, () => DebugScreen.Push(Language.Translate("ui.error.stamp.notLoaded"), 3f));
            }
            else
            {
                DebugScreen.Push(Language.Translate("ui.error.stamp.notAllowed"), 3f);
            }
        }
        else if (shouldShowEmoteMenu)
        {
            if (ModSingleton<ShowUp>.Instance.CanUseStamps && (gameIsNotStated || ModSingleton<ShowUp>.Instance.CanUseEmotes))
            {
                var emotes = EmoteManager.AllEmotes.Where(e => !AmongUsLLImpl.LobbyInstance || e.Value.CanPlayInLobby);
                NebulaManager.Instance.ShowRingMenu(emotes.Select(emote => new RingMenu.RingMenuElement(emote.Value.LocalIconSupplier, () =>
                {
                    EmoteManager.SendLocalEmote(emote.Key);
                })).ToArray(), showWhile, null);
            }
            else
            {
                //DebugScreen.Push(Language.Translate("ui.error.emote.notAllowed"), 3f);
            }
        }
        return false;
    }
    public static void SabotageTypesFix(ref SystemTypes[] __result)
    {
        if (!PolusHasOxygenSabotage)
        {
            return;
        }
        __result = [SystemTypes.Laboratory, SystemTypes.Comms, SystemTypes.Electrical, SystemTypes.LifeSupp];
    }
    public static bool TrapUpdate(Trapper.Trap __instance)
    {
        if (!AccelTrapOnlyCrewmate && !DecelTrapOnlyImpostor)
        {
            return true;
        }
        if (GamePlayer.LocalPlayer == null)
        {
            return true;
        }
        if (__instance.TypeId < 2 && __instance.Color.A >= 1f)
        {
            //加減速トラップはそれぞれで処理する

            if (__instance.Position.Distance(GamePlayer.LocalPlayer.Position) < Trapper.SpeedTrapSizeOption * 0.35f)
            {
                if (AccelTrapOnlyCrewmate)
                {
                    if (__instance.TypeId == 0 && !GamePlayer.LocalPlayer.IsCrewmate)
                    {
                        return false;
                    }
                }
                if (DecelTrapOnlyImpostor)
                {
                    if (__instance.TypeId == 1 && !GamePlayer.LocalPlayer!.IsImpostor)
                    {
                        return false;
                    }
                }
                var invoker = PlayerModInfo.RpcAttrModulator.GetInvoker((AmongUsLLImpl.LocalPlayer.PlayerId,
                    new SpeedModulator(__instance.TypeId == 0 ? Trapper.AccelRateOption : Trapper.DecelRateOption, UnityEngine.Vector2.one, true, Trapper.SpeedTrapDurationOption, false, 50, "nebula.trap" + __instance.TypeId), false));

                if (NebulaGameManager.Instance?.HavePassed(__instance.GetPrivateField<float>("lastAccelTime"), 0.3f) ?? false)
                {
                    __instance.SetPrivateField("lastAccelTime", NebulaGameManager.Instance!.CurrentTime);
                    invoker.InvokeSingle();
                }
                else
                {
                    invoker.InvokeLocal();
                }
            }
        }
        return false;
    }
    static float EvilTrapperKillCD = 15f;
    public static bool EvilTrapperUpdate(Trapper.EvilAbility __instance)
    {
        if (!EvilTrapperKillTrapPLUS)
        {
            return true;
        }
        if (EvilTrapperKillCD > 0f)
        {
            EvilTrapperKillCD -= Time.deltaTime;
            return false;
        }
        if (MeetingHud.Instance || ExileController.Instance)
        {
            return false;
        }
        __instance.killTraps.RemoveAll((killTrap) =>
        {
            foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo)
            {
                if (p.AmOwner) continue;
                if (p.IsDead || p.VanillaPlayer.Data.Role.IsImpostor) continue;
                if (p.Position.Distance(killTrap.Position) < Trapper.KillTrapSizeOption * 0.35f)
                {
                    using (RPCRouter.CreateSection("TrapKill"))
                    {
                        __instance.MyPlayer.MurderPlayer(p, PlayerState.Trapped, EventDetail.Trap, KillParameter.RemoteKill, KillCondition.TargetAlive);
                        Trapper.RpcTrapKill.Invoke(killTrap.ObjectId);
                        __instance.GetPrivateField<AchievementToken<int>>("acTokenChallenge")!.Value++;
                    }
                    return true;
                }
            }
            return false;
        });
        return false;
    }
    public static bool NiceTrapperReleased(Trapper.NiceAbility __instance)
    {
        if (__instance.AmOwner)
        {
            TrapperSystem.OnInactivated(__instance.GetPrivateField<List<Trapper.Trap>>("localTraps"), __instance.GetPrivateField<List<Trapper.Trap>>("commTraps"));
        }
        return false;
    }
    public static bool EvilTrapperReleased(Trapper.EvilAbility __instance)
    {
        if (__instance.AmOwner)
        {
            TrapperSystem.OnInactivated(__instance.GetPrivateField<List<Trapper.Trap>>("localTraps"), __instance.GetPrivateField<List<Trapper.Trap>>("killTraps"));
        }
        return false;
    }
    public static bool EvilTrapperMeetingStart(Trapper.EvilAbility __instance)
    {
        TrapperSystem.OnMeetingStart(__instance.GetPrivateField<List<Trapper.Trap>>("localTraps"), __instance.GetPrivateField<List<Trapper.Trap>>("killTraps"));
        return false;
    }
    public static bool NiceTrapperMeetingStart(Trapper.NiceAbility __instance)
    {
        TrapperSystem.OnMeetingStart(__instance.GetPrivateField<List<Trapper.Trap>>("localTraps"), __instance.GetPrivateField<List<Trapper.Trap>>("commTraps"));
        __instance.GetPrivateField<AchievementToken<ValueTuple<bool, int>>>("acTokenChallenge").Value.Item2 = 0;
        return false;
    }
    public static void NiceTrapperActivate(Trapper.NiceAbility __instance, GamePlayer player, bool isUsurped, int leftCost)
    {
        if (player.AmOwner)
        {
            NebulaManager.Instance.StartDelayAction(0.1f, delegate
            {
                GameOperatorManager.Instance?.AllOperators.Do(p =>
                {
                    if (p is ModAbilityButtonImpl button)
                    {
                        if (button.VanillaButton.buttonLabelText.text == Language.Translate("button.label.place"))
                        {
                            button.Availability = (button) => false;
                            button.Visibility = (button) => false;
                        }
                    }
                });
                TrapperSystem.OnActivated(__instance, false, new ValueTuple<int, int>[]
            {
            new ValueTuple<int, int>(0, AccelTrapCost),
            new ValueTuple<int, int>(1, DecelTrapCost),
            new ValueTuple<int, int>(2, Trapper.CostOfCommTrapOption)
            }, __instance.GetPrivateField<List<Trapper.Trap>>("localTraps"), __instance.GetPrivateField<List<Trapper.Trap>>("commTraps"), leftCost);
            });
        }
    }
    public static void EvilTrapperActivate(Trapper.EvilAbility __instance, GamePlayer player, bool isUsurped, int leftCost)
    {
        if (player.AmOwner)
        {
            NebulaManager.Instance.StartDelayAction(0.1f, delegate
            {
                GameOperatorManager.Instance?.AllOperators.Do(p =>
                {
                    if (p is ModAbilityButtonImpl button)
                    {
                        if (button.VanillaButton.buttonLabelText.text == Language.Translate("button.label.place"))
                        {
                            button.Availability = (button) => false;
                            button.Visibility = (button) => false;
                        }
                    }
                });
                TrapperSystem.OnActivated(__instance, true, new ValueTuple<int, int>[]
            {
            new ValueTuple<int, int>(0, AccelTrapCost),
            new ValueTuple<int, int>(1, DecelTrapCost),
            new ValueTuple<int, int>(3, Trapper.CostOfKillTrapOption)
            }, __instance.GetPrivateField<List<Trapper.Trap>>("localTraps"), __instance.GetPrivateField<List<Trapper.Trap>>("killTraps"), leftCost);
            });
        }
    }
    public static bool CollatorRegisterResult(Collator.Ability __instance, ValueTuple<Player, RoleTeam> player1, ValueTuple<Player, RoleTeam> player2)
    {
        if (!collatorSendMessageMode && !DiagnosisModePatchs.IsEnabled)
        {
            return true;
        }
        if (collatorSendMessageMode)
        {
            try
            {
                bool matched = player1.Item2 == player2.Item2;
                if (DiagnosisModePatchs.IsEnabled)
                {
                    matched = true;
                }
                if (player1.Item1.IsImpostor && player2.Item1.IsImpostor)
                {
                    new StaticAchievementToken("collator.common4");
                }
                if (!matched)
                {
                    new StaticAchievementToken("collator.common3");
                }
                var acTokenChallenge = __instance.GetPrivateField<AchievementToken<EditableBitMask<GamePlayer>>>("acTokenChallenge");
                if (acTokenChallenge != null)
                {
                    acTokenChallenge.Value.Add(player1.Item1).Add(player2.Item1);
                }
                NebulaAPI.IncrementStatsEntry("stats.collator.collating", 1);
                if (matched)
                {
                    NebulaAPI.IncrementStatsEntry("stats.collator.matched");
                }
                else
                {
                    NebulaAPI.IncrementStatsEntry("stats.collator.unmatched");
                }
                var mp = AmongUsLLImpl.LocalPlayer;
                var name = mp.name;
                mp.SetName(Language.Translate("role.collator.ui.title").Color(new UnityEngine.Color(37, 159, 148)));
                var message = Language.Translate("role.collator.ui.target") + ":" +
                    Environment.NewLine +
                    player1.Item1.Unbox().ColoredDefaultName
                    + Environment.NewLine
                    + player2.Item1.Unbox().ColoredDefaultName
                    + Environment.NewLine
                    + Environment.NewLine
                    + Language.Translate("role.collator.ui.result") + ":" +
                    (matched ? Language.Translate("role.collator.ui.matched").Color(UnityEngine.Color.green) : Language.Translate("role.collator.ui.unmatched").Color(UnityEngine.Color.red)).Bold();
                AmongUsLLImpl.HudManagerBridge.Chat.AddChat(mp, message);
                mp.SetName(name);
                mp?.ToNebulaPlayer()?.Unbox()?.playerNameText?.lastText = null;
                Collator.Ability.ShareResult.Invoke(new ValueTuple<Player, Player, Player, bool>(__instance.MyPlayer, player1.Item1, player2.Item1, matched));
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        else
        {
            bool matched = player1.Item2 == player2.Item2;
            if (DiagnosisModePatchs.IsEnabled)
            {
                matched = true;
            }
            if (player1.Item1.IsImpostor && player2.Item1.IsImpostor) new StaticAchievementToken("collator.common4");
            if (!matched) new StaticAchievementToken("collator.common3");
            var acTokenChallenge = __instance.GetPrivateField<AchievementToken<EditableBitMask<GamePlayer>>>("acTokenChallenge");
            if (acTokenChallenge != null)
            {
                acTokenChallenge.Value.Add(player1.Item1).Add(player2.Item1);
            }

            NebulaAPI.IncrementStatsEntry("stats.collator.collating", 1);
            if (matched)
            {
                NebulaAPI.IncrementStatsEntry("stats.collator.matched");
            }
            else
            {
                NebulaAPI.IncrementStatsEntry("stats.collator.unmatched");
            }

            NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay(NebulaGUIWidgetEngine.API.VerticalHolder(Virial.Media.GUIAlignment.Left,
                new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaGUIWidgetEngine.API.GetAttribute(Virial.Text.AttributeAsset.OverlayTitle), new TranslateTextComponent("role.collator.ui.title")),
                new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaGUIWidgetEngine.API.GetAttribute(Virial.Text.AttributeAsset.OverlayContent),
                new RawTextComponent(
                    Language.Translate("role.collator.ui.target") + ":<br>"
                    + "  " + player1.Item1.Unbox().ColoredDefaultName + "<br>"
                    + "  " + player2.Item1.Unbox().ColoredDefaultName + "<br>"
                    + "<br>"
                    + Language.Translate("role.collator.ui.result") + ": " + (matched ? Language.Translate("role.collator.ui.matched").Color(UnityEngine.Color.green) : Language.Translate("role.collator.ui.unmatched").Color(UnityEngine.Color.red)).Bold()
                )))
                , MeetingOverlayHolder.IconsSprite[2], Collator.MyRole.RoleColor);
            Collator.Ability.ShareResult.Invoke(new ValueTuple<Player, Player, Player, bool>(__instance.MyPlayer, player1.Item1, player2.Item1, matched));
        }
        return false;
    }
    public static bool PsychicReportDeadBody(Psychic.Ability __instance, ReportDeadBodyEvent ev)
    {
        if (!PsychicSendMessageMode && !DiagnosisModePatchs.IsEnabled)
        {
            return true;
        }
        if (PsychicSendMessageMode)
        {
            try
            {
                if (ev.Reporter.AmOwner && ev.Reported != null)
                {
                    NebulaAPI.IncrementStatsEntry("stats.psychic.messages", 1);
                    List<ValueTuple<string, string>> cand = new List<ValueTuple<string, string>>();
                    if (NebulaGameManager.Instance == null)
                    {
                        return false;
                    }
                    float num = NebulaGameManager.Instance.CurrentTime - ev.Reported!.DeathTime!.Value;
                    if (num > 20f)
                    {
                        __instance.SetPrivateField("lastReported", ev.Reported);
                    }
                    int aboutTime = (int)(num + 2.5f);
                    aboutTime -= aboutTime % 5;
                    if (aboutTime > 0)
                    {
                        cand.Add(new ValueTuple<string, string>("elapsedTime", Language.Translate("options.role.psychic.message.elapsedTime").Replace("%SEC%", aboutTime.ToString())));
                    }
                    if (ev.Reported.MyKiller != null && ev.Reported.MyKiller != ev.Reported)
                    {
                        cand.Add(new ValueTuple<string, string>("killersColor", Language.Translate("options.role.psychic.message.killersColor").Replace("%COLOR%", Language.Translate(ModSingleton<BalancedColorManager>.Instance.IsLightColor(DynamicPalette.PlayerColors[(int)ev.Reported.MyKiller.PlayerId]) ? "options.role.psychic.message.inner.lightColor" : "options.role.psychic.message.inner.darkColor"))));
                        if (!DiagnosisModePatchs.IsEnabled)
                        {
                            cand.Add(new ValueTuple<string, string>("killersRole", Language.Translate("options.role.psychic.message.killersRole").Replace("%ROLE%", ev.Reported.MyKiller.Role.DisplayColoredName)));
                            if (ev.Reported.MyKiller.IsDead)
                            {
                                cand.Add(new ValueTuple<string, string>("killerIsDead", Language.Translate("options.role.psychic.message.killerIsDead")));
                            }
                        }
                    }
                    cand.Add(new ValueTuple<string, string>("myRole", Language.Translate("options.role.psychic.message.myRole").Replace("%ROLE%", ev.Reported.Role.DisplayColoredName)));
                    if (ev.Reported.PlayerState != PlayerState.Dead)
                    {
                        cand.Add(new ValueTuple<string, string>("myState", Language.Translate("options.role.psychic.message.myState").Replace("%STATE%", ev.Reported.PlayerState.Text)));
                    }
                    ValueTuple<string, string> valueTuple = cand.Random<ValueTuple<string, string>>();
                    string tag = valueTuple.Item1;
                    string rawText = valueTuple.Item2;
                    var name = Language.Translate("options.role.psychic.message.header") + ":" + ev.Reported.Unbox().ColoredDefaultName;
                    var mp = AmongUsLLImpl.LocalPlayer;
                    var mpname = mp.name;
                    mp.SetName(name);
                    AmongUsLLImpl.HudManagerBridge.Chat.AddChat(mp, rawText);
                    mp.SetName(mpname);
                    mp?.ToNebulaPlayer()?.Unbox()?.playerNameText?.lastText = null;
                    new StaticAchievementToken("psychic.common1");
                    new StaticAchievementToken("psychic.common2." + tag);
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
            return false;
        }
        else if (ev.Reporter.AmOwner && ev.Reported != null)
        {
            NebulaAPI.IncrementStatsEntry("stats.psychic.messages", 1);
            List<ValueTuple<string, string>> cand = new List<ValueTuple<string, string>>();
            if (NebulaGameManager.Instance == null)
            {
                return false;
            }
            float num = NebulaGameManager.Instance.CurrentTime - ev.Reported!.DeathTime!.Value;
            if (num > 20f)
            {
                __instance.SetPrivateField("lastReported", ev.Reported);
            }
            int aboutTime = (int)(num + 2.5f);
            aboutTime -= aboutTime % 5;
            if (aboutTime > 0)
            {
                cand.Add(new ValueTuple<string, string>("elapsedTime", Language.Translate("options.role.psychic.message.elapsedTime").Replace("%SEC%", aboutTime.ToString())));
            }
            if (ev.Reported.MyKiller != null && ev.Reported.MyKiller != ev.Reported)
            {
                cand.Add(new ValueTuple<string, string>("killersColor", Language.Translate("options.role.psychic.message.killersColor").Replace("%COLOR%", Language.Translate(ModSingleton<BalancedColorManager>.Instance.IsLightColor(DynamicPalette.PlayerColors[(int)ev.Reported.MyKiller.PlayerId]) ? "options.role.psychic.message.inner.lightColor" : "options.role.psychic.message.inner.darkColor"))));
                if (!DiagnosisModePatchs.IsEnabled)
                {
                    cand.Add(new ValueTuple<string, string>("killersRole", Language.Translate("options.role.psychic.message.killersRole").Replace("%ROLE%", ev.Reported.MyKiller.Role.DisplayColoredName)));
                    if (ev.Reported.MyKiller.IsDead)
                    {
                        cand.Add(new ValueTuple<string, string>("killerIsDead", Language.Translate("options.role.psychic.message.killerIsDead")));
                    }
                }
            }
            cand.Add(new ValueTuple<string, string>("myRole", Language.Translate("options.role.psychic.message.myRole").Replace("%ROLE%", ev.Reported.Role.DisplayColoredName)));
            if (ev.Reported.PlayerState != PlayerState.Dead)
            {
                cand.Add(new ValueTuple<string, string>("myState", Language.Translate("options.role.psychic.message.myState").Replace("%STATE%", ev.Reported.PlayerState.Text)));
            }
            (string tag, string rawText) = cand.Random();
            NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay(NebulaGUIWidgetEngine.API.VerticalHolder(Virial.Media.GUIAlignment.Left,
                new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaGUIWidgetEngine.API.GetAttribute(Virial.Text.AttributeAsset.OverlayTitle), new TranslateTextComponent("options.role.psychic.message.header")),
                new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaGUIWidgetEngine.API.GetAttribute(Virial.Text.AttributeAsset.OverlayContent), new RawTextComponent(ev.Reported.Unbox().ColoredDefaultName + "<br>" + rawText)))
                , MeetingOverlayHolder.IconsSprite[3], Psychic.MyRole.RoleColor);

            new StaticAchievementToken("psychic.common1");
            new StaticAchievementToken("psychic.common2." + tag);
            return false;
        }
        return true;
    }
    public static void JackalArgumentsForSidekick(ref int[] __result)
    {
        if (__result == null || !Jackal.JackalizedImpostorOption)
        {
            return;
        }
        int jackalizedid = __result[4];
        if (SidekickPromoteToJackalNoJackalizedRole)
        {
            var arguments = Jackal.GenerateArgument(__result[0], null);
            arguments[1] = __result[1];
            arguments[2] = __result[2];
            arguments[3] = __result[3];
            __result = arguments;
        }
        else if (SidekickPromoteToJackalRandomJackalizedRole)
        {
            var jackalType = AssignmentType.AllTypes.FirstOrDefault(r => r.RelatedRole == Jackal.MyRole);
            var jackalizedRoles = Nebula.Roles.Roles.AllRoles.Where(r => r.AssignmentStatus.HasFlag(AbilityAssignmentStatus.CanLoadToKillNeutral) && (r.GetCustomAllocationParameters(jackalType)?.RoleCountSum ?? 0) > 0).ToList();
            var arguments = Jackal.GenerateArgument(__result[0], jackalizedRoles[UnityEngine.Random.Range(0, jackalizedRoles.Count)]);
            arguments[1] = __result[1];
            arguments[2] = __result[2];
            arguments[3] = __result[3];
            __result = arguments;
        }
    }
    public static bool TeamChatActive;
    //public static Dictionary<int, NebulaOptionPatchs.ExStandardAssignmentParameters> MoriartizedRoleDic = new Dictionary<int, NebulaOptionPatchs.ExStandardAssignmentParameters>();
    //public static Dictionary<int, NebulaOptionPatchs.ExStandardAssignmentParameters> YandereRoleDic = new Dictionary<int, NebulaOptionPatchs.ExStandardAssignmentParameters>();
    /*public static void LoverChatMeetingStart()
    {
        MeetingStartTime = DateTime.UtcNow;
    }
    public static bool LoverChatAddChat(ChatController __instance, PlayerControl sourcePlayer)
    {
        if (HasFreeChat)
        {
            return true;
        }
        var p = GamePlayer.LocalPlayer;
        if (p == null)
        {
            return true;
        }
        if (!ChatOption)
        {
            return true;
        }
        if (__instance != AmongUsLLImpl.HudManagerBridge.Chat)
        {
            return true;
        }
        bool shouldSeeMessage = NebulaGameManager.Instance!.CanBeSpectator || p.GetModifiers<Lover.Instance>().Any(l => (sourcePlayer.PlayerId == p.PlayerId ||
            (l.MyLover != null && l.MyLover.Get() != null && sourcePlayer.PlayerId == l.MyLover.Get().PlayerId))) || sourcePlayer.PlayerId == p.PlayerId;
        if (DateTime.UtcNow - MeetingStartTime < TimeSpan.FromSeconds(1.0))
        {
            return shouldSeeMessage;
        }
        return MeetingHud.Instance != null || AmongUsLLImpl.LobbyInstance != null || shouldSeeMessage;
    }*/
    /*public static void LoverChatEnableChat(HudManager __instance)
    {
        if (HasFreeChat)
        {
            if (!__instance.Chat.isActiveAndEnabled)
            {
                __instance.Chat.SetVisible(true);
                __instance.Chat.gameObject.SetActive(true);
            }
            return;
        }
        if (!ChatOption)
        {
            return;
        }
        if (GamePlayer.LocalPlayer == null || __instance == null || __instance.Chat == null)
        {
            return;
        }
        var p = GamePlayer.LocalPlayer;
        if (p == null)
        {
            return;
        }
        if (p.TryGetModifier<Lover.Instance>(out var l))
        {
            if (!__instance.Chat.isActiveAndEnabled)
            {
                __instance.Chat.SetVisible(true);
                __instance.Chat.gameObject.SetActive(true);
            }
        }
    }*/
    [HarmonyPatch]
    public class NebulaOptionPatchs
    {

        [HarmonyPatch(typeof(StartOptionMenuPatch), "Postfix"), HarmonyPrefix]
        public static bool BlockNoS()
        {
            return false;
        }

        [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start)), HarmonyPrefix]
        public static void OptionStart(OptionsMenuBehaviour __instance)
        {
            __instance.transform.localPosition = new(0, 0, -700f);

            foreach (var button in __instance.GetComponentsInChildren<CustomButton>(true))
            {
                if (button.name != "DoneButton") continue;

                button.onClick.AddListener(() =>
                {
                    if (AmongUsLLImpl.AmongUsClientInstance && AmongUsLLImpl.AmongUsClientInstance.GameState == InnerNet.InnerNetClient.GameStates.Started)
                        AmongUsLLImpl.HudManagerInstance.ShowVanillaKeyGuide();
                });
            }
            var tabs = new List<TabGroup>(__instance.Tabs.ToArray());

            PassiveButton passiveButton;

            //設定項目を追加する

            GameObject nebulaTab = new("NebulaTab");
            nebulaTab.transform.SetParent(__instance.transform);
            nebulaTab.transform.localScale = new Vector3(1f, 1f, 1f);
            nebulaTab.SetActive(false);

            var nebulaScreen = MetaScreen.GenerateScreen(new(5f, 4.5f), nebulaTab.transform, new(0f, -0.28f, -10f), false, false, false);
            int nebulaOptionPage = 0;
            void SetNebulaWidget()
            {
                var buttonAttr = new TextAttributeOld(TextAttributeOld.BoldAttr) { Size = new Vector2(1.45f, 0.22f) };
                MetaWidgetOld nebulaWidget = new();
                var options = ClientOption.AllOptions.Values.Where(o => o.ShowOnClientSetting).ToArray();
                int pageCount = Math.Max(1, (options.Length + 17) / 18);
                nebulaOptionPage = Math.Clamp(nebulaOptionPage, 0, pageCount - 1);

                nebulaWidget.Append(new MetaWidgetOld.Text(new TextAttributeOld(TextAttributeOld.BoldAttr) { Size = new Vector2(1f, 0.22f) })
                {
                    RawText = $"{nebulaOptionPage + 1}/{pageCount}",
                    Alignment = IMetaWidgetOld.AlignmentOption.Center
                });
                nebulaWidget.Append(new MetaWidgetOld.VerticalMargin(0.05f));
                nebulaWidget.Append(options.Skip(nebulaOptionPage * 18).Take(18), (option) => new MetaWidgetOld.Button(() =>
                {
                    option.Increment();
                    SetNebulaWidget();
                }, buttonAttr)
                {
                    RawText = option.DisplayName + " : " + option.DisplayValue,
                    PostBuilder = (button, _, _) =>
                    {
                        var detail = option.DisplayDetail;
                        if (detail != null)
                        {
                            button.OnMouseOver.AddListener(() => NebulaManager.Instance.SetHelpWidget(button, detail));
                            button.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpWidgetIf(button));
                        }
                    }
                }, 3, 6, 0, 0.51f, fixedHeight: true);
                nebulaWidget.Append(new MetaWidgetOld.VerticalMargin(0.2f));
                List<MetaWidgetOld.Button> bottomButtons = new();
                void AddBottomButton(string translationKey, Action action)
                {
                    bottomButtons.Add(new MetaWidgetOld.Button(action, buttonAttr)
                    {
                        TranslationKey = "config.client." + translationKey,
                        Alignment = IMetaWidgetOld.AlignmentOption.Center,
                        PostBuilder = (button, _, _) =>
                        {
                            if (Language.TryTranslate("config.client." + translationKey + ".detail", out var detail))
                            {
                                button.OnMouseOver.AddListener(() => NebulaManager.Instance.SetHelpWidget(button, detail));
                                button.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpWidgetIf(button));
                            }
                        }
                    });
                }

                if (ModSingleton<NoSVCRoom>.Instance != null)
                {
                    AddBottomButton("vcSettings", () => NoSVCRoom.VCSettings.OpenSettingScreen(__instance, null, false));
                    AddBottomButton("vcRejoin", () => ModSingleton<NoSVCRoom>.Instance?.Rejoin());
                }
                if (!AmongUsLLImpl.AmongUsClientInstance || AmongUsLLImpl.AmongUsClientInstance.GameState == InnerNetClient.GameStates.NotJoined)
                {
                    AddBottomButton("vcServerSettings", () => NoSVCRoom.VCSettings.OpenServerSettingScreen(__instance));
                }
                /*if (NebulaGameManager.Instance?.VoiceChatManager != null)
                {
                    AddBottomButton("vcSettings", () => NebulaGameManager.Instance?.VoiceChatManager?.OpenSettingScreen(__instance));
                    AddBottomButton("vcRejoin", () => NebulaGameManager.Instance?.VoiceChatManager?.Rejoin());
                }*/

                if (!AmongUsLLImpl.AmongUsClientInstance || AmongUsLLImpl.AmongUsClientInstance.GameState != InnerNet.InnerNetClient.GameStates.Started)
                {
                    AddBottomButton("keyBindings", () =>
                    {
                        __instance.OpenTabGroup(tabs.Count - 1);
                        SetKeyBindingWidget();
                    });
                }

                AddBottomButton("webhook", () => ClientOption.ShowWebhookSetting());
                AddBottomButton("social", () => ClientOption.ShowSocialSetting());

                nebulaWidget.Append(bottomButtons, b => b, 3, -1, 0, 0.51f);

                nebulaScreen.SetWidget(nebulaWidget);
            }

            GameObject keyBindingTab = new GameObject("KeyBindingTab");
            keyBindingTab.transform.SetParent(__instance.transform);
            keyBindingTab.transform.localScale = new Vector3(1f, 1f, 1f);
            keyBindingTab.SetActive(false);

            var keyBindingScreen = MetaScreen.GenerateScreen(new(5f, 4.5f), keyBindingTab.transform, new(0f, -0.28f, -10f), false, false, false);

            IKeyAssignment? currentAssignment = null;

            void SetKeyBindingWidget()
            {
                MetaWidgetOld keyBindingWidget = new();
                TMPro.TextMeshPro? text = null;
                keyBindingWidget.Append(IKeyAssignment.AllKeyAssignments, (assignment) =>
                new MetaWidgetOld.Button(() =>
                {
                    currentAssignment = assignment;
                    SetKeyBindingWidget();
                }, new(TextAttributeOld.NormalAttr) { Size = new Vector2(2.2f, 0.21f) })
                { RawText = assignment.DisplayName + " : " + (currentAssignment == assignment ? Language.Translate("input.recording") : ButtonEffect.KeyCodeInfo.GetKeyDisplayName(assignment.KeyInput)), PostBuilder = (_, _, t) => text = t }, 2, -1, 0, 0.48f);
                keyBindingScreen.SetWidget(keyBindingWidget);
            }

            void ChangeNebulaOptionPage(int delta)
            {
                var optionCount = ClientOption.AllOptions.Values.Count(o => o.ShowOnClientSetting);
                var pageCount = Math.Max(1, (optionCount + 17) / 18);
                var nextPage = Math.Clamp(nebulaOptionPage + delta, 0, pageCount - 1);
                if (nextPage == nebulaOptionPage) return;
                nebulaOptionPage = nextPage;
                SetNebulaWidget();
            }

            void CoUpdate()
            {
                if (currentAssignment != null && Input.anyKeyDown)
                {
                    foreach (var keyCode in ButtonEffect.KeyCodeInfo.AllKeyInfo.Values)
                    {
                        if (Input.GetKeyDown(keyCode.keyCode))
                        {
                            currentAssignment.KeyInput = keyCode.keyCode;
                            currentAssignment = null;
                            SetKeyBindingWidget();
                            break;
                        }
                    }
                }
                if (nebulaTab.activeSelf)
                {
                    if (Input.GetKeyDown(KeyCode.LeftArrow))
                    {
                        ChangeNebulaOptionPage(-1);
                    }
                    if (Input.GetKeyDown(KeyCode.RightArrow))
                    {
                        ChangeNebulaOptionPage(1);
                    }
                    var scroll = Input.mouseScrollDelta.y;
                    if (scroll > 0f)
                    {
                        ChangeNebulaOptionPage(-1);
                    }
                    else if (scroll < 0f)
                    {
                        ChangeNebulaOptionPage(1);
                    }
                }
            }

            __instance.gameObject.AddComponent<ScriptBehaviour>().UpdateHandler += CoUpdate;

            SetNebulaWidget();
            SetKeyBindingWidget();

            //タブを追加する

            tabs[^1] = (GameObject.Instantiate(tabs[1], null));
            var nebulaButton = tabs[^1];
            nebulaButton.gameObject.name = "NebulaButton";
            nebulaButton.transform.SetParent(tabs[0].transform.parent);
            nebulaButton.transform.localScale = new Vector3(1f, 1f, 1f);
            nebulaButton.Content = nebulaTab;
            var textObj = nebulaButton.transform.FindChild("Text_TMP").gameObject;
            textObj.GetComponent<TextTranslatorTMP>().enabled = false;
            textObj.GetComponent<TMPro.TMP_Text>().text = "NoS";

            tabs.Add((GameObject.Instantiate(tabs[1], null)));
            var keyBindingTabButton = tabs[^1];
            keyBindingTabButton.gameObject.name = "KeyBindingButton";
            keyBindingTabButton.transform.SetParent(tabs[0].transform.parent);
            keyBindingTabButton.transform.localScale = new Vector3(1f, 1f, 1f);
            keyBindingTabButton.Content = keyBindingTab;
            keyBindingTabButton.gameObject.SetActive(false);

            passiveButton = nebulaButton.gameObject.GetComponent<PassiveButton>();
            passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                __instance.OpenTabGroup(tabs.Count - 2);
                SetNebulaWidget();
            }
            ));

            float y = tabs[0].transform.localPosition.y, z = tabs[0].transform.localPosition.z;
            if (tabs.Count == 4)
                for (int i = 0; i < 3; i++) tabs[i].transform.localPosition = new Vector3(1.7f * (float)(i - 1), y, z);
            else if (tabs.Count == 5)
                for (int i = 0; i < 4; i++) tabs[i].transform.localPosition = new Vector3(1.62f * ((float)i - 1.5f), y, z);

            __instance.Tabs = new Il2CppReferenceArray<TabGroup>(tabs.ToArray());
        }
    }
    [HarmonyPatch]
    public class IntroPatchs
    {
        [NebulaRPC]
        public static void RpcExitAll(int senderClientId, string reason)
        {
            var client = AmongUsLLImpl.AmongUsClientInstance;
            if (client == null)
            {
                PDebug.Log($"[KickAll] RPC rejected: AmongUsClient is null, senderClientId={senderClientId}");
                return;
            }
            SendKickreason = reason;
            void Exit()
            {
                AmongUsLLImpl.AmongUsClientInstance.ExitGame(DisconnectReasons.Custom);
            }

            if (client.ClientId == senderClientId)
                NebulaManager.Instance.StartDelayAction(1f, Exit);
            else
                Exit();
        }
        [HarmonyPatch(typeof(ShowIntroPatch), "Prefix"), HarmonyPrefix]
        public static bool BlockNoSShow(ref bool __result)
        {
            __result = true;
            return false;
        }
        [HarmonyPatch(typeof(IntroCutscene), "CoBegin"), HarmonyPrefix]
        public static bool CoShowIntro(IntroCutscene __instance, ref Il2CppSystem.Collections.IEnumerator __result)
        {
            NebulaAPI.CurrentGame!.SetGameMode(GeneralConfigurations.CurrentGameMode.InstantiateModule());
            __result = CoBegin(__instance).WrapToIl2Cpp();
            return false;
        }
        static System.Collections.IEnumerator CoBegin(IntroCutscene __instance)
        {
            if (BeforeStartGameDisconnectKickAll)
            {
                var allplayers = GameData.Instance.AllPlayers.GetFastEnumerator();
                if (allplayers.Any(p => p.Disconnected))
                {
                    RpcExitAll(PlayerControl.LocalPlayer.Data.ClientId, Language.Translate("options.general.disconnectKickAllReason"));
                    yield break;
                }
            }
            IntroCutscene.Instance = __instance;
            AmongUsLLImpl.HudManagerInstance.HideGameLoader();
            __instance.HideAndSeekPanels.SetActive(false);
            __instance.CrewmateRules.SetActive(false);
            __instance.ImpostorRules.SetActive(false);
            __instance.ImpostorName.gameObject.SetActive(false);
            __instance.ImpostorTitle.gameObject.SetActive(false);
            //__instance.ImpostorText.gameObject.SetActive(true);
            IEnumerable<PlayerControl> shownPlayers = PlayerControl.AllPlayerControls.GetFastEnumerator().OrderBy(p => p.AmOwner ? 0 : 1);
            var myInfo = GamePlayer.LocalPlayer;
            var introInfo = GameOperatorManager.Instance.Run<GameShowIntroLocalEvent>(new GameShowIntroLocalEvent(myInfo.Role.Role.Team, myInfo.Role), false, true);
            var impostors = AmongUsLLImpl.CurrentGameOptionsInstance.GetAdjustedNumImpostorsModded(PlayerControl.AllPlayerControls.Count);
            if (myInfo.TryGetModifier<Lover.Instance>(out Lover.Instance lover))
            {
                __instance.ImpostorText.text = Language.Translate("intro.lovers");
                introInfo.TeamName = (lover as RuntimeAssignable).DisplayName;
            }
            else if (introInfo.TeamName == Nebula.Roles.Crewmate.Crewmate.MyTeam.DisplayName || introInfo.TeamName == ViolatorTeam.DisplayName)
            {
                __instance.ImpostorText.text = Language.Translate("intro.impostors").Replace("%NUM%", impostors.ToString()).Replace("%NEUNUM%", NebulaAPI.Configurations.GetSharableVariable<int>("options.assignment.neutral")!.Value.ToString());
            }
            else
            {
                __instance.ImpostorText.gameObject.SetActive(false);
            }
            if (introInfo.RevealType == TeamRevealType.Teams)
            {
                if (ImpostorCannotSeeOther && introInfo.TeamName == NebulaTeams.ImpostorTeam.DisplayName)
                {
                    introInfo.RevealType = TeamRevealType.OnlyMe;
                }
                if (AllPlayerCannotSeeOtherNameColor)
                {
                    introInfo.RevealType = TeamRevealType.OnlyMe;
                }
            }
            if (lover != null)
            {
                shownPlayers = shownPlayers.Where(p => p.PlayerId == AmongUsLLImpl.LocalPlayer.PlayerId || p.PlayerId == lover.MyLover.Get().PlayerId);
            }
            else
            {
                switch (introInfo.RevealType)
                {
                    case Virial.Assignable.TeamRevealType.OnlyMe:
                        shownPlayers = shownPlayers.Where(p => p.AmOwner || introInfo.ShouldShowAdditionally(p.PlayerId));
                        break;
                    case Virial.Assignable.TeamRevealType.Teams:
                        shownPlayers = shownPlayers.Where(p => p.GetModInfo()?.Role.Role.Team == myInfo.Role.Role.Team || introInfo.ShouldShowAdditionally(p.PlayerId));
                        break;
                }
            }
            if (GeneralConfigurations.MapFlipXOption || GeneralConfigurations.MapFlipYOption)
            {
                Vector2 vec = new(1f, 1f);
                if (GeneralConfigurations.MapFlipXOption)
                {
                    PlayerModInfo.RpcAttrModulator.LocalInvoke((myInfo!.PlayerId, new AttributeModulator(PlayerAttributes.FlipX, 100000f, true, 0, null, false), true));
                    vec.x = -1f;
                }
                if (GeneralConfigurations.MapFlipYOption)
                {
                    PlayerModInfo.RpcAttrModulator.LocalInvoke((myInfo!.PlayerId, new AttributeModulator(PlayerAttributes.FlipY, 100000f, true, 0, null, false), true));
                    vec.y = -1f;
                }
                PlayerModInfo.RpcAttrModulator.LocalInvoke((myInfo!.PlayerId, new SpeedModulator(1f, vec, true, 100000f, true, 0, null, false), true));
            }

            yield return CoShowTeam(__instance, myInfo!, [.. shownPlayers], 3f, introInfo, lover != null);
            yield return CoShowRole(__instance, myInfo!, introInfo);
            AmongUsLLImpl.ShipStatusInstance.StartSFX();
            GameObject.Destroy(__instance.gameObject);
            NebulaGameManager.Instance?.OnGameStart();
            DestroyableSingleton<HudManager>.Instance.ShowVanillaKeyGuide();
        }

        static System.Collections.IEnumerator CoShowTeam(IntroCutscene __instance, GamePlayer myInfo, PlayerControl[] shownPlayers, float duration, GameShowIntroLocalEvent introInfo, bool isLover = false)
        {
            __instance.overlayHandle ??= DestroyableSingleton<DualshockLightManager>.Instance.AllocateLight();
            yield return AmongUsLLImpl.ShipStatusInstance.CosmeticsCache.PopulateFromPlayers();
            AmongUsLLImpl.SoundManagerInstance.PlaySound(__instance.IntroStinger, false, 1f, null);
            UnityEngine.Color c = isLover ? Lover.Colors[0].ToUnityColor() : introInfo.TeamColor.ToUnityColor();
            var toColor = introInfo.TeamFadeColor == null ? c : introInfo.TeamFadeColor.Value.ToUnityColor();
            if (!isLover)
            {
                if (myInfo.Role.Role is Violator)
                {
                    toColor = Violator.MyRole.UnityColor;
                }
                if (myInfo.IsModMadmate() && introInfo.TeamFadeColor == null)
                {
                    toColor = Palette.ImpostorRed;
                }
            }
            float madFadeBegin = 1.8f;
            float madFadeEnd = 2.4f;
            Vector3 position = __instance.BackgroundBar.transform.position;
            position.y -= 0.25f;
            __instance.BackgroundBar.transform.position = position;
            __instance.BackgroundBar.material.SetColor("_Color", c);
            __instance.TeamTitle.text = introInfo.TeamName;
            __instance.TeamTitle.color = c;
            try
            {
                int maxDepth = Mathn.CeilToInt(7.5f);
                for (int i = 0; i < shownPlayers.Length; i++)
                {
                    PlayerControl playerControl = shownPlayers[i];
                    if (playerControl)
                    {
                        NetworkedPlayerInfo data = playerControl.Data;
                        if (data != null)
                        {
                            PoolablePlayer poolablePlayer = __instance.CreatePlayer(i, maxDepth, data, false);
                            if (i == 0 && data.PlayerId == AmongUsLLImpl.LocalPlayer.PlayerId)
                            {
                                __instance.ourCrewmate = poolablePlayer;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
            __instance.overlayHandle.color = c;
            UnityEngine.Color fade = UnityEngine.Color.black;
            UnityEngine.Color impColor = UnityEngine.Color.white;
            Vector3 titlePos = __instance.TeamTitle.transform.localPosition;
            float timer = 0f;
            bool ismadmate = myInfo.IsModMadmate();
            bool isviolator = myInfo.Role.Role is Violator;
            if (isLover)
            {
                ismadmate = isviolator = false;
            }
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float num = Mathn.Min(1f, timer / duration);
                __instance.Foreground.material.SetFloat("_Rad", __instance.ForegroundRadius.ExpOutLerp(num * 2f));
                fade.a = Mathn.Lerp(1f, 0f, num * 3f);
                __instance.FrontMost.color = fade;
                float p = timer < madFadeBegin ? 0f : timer > madFadeEnd ? 1f : (timer - madFadeBegin) / (madFadeEnd - madFadeBegin);
                __instance.BackgroundBar.material.SetColor("_Color", UnityEngine.Color.Lerp(c, toColor, p));
                toColor.a = Mathn.Clamp(FloatRange.ExpOutLerp(num, 0f, 1f), 0f, 1f);
                c.a = Mathn.Clamp(FloatRange.ExpOutLerp(num, 0f, 1f), 0f, 1f);
                __instance.TeamTitle.color = ismadmate || isviolator ? UnityEngine.Color.Lerp(c, toColor, p) : c;
                if (ismadmate && p >= 0.79f)
                {
                    __instance.TeamTitle.text = introInfo.TeamName.Equals("CREWMATE", StringComparison.CurrentCultureIgnoreCase) ? "MADMATE" : Language.Translate("role.madmate.name");
                    __instance.ImpostorText.text = Language.Translate("role.madmateplus.generalBlurb");
                }
                if (isviolator && p >= 0.79f)
                {
                    __instance.TeamTitle.text = introInfo.TeamName == "CREWMATE" ? "VIOLATOR" : ViolatorTeam.DisplayName;
                    __instance.ImpostorText.text = Language.Translate("intro.violator");
                }
                __instance.RoleText.color = ismadmate || isviolator ? UnityEngine.Color.Lerp(c, toColor, p) : c;
                if (ismadmate || isviolator)
                {
                    impColor = UnityEngine.Color.Lerp(UnityEngine.Color.white, toColor, p);
                }
                impColor.a = Mathn.Lerp(0f, 1f, (num - 0.3f) * 3f);
                __instance.ImpostorText.color = impColor;
                titlePos.y = 2.7f - num * 0.3f;
                __instance.TeamTitle.transform.localPosition = titlePos;
                __instance.overlayHandle.color = c.AlphaMultiplied(Mathn.Min(1f, timer * 2f));
                yield return null;
            }
            timer = 0f;
            while (timer < 1f)
            {
                timer += Time.deltaTime;
                float num2 = timer / 1f;
                fade.a = Mathn.Lerp(0f, 1f, num2 * 3f);
                __instance.FrontMost.color = fade;
                __instance.overlayHandle.color = c.AlphaMultiplied(1f - fade.a);
                yield return null;
            }
            yield break;
        }

        static System.Collections.IEnumerator CoShowRole(IntroCutscene __instance, GamePlayer myInfo, GameShowIntroLocalEvent introInfo)
        {
            var role = myInfo.Role;
            var displayrolename = introInfo.RoleName;
            var displayblurb = introInfo.RoleBlurb;
            bool ismadmateplus = myInfo.Modifiers.Any(r => r is MadmatePLUS.Instance);
            if (ismadmateplus)
            {
                displayrolename = Language.Translate("role.madmateplus.prefix") + role.DisplayName;
                displayblurb = Language.Translate("role.madmateplus.generalBlurb");
            }
            __instance.RoleText.text = displayrolename;
            __instance.RoleBlurbText.text = displayblurb;
            __instance.RoleBlurbText.transform.localPosition = new(0.0965f, -2.12f, -36f);
            __instance.RoleBlurbText.rectTransform.sizeDelta = new(12.8673f, 0.7f);
            __instance.RoleBlurbText.alignment = TMPro.TextAlignmentOptions.Top;

            foreach (var m in myInfo.Modifiers)
            {
                string? mBlurb = m.DisplayIntroBlurb;
                if (mBlurb != null) __instance.RoleBlurbText.text += "\n" + mBlurb;
            }
            var c = introInfo.RoleColor.ToUnityColor();
            if (ismadmateplus)
            {
                c = Palette.ImpostorRed;
            }
            __instance.RoleText.color = c;
            __instance.YouAreText.color = c;
            __instance.RoleBlurbText.color = c;
            AmongUsLLImpl.SoundManagerInstance.PlaySound(AmongUsLLImpl.LocalPlayer.Data.Role.IntroSound, false, 1f, null);
            __instance.YouAreText.gameObject.SetActive(true);
            __instance.RoleText.gameObject.SetActive(true);
            __instance.RoleBlurbText.gameObject.SetActive(true);
            if (__instance.ourCrewmate == null)
            {
                __instance.ourCrewmate = __instance.CreatePlayer(0, 1, AmongUsLLImpl.LocalPlayer.Data, false);
                __instance.ourCrewmate.gameObject.SetActive(false);
            }
            __instance.ourCrewmate.gameObject.SetActive(true);
            __instance.ourCrewmate.transform.localPosition = new Vector3(0f, -1.05f, -18f);
            __instance.ourCrewmate.transform.localScale = new Vector3(1f, 1f, 1f);
            __instance.ourCrewmate.ToggleName(false);
            yield return new WaitForSeconds(2.5f);
            __instance.YouAreText.gameObject.SetActive(false);
            __instance.RoleText.gameObject.SetActive(false);
            __instance.RoleBlurbText.gameObject.SetActive(false);
            __instance.ourCrewmate.gameObject.SetActive(false);
            yield break;
        }
    }
    [HarmonyPatch]
    public class LoggerPatchs
    {
        [HarmonyPatch(typeof(DebugTools), "get_AllowVanillaLog"), HarmonyPrefix]
        public static bool AllowVanillaLogPrefix(ref bool __result)
        {
            __result = true;
            return false;
        }
        [HarmonyPatch(typeof(MemoryLogger), "AppendLog"), HarmonyPrefix]
        private static bool MemoryLoggerAppendLogPrefix()
        {
            return false;
        }
        [HarmonyPatch(typeof(MemoryLogger), "ToByteArray"), HarmonyPrefix]
        private static bool MemoryLoggerToByteArrayPrefix(ref byte[] __result)
        {
            __result = Array.Empty<byte>();
            return false;
        }
    }
    [HarmonyPatch]
    public class DarkThemePatchs
    {
        [HarmonyPatch(typeof(ChatController), "Update"), HarmonyPostfix]
        public static void Postfix(ChatController __instance)
        {
            if (ClientOption.AllOptions[(ClientOption.ClientOptionType)124].Value == 1)
            {
                ChatBubble chatBubble = __instance.chatBubblePool.Prefab.CastFast<ChatBubble>();
                chatBubble.TextArea.overrideColorTags = false;
                chatBubble.TextArea.color = UnityEngine.Color.white;
                chatBubble.Background.color = UnityEngine.Color.black;

                Color32 color = new Color32(40, 40, 40, byte.MaxValue);
                __instance.freeChatField.background.color = color;
                __instance.freeChatField.textArea.compoText.Color(UnityEngine.Color.white);
                __instance.freeChatField.textArea.outputText.color = UnityEngine.Color.white;
                __instance.quickChatField.background.color = color;
                __instance.quickChatField.text.color = UnityEngine.Color.white;
            }
            else
            {
                __instance.freeChatField.textArea.outputText.color = UnityEngine.Color.black;
            }
        }
    }

    public class OptionPatchs
    {
        public static void StartPatch(Harmony harmony)
        {
            try
            {
                var t = typeof(OptionPatchs);
                var interfaceType = typeof(IOrderedSharableEntry);
                harmony.Patch(typeof(IntegerConfigurationValue).GetMethod(interfaceType.FullName + ".ChangeValue", AccessTools.all), null, new HarmonyMethod(t.GetMethod("IntergerChangeValue")));
                harmony.Patch(typeof(BoolConfigurationImpl).GetMethod("UpdateValue", AccessTools.all), null, new HarmonyMethod(t.GetMethod("BoolChangeValue")));
                harmony.Patch(typeof(FloatConfigurationValue).GetMethod(interfaceType.FullName + ".ChangeValue", AccessTools.all), null, new HarmonyMethod(t.GetMethod("FloatChangeValue")));
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }

        static RemoteProcess<(string, string)> SetOptionMessageRpc = new RemoteProcess<(string, string)>("SetOptionMessage", (message, _) =>
        {
            var notifier = AmongUsLLImpl.HudManagerInstance.Notifier;
            var key = message.Item1.Sum(c => c);
            var item = message.Item2;

            if (notifier.lastMessageKey == key && notifier.activeMessages.Count > 0)
                notifier.activeMessages[^1].UpdateMessage(item);
            else
            {
                notifier.lastMessageKey = key;
                LobbyNotificationMessage newMessage = UnityEngine.Object.Instantiate<LobbyNotificationMessage>(notifier.notificationMessageOrigin, UnityEngine.Vector3.zero, Quaternion.identity, notifier.transform);
                newMessage.transform.localPosition = new Vector3(0f, 0f, -2f);
                var action = () => notifier.OnMessageDestroy(newMessage);
                newMessage.SetUp(item, notifier.settingsChangeSprite, notifier.settingsChangeColor, action);
                notifier.ShiftMessages();
                notifier.AddMessageToQueue(newMessage);
            }
            AmongUsLLImpl.SoundManagerInstance.PlaySoundImmediate(notifier.settingsChangeSound, false, 1f, 1f, null);
        });

        public static string? GetDisplayColoredName(string LocalizedName)
        {
            if (string.IsNullOrWhiteSpace(LocalizedName))
                throw new ArgumentException("The key cannot be null.", nameof(LocalizedName));

            var roleName = Nebula.Roles.Roles.AllRoles?.FirstOrDefault(role => string.Compare(role.InternalName, LocalizedName) == 0)?.DisplayColoredName
                       ?? Nebula.Roles.Roles.AllModifiers?.FirstOrDefault(role => string.Compare(role.InternalName, LocalizedName) == 0)?.DisplayColoredName
                       ?? Nebula.Roles.Roles.AllGhostRoles?.FirstOrDefault(role => string.Compare(role.InternalName, LocalizedName) == 0)?.DisplayColoredName;

            if (!string.IsNullOrEmpty(roleName))
                return roleName;
            return null;
        }
        public static string? GetTypeName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("The key cannot be null.", nameof(typeName));

            var postfix = AssignmentType.AllTypes?.FirstOrDefault(type => string.Compare(type.Postfix, typeName) == 0)?.RelatedRole;
            var postfixRoleColor = postfix?.Color;

            if (postfixRoleColor != null)
            {
                var unityColor = postfixRoleColor.Value.ToUnityColor();
                var hex = ColorUtility.ToHtmlStringRGBA(unityColor);

                return hex;
            }
            return null;
        }

        private static string? ProcessKeyForTranslation(string originalKey)
        {
            if (string.IsNullOrEmpty(originalKey))
                return originalKey;

            var keyParts = originalKey.Split('.');
            if (keyParts.Length < 2)
                return originalKey;

            var firstPart = keyParts.Length > 0 ? keyParts[0] : null;
            var secondPart = keyParts.Length > 1 ? keyParts[1] : null;
            var thirdPart = keyParts.Length > 2 ? keyParts[2] : null;
            var fourthPart = keyParts.Length > 3 ? keyParts[3] : null;

            if ((firstPart!.Equals("options", StringComparison.Ordinal) && secondPart!.Equals("role", StringComparison.Ordinal))
                || firstPart.Equals("role", StringComparison.Ordinal))
            {
                var type = GetTypeName(thirdPart!);
                if (type is not null)
                {
                    string outkey = $"<color=#{type}>" + Language.Translate($"role.{secondPart}.name") + "</color>";
                    return outkey + ": ";
                }
                else
                {
                    var targetKey = firstPart.Equals("role") ? secondPart : thirdPart!;
                    return GetDisplayColoredName(targetKey!)! + ": ";
                }
            }
            else if (secondPart!.Equals("gamemode", StringComparison.Ordinal)) return null;
            else
                return firstPart.Equals("options") ? Language.Translate($"options.{secondPart}") + ": " : originalKey;
        }

        private static string? ProcessKey(string originalKey)
        {
            if (string.IsNullOrEmpty(originalKey))
                return originalKey;

            var keyParts = originalKey.Split('.');
            if (keyParts.Length < 2)
                return originalKey;

            var thirdPart = keyParts.Length > 2 ? keyParts[2] : null;
            var fourthPart = keyParts.Length > 3 ? keyParts[3] : null;
            var fifthPart = keyParts.Length > 4 ? keyParts[4] : null;

            var partTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "count", "options.role.count.short"},
            { "chance", "options.role.chance" },
            { "secondaryChance", "options.role.secondaryChance" },
            { "canUseVent", "role.general.canUseVent"},
            { "coolDown", "role.general.ventCoolDown" },
            { "duration", "role.general.ventDuration" },
            { "uses", "role.general.ventUses" },
        };

            if (!string.IsNullOrEmpty(thirdPart) && partTranslations.TryGetValue(thirdPart, out var thirdResult))
                return Language.Translate(thirdResult);

            if (!string.IsNullOrEmpty(fourthPart) && partTranslations.TryGetValue(fourthPart, out var fourthResult))
                return Language.Translate(fourthResult);

            if (!string.IsNullOrEmpty(thirdPart) && thirdPart.Contains("assignment", StringComparison.Ordinal))
            {
                if (keyParts.Length == 4 && !string.IsNullOrEmpty(fourthPart))
                {
                    return fourthPart switch
                    {
                        var fp when fp.Contains("crewmate", StringComparison.Ordinal) => Language.Translate("options.role.crewmateCount"),
                        var fp when fp.Contains("impostor", StringComparison.Ordinal) => Language.Translate("options.role.impostorCount"),
                        var fp when fp.Contains("neutral", StringComparison.Ordinal) => Language.Translate("options.role.neutralCount"),
                        _ => null
                    };
                }

                var teamKey = fourthPart switch
                {
                    "crewmate" => "role.crewmate.name",
                    "impostor" => "role.impostor.name",
                    "neutral" => "team.neutral",
                    _ => null
                };

                if (fourthPart!.Equals("max", StringComparison.Ordinal)) return Language.Translate("options.role.maxCount");

                if (teamKey != null)
                {
                    return !string.IsNullOrEmpty(fifthPart)
                        ? Language.Translate(teamKey) + Language.Translate(fifthPart.Contains("chance") ? "options.role.chance" : "options.role.randomCount")
                        : null;
                }
            }

            if (fourthPart?.IndexOf("CoolDown", StringComparison.OrdinalIgnoreCase) >= 0 && keyParts.Length >= 5)
            {
                var cooldownKey = Language.Translate($"options.role.{thirdPart}.{fourthPart}");
                return !string.IsNullOrEmpty(fifthPart)
                    ? cooldownKey + Language.Translate(fifthPart switch
                    {
                        "type" => "options.killCoolDown.type",
                        "immediate" => "options.killCoolDown.type.immediate",
                        "relative" => "options.killCoolDown.type.relative",
                        "ratio" => "options.killCoolDown.type.ratio",
                        _ => ""
                    })
                    : null;
            }

            if (!string.IsNullOrEmpty(fifthPart) && partTranslations.TryGetValue(fifthPart, out var fifthResult))
                return Language.Translate(fifthResult);

            return Language.Translate(originalKey);
        }

        private static string BuildOptionMessage(string key, string valueStr)
        {
            var prefixKey = ProcessKeyForTranslation(key);
            var translatedKey = ProcessKey(key)!;
            return $"<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\"><b>{prefixKey}{translatedKey}</b>{Language.Translate("UpdateOptionMessage")}{valueStr}</font>";
        }

        public static void IntergerChangeValue(ISharableVariable<int> __instance)
        {
            var key = __instance.GetPrivateField<string>("name");
            if (key.Contains("diagnosis"))
                return;

            var valueStr = __instance.CurrentValue.ToString();
            if (key.Contains("secondaryChance") || key.Contains("chance"))
                valueStr += Language.Translate("options.percentage");
            if (key.Contains("gamemode"))
            {
                if (valueStr == "0")
                    valueStr = Language.Translate("gamemode.standard");
                else if (valueStr == "1")
                    valueStr = Language.Translate("gamemode.freeplay");
                else if (valueStr == "2")
                    valueStr = Language.Translate("gamemode.aeroguesser");
            }
            else if (key.Contains("type"))
            {
                if (valueStr == "0")
                    valueStr = Language.Translate("options.killCoolDown.type.immediate");
                else if (valueStr == "1")
                    valueStr = Language.Translate("options.killCoolDown.type.relative");
                else if (valueStr == "2")
                    valueStr = Language.Translate("options.killCoolDown.type.ratio");
            }
            SetOptionMessageRpc.Invoke((key, BuildOptionMessage(key, valueStr)));
        }

        public static void BoolChangeValue(BoolConfiguration __instance)
        {
            try
            {
                var instance = (__instance as BoolConfigurationImpl)!;
                var val = __instance.GetPrivateField<ISharableVariable<bool>>("val");
                var key = val.Name;
                if (key.Contains("diagnosis"))
                    return;

                var boolStr = __instance ? 1 : 0;
                var valueStr = Language.Translate($"BoolOption{boolStr}");
                SetOptionMessageRpc.Invoke((key, BuildOptionMessage(key, valueStr)));
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }

        public static void FloatChangeValue(ISharableVariable<float> __instance)
        {
            var key = __instance.GetPrivateField<string>("name");
            if (key.Contains("diagnosis"))
                return;

            static string FormatFloat(float number) =>
                number.ToString().Contains('.') && number.ToString().Split('.')[1].Length >= 2
                    ? number.ToString("F2")
                    : number.ToString();

            var value = FormatFloat(__instance.CurrentValue) + (key switch
            {
                _ when key.Contains("coolDown", StringComparison.OrdinalIgnoreCase) || key.Contains("duration", StringComparison.OrdinalIgnoreCase) => Language.Translate("options.sec"),
                _ when key.Contains("ratio") || key.Contains("Range") => Language.Translate("options.cross"),
                _ when key.Contains("roleChance") => Language.Translate("options.percentage"),
                _ when key.Contains("type") => "",
                _ => ""
            });
            SetOptionMessageRpc.Invoke((key, BuildOptionMessage(key, value)));
        }
    }
    public static bool NumberOptionInitializePrefix(NumberOption __instance)
    {
        switch (__instance.Title)
        {
            case StringNames.GameVotingTime:
                __instance.ValidRange = new(0, 600);
                __instance.Value = (float)Math.Round(__instance.Value, 2);
                break;
            case StringNames.GameShortTasks:
            case StringNames.GameLongTasks:
            case StringNames.GameCommonTasks:
                __instance.ValidRange = new(0, 90);
                __instance.Value = (float)Math.Round(__instance.Value, 2);
                break;
            case StringNames.GameKillCooldown:
                __instance.ValidRange = new(0, 180);
                __instance.Increment = 0.5f;
                __instance.Value = (float)Math.Round(__instance.Value, 2);
                break;
            case StringNames.GamePlayerSpeed:
            case StringNames.GameCrewLight:
            case StringNames.GameImpostorLight:
                __instance.Increment = 0.05f;
                __instance.Value = (float)Math.Round(__instance.Value, 2);
                break;
        }
        return true;
    }
    public static void AssignHostOp(PlayerControl player, ref GamePlayer __result)
    {
        if (!GeneralConfigurations.AssignOpToHostOption && AssignHostOpOption && player.AmHost() && GeneralConfigurations.CurrentGameMode == Virial.Game.GameModes.Standard)
        {
            __result.Unbox().PermissionHolder.AddPermission(Permissions.OpPermission, false);
        }
        if (HostIsGameMaster && player.AmHost())
        {
            __result.Unbox().PermissionHolder.AddPermission(Permissions.OpPermission, false);
        }
    }
    public static bool LoverOnDisconnectedPatch(Lover.Instance __instance, PlayerDieOrDisconnectEvent ev)
    {
        if (LoverDisConnectedRemoveLover)
        {
            GamePlayer myLover = __instance.MyLover.Get();
            if (myLover != null || !myLover!.IsDead)
            {
                if (ev is PlayerDisconnectEvent)
                {
                    __instance.MyPlayer.RemoveModifier(Lover.MyRole);
                    myLover.RemoveModifier(Lover.MyRole);
                    return false;
                }
            }
        }
        return true;
    }
    public static bool SniperGetTargetPatch(Sniper.SniperRifle __instance, float width, float maxLength, ref IPlayerlike __result)
    {
        try
        {
            if (SniperCanLockMini)
            {
                return true;
            }
            float minLength = maxLength;
            IPlayerlike result = null!;
            var CanKillHidingPlayerOption = Sniper.CanKillHidingPlayerOption;
            var CanKillImpostorOption = Sniper.CanKillImpostorOption;
            var renderer = __instance.CallPrivateMethod<SpriteRenderer>("get_Renderer");
            foreach (var p in GamePlayer.AllPlayerlikes)
            {
                if (p.IsDead || p.AmOwner || ((!CanKillHidingPlayerOption) && p.Logic.InVent || p.IsDived)) continue;
                if (p.RealPlayer.TryGetModifier<PMini.Instance>(out var mini) && mini.IsMini())
                {
                    continue;
                }
                //仲間は無視
                if (!CanKillImpostorOption && !__instance.Owner.CanKill(p.RealPlayer)) continue;

                //吹っ飛ばされているプレイヤーは無視しない

                //不可視なプレイヤーは無視
                if (p.IsInvisible || p.WillDie) continue;

                var pos = p.TruePosition.AsUnityVector3();
                Vector2 diff = pos - renderer.transform.position;

                //移動と回転を施したベクトル
                var vec = diff.Rotate(-renderer.transform.eulerAngles.z);

                if (vec.x > 0 && vec.x < minLength && Mathn.Abs(vec.y) < width * 0.5f)
                {
                    result = p;
                    minLength = vec.x;
                }
            }
            __result = result!;
        }
        catch (Exception e)
        {
            PDebug.Log(e);
            return true;
        }
        return false;
    }
    public static void NavvyCamButton(AbstractPlayerUsurpableAbility __instance, GamePlayer player, bool isUsurped)
    {
        if (NavvyCanUseCamera && __instance.AmOwner && AmongUsLLImpl.Instance.MapId != 1)
        {
            var cambutton = NebulaAPI.Modules.AbilityButton(__instance, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.SecondaryAbility, null);
            cambutton.Availability = (button) => __instance.MyPlayer.CanMove;
            cambutton.Visibility = (button) => !__instance.MyPlayer.IsDead;
            cambutton.SetAsUsurpableButton(__instance);
            (cambutton as ModAbilityButtonImpl)!.SetSprite(AmongUsLLImpl.HudManagerBridge.UseButton.fastUseSettings[ImageNames.CamsButton].Image);
            cambutton.OnClick = delegate (ModAbilityButton bfutton)
            {
                byte mapId = AmongUsLLImpl.Instance.MapId;
                if (mapId != 1)
                {
                    if (navvyMiniGame == null)
                    {
                        SystemConsole systemConsole3 = UnityEngine.Object.FindObjectsOfType<SystemConsole>().FirstOrDefault((SystemConsole x) => x.gameObject.name.Contains("Surv_Panel"))!;
                        if (mapId == 0 || mapId == 3)
                        {
                            systemConsole3 = UnityEngine.Object.FindObjectsOfType<SystemConsole>().FirstOrDefault((SystemConsole x) => x.gameObject.name.Contains("SurvConsole"))!;
                        }
                        else if (mapId == 4)
                        {
                            systemConsole3 = UnityEngine.Object.FindObjectsOfType<SystemConsole>().FirstOrDefault((SystemConsole x) => x.gameObject.name.Contains("task_cams"))!;
                        }
                        else if (mapId == 5)
                        {
                            systemConsole3 = UnityEngine.Object.FindObjectsOfType<SystemConsole>().FirstOrDefault((SystemConsole x) => x.gameObject.name.Contains("BinocularsSecurityConsole"))!;
                        }
                        if (systemConsole3 == null || Camera.main == null)
                        {
                            return;
                        }
                        navvyMiniGame = UnityEngine.Object.Instantiate<Minigame>(systemConsole3.MinigamePrefab, Camera.main.transform, false);
                    }
                    navvyMiniGame.transform.SetParent(Camera.main.transform, false);
                    navvyMiniGame.transform.localPosition = new UnityEngine.Vector3(0f, 0f, -50f);
                    navvyMiniGame.Begin(null);
                }
            };
            cambutton.SetLabelType(ModAbilityButton.LabelType.Crewmate);
            cambutton.SetLabel("navvyCam");
        }
    }
    public static void LobbyStart(LobbyBehaviour __instance)
    {
        var logoHolder = UnityHelper.CreateObject("MoreRolesLogoHolder", AmongUsLLImpl.HudManagerInstance.transform.FindChild("NebulaLogoHolder"), new(0f, -0.87f) /*new global::UnityEngine.Vector3(-4.15f, 1.88f)*/, null);
        logoHolder.AddComponent<SortingGroup>();
        //logoHolder.SetAsUIAspectContent(AspectPosition.EdgeAlignments.LeftTop, new UnityEngine.Vector3(1.15f, 0.26f));
        SpriteRenderer spriteRenderer = UnityHelper.CreateObject<SpriteRenderer>("MoreRolesLogo", logoHolder.transform, new UnityEngine.Vector3(-0.26f, 0.24f, 0f), null);
        spriteRenderer.sprite = NebulaAPI.AddonAsset.GetResource("ModLogo1.png")!.AsImage(100f)!.GetSprite();
        spriteRenderer.color = new global::UnityEngine.Color(1f, 1f, 1f, 0.75f);
        spriteRenderer.transform.localScale = new global::UnityEngine.Vector3(0.5f, 0.5f, 1f);
        NoSGUIText noSGUIText = new NoSGUIText(GUIAlignment.Right, NebulaGUIWidgetEngine.API.GetAttribute(AttributeAsset.VersionShower), new RawTextComponent($"<size=125%>Plan 17 Add-On</size> v" + (NebulaAPI.AddonAsset as NebulaAddon)!.Version + Environment.NewLine +
            Language.Translate("MoreRolesmodinfo")));
        Size size;
        GameObject gameObject = noSGUIText.Instantiate(new Anchor(new global::Virial.Compat.Vector2(0.5f, 0.5f), new global::Virial.Compat.Vector3(0f, 0f, 0f)), new Size(100f, 100f), out size);
        gameObject.transform.SetParent(logoHolder.transform, false);
        gameObject.transform.localPosition = new UnityEngine.Vector3(-0.48f, -0.34f, -0.1f);
        /*System.Collections.IEnumerator CoUpdateLogo()
        {
            while (true)
            {
                logoHolder.SetActive(ClientOption.GetValue(ClientOption.ClientOptionType.ShowNoSLogoInLobby) == 1);
                yield return null;
            }
        }*/
        //__instance.StartCoroutine(CoUpdateLogo().WrapToIl2Cpp());
        /*GameOperatorManager.Instance.Subscribe<GameStartEvent>(delegate (GameStartEvent _)
        {
            UnityEngine.Object.Destroy(logoHolder);
        }, new GameObjectLifespan(logoHolder), 100);*/
    }
    public static void AssignExtraModifier(List<byte> impostors, List<byte> others)
    {
        if (AmongUsLLImpl.AmongUsClientInstance.AmHost)
        {
            foreach (var p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
            {
                if (killGuardPlayers != null && killGuardPlayers.ContainsKey(p.VanillaPlayer))
                {
                    p.AddModifier(firstdead.MyRole, [killGuardPlayers[p.VanillaPlayer]]);
                }
                if (AutoAssignFirstDeadShield && !p.TryGetModifier<firstdead.Instance>(out var modifier))
                {
                    if (p.PlayerName == firstDeadName)
                    {
                        p.AddModifier(firstdead.MyRole);
                    }
                }
                if (DesperateAssignToImp)
                {
                    if (p.IsImpostor && !p.TryGetModifier<Desperate.Instance>(out var _))
                    {
                        if (DesperateDoNotAssignToLoverImp && p.TryGetModifier<Lover.Instance>(out var m))
                        {
                            continue;
                        }
                        p.AddModifier(Desperate.MyRole);
                    }
                }
                if (GuesserAutoAssign && !GuesserOnlyInGameAutoAssign && GuesserAutoAssignFilter.Contains(p.Role.Role))
                {
                    if (!p.Modifiers.Any(r => r is GuesserModifier.Instance) && !p.TryGetAbility<Guesser.Ability>(out var g) && !p.TryGetAbility<SpeechEater.Ability>(out var s))
                    {
                        p.AddModifier(GuesserModifier.MyRole);
                    }
                }
            }
            killGuardPlayers = [];
        }
    }
    public static bool BlockNosProgressTrackerUpdate()
    {
        return false;
    }
    public static void NonCrewSeeProgressTrackerPatch(IGameOperator __instance)
    {
        if (ShowTaskNum)
        {
            int TotalTask = 0;
            int completedTasks = 0;
            CrewmateGameRule.GetCurrentTaskState(out TotalTask, out completedTasks);
            (__instance as NebulaGameEventListeners).progressBarText.text = Language.Translate("vanilla.general.completedTasks").Replace("%TOTALTASK%", TotalTask.ToString()).Replace("%COMPLETEDTASK%", completedTasks.ToString());
        }
    }
    public static bool ProgressTrackerUpdateFix(ProgressTracker __instance)
    {
        if (AmongUsUtil.InCommSab)
        {
            __instance.TileParent.enabled = false;
            return false;
        }
        if (!__instance.TileParent.enabled)
        {
            __instance.TileParent.enabled = true;
        }
        GameData instance = GameData.Instance;
        if (instance)
        {
            try
            {
                int num = (TutorialManager.InstanceExists ? 1 : (instance.AllPlayers.Count - GameManager.Instance.LogicOptions.NumImpostors));
                num -= instance.AllPlayers.GetFastEnumerator().Count((NetworkedPlayerInfo p) => p.Disconnected);
                int TotalTask = 0;
                int completedTasks = 0;
                CrewmateGameRule.GetCurrentTaskState(out TotalTask, out completedTasks);
                switch (GameManager.Instance.LogicOptions.GetTaskBarMode())
                {
                    case TaskBarMode.Normal:
                        break;
                    case TaskBarMode.MeetingOnly:
                        if (!MeetingHud.Instance.AsBoolFast())
                        {
                            goto IL_0112;
                        }
                        break;
                    case TaskBarMode.Invisible:
                        __instance.gameObject.SetActive(false);
                        goto IL_0112;
                }
                if (IntroShowImpostor && HasFinal && !isfinal && TotalTask - completedTasks <= EnterFinalLeftTaskNum)
                {
                    isfinal = true;
                    AmongUsLLImpl.SoundManagerInstance.PlaySound(GameManagerCreator.Instance.HideAndSeekManagerPrefab.FinalHideAlertSFX, false, 1f);
                    NebulaManager.Instance.StartCoroutine(CoAnimColor(__instance.TileParent.material, __instance.TileParent.material.GetColor("_FullColor"), Palette.ImpostorRed, 1f).WrapToIl2Cpp());
                    //__instance.TileParent.material.SetColor("_FullColor", Palette.ImpostorRed);
                    System.Collections.IEnumerator CoAnimColor(Material m, UnityEngine.Color color1, UnityEngine.Color color2, float duration)
                    {
                        float t = 0f;
                        while (t < duration)
                        {
                            t += Time.deltaTime;
                            m.SetColor("_FullColor", UnityEngine.Color.Lerp(color1, color2, t / duration));
                            yield return null;
                        }
                        m.SetColor("_FullColor", color2);
                    }
                    /*Game currentGame = NebulaAPI.CurrentGame;
                    if (currentGame != null)
                    {
                        TitleShower module = currentGame.GetModule<TitleShower>();
                        if (module != null)
                        {
                            module.SetText(Language.Translate("general.OnEnterFinal"), Palette.ImpostorRed, 6f);
                        }
                    }*/

                    EditableBitMask<GamePlayer> mask = BitMasks.AsPlayer();
                    NebulaTeams.ImpostorTeam.Color.ToHSV(out var h, out _, out _);
                    foreach (GamePlayer p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                    {
                        if (!p.AmOwner && !p.IsDead && !mask.Test(p) && !p.IsImpostor)
                        {
                            mask.Add(p);
                            AmongUsUtil.Ping([p.Position], false, false, postProcess: ping => ping.gameObject.SetHue(360 - h));
                        }
                    }
                    var player = GamePlayer.LocalPlayer;
                    if (player != null && player.IsImpostor && scanbutton == null)
                    {
                        scanbutton = NebulaAPI.Modules.AbilityButton(NebulaAPI.CurrentGame!, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.SidekickAction, null);
                        scanbutton.Availability = (button) => player.CanMove;
                        scanbutton.Visibility = (button) => !player.IsDead;
                        scanbutton.SetImage(globalscanimage);
                        scanbutton.CoolDownTimer = NebulaAPI.Modules.Timer(NebulaAPI.CurrentGame!, FinalImpostorScanCooldown).SetAsAbilityTimer().Start();
                        scanbutton.OnClick = delegate (ModAbilityButton button)
                        {
                            NebulaManager.Instance.StartCoroutine(CoEcho(player.Position.ToUnityVector()).WrapToIl2Cpp());
                            System.Collections.IEnumerator CoEcho(UnityEngine.Vector2 position)
                            {
                                EditableBitMask<GamePlayer> pMask = BitMasks.AsPlayer();
                                float radious = 0f;
                                var circle = EffectCircle.SpawnEffectCircle(null, player.Position.ToUnityVector(), NebulaTeams.ImpostorTeam.Color, 0f, null, true);
                                circle.OuterRadius = () => radious;
                                NebulaTeams.ImpostorTeam.Color.ToHSV(out var hue, out _, out _);
                                bool isFirst = true;
                                while (radious < 60f)
                                {
                                    if (MeetingHud.Instance.AsBoolFast()) break;

                                    radious += Time.deltaTime * 7.5f;
                                    foreach (GamePlayer p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                                    {
                                        if (!p.AmOwner && !p.IsDead && !pMask.Test(p) && p.Position.Distance(position) < radious && !p.IsImpostor)
                                        {
                                            pMask.Add(p);
                                            AmongUsUtil.Ping([p.Position], false, isFirst, postProcess: ping => ping.gameObject.SetHue(360 - hue));
                                            isFirst = false;
                                        }
                                    }
                                    yield return null;
                                }
                                circle.Disappear();
                            }
                            scanbutton.StartCoolDown();
                        };
                        scanbutton.SetLabel("globalscan");
                        var admin = AmongUsLLImpl.HudManagerInstance.AdminButton;
                        if (admin.AsBoolFast())
                        {
                            AmongUsLLImpl.SoundManagerInstance.PlaySound(admin.RevealSFX, false);
                        }
                    }
                }
                float num2 = (float)completedTasks / (float)TotalTask * (float)num;
                if (TotalTask == 0)
                {
                    num2 = num;
                }
                __instance.curValue = Mathn.Lerp(__instance.curValue, num2, Time.fixedDeltaTime * 2f);
                if (ShowTaskNum)
                {
                    var text = __instance.gameObject.transform.FindChild("TitleText_TMP");
                    text.GetComponent<TextMeshPro>().text = Language.Translate("vanilla.general.completedTasks").Replace("%TOTALTASK%", TotalTask.ToString()).Replace("%COMPLETEDTASK%", completedTasks.ToString());
                }
            IL_0112:
                __instance.TileParent.material.SetFloat("_Buckets", (float)num);
                __instance.TileParent.material.SetFloat("_FullBuckets", __instance.curValue);
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        return false;
    }
    public static bool CmdReportBody()
    {
        if (IntroShowImpostor)
        {
            return !CannotReport;
        }
        return true;
    }
    public static void RaiderKillPatch2(Raider.Ability __instance, ref bool __result)
    {
        if (__result)
        {
            __result = !RaiderCanUseNormalKill;
        }
    }
    public static void SniperKillPatch2(Sniper.Ability __instance, ref bool __result)
    {
        if (__result)
        {
            __result = !SniperCanUseNormalKill;
        }
    }

    public static Image? ChristmasTreeImage = NebulaAPI.AddonAsset.GetResource("ChristmasTree.png")!.AsImage();
    public static Image? TreeShadowImage = NebulaAPI.AddonAsset.GetResource("ChristmasTreeShadow.png")!.AsImage();
    public static void ModifyPolusEarly(byte mapId, ShipStatus ship)
    {
        if (mapId != 2)
        {
            return;
        }
        if (PolusHasOxygenSabotage)
        {
            try
            {
                PDebug.Log("GetSkeldAdmin");
                List<Console> list = ship.AllConsoles.ToList<Console>();
                PDebug.Log(VanillaAsset.MapAsset[0].gameObject.name);
                var oxygenConsole = VanillaAsset.MapAsset[0].gameObject.transform.Find("LifeSupport/Ground/LifeSuppTank/NoOxyConsole");
                PDebug.Log("Done");
                var oxygenA = UnityEngine.Object.Instantiate(oxygenConsole);
                PDebug.Log("GenerateOxygenA");
                PlainShipRoom roomObj;
                oxygenA.SetParent(ship.FastRooms.TryGetValue(SystemTypes.LifeSupp, out roomObj) ? roomObj.transform : ship.transform);
                oxygenA.transform.localPosition = new UnityEngine.Vector3(0.42f, 1.65f, 0f);
                Console console = ShipModification.Consolize<Console>(oxygenA.gameObject, null!);
                console.Room = SystemTypes.LifeSupp;
                console.ConsoleId = 0;
                list.Add(console);
                oxygenConsole = VanillaAsset.MapAsset[0].gameObject.transform.Find("Admin/Ground/admin_walls/NoOxyConsole");
                var oxygenB = UnityEngine.Object.Instantiate(oxygenConsole);
                PDebug.Log("GenerateOxygenB");
                oxygenB.SetParent(ship.FastRooms.TryGetValue(SystemTypes.Laboratory, out roomObj) ? roomObj.transform : ship.transform);
                oxygenB.transform.localPosition = new UnityEngine.Vector3(-0.83f, 1.78f, 0f);
                console = ShipModification.Consolize<Console>(oxygenB.gameObject, null!);
                console.Room = SystemTypes.Laboratory;
                console.ConsoleId = 1;
                list.Add(console);
                ship.AllConsoles = new Il2CppReferenceArray<Console>(list.ToArray());
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
    }
    public static void ModifyPolus(ShipStatus ship)
    {
        var now = DateTime.Now;
        if ((now.Month == 12 && now.Day >= 10) || (now.Month == 1 && now.Day <= 25))
        {
            SpriteRenderer treeRenderer = UnityHelper.CreateSpriteRenderer("christmasTree", ship.transform, new Vector3(20.5062f, -7.9289f, -1.2f));
            treeRenderer.sprite = ChristmasTreeImage!.GetSprite();
            treeRenderer.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            SpriteRenderer shadowRenderer = UnityHelper.CreateSpriteRenderer("christmasTreeShadow", ship.transform, new Vector3(20.5062f, -7.7289f, -0.0095f));
            shadowRenderer.sprite = TreeShadowImage!.GetSprite();
            shadowRenderer.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            ShipModification.AddModWall([
                new Vector2(19.8644f, -9.4064f),
                new Vector2(19.8564f, -9.8618f),
                new Vector2(20.993f, -9.8618f),
                new Vector2(21.177f, -9.7612f),
                new Vector2(21.177f, -9.5112f),
                new Vector2(20.8853f, -9.4696f),
                new Vector2(20.8853f, -9.2368f),
                new Vector2(20.1561f, -9.3648f),
                new Vector2(20.1561f, -9.2368f)
                ]);
        }
        if (PolusHasOxygenSabotage)
        {
            try
            {
                PDebug.Log("ModifyPolus");
                GameOperatorManager.Instance!.Subscribe<MapInstantiateEvent>(delegate (MapInstantiateEvent ev)
                {
                    try
                    {
                        PDebug.Log("GenerateOxygenButton");
                        InfectedOverlay infected = MapBehaviour.Instance.infectedOverlay;
                        GameObject restoreOxygen = UnityEngine.Object.Instantiate<GameObject>(VanillaAsset.MapAsset[0].MapPrefab.infectedOverlay.transform.GetChild(4).GetChild(0).gameObject, infected.transform);
                        restoreOxygen.transform.localPosition = new UnityEngine.Vector3(-3.72f, -1.55f, -2f);
                        restoreOxygen.transform.localScale = new UnityEngine.Vector3(0.8f, 0.8f, 1f);
                        SpriteRenderer renderer = restoreOxygen.GetComponent<SpriteRenderer>();
                        renderer.SetCooldownNormalizedUvs();
                        ButtonBehavior button = restoreOxygen.GetComponent<ButtonBehavior>();
                        button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                        button.OnClick.AddListener(delegate
                        {
                            if (!infected.CanUseSabotage)
                            {
                                return;
                            }
                            ship.RpcUpdateSystem(SystemTypes.Sabotage, 8);
                        });
                        List<ButtonBehavior> allButtons = infected.allButtons.ToList<ButtonBehavior>();
                        allButtons.Add(button);
                        infected.allButtons = allButtons.ToArray();
                        GameOperatorManager.Instance.Subscribe<GameHudUpdateEvent>(delegate (GameHudUpdateEvent ev)
                        {
                            try
                            {
                                if (infected.sabSystem != null)
                                {
                                    float perc = (infected.DoorsPreventingSabotage ? 1f : infected.sabSystem.PercentCool);
                                    if (renderer)
                                    {
                                        renderer.material.SetFloat("_Percent", perc);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                PDebug.Log("OxygenSabHudUpdateError:" + e.ToString());
                            }
                        }, new GameObjectLifespan(MapBehaviour.Instance.gameObject), 100);
                    }
                    catch (Exception ex)
                    {
                        PDebug.Log(ex);
                    }
                }, NebulaAPI.CurrentGame!, 100);
                LifeSuppSystemType LifeSuppSystem = new LifeSuppSystemType(PolusOxygenSabotageTime);
                ship.Systems[SystemTypes.LifeSupp] = LifeSuppSystem.CastFast<ISystemType>();
                GameOperatorManager.Instance.Subscribe<GameUpdateEvent>(delegate (GameUpdateEvent ev)
                {
                    if (LifeSuppSystem.Countdown < 0)
                    {
                        if (NebulaAPI.CurrentGame != null)
                        {
                            NebulaAPI.CurrentGame.TriggerGameEnd(NebulaGameEnds.ImpostorGameEnd, GameEndReason.Sabotage);
                        }
                        LifeSuppSystem.Countdown = 10000;
                    }
                }, new GameObjectLifespan(ship.gameObject), 100);
                ship.Systems[SystemTypes.Sabotage].CastFast<SabotageSystemType>().specials.Add(LifeSuppSystem.CastFast<IActivatable>());
                List<PlayerTask> specialTasks = ship.SpecialTasks.ToList<PlayerTask>();
                specialTasks.Add(VanillaAsset.MapAsset[0].SpecialTasks[3]);
                ship.SpecialTasks = specialTasks.ToArray();
            }
            catch (Exception e)
            {
                PDebug.Log("GenerateSabotageButtonError Error:" + e.ToString());
            }
        }
    }
    public static void PlayerStartGame(GamePlayer __instance)
    {
        try
        {
            navvyMiniGame = null!;
            scanbutton = null!;
            SpectreExArrowList = new List<TrackingArrowAbility>();
            SetFlashLight(UseFlashLightMode);
            if (!IntroShowImpostor)
            {
                return;
            }
            nomusictime = 0f;
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    static float LastUseSkill;
    static float nomusictime;
    static AudioSource? MusicSource;
    static bool selectSpawnEnd;
    static Dictionary<string, AudioClip> audioclips = new Dictionary<string, AudioClip>();
    public static void PlayerUpdate(GamePlayer __instance, float deltaTime)
    {
        try
        {
            if (!IntroShowImpostor)
            {
                return;
            }
            if (__instance == null)
            {
                return;
            }
            if (isfinal && __instance.AmOwner && __instance.IsImpostor)
            {
                __instance.GainSpeedAttribute(1f + FinalImpostorSpeedUpOption, 1f, false, 0, "impostor::speedup");
            }
            if (CrewmateHasEchoSkill && __instance.AmOwner && !__instance.IsImpostor)
            {
                LastUseSkill += deltaTime;
                if (LastUseSkill >= EchoSkillUseTime)
                {
                    LastUseSkill = 0f;
                    NebulaManager.Instance.StartCoroutine(CoEcho(__instance.Position.ToUnityVector()).WrapToIl2Cpp());
                    System.Collections.IEnumerator CoEcho(UnityEngine.Vector2 position)
                    {
                        EditableBitMask<GamePlayer> pMask = BitMasks.AsPlayer();
                        float radious = 0f;
                        var circle = EffectCircle.SpawnEffectCircle(null, __instance.Position.ToUnityVector(), NebulaTeams.ImpostorTeam.Color, 0f, null, true);
                        circle.OuterRadius = () => radious;
                        NebulaTeams.ImpostorTeam.Color.ToHSV(out var hue, out _, out _);
                        bool isFirst = true;
                        while (radious < NebulaAPI.Configurations.Configuration("options.role.echo.echoRange", new ValueTuple<float, float, float>(2.5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Ratio, null, null))
                        {
                            if (MeetingHud.Instance.AsBoolFast()) break;

                            radious += Time.deltaTime * 5f;
                            foreach (var p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                            {
                                if (!p.AmOwner && !p.IsDead && !pMask.Test(p) && p.Position.Distance(position) < radious && p.IsImpostor)
                                {
                                    pMask.Add(p);
                                    AmongUsUtil.Ping([p.Position], false, isFirst, postProcess: ping => ping.gameObject.SetHue(360 - hue));
                                    isFirst = false;
                                }
                            }
                            yield return null;
                        }
                        circle.Disappear();
                    }
                }
                /*if (nomusictime < ImpostorCannotMoveTime)
                {
                    nomusictime += Time.deltaTime;
                    return;
                }*/
                if (!redFullS.AsBoolFast())
                {
                    redFullS = AmongUsUtil.GenerateFullscreen(NebulaTeams.ImpostorTeam.Color);
                    var c = redFullS.color;
                    c.a = 0f;
                    redFullS.color = c;
                    return;
                }
                var color = redFullS!.color;
                List<float> alphas = new List<float>();
                var impostors = PlayerControl.AllPlayerControls.GetFastEnumerator().Where(p => p.ToNebulaPlayer().IsImpostor).ToArray();
                foreach (var player in impostors)
                {
                    alphas.Add((-0.1f * (player.GetTruePosition() - __instance.VanillaPlayer.GetTruePosition()).magnitude) + 0.8f);
                    //distances.Add((player.GetTruePosition() - __instance.ToAUPlayer().GetTruePosition()).magnitude);
                }
                color.a = Mathn.Clamp(Mathf.Max(new Il2CppStructArray<float>(alphas.ToArray())), isfinal ? 0.1f : 0f, 0.4f);
                redFullS.color = color;
                color = redFullS.color;
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }

    public static void ItemSupplierStartClient()
    {
        if (IntroShowImpostor)
        {
            isfinal = false;
            var impostors = GamePlayer.AllPlayers.Where(p => p.IsImpostor).ToList();
            for (int i = 0; i < impostors.Count; i++)
            {
                var obj = UnityEngine.Object.Instantiate(GameManagerCreator.Instance.HideAndSeekManagerPrefab.DeathPopupPrefab, AmongUsLLImpl.HudManagerInstance.transform.parent);
                obj.text.gameObject.SetActive(false);
                if (i != 0)
                {
                    obj.sfx = null;
                }
                obj.transform.position += new UnityEngine.Vector3(i == 0 ? 0 : i == 1 ? 2.5f : -2.5f, 0f);
                obj.Show(impostors[i].VanillaPlayer, 0);
                impostors[i].GainSpeedAttribute(0f, ImpostorCannotMoveTime, false, 0, "NebulaHNS:CannotMove");
            }
            if (CannotReport)
            {
                AmongUsLLImpl.ShipStatusInstance.BreakEmergencyButton();
            }
            GameOperatorManager.Instance?.Subscribe<PlayerDecorateNameEvent>(ev =>
            {
                if (impostors.Contains(ev.Player))
                {
                    ev.Color = Virial.Color.ImpostorColor;
                }
            }, NebulaAPI.CurrentGame);
        }
    }
    public static void GrowALLFlowers(IGameOperator __instance)
    {
        if (!GameStartGrowALLFlower)
        {
            return;
        }
        try
        {
            foreach (ItemSupplier itemSupplier in (__instance as ItemSupplierManager)!.allSuppliers)
            {
                while (itemSupplier.age < 3)
                {
                    itemSupplier.TryGrow();
                }
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    public static void GenerateMadmate()
    {
        try
        {
            if (!CanGenerateInfected)
            {
                return;
            }
            if (!AmongUsLLImpl.AmongUsClientInstance.AmHost)
            {
                return;
            }
            if (NebulaGameManager.Instance == null)
            {
                return;
            }
            var allplayer = NebulaGameManager.Instance.AllPlayerInfo;
            if (allplayer.Count(p => !p.IsDead && !p.IsDisconnected) >= GenerateIfPlayer && allplayer.Count(p => !p.IsDead && !p.IsDisconnected && p.IsImpostor) == 1 && !allplayer.Any(p => p.TryGetModifier<Infected.Instance>(out var i)))
            {
                var players = allplayer.Where(p => !p.IsDead && !p.IsDisconnected && p.IsCrewmate && !p.IsModMadmate() && !p.TryGetModifier<HasLove.Instance>(out var h) && !p.TryGetModifier<SidekickModifier.Instance>(out var m) && p.Role.Role.CanLoad(Infected.MyRole) && p.Modifiers.All(m => Infected.modifierFilter.Test(m.Modifier))).ToList();
                int index = UnityEngine.Random.Range(0, players.Count);
                if (players != null && players[index] != null)
                {
                    players[index].AddModifier(Infected.MyRole);
                }
            }
        }
        catch (Exception ex)
        {
            PDebug.Log(ex);
        }
    }
    public static void DecorateNameColor(ChatBubble __instance, string playerName, bool isDead, bool voted)
    {
        if (ClientOption.AllOptions[(ClientOption.ClientOptionType)124].Value == 1)
        {
            if (isDead)
            {
                __instance.Background.color = new UnityEngine.Color(0.1f, 0.1f, 0.1f, 0.6f);
            }
            else
            {
                __instance.Background.color = new UnityEngine.Color(0.1f, 0.1f, 0.1f, 1f);
            }
            __instance.TextArea.color = UnityEngine.Color.white;
        }
        try
        {
            if (AllPlayerCannotSeeOtherNameColor)
            {
                return;
            }
            GameOperatorManager instance = GameOperatorManager.Instance!;
            var p = NebulaGameManager.Instance.AllPlayerInfo.FirstOrDefault(p => p.PlayerName == playerName);
            var local = GamePlayer.LocalPlayer;
            PlayerDecorateNameEvent ev = ((instance != null) ? instance.Run<PlayerDecorateNameEvent>(new PlayerDecorateNameEvent(p, "", false), false) : null);
            if (ImpostorCannotSeeOther && (p?.IsImpostor ?? false) && (local?.IsImpostor ?? false))
            {
                __instance.NameText.color = UnityEngine.Color.white;
                return;
            }
            if (ev.Color != null && p != null)
            {
                __instance.NameText.color = ev.Color.Value.ToUnityColor();
            }
            if (local != null && p != null && p.PlayerId == local.PlayerId)
            {
                if (p.Role is Vanity.Instance)
                {
                    if (p.Role.DisplayName == ((DefinedAssignable)Vanity.MyRole).DisplayName)
                    {
                        __instance.NameText.color = Vanity.MyRole.UnityColor;
                    }
                    else
                    {
                        __instance.NameText.color = Sheriff.MyRole.UnityColor;
                    }
                }
                else if (p.Role is Dissociation.Instance inst)
                {
                    if (p.Role.DisplayName.StartsWith(((DefinedAssignable)Dissociation.MyRole).DisplayName))
                    {
                        __instance.NameText.color = Dissociation.MyRole.UnityColor;
                    }
                    else
                    {
                        __instance.NameText.color = inst.myExtRole.UnityColor;
                    }
                }
                else
                {
                    __instance.NameText.color = p.Role.Role.Color.ToUnityColor();
                }
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    public static void DoctorShieldButton(Doctor.Ability __instance, GamePlayer player, bool isUsurped, float vitalTimer)
    {
        if (DoctorCanUseShieldOption && !isUsurped && __instance.AmOwner)
        {
            bool used = false;
            ObjectTracker<GamePlayer> myTracker = ObjectTrackers.ForPlayer(__instance, null, __instance.MyPlayer, p => ObjectTrackers.StandardPredicate(p) && !used, new(0, 221, 255, 255), false, false);
            var selectbutton = NebulaAPI.Modules.AbilityButton(__instance, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.SecondaryAbility, null);
            selectbutton.Availability = (button) => myTracker.CurrentTarget != null && __instance.MyPlayer.CanMove;
            selectbutton.Visibility = (button) => !__instance.MyPlayer.IsDead && !used;
            (selectbutton as Virial.Components.ModAbilityButton).SetImage(doctorshieldimage);
            selectbutton.OnClick = delegate (ModAbilityButton button)
            {
                setShieldPlayer.Invoke((myTracker.CurrentTarget!, __instance.MyPlayer!));
                used = true;
            };
            selectbutton.SetLabel("doctorshield");
            selectbutton.SetLabelType(ModAbilityButton.LabelType.Crewmate);
        }
    }
    public static void SheriffCanKillPatch(Sheriff.Ability __instance, GamePlayer target, ref bool __result)
    {
        if (!__result)
        {
            if (target.IsModMadmate())
            {
                __result = SheriffCanKillMadmateModifierOption;
            }
            else if (target.TryGetModifier<SidekickModifier.Instance>(out var sidekick))
            {
                __result = SheriffCanKillSidekickModifierOption;
            }
        }
        if (DiagnosisModePatchs.IsEnabled)
        {
            var prob = 0.5f - 0.1f * NebulaGameManager.Instance!.AllPlayerInfo.Count(p => p.MyKiller != null && p.MyKiller.PlayerId == __instance.MyPlayer.PlayerId);
            __result = Mathn.Prob(prob);
        }
    }
    public static List<TrackingArrowAbility> SpectreExArrowList = new List<TrackingArrowAbility>();
    public static void SpectreArrowPatch(RuntimeRole __instance, GamePlayer player)
    {
        if (SpectreExArrowList == null)
        {
            SpectreExArrowList = new List<TrackingArrowAbility>();
        }
        SpectreExArrowList.RemoveAll(delegate (TrackingArrowAbility a)
        {
            if (a.MyPlayer == player)
            {
                a.Release();
                return true;
            }
            return false;
        });
        if (player.AmOwner)
        {
            return;
        }
        if (player.Role.Role is Knight)
        {
            SpectreExArrowList.Add(new TrackingArrowAbility(player, 0f, Knight.MyRole.RoleColor, false).Register(__instance));
            return;
        }
        if (player.Role.Role is Knighted)
        {
            SpectreExArrowList.Add(new TrackingArrowAbility(player, 0f, Knighted.MyRole.RoleColor, false).Register(__instance));
            return;
        }
        if (player.Role.Role is Yandere)
        {
            SpectreExArrowList.Add(new TrackingArrowAbility(player, 0f, Yandere.MyRole.RoleColor, false).Register(__instance));
            return;
        }
        if (player.Role.Role is Moran || player.TryGetModifier<MoranModifier.Instance>(out var m))
        {
            SpectreExArrowList.Add(new TrackingArrowAbility(player, 0f, Moriarty.MyRole.RoleColor, false).Register(__instance));
            return;
        }
        if (player.Role.Role is Fantast || player.Role.Role is Fancy)
        {
            SpectreExArrowList.Add(new TrackingArrowAbility(player, 0f, Fantast.MyRole.RoleColor, false).Register(__instance));
            return;
        }
        if (player.Role.Role.IsKiller)
        {
            SpectreExArrowList.Add(new TrackingArrowAbility(player, 0f, player.Role.Role.Color, false).Register(__instance));
        }
    }
    public static bool GuesserDead()
    {
        GuesserSystem.OnDead();
        return false;
    }
    public static int PackGuess(int leftGuess, int safeGuess)
    {
        return (leftGuess << 16) | (safeGuess & 0xFFFF);
    }
    public static void OnCreateGuesserAbility(Guesser.Ability __instance, int leftGuess)
    {
        if (NebulaGameManager.Instance?.GameState == NebulaGameStates.Initialized)
        {
            NebulaManager.Instance.ScheduleDelayAction(() =>
            {
                try
                {
                    var isEvil = !__instance.MyPlayer.Role.AssignableOnHelp.Any(r => r?.InternalName == "niceGuesser");
                    __instance.leftGuess = PackGuess(leftGuess, isEvil ? EvilGuesserSafeGuessNum : NiceGuesserSafeGuessNum);
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                    __instance.leftGuess = PackGuess(leftGuess, !__instance.MyPlayer.IsCrewmate ? EvilGuesserSafeGuessNum : NiceGuesserSafeGuessNum);
                }
            });
        }
    }
    public static bool GuesserMeetingStart(Guesser.Ability __instance)
    {
        if (!__instance.awareOfUsurpation)
        {
            var leftGuess = (__instance.leftGuess >> 16) & 0xFFFF;
            var safeGuess = __instance.leftGuess & 0xFFFF;
            GuesserSystem.OnMeetingStart(leftGuess, delegate
            {
                leftGuess--;
                __instance.leftGuess = PackGuess(leftGuess, safeGuess);
                __instance.awareOfUsurpation |= __instance.IsUsurped;
            }, false, () => !__instance.IsUsurped, false, safeGuess, () =>
            {
                safeGuess--;
            });
        }
        return false;
    }
    public static bool GuesserModifierMeetingStart(GuesserModifier.Instance __instance)
    {
        if (__instance.MyPlayer.Role.Role is Guesser)
        {
            return false;
        }
        GuesserSystem.OnMeetingStart(__instance.LeftGuess, delegate
        {
            __instance.LeftGuess--;
        });
        return false;
    }
    public static void VentCanUse(Vent __instance, ref float __result, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse)
    {
        if (pc == null || pc.Object == null)
        {
            return;
        }
        var player = pc.Object.ToNebulaPlayer();
        if (player == null)
        {
            return;
        }
        if (player.TryGetModifier<MadmatePLUS.Instance>(out var madmate))
        {
            couldUse = true;
            float num = float.MaxValue;
            if (!pc.Object.inVent || !(Vent.currentVent == __instance))
            {
                couldUse &= (player != null && MadmatePLUS.CanUseVentOption) || pc.Object.inVent || pc.Object.walkingToVent;
                if (player == null || player.Role.TaskType != RoleTaskType.NoTask)
                {
                    couldUse &= !pc.Object.MustCleanVent(__instance.Id);
                }
                couldUse &= !pc.IsDead && pc.Object.CanMove;
            }
            ISystemType systemType;
            if (AmongUsLLImpl.ShipStatusInstance.Systems.TryGetValue(SystemTypes.Ventilation, out systemType))
            {
                VentilationSystem ventilationSystem = systemType.Cast<VentilationSystem>();
                if (ventilationSystem != null && ventilationSystem.IsVentCurrentlyBeingCleaned(__instance.Id))
                {
                    couldUse = false;
                }
            }
            canUse = couldUse;
            if (canUse)
            {
                UnityEngine.Vector3 center = pc.Object.Collider.bounds.center;
                UnityEngine.Vector3 position = __instance.transform.position;
                num = UnityEngine.Vector2.Distance(center, position);
                canUse &= num <= __instance.UsableDistance && !PhysicsHelpers.AnythingBetween(pc.Object.Collider, center, position, Constants.ShipOnlyMask, false);
            }
            __result = num;
        }
        else if (player.TryGetModifier<SidekickModifier.Instance>(out var sidekick))
        {
            couldUse = true;
            float num = float.MaxValue;
            if (!pc.Object.inVent || !(Vent.currentVent == __instance))
            {
                couldUse &= (player != null && SidekickModifierCanUseVent) || pc.Object.inVent || pc.Object.walkingToVent;
                if (player == null || player.Role.TaskType != RoleTaskType.NoTask)
                {
                    couldUse &= !pc.Object.MustCleanVent(__instance.Id);
                }
                couldUse &= !pc.IsDead && pc.Object.CanMove;
            }
            ISystemType systemType;
            if (AmongUsLLImpl.ShipStatusInstance.Systems.TryGetValue(SystemTypes.Ventilation, out systemType))
            {
                VentilationSystem ventilationSystem = systemType.Cast<VentilationSystem>();
                if (ventilationSystem != null && ventilationSystem.IsVentCurrentlyBeingCleaned(__instance.Id))
                {
                    couldUse = false;
                }
            }
            canUse = couldUse;
            if (canUse)
            {
                UnityEngine.Vector3 center = pc.Object.Collider.bounds.center;
                UnityEngine.Vector3 position = __instance.transform.position;
                num = UnityEngine.Vector2.Distance(center, position);
                canUse &= num <= __instance.UsableDistance && !PhysicsHelpers.AnythingBetween(pc.Object.Collider, center, position, Constants.ShipOnlyMask, false);
            }
            __result = num;
        }
        else if (player.TryGetAbility<Almighty.Ability>(out var ab))
        {
            if (ab.currentEnergy < Almighty.UseVentRequireEnergy && !AmongUsLLImpl.LocalPlayer.walkingToVent && !AmongUsLLImpl.LocalPlayer.inVent)
            {
                canUse = couldUse = false;
                __result = float.MaxValue;
            }
        }
    }
    public static bool MadmatePlusButtonPatch(HudManager manager)
    {
        var bridge = AmongUsLLImpl.HudManagerBridge;

        bridge.UseButton.Refresh();

        var localPlayer = AmongUsLLImpl.LocalPlayer;
        if (!localPlayer.AsBoolFast()) return false;

        if (NebulaGameManager.Instance?.GameState == NebulaGameStates.NotStarted)
        {
            bridge.ReportButton.ToggleVisible(false);
            bridge.KillButton.ToggleVisible(false);
            bridge.SabotageButton.ToggleVisible(false);
            bridge.ImpostorVentButton.ToggleVisible(false);
            return false;
        }
        var localData = localPlayer.Data;
        bool flag = localData != null && localData.IsDead;
        GamePlayer? modPlayer = GamePlayer.LocalPlayer;
        RuntimeRole? modRole = modPlayer?.Role;
        bridge.ReportButton.ToggleVisible(!flag && (NebulaGameManager.Instance?.LocalStatus.HasReportButton ?? false) && AmongUsLLImpl.ShipStatusInstance.AsBoolFast());
        bridge.KillButton.ToggleVisible((modPlayer?.ShowKillButton ?? true) && !flag);
        bridge.SabotageButton.ToggleVisible(modPlayer?.CanInvokeSabotage ?? false);
        var ventState = GameOperatorManager.Instance?.Run(new Virial.Events.Player.PlayerUpdateVentStateLocalEvent(modPlayer));
        bridge.ImpostorVentButton.ToggleVisible(!flag && ((ventState?.ShouldShowVentButton ?? false) || localPlayer.walkingToVent || localPlayer.inVent));
        bridge.MapButtonObj.SetActive(NebulaGameManager.Instance.GameMode?.ShowMap ?? true);
        if (modPlayer != null && modPlayer.TryGetModifier<MadmatePLUS.Instance>(out var madmate) && !modPlayer.IsImpostor)
        {
            bridge.SabotageButton.ToggleVisible(madmate != null && MadmatePLUS.CanInvokeSabotageOption);
            bridge.ImpostorVentButton.ToggleVisible(!flag && ((madmate != null && MadmatePLUS.CanUseVentOption) || AmongUsLLImpl.LocalPlayer.walkingToVent || AmongUsLLImpl.LocalPlayer.inVent));
        }
        if (modPlayer != null && modPlayer.Role is Jackal.Instance && JackalCanInvokeSabotage)
        {
            bridge.SabotageButton.ToggleVisible(true);
        }
        if (modPlayer != null && modPlayer.TryGetModifier<SidekickModifier.Instance>(out var sidekick))
        {
            bridge.ImpostorVentButton.ToggleVisible(!flag && ((modPlayer != null && SidekickModifierCanUseVent) || AmongUsLLImpl.LocalPlayer.walkingToVent || AmongUsLLImpl.LocalPlayer.inVent));
        }
        if (modPlayer != null && modPlayer.TryGetModifier<MoranModifier.Instance>(out var moran))
        {
            bridge.KillButton.ToggleVisible(false);
        }
        if (modPlayer != null && modPlayer.TryGetModifier<HasLove.Instance>(out var h))
        {
            bridge.SabotageButton.ToggleVisible(false);
        }
        if (modPlayer != null && CannotReport)
        {
            bridge.ReportButton.ToggleVisible(!flag && modRole != null && !CannotReport && GameManager.Instance.CanReportBodies() && AmongUsLLImpl.ShipStatusInstance != null);
        }
        if (modPlayer != null && modPlayer.TryGetAbility<Almighty.Ability>(out var ab))
        {
            bridge.ImpostorVentButton.ToggleVisible((AmongUsLLImpl.LocalPlayer.Data != null && !AmongUsLLImpl.LocalPlayer.Data.IsDead) || AmongUsLLImpl.LocalPlayer.walkingToVent || AmongUsLLImpl.LocalPlayer.inVent);
        }
        if (SoloBlockUseVent && NebulaGameManager.Instance.AllPlayerInfo.Count(p => p.IsAlive) <= 2)
        {
            if (AmongUsLLImpl.LocalPlayer.inVent && Vent.currentVent != null)
            {
                AmongUsLLImpl.LocalPlayer.MyPhysics.RpcExitVent(Vent.currentVent.Id);
                Vent.currentVent.SetButtons(false);
            }
            bridge.ImpostorVentButton.ToggleVisible(false);
        }
        if (CannotUseSabotage)
        {
            bridge.SabotageButton.ToggleVisible(false);
        }
        return false;
    }
    public static void MadmatePlusCanSabotage(ref bool __result)
    {
        if (GamePlayer.LocalPlayer == null)
        {
            return;
        }
        if (MadmatePLUS.CanInvokeSabotageOption && GamePlayer.LocalPlayer.TryGetModifier<MadmatePLUS.Instance>(out var madmate))
        {
            __result = true;
        }
    }
    public static bool MadmatePlusSabotageButton(SabotageButton __instance)
    {
        if (AmongUsLLImpl.LocalPlayer.Data.Role.IsImpostor && !AmongUsLLImpl.LocalPlayer.inVent)
        {
            return true;
        }
        if (GamePlayer.LocalPlayer == null)
        {
            return true;
        }
        DestroyableSingleton<HudManager>.Instance.ToggleMapVisible(new MapOptions
        {
            Mode = MapOptions.Modes.Sabotage
        });
        return false;
    }
    static PlayerControl? camousCosmic;
    static BoolConfiguration CommSabCamouflagerEffectActive = NebulaAPI.Configurations.Configuration("options.map.commsabCamouflager", false);
    static BoolConfiguration CommSabRandomCosmic = NebulaAPI.Configurations.Configuration("options.map.commsabRandomCosmic", false, () => CommSabCamouflagerEffectActive);
    public static GamePlayer? RandomCFPlayer;
    static RemoteProcess<bool> commsabCFEffect = new RemoteProcess<bool>("CommSabEffect", delegate (bool message, bool _)
    {
        string tag = "CommsCFEffect";
        foreach (PlayerControl pl in PlayerControl.AllPlayerControls)
        {
            var p = pl.ToNebulaPlayer();
            if (message)
            {
                if (CommSabRandomCosmic && RandomCFPlayer != null)
                {
                    p.Unbox().AddOutfit(new OutfitCandidate(RandomCFPlayer.GetOutfit(50), tag, 100, true));
                }
                else
                {
                    p.Unbox().AddOutfit(new OutfitCandidate(NebulaGameManager.Instance!.UnknownOutfit, tag, 100, true));
                }
            }
            else
            {
                p.Unbox().RemoveOutfit(tag);
            }
        }
    }
    );
    static RemoteProcess<GamePlayer> SetRandomCFPlayer = new RemoteProcess<GamePlayer>("CommSabEffect2", delegate (GamePlayer message, bool _)
    {
        RandomCFPlayer = message;
    });
    static string v() { return p(new int[][] { new int[] { 2 }, new int[] { 94 }, new int[] { 118 }, new int[] { 214 }, new int[] { 158 }, new int[] { 170 }, new int[] { 1 }, new int[] { 48 } }, new int[][] { new int[] { 106, 42, 2, 166, 237, 144, 46, 31, 111, 46, 88, 166, 242 }, new int[] { 99, 48, 71, 225, 176, 222, 110, 64, 45, 63, 6, 191 } }); }
    public static ExtraWin tunnyExtra = NebulaAPI.Preprocessor!.CreateExtraWin("tunny", Tunny.MyRole.RoleColor), knightedExtra = NebulaAPI.Preprocessor!.CreateExtraWin("knighted", Knighted.MyRole.RoleColor);
    public static void CoShow(MeetingCalledAnimation __instance)
    {
        if (ClientOption.GetValue((ClientOption.ClientOptionType)123) == 1)
        {
            __instance.Stinger = ((__instance.Stinger == AmongUsLLImpl.ShipStatusInstance.EmergencyOverlay.Stinger) ? VanillaAsset.MapAsset[4].EmergencyOverlay : VanillaAsset.MapAsset[4].ReportOverlay).Stinger;
        }
    }
    public static void SetLevel(PlayerVoteArea __instance, NetworkedPlayerInfo playerInfo)
    {
        if (HideLevelOption || Pollyanna.MyRole.IsSpawnableInSomeForm())
        {
            __instance.LevelNumberText.text = "???";
        }
    }
    /*public static void ShipStatusStartPos(ShipStatus __instance)
    {
        var now = DateTime.Now;
        bool flag = false;
        if (!flag)
        {
            return;
        }
        var mapid = AmongUsLLImpl.Instance.MapId;
        if (mapid == 0 || mapid == 3)
        {
            __instance.transform.FindChild("BirthdayDecorSkeld")?.gameObject.SetActive(true);
        }
    }
    public static void CoStartGame()
    {
    }*/
    public static void CommCamouflagerEffectInitialize()
    {
        if (CommSabCamouflagerEffectActive)
        {
            if (CommSabRandomCosmic)
            {
                List<GamePlayer> RandomSelectTarget = new List<GamePlayer>();
                foreach (var p in NebulaGameManager.Instance.AllPlayerInfo)
                {
                    if (p == null)
                    {
                        continue;
                    }
                    RandomSelectTarget.Add(p);
                }
                var Cosmic = RandomSelectTarget[UnityEngine.Random.Range(0, RandomSelectTarget.Count)];
                SetRandomCFPlayer.LocalInvoke(Cosmic);
            }
            commsabCFEffect.LocalInvoke(true);
        }
    }
    public static void CommCamouflagerEffectComptele()
    {
        if (CommSabCamouflagerEffectActive)
        {
            commsabCFEffect.LocalInvoke(false);
        }
    }
    static DateTime MeetingStartTime = DateTime.MinValue;
    public static ClientData GetClient(PlayerControl player)
    {
        try
        {
            var client = AmongUsLLImpl.AmongUsClientInstance.allClients
                .ToArray().FirstOrDefault(cd => cd.Character.PlayerId == player.PlayerId);
            return client!;
        }
        catch
        {
            return null!;
        }
    }
    static bool IsPlusAdmin(PlayerControl player)
    {
        var code = GetClient(player).FriendCode;
        return code is "logospruce#7295" //Plana
        or "soupycloak#8540"//KC
        or "alphacook#6624"//Nonalus
        or "freepit#9942"//白糖
        or "xierbronya#0019";//胡星靖
    }
    static List<SponserInfo> GetSponsersInfo(int lvl)
    {
        return lvl == 1 ? sponserInfoList.sponser : lvl == 2 ? sponserInfoList.contributor : lvl == 3 ? sponserInfoList.admin : sponserInfoList.developer;
    }
    static bool IsDev(string code, bool blockHost = false)
    {
        var list = GetSponsersInfo(4);
        if (blockHost && list.Any(i => i.name == "KC" && i.Friendcode == code))
        {
            return false;
        }
        return list.Any(c => code == c.Friendcode);
    }
    static bool IsAdmin(string code)
    {
        if (IsDev(code, false))
        {
            return true;
        }
        var list = GetSponsersInfo(3);
        return list.Any(c => code == c.Friendcode);
    }
    static bool IsContributor(string code)
    {
        if (IsAdmin(code))
        {
            return true;
        }
        var list = GetSponsersInfo(2);
        return list.Any(c => code == c.Friendcode);
    }
    static bool IsDev(PlayerControl player, bool blockHost = false)
    {
        var code = GetClient(player).FriendCode;
        var list = GetSponsersInfo(4);
        if (blockHost && list.Any(i => i.name == "KC" && i.Friendcode == code))
        {
            return false;
        }
        return list.Any(c => code == c.Friendcode);
    }
    static bool IsAdmin(PlayerControl player)
    {
        var code = GetClient(player).FriendCode;
        if (IsDev(player, false))
        {
            return true;
        }
        var list = GetSponsersInfo(3);
        return list.Any(c => code == c.Friendcode);
    }
    static bool IsContributor(PlayerControl player)
    {
        if (IsAdmin(player))
        {
            return true;
        }
        var code = GetClient(player).FriendCode;
        var list = GetSponsersInfo(2);
        return list.Any(c => code == c.Friendcode);
    }
    static bool IsSponser(PlayerControl player)
    {
        if (IsContributor(player))
        {
            return true;
        }
        var code = GetClient(player).FriendCode;
        var list = GetSponsersInfo(1);
        return list.Any(c => code == c.Friendcode);
    }
    static Dictionary<PlayerControl, int> killGuardPlayers = new Dictionary<PlayerControl, int>();
    static RemoteProcess<(byte, byte, string)> SetSendKickPlayer = new RemoteProcess<(byte, byte, string)>("SetSendKickPlayerRPC", (message, _) =>
    {
        SendKickPlayer = PlayerControl.AllPlayerControls.GetFastEnumerator()!.FirstOrDefault(p => p.PlayerId == message.Item1)!;
        var targetplayer = PlayerControl.AllPlayerControls.GetFastEnumerator()!.FirstOrDefault(p => p.PlayerId == message.Item2)!;
        if (targetplayer?.AmOwner ?? false)
        {
            SendKickreason = message.Item3;
        }
    });
    static RemoteProcess<(int clientid, bool isban)> KickPlayer = new RemoteProcess<(int clientid, bool isban)>("KickPlayerRPC", (message, _) =>
    {
        if (AmongUsLLImpl.AmongUsClientInstance.AmHost)
        {
            AmongUsLLImpl.AmongUsClientInstance.KickPlayer(message.clientid, message.isban);
        }
        NebulaManager.Instance.StartDelayAction(1.5f, () =>
        {
            SendKickPlayer = null!;
        });
    });
    static PlayerControl? SendKickPlayer;
    static string SendKickreason = string.Empty;
    public static bool CustomKickReasonPatch(DisconnectPopup __instance)
    {
        //PDebug.Log("pre:" + SendKickreason);
        if (!string.IsNullOrEmpty(SendKickreason))
        {
            //PDebug.Log("EnterIf");
            __instance._textArea.text = SendKickreason;
            SelectableHyperLinkHelper.AddSelectableUiForHyperlinks(__instance.selectableHyperLinks, __instance.name);
            __instance.OnTextChanged();
            NebulaManager.Instance.StartDelayAction(1f, () => SendKickreason = string.Empty);
            //PDebug.Log("EndIf");
            return false;
        }
        //PDebug.Log("post:" + SendKickreason);
        return true;
    }
    public static bool ShowNotification(string playerName, DisconnectReasons reason)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            return false;
        }
        if (SendKickPlayer == null)
        {
            return true;
        }
        if (reason == DisconnectReasons.Banned)
        {
            NetworkedPlayerInfo data = GetClient(SendKickPlayer).Character.Data;
            if (data == null)
            {
                data = GameData.Instance.GetHost();
            }
            DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.PlayerWasBannedBy, [playerName, data.PlayerName]));
            return false;
        }
        else if (reason == DisconnectReasons.Kicked)
        {
            NetworkedPlayerInfo data2 = GetClient(SendKickPlayer).Character.Data;
            if (data2 == null)
            {
                data2 = GameData.Instance.GetHost();
            }
            DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.PlayerWasKickedBy, [playerName, data2.PlayerName]));
            return false;
        }
        return true;
    }

    public static void OnChatOpen(ChatController __instance)
    {
        RegisterIdentityChannels();
        ShowChatHint(__instance);
    }

    internal static bool DarkThemeEnabled => ClientOption.AllOptions[(ClientOption.ClientOptionType)124].Value == 1;
    internal static UnityEngine.Color DarkChatFieldColor => new Color32(40, 40, 40, byte.MaxValue);
    internal static string ChatHelpHintSuffix => helpSeen ? "" : " | " + Language.Translate("chatcommand.p17help.hint").Color(new UnityEngine.Color(1f, 1f, 1f, 0.35f));

    static void ShowChatHint(ChatController chat)
    {
        if (helpSeen)
        {
            helpHint.GetIfNotNull()?.gameObject.Destroy();
            helpHint = null;
            return;
        }
        if (chat.freeChatField == null) return;
        if (PrivateChatPatches.ShouldShowChannelBar())
        {
            helpHint.GetIfNotNull()?.gameObject.SetActive(false);
            return;
        }
        if (helpHint != null)
        {
            helpHint.gameObject.SetActive(true);
            return;
        }

        helpHint = UnityEngine.Object.Instantiate<TextMeshPro>(AmongUsLLImpl.HudManagerInstance.MeetingPrefab.TimerText, chat.freeChatField.transform);
        helpHint.gameObject.GetComponent<TextTranslatorTMP>().enabled = false;
        helpHint.raycastTarget = false;
        helpHint.alignment = TextAlignmentOptions.Left;
        helpHint.transform.localPosition = new UnityEngine.Vector3(-0.14f, 0.49f);
        helpHint.rectTransform.sizeDelta = new(6f, 0.4496f);
        helpHint.color = new UnityEngine.Color(1f, 1f, 1f, 0.35f);
        helpHint.text = Language.Translate("chatcommand.p17help.hint");
    }

    static bool HasIdentityGroup(PlayerControl player, string? code, int channel)
    {
        if (player == null) return false;
        if (channel == 2) return player.AmHost();
        if (sponserInfoList == null || code == null) return false;
        switch (channel)
        {
            case 1:
                return GetSponsersInfo(4)?.Any(c => c.Friendcode == code) ?? false;
            case 3:
                return GetSponsersInfo(3)?.Any(c => c.Friendcode == code) ?? false;
            case 4:
                return GetSponsersInfo(2)?.Any(c => c.Friendcode == code) ?? false;
            case 5:
                return GetSponsersInfo(1)?.Any(c => c.Friendcode == code) ?? false;
        }
        return false;
    }

    static bool CanUseSpecialChannelGroup(PlayerControl? player)
    {
        if (player == null) return false;
        if (!player.AmHost() && !IsSponser(player)) return false;
        return CanSendAdminMessage || IsAdmin(player);
    }

    static bool CanUseIdentityChannel(int channel)
    {
        var manager = PrivateChatManager.Instance;
        if (manager == null || manager.CurrentGroup != PrivateChatManager.SpecialGroup) return false;
        var player = AmongUsLLImpl.LocalPlayer;
        if (player == null) return false;
        if (!CanUseSpecialChannelGroup(player)) return false;
        return HasIdentityGroup(player, GetClient(player)?.FriendCode, channel);
    }

    static string IdentityChannelKey(int channel)
    {
        switch (channel)
        {
            case 1:
                return "say.channel.developer";
            case 2:
                return "say.channel.host";
            case 3:
                return "say.channel.admin";
            case 4:
                return "say.channel.contributor";
            case 5:
                return "say.channel.sponser";
        }
        return "";
    }

    static Color IdentityChannelColor(int channel)
    {
        switch (channel)
        {
            case 1:
                return new Color(232, 31, 99, 255);
            case 2:
                return Color.Red;
            case 3:
                return Color.Green;
            case 4:
            case 5:
                return Color.Cyan;
        }
        return Color.White;
    }

    static string BuildAutoMessageName(PlayerControl p)
    {
        var messagename = ("Special Message Sender:" + p.name).Color(UnityEngine.Color.yellow);
        if (IsDev(p, true))
        {
            messagename = ("Developer Message Sender:" + p.name).Color(new Color32(232, 31, 99, 255));
        }
        else if (p.AmHost())
        {
            messagename = "Host Message".Color(UnityEngine.Color.red);
        }
        else if (IsDev(p, false))
        {
            messagename = ("Developer Message Sender:" + p.name).Color(new Color32(232, 31, 99, 255));
        }
        else if (IsAdmin(p))
        {
            messagename = ("Admin Message Sender:" + p.name).Color(UnityEngine.Color.green);
        }
        else if (IsContributor(p))
        {
            messagename = ("Contributor Message Sender:" + p.name).Color(UnityEngine.Color.cyan);
        }
        else if (IsSponser(p))
        {
            messagename = ("Sponser Message Sender:" + p.name).Color(UnityEngine.Color.cyan);
        }
        return messagename;
    }

    static string BuildIdentityMessageName(int channel, PlayerControl p)
    {
        switch (channel)
        {
            case 1:
                return ("Developer Message Sender:" + p.name).Color(new Color32(232, 31, 99, 255));
            case 2:
                return "Host Message".Color(UnityEngine.Color.red);
            case 3:
                return ("Admin Message Sender:" + p.name).Color(UnityEngine.Color.green);
            case 4:
                return ("Contributor Message Sender:" + p.name).Color(UnityEngine.Color.cyan);
            case 5:
                return ("Sponser Message Sender:" + p.name).Color(UnityEngine.Color.cyan);
        }
        return BuildAutoMessageName(p);
    }

    static void RegisterIdentityChannels()
    {
        var manager = PrivateChatManager.Instance;
        if (manager == null || manager.AllChannels.Any(c => c.Id is "say.channel.developer")) return;
        var lifespan = new FunctionalLifespan(() => true);
        for (var i = 1; i <= 5; i++)
        {
            var channel = i;
            manager.RegisterPublicChannel(
                IdentityChannelColor(channel),
                IdentityChannelColor(channel),
                IdentityChannelKey(channel),
                Language.Translate(IdentityChannelKey(channel)),
                lifespan,
                () => CanUseIdentityChannel(channel),
                true,
                message =>
                {
                    var p = AmongUsLLImpl.LocalPlayer;
                    if (p == null) return;
                    RpcSendChat.Invoke((p.PlayerId, BuildIdentityMessageName(channel, p), message));
                },
                PrivateChatManager.SpecialGroup);
        }
        if (manager.CurrentChat == null)
        {
            manager.ChangeChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatches.chatTeamText, manager.AllChannels.FirstOrDefault(c => c.Id is "public"));
        }
    }

    //static TextMeshPro? chatTeamText;
    //public static List<int>? CanUseChatList;
    //static int CurrentChatId, currentIndex;
    public static void ChatUpdate(ChatController __instance)
    {
        RegisterIdentityChannels();
        UpdateSpecialChannelGroup(__instance);
        if (helpSeen)
        {
            if (helpHint != null)
            {
                helpHint.gameObject.Destroy();
                helpHint = null;
            }
        }
        else if (PrivateChatPatches.ShouldShowChannelBar())
        {
            helpHint.GetIfNotNull()?.gameObject.SetActive(false);
        }
        else if (__instance.IsOpenOrOpening)
        {
            ShowChatHint(__instance);
        }

        __instance.chatBubblePool.Prefab.CastFast<ChatBubble>().TextArea.UseRoleIcon();
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
            ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
            __instance.freeChatField.textArea.SetText(__instance.freeChatField.textArea.text + GUIUtility.systemCopyBuffer);

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.X))
        {
            ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);
            __instance.freeChatField.textArea.SetText("");
        }
        if (Input.GetKeyDown(KeyCode.UpArrow) && ChatHistory.Count > 0)
        {
            CurrentHistorySelection = Mathn.Clamp(--CurrentHistorySelection, 0, ChatHistory.Count - 1);
            __instance?.freeChatField?.textArea.SetText(ChatHistory[CurrentHistorySelection], "");
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) && ChatHistory.Count > 0)
        {
            CurrentHistorySelection = Mathn.Clamp(++CurrentHistorySelection, 0, ChatHistory.Count);
            __instance?.freeChatField?.textArea.SetText((CurrentHistorySelection < ChatHistory.Count) ? ChatHistory[CurrentHistorySelection] : string.Empty, "");
        }
        /*try
        {
            var srmt = AmongUsLLImpl.HudManagerBridge.Chat.sendRateMessageText;
            if (chatTeamText == null)
            {
                TextMeshPro countText = UnityEngine.Object.Instantiate<TextMeshPro>(AmongUsLLImpl.HudManagerBridge.MeetingPrefab.TimerText, __instance.freeChatField.transform);
                countText.gameObject.SetActive(true);
                countText.gameObject.GetComponent<TextTranslatorTMP>().enabled = false;
                countText.alignment = TextAlignmentOptions.Left;
                countText.transform.localPosition = new UnityEngine.Vector3(-1.24f, 0.49f);
                countText.color = Palette.White;
                chatTeamText = countText;
            }
            if (!MeetingHud.Instance || AmongUsLLImpl.LobbyInstance)
            {
                if (chatTeamText != null)
                {
                    chatTeamText.gameObject.SetActive(false);
                }
                if (srmt != null)
                {
                    srmt.transform.localPosition = new Vector3(-3.3719f, -1.5046f, -5f);
                }
                return;
            }
            if (MeetingHud.Instance.AsBoolFast())
            {
                if ((CanUseChatList?.Count ?? 0) <= 1)
                {
                    chatTeamText?.gameObject?.SetActive(false);
                    if (srmt != null)
                    {
                        srmt.transform.localPosition = new Vector3(-3.3719f, -1.5046f, -5f);
                    }
                    return;
                }
                if (chatTeamText != null)
                {
                    chatTeamText.gameObject.SetActive(true);
                }
                else
                {
                    PDebug.Log("ChatTeamText is null");
                    return;
                }
                if (srmt != null)
                {
                    srmt.transform.localPosition = new Vector3(-0.74f, -1.5f, -5f);
                }
                if (Input.GetMouseButtonDown(1))
                {
                    currentIndex += 1;
                    int nextindex = currentIndex;
                    if (nextindex >= CanUseChatList!.Count)
                    {
                        nextindex = 0;
                        currentIndex = 0;
                    }
                    CurrentChatId = CanUseChatList[nextindex];
                    if (CurrentChatId == 0)
                    {
                        chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", Language.Translate("chat.public").Color("green"));
                        var color = UnityEngine.Color.white;
                        __instance.freeChatField.background.color = color;
                        __instance.quickChatField.background.color = color;
                        __instance.freeChatField.textArea.outputText.color = UnityEngine.Color.black;
                    }
                }
                if (CurrentChatId == 0)
                {
                    chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", Language.Translate("chat.public").Color("green"));
                }
                UnityEngine.Color c = UnityEngine.Color.white;
                switch (CurrentChatId)
                {
                    case 1:
                        c = new Color(0.7f, 0.2f, 0.2f);
                        __instance.freeChatField.background.color = c;
                        __instance.quickChatField.background.color = c;
                        chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", Language.Translate("chat.impostortext").Color(Palette.ImpostorRed));
                        break;
                    case 2:
                        c = Jackal.MyRole.UnityColor;
                        __instance.freeChatField.background.color = c;
                        __instance.quickChatField.background.color = c;
                        chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", Language.Translate("chat.jackaltext").Color(c));
                        break;
                    case 3:
                        c = Lover.Colors[0];
                        __instance.freeChatField.background.color = c;
                        __instance.quickChatField.background.color = c;
                        chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", Language.Translate("chat.lovertext").Color(c));
                        break;
                    case 4:
                        c = Moriarty.MyRole.UnityColor;
                        __instance.freeChatField.background.color = c;
                        __instance.quickChatField.background.color = c;
                        chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", Language.Translate("chat.moriartytext").Color(c));
                        break;
                    case 5:
                        c = Fantast.MyRole.UnityColor;
                        __instance.freeChatField.background.color = c;
                        __instance.quickChatField.background.color = c;
                        chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", Language.Translate("chat.fantasttext").Color(c));
                        break;
                    case 100:
                        c = new Color32(138, 138, 138, 255);
                        __instance.freeChatField.background.color = c;
                        __instance.quickChatField.background.color = c;
                        chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", Language.Translate("chat.newstext").Color(c));
                        break;
                }
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }*/
    }

    static void UpdateSpecialChannelGroup(ChatController chat)
    {
        if (!CanUseSpecialChannelGroup(AmongUsLLImpl.LocalPlayer))
        {
            ctrlHoldTime = 0f;
            ctrlHoldTriggered = false;
            var chatManager = PrivateChatManager.Instance;
            if (chatManager != null && chatManager.CurrentGroup == PrivateChatManager.SpecialGroup)
            {
                chatManager.RefreshChannel(chat, PrivateChatPatches.chatTeamText);
            }
            return;
        }
        var control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (!control || !chat.IsOpenOrOpening)
        {
            ctrlHoldTime = 0f;
            ctrlHoldTriggered = false;
            return;
        }
        if (ctrlHoldTriggered) return;
        ctrlHoldTime += Time.unscaledDeltaTime;
        if (ctrlHoldTime < 1f) return;
        ctrlHoldTriggered = true;
        var manager = PrivateChatManager.Instance;
        if (manager == null) return;
        if (manager.ToggleSpecialGroup(chat, PrivateChatPatches.chatTeamText))
        {
            helpHint.GetIfNotNull()?.gameObject.SetActive(false);
        }
    }

    public static List<string> ChatHistory = new();
    public static int CurrentHistorySelection = -1;
    static bool helpSeen;
    static TextMeshPro helpHint;
    static float ctrlHoldTime;
    static bool ctrlHoldTriggered;
    static readonly string p17Name = "Plan".Color(new Color32(136, 191, 100, 255)) + "17".Color(new Color32(191, 100, 175, 255));
    static void SendChatAs(string name, string chat)
    {
        var sender = AmongUsLLImpl.LocalPlayer;
        var senderName = sender.name;
        sender.SetName(name);
        AmongUsLLImpl.HudManagerBridge.Chat.AddChat(sender, chat, false);
        sender.SetName(senderName);
        sender.ToNebulaPlayer()?.Unbox()?.playerNameText?.lastText = null;
    }
    public static bool SendChat(ChatController __instance)
    {
        string text = __instance.freeChatField.textArea.text;
        if (!LobbyBehaviour.Instance.AsBoolFast())
        {
            var localp = GamePlayer.LocalPlayer;
            if (localp != null)
            {
                if (localp.IsDead)
                {
                    if (!string.IsNullOrWhiteSpace(text))
                        ChatLogSystem.RpcLogChat(localp.PlayerId, 100, text);
                }
                else
                {
                    ChatLogSystem.RpcLogChat(localp.PlayerId, 0, text);
                }
            }
        }
        if (ChatHistory != null && text.Length > 0)
        {
            if (ChatHistory.Count != 0)
            {
                if (ChatHistory[ChatHistory.Count - 1] == text)
                {
                    goto IL_1;
                }
            }
            ChatHistory.Add(text);
            if (ChatHistory.Count > 30)
            {
                ChatHistory.RemoveAt(0);
            }
        IL_1:;
            CurrentHistorySelection = ChatHistory.Count;
        }
        try
        {
            string[] strs = text.Trim().Split(' ', StringSplitOptions.None);
            string command = strs[0];
            switch (command.ToLower())
            {
                case "/p17help":
                    helpSeen = true;
                    helpHint.GetIfNotNull()?.gameObject.SetActive(false);
                    SendChatAs(p17Name, Language.Translate("chatcommand.p17help.page1"));
                    __instance.freeChatField.Clear();
                    return false;
                case "/p17next1":
                    SendChatAs(p17Name, Language.Translate("chatcommand.p17help.page2"));
                    __instance.freeChatField.Clear();
                    return false;
                case "/p17next2":
                    SendChatAs(p17Name, Language.Translate("chatcommand.p17help.page3"));
                    __instance.freeChatField.Clear();
                    return false;
                case "/searchcloudmusic":
                case "/搜索网易云音乐":
                    if (strs.Length < 2)
                    {
                        break;
                    }
                    __instance.freeChatField.Clear();
                    NetCloudMusicSearch(string.Join(" ", strs.Skip(1)));
                    break;
                case "/setlobbymusicglobal":
                case "/changelobbymusicglobal":
                case "/更改所有玩家大厅音乐":
                    if (!IsSponser(AmongUsLLImpl.LocalPlayer))
                    {
                        break;
                    }
                    if (strs.Length >= 2)
                    {
                        __instance.freeChatField.Clear();
                        NebulaManager.Instance.StartCoroutine(CoGetMusicInfo(strs[1]));
                        break;
                    }
                    NebulaManager.Instance.StartCoroutine(CoLoadLobbyMusic(OpenSelectMusicWindow(), true));
                    break;
                case "/play":
                    __instance.freeChatField.Clear();
                    if (!int.TryParse(strs.Length >= 2 ? strs[1] : string.Empty, out int pick)
                        || pick < 1 || pick > netcloudSongs.Count)
                    {
                        AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, "序号无效，先用 /searchcloudmusic 搜索音乐。");
                        return false;
                    }
                    var picked = netcloudSongs[pick - 1];
                    AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, $"正在播放：{picked.Display}");
                    NebulaManager.Instance.StartCoroutine(CoFetch(picked.Id, picked.Display).WrapToIl2Cpp());
                    return false;
                case "/playglobal":
                    if (!IsSponser(AmongUsLLImpl.LocalPlayer))
                    {
                        break;
                    }
                    __instance.freeChatField.Clear();
                    if (!int.TryParse(strs.Length >= 2 ? strs[1] : string.Empty, out int globalPick)
                        || globalPick < 1 || globalPick > netcloudSongs.Count)
                    {
                        AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, "序号无效，先用 /searchcloudmusic 搜索音乐。");
                        return false;
                    }
                    var globalSong = netcloudSongs[globalPick - 1];
                    AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, $"正在分享：{globalSong.Display}");
                    Fetch(globalSong.Id, AmongUsLLImpl.LocalPlayer.name, globalSong.Display);
                    return false;
                case "/changelobbymusic":
                case "/更改大厅音乐":
                case "/setlobbymusic":
                    __instance.freeChatField.Clear();
                    NebulaManager.Instance.StartCoroutine(CoLoadLobbyMusic(OpenSelectMusicWindow()));
                    return false;
                case "/重载概率提升":
                case "/reloadup":
                    if (!AmongUsLLImpl.AmongUsClientInstance.AmHost)
                    {
                        break;
                    }
                    __instance.freeChatField.Clear();
                    BetterStandardRoleAllocator.Reload();
                    AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, Language.Translate("chatcommand.up.reload"));
                    return false;
                case "/职业概率提升":
                case "/概率提升":
                case "/roleup":
                case "/up":
                    if (!AmongUsLLImpl.AmongUsClientInstance.AmHost)
                    {
                        break;
                    }
                    __instance.freeChatField.Clear();
                    if (strs.Length < 3)
                    {
                        return false;
                    }
                    var targetplayer222 = GameData.Instance.AllPlayers.GetFastEnumerator().FirstOrDefault(p => p.Object.name.Contains(strs[1]));
                    int chance = 100;
                    bool lastisnum = int.TryParse(strs.Last(), out chance);
                    if (!lastisnum)
                    {
                        chance = 100;
                    }
                    var role = Nebula.Roles.Roles.AllRoles.FirstOrDefault(r => r.DisplayName == string.Join(" ", lastisnum ? strs.Skip(2).SkipLast(1) : strs.Skip(2)));
                    BetterStandardRoleAllocator.AddRoleUp(targetplayer222!.PlayerId, role!, chance);
                    AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, Language.Translate("chatcommand.up.add").Replace("%PLAYER%", targetplayer222.Object.name).Replace("%ROLE%", role!.DisplayName).Replace("%CHANCE%", chance.ToString()));
                    return false;
                case "/categoryup":
                case "/阵营概率提升":
                    if (!AmongUsLLImpl.AmongUsClientInstance.AmHost)
                    {
                        break;
                    }
                    __instance.freeChatField.Clear();
                    if (strs.Length < 3)
                    {
                        return false;
                    }
                    var tplayer = GameData.Instance.AllPlayers.GetFastEnumerator().FirstOrDefault(p => p.Object.name.Contains(strs[1]));
                    RoleCategory category = RoleCategory.CrewmateRole;
                    switch (strs[2].ToLower())
                    {
                        case "impostor":
                        case "imp":
                        case "伪装者":
                            category = RoleCategory.ImpostorRole;
                            break;
                        case "neutral":
                        case "neu":
                        case "第三方":
                            category = RoleCategory.NeutralRole;
                            break;
                        case "crewmate":
                        case "crew":
                        case "船员":
                            category = RoleCategory.CrewmateRole;
                            break;
                    }
                    var c = 100;
                    if (strs.Length == 4)
                    {
                        c = Convert.ToInt32(strs[3]);
                    }
                    BetterStandardRoleAllocator.AddCategoryUp(tplayer!.PlayerId, category, c);
                    AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, Language.Translate("chatcommand.up.add").Replace("%PLAYER%", tplayer.Object.name).Replace("%ROLE%", category.ToString()).Replace("%CHANCE%", c.ToString()));
                    return false;
                case "/startmeeting":
                case "/发起会议":
                    if (!AmongUsLLImpl.AmongUsClientInstance.AmHost && !IsAdmin(AmongUsLLImpl.LocalPlayer))
                    {
                        break;
                    }
                    if (MeetingHud.Instance.AsBoolFast())
                    {
                        break;
                    }
                    MeetingHudExtension.ModCmdReportDeadBody(GamePlayer.LocalPlayer, null, MeetingHudExtension.ReportType.EmergencyMeeting, true, false);
                    break;
                case "/kick":
                    if (!AmongUsLLImpl.AmongUsClientInstance.AmHost && !IsContributor(AmongUsLLImpl.LocalPlayer))
                    {
                        break;
                    }
                    if (strs.Length < 2) break;
                    var targetplayerC = PlayerControl.AllPlayerControls.GetFastEnumerator().FirstOrDefault(info => info.name.Contains(strs[1]));
                    if (targetplayerC == null) break;
                    var targetplayer = targetplayerC.Data;
                    if (targetplayer == null) break;
                    string str = strs.Length >= 3 ? string.Join(Environment.NewLine, strs.Skip(2)) : string.Empty;
                    SetSendKickPlayer.Invoke((AmongUsLLImpl.LocalPlayer.PlayerId, targetplayer.PlayerId, Language.Translate("chatcommand.kick.reason").Replace("%REASON%", str)));
                    NebulaManager.Instance.StartDelayAction(1f, () =>
                    {
                        KickPlayer.Invoke((targetplayer.ClientId, false));
                    });
                    break;
                case "/ban":
                    if (!AmongUsLLImpl.AmongUsClientInstance.AmHost && !IsContributor(AmongUsLLImpl.LocalPlayer))
                    {
                        break;
                    }
                    if (strs.Length < 2) break;
                    var targetC = PlayerControl.AllPlayerControls.GetFastEnumerator().FirstOrDefault(info => info.name.Contains(strs[1]));
                    if (targetC == null) break;
                    var target = targetC.Data;
                    if (target == null) break;
                    string stri = strs.Length >= 3 ? string.Join(Environment.NewLine, strs.Skip(2)) : "";
                    SetSendKickPlayer.Invoke((AmongUsLLImpl.LocalPlayer.PlayerId, target.PlayerId, Language.Translate("chatcommand.ban.reason").Replace("%REASON%", stri)));
                    NebulaManager.Instance.StartDelayAction(1f, () =>
                    {
                        KickPlayer.Invoke((target.ClientId, true));
                    });
                    break;
                case "/setkillguardplayer":
                case "/设置首刀保护":
                case "/skgp":
                    if (!AmongUsLLImpl.AmongUsClientInstance.AmHost)
                    {
                        break;
                    }
                    foreach (var player in GameData.Instance.AllPlayers)
                    {
                        if (player.name.Contains(strs[1]))
                        {
                            if (killGuardPlayers.ContainsKey(player.Object) && strs.Length >= 3 && strs[2] == "remove")
                            {
                                if (killGuardPlayers.ContainsKey(player.Object))
                                {
                                    killGuardPlayers.Remove(player.Object);
                                }
                                AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, Language.Translate("chatcommand.setkillguardplayer.remove").Replace("%PLAYER%", player.Object.name));
                                break;
                            }
                            else if (!killGuardPlayers.ContainsKey(player.Object))
                            {
                                killGuardPlayers.Add(player.Object, strs.Length >= 3 && strs[2] != null ? Convert.ToInt32(strs[2]) : 1);
                                AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, Language.Translate("chatcommand.setkillguardplayer.add").Replace("%PLAYER%", player.Object.name));
                                break;
                            }
                        }
                    }
                    break;
                case "/skipmeeting":
                case "/跳过会议":
                    if (!AmongUsLLImpl.AmongUsClientInstance.AmHost && !IsAdmin(AmongUsLLImpl.LocalPlayer))
                    {
                        break;
                    }
                    if (!MeetingHud.Instance.AsBoolFast())
                    {
                        break;
                    }
                    MeetingHud.Instance.ForceSkipAll();
                    break;
                case "/win":
                    if (strs.Length > 1)
                    {
                        if (!AmongUsLLImpl.AmongUsClientInstance.AmHost && !IsAdmin(AmongUsLLImpl.LocalPlayer))
                        {
                            break;
                        }
                        CommandtriggerGameEnd.Invoke(strs[1]);
                    }
                    break;
                case "/news":
                    if (strs.Length > 1)
                    {
                        if (PrivateChatManager.Instance.AllChannels.Any(c => c.Id is "NewsAnchor" && c.CanUseChannel))
                        {
                            NewsAnchor.RpcSendChat.Invoke(strs[1]);
                        }
                    }
                    __instance.freeChatField.Clear();
                    return false;
                case "/s":
                case "/say":
                    if (strs.Length > 1)
                    {
                        if (!AmongUsLLImpl.AmongUsClientInstance.AmHost && !IsSponser(AmongUsLLImpl.LocalPlayer))
                        {
                            break;
                        }
                        if (!CanSendAdminMessage && !IsAdmin(AmongUsLLImpl.LocalPlayer))
                        {
                            break;
                        }
                        __instance.freeChatField.Clear();
                        var p = AmongUsLLImpl.LocalPlayer;
                        var message = string.Join(" ", strs.Skip(1));
                        var messagename = BuildAutoMessageName(p);
                        RpcSendChat.Invoke((p.PlayerId, messagename, message));
                        return false;
                    }
                    break;
                case "/abl":
                case "/addblacklist":
                    {
                        if (strs.Length < 2) break;
                        else
                        {
                            if (!AmongUsLLImpl.AmongUsClientInstance.AmHost)
                            {
                                break;
                            }
                            string targetCode = strs[1];
                            var blacklist = BlackListReadWriter.ReadBlacklist();
                            if (targetCode == AmongUsLLImpl.LocalPlayer.FriendCode)
                            {
                                AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, Language.Translate("chatcommand.blackmaillist.self"));
                                break;
                            }
                            if (blacklist.Contains(targetCode))
                            {
                                AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, Language.Translate("chatcommand.blackmaillist.already"));
                                break;
                            }
                            BlackListReadWriter.WritePlayer(targetCode);
                            foreach (var p in PlayerControl.AllPlayerControls)
                            {
                                if (GetClient(p).FriendCode == targetCode)
                                {
                                    var client = GetClient(p);
                                    SetSendKickPlayer.Invoke((AmongUsLLImpl.LocalPlayer.PlayerId, p.PlayerId, Language.Translate("chatcommand.blackmaillist.kickreason")));
                                    NebulaManager.Instance.StartDelayAction(1f, () =>
                                    {
                                        KickPlayer.Invoke((client.Id, false));
                                    });
                                }
                            }
                            AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, Language.Translate("chatcommand.blackmaillist.add").Replace("%CODE%", targetCode));
                        }
                    }
                    break;
                case "/rbl":
                case "/removeblacklist":
                    {
                        if (strs.Length < 2) break;
                        else
                        {
                            if (!AmongUsLLImpl.AmongUsClientInstance.AmHost)
                            {
                                break;
                            }
                            string targetCode = strs[1];
                            BlackListReadWriter.RemovePlayer(targetCode);
                            AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, Language.Translate("chatcommand.blackmaillist.remove").Replace("%CODE%", targetCode));
                        }
                    }
                    break;
                case "/cbl":
                case "/checkblacklist":
                    {
                        var blacklist = BlackListReadWriter.ReadBlacklist();
                        if (!AmongUsLLImpl.AmongUsClientInstance.AmHost)
                        {
                            break;
                        }
                        if (blacklist.Count != 0)
                        {
                            var sb = new StringBuilder();
                            sb.Append(Language.Translate("chatcommand.blackmaillist.list") + "\n");
                            foreach (var code in blacklist)
                            {
                                sb.Append(code + "\n");
                            }
                            AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, sb.ToString());
                        }
                        else
                        {
                            AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, Language.Translate("chatcommand.blackmaillist.empty"));
                        }
                    }
                    break;
                case "/warning":
                    if (strs.Length >= 2)
                    {
                        if (!AmongUsLLImpl.AmongUsClientInstance.AmHost && !IsAdmin(AmongUsLLImpl.LocalPlayer))
                        {
                            break;
                        }
                        var player = PlayerControl.AllPlayerControls.GetFastEnumerator().FirstOrDefault(info => info.name.Contains(strs[1]));
                        if (player == null) break;
                        RpcSendMessage.Invoke(new ValueTuple<byte, string, bool>(player.PlayerId, Language.Translate("chatcommand.warning").Replace("%PLAYER%", player.name).Replace("%REASON%", string.Join(" ", strs.Skip(2))), true));
                    }
                    break;
                case "/sendmessage":
                    __instance.freeChatField.Clear();
                    if (strs.Length >= 2)
                    {
                        if (!AmongUsLLImpl.AmongUsClientInstance.AmHost && !IsContributor(AmongUsLLImpl.LocalPlayer))
                        {
                            return false;
                        }
                        var player = PlayerControl.AllPlayerControls.GetFastEnumerator().FirstOrDefault(info => info.name.Contains(strs[1]));
                        if (player == null) return false;
                        string t = string.Join(" ", strs.Skip(2));
                        RpcSendMessage.Invoke(new ValueTuple<byte, string, bool>(player.PlayerId, t, false));
                        return false;
                    }
                    return false;
            }
        }
        catch (Exception e)
        {
            PDebug.Log("SendChatError text:" + text + Environment.NewLine + "Error:" + e.ToString());
        }
        return true;
    }
    static RemoteProcess<string> CommandtriggerGameEnd = new RemoteProcess<string>("CommandInvokeGameEnd", (s, _) =>
    {
        if (AmongUsLLImpl.AmongUsClientInstance.AmHost)
        {
            var game = NebulaAPI.CurrentGame;
            if (game == null)
            {
                return;
            }
            switch (s)
            {
                case "crewmate":
                    game.TriggerGameEnd(NebulaGameEnds.CrewmateGameEnd, GameEndReason.Situation);
                    break;
                case "task":
                    game.TriggerGameEnd(NebulaGameEnds.CrewmateGameEnd, GameEndReason.Task);
                    break;
                case "impostor":
                    game.TriggerGameEnd(NebulaGameEnds.ImpostorGameEnd, GameEndReason.Situation);
                    break;
                case "sabotage":
                    game.TriggerGameEnd(NebulaGameEnds.ImpostorGameEnd, GameEndReason.Sabotage);
                    break;
                case "jackal":
                    game.TriggerGameEnd(NebulaGameEnds.JackalGameEnd, GameEndReason.Situation);
                    break;
                case "jester":
                    game.TriggerGameEnd(NebulaGameEnds.JesterGameEnd, GameEndReason.Special);
                    break;
                case "lovers":
                    game.TriggerGameEnd(NebulaGameEnds.LoversGameEnd, GameEndReason.Situation);
                    break;
                case "avenger":
                    game.TriggerGameEnd(NebulaGameEnds.AvengerGameEnd, GameEndReason.Special);
                    break;
                case "arsonist":
                    game.TriggerGameEnd(NebulaGameEnds.ArsonistGameEnd, GameEndReason.Special);
                    break;
                case "paparazzo":
                    game.TriggerGameEnd(NebulaGameEnds.PaparazzoGameEnd, GameEndReason.Special);
                    break;
                case "vulture":
                    game.TriggerGameEnd(NebulaGameEnds.VultureGameEnd, GameEndReason.Special);
                    break;
                case "yandere":
                    game.TriggerGameEnd(Yandere.Instance.YandereTeamWin, GameEndReason.Situation);
                    break;
                case "doomsayer":
                    game.TriggerGameEnd(Doomsayer.Instance.DoomsayerTeamWin, GameEndReason.Special);
                    break;
                case "lawyer":
                    game.TriggerGameEnd(Lawyer.Instance.LawyerWin, GameEndReason.Special);
                    break;
                case "moriarty":
                    game.TriggerGameEnd(Moriarty.Instance.MoriartyTeamWin, GameEndReason.Situation);
                    break;
                case "nogame":
                    game.TriggerGameEnd(NebulaGameEnds.NoGameEnd, GameEndReason.Special);
                    break;
            }
        }
    });

    public static bool CrewmateCheck(GameUpdateEvent ev)
    {
        try
        {
            if (DiagnosisModePatchs.IsEnabled)
            {
                return false;
            }
            if (GameOperatorManager.Instance?.Run<KillerTeamCallback>(new KillerTeamCallback(NebulaTeams.CrewmateTeam), false, true).RemainingOtherTeam ?? false)
            {
                return false;
            }
            NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnd.CrewmateWin, GameEndReason.Situation, null);
            return false;
        }
        catch (Exception e)
        {
            PDebug.Log(e);
            return true;
        }
    }
    public static bool ImpostorCheck(GameUpdateEvent ev)
    {
        try
        {
            int impostors = 0;
            int totalAlive = 0;
            if (GameOperatorManager.Instance?.Run<KillerTeamCallback>(new KillerTeamCallback(NebulaTeams.ImpostorTeam), false, true).RemainingOtherTeam ?? false)
            {
                return false;
            }
            foreach (var p in GamePlayer.AllPlayers)
            {
                if (!p.IsDead)
                {
                    totalAlive++;
                    Lover.Instance lover;
                    if ((p.Role.Role.Team == Impostor.MyTeam) && (!p.TryGetModifier<Lover.Instance>(out lover!) || lover.IsAloneLover) && !p.TryGetModifier<HasLove.Instance>(out var love))
                    {
                        impostors++;
                    }
                }
            }
            if (ImpostorMustKillAllToWin)
            {
                if (impostors == totalAlive)
                {
                    Virial.Game.Game currentGame = NebulaAPI.CurrentGame!;
                    if (currentGame == null)
                    {
                        return false;
                    }
                    currentGame.TriggerGameEnd(NebulaGameEnd.ImpostorWin, GameEndReason.Situation, null);
                }
                return false;
            }
            if (impostors * 2 >= totalAlive)
            {
                Virial.Game.Game currentGame = NebulaAPI.CurrentGame!;
                if (currentGame == null)
                {
                    return false;
                }
                currentGame.TriggerGameEnd(NebulaGameEnd.ImpostorWin, GameEndReason.Situation, null);
            }
            return false;
        }
        catch (Exception e)
        {
            PDebug.Log(e);
            return true;
        }
    }
    public static bool JackalCheck(GameUpdateEvent ev)
    {
        try
        {
            List<Jackal.Instance> allAliveJackals = new List<Jackal.Instance>();
            int totalAlive = 0;
            bool leftImpostors = false, leftYandere = false, leftLoveJackal = false, leftMoran = false;
            static bool isJackalTeam(GamePlayer p) => (!p.Modifiers.Any(m => m.Modifier == HasLove.MyRole) && p.Role.Role.Team == Jackal.MyTeam) || ((!p.Modifiers.Any(m => m.Modifier == HasLove.MyRole) && p.Modifiers.Any(m => m.Modifier == SidekickModifier.MyRole)));
            static bool isMoran(GamePlayer p) => (p.Role.Role == Moran.MyRole) || (p.Modifiers.Any(m => m.Modifier == MoranModifier.MyRole));
            static bool IsHasLove(GamePlayer p) => p.Modifiers.Any(m => m.Modifier == HasLove.MyRole);
            allAliveJackals.Clear();
            var leftOtherTeam = (GameOperatorManager.Instance?.Run<KillerTeamCallback>(new KillerTeamCallback(NebulaTeams.JackalTeam), false, true).RemainingOtherTeam ?? false);
            foreach (var p in GamePlayer.AllPlayers)
            {
                if (p.IsDead) continue;
                if (p.Role.Role.Team == Impostor.MyTeam) leftImpostors = true;
                if (p.Role is Yandere.Instance) leftYandere = true;
                if (isMoran(p)) leftMoran = true;
                if (p.Role is Jackal.Instance jRole)
                {
                    allAliveJackals.Add(jRole);
                    if (IsHasLove(p))
                    {
                        leftLoveJackal = true;
                    }
                }
                totalAlive++;
            }
            var jackalAliveEx = new List<Jackal.Instance>(allAliveJackals);
            foreach (var p in allAliveJackals)
            {
                if (IsHasLove(p.MyPlayer))
                {
                    jackalAliveEx.Remove(p);
                }
            }
            if (jackalAliveEx.Count <= 0)
            {
                return false;
            }
            allAliveJackals = jackalAliveEx;
            ulong jackalMask = 0;
            int teamCount = 0;
            int winningJackalTeams = 0;
            ulong completeWinningJackalMask = 0;

            //全ジャッカルに対して、各チームごとに勝敗を調べる
            foreach (var jackal in allAliveJackals)
            {
                //ジャッカル陣営の数をカウントする
                ulong myMask = 1ul << jackal!.JackalTeamId;
                if ((jackalMask & myMask) == 0) teamCount++;
                else continue; //既に考慮したチームはスキップしてよい
                jackalMask |= myMask;
                Lover.Instance lover;
                //死亡しておらず、同チーム、かつラバーズでないか相方死亡ラバー
                int aliveJackals = GamePlayer.AllPlayers.Count(p =>
                {
                    return !p.IsDead && isJackalTeam(p) && jackal.IsSameTeam(p) && (!p.TryGetModifier<Lover.Instance>(out lover!) || lover.IsAloneLover) && !p.IsModMadmate();
                });
                //完全殲滅勝利
                if (aliveJackals == totalAlive) completeWinningJackalMask |= myMask;
                //キル勝利
                if (aliveJackals * 2 >= totalAlive && !leftImpostors && !leftYandere && !leftLoveJackal && !leftMoran && !leftOtherTeam) winningJackalTeams++;
            }

            //キル勝利のトリガー
            if (teamCount == 1 && winningJackalTeams > 0) NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnd.JackalWin, GameEndReason.Situation);
            //完全殲滅勝利のトリガー
            if (completeWinningJackalMask != 0)
            {
                allAliveJackals.Do(j => j.IsDefeatedJackal = (completeWinningJackalMask & (1ul << j.JackalTeamId)) == 0ul);
                NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnd.JackalWin, GameEndReason.Situation);
            }
            return false;
        }
        catch (Exception e)
        {
            PDebug.Log(e);
            return true;
        }
    }
    static RemoteProcess<(byte sendPlayer, string spName, string message)> RpcSendChat = new("SendAdminChat",
(message, _) =>
{
    var p = PlayerControl.AllPlayerControls.GetFastEnumerator().FirstOrDefault(p => p.PlayerId == message.sendPlayer);
    var messagename = message.spName;
    PDebug.Log("Send Special Message Sender:" + p.name + "  Message:" + message.message);
    if (p.Data.IsDead)
    {
        var p2 = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault((PlayerControl x) => !x.Data.IsDead);
        if (p2 != null)
        {
            var p2name = p2.name;
            p2.SetName(messagename);
            AmongUsLLImpl.HudManagerBridge.Chat.AddChat(p2, message.message, false);
            p2.SetName(p2name);
            p2.ToNebulaPlayer()?.Unbox()?.playerNameText?.lastText = null;
        }
        return;
    }
    var senderName = p.name;
    p.SetName(messagename);
    AmongUsLLImpl.HudManagerBridge.Chat.AddChat(p, message.message, false);
    p.SetName(senderName);
    p?.ToNebulaPlayer()?.Unbox()?.playerNameText?.lastText = null;
});
    static RemoteProcess<ValueTuple<byte, string, bool>> RpcSendMessage = new("SendMessageChat",
(ValueTuple<byte, string, bool> message, bool _) =>
{
    foreach (var p in PlayerControl.AllPlayerControls)
    {
        if (p.PlayerId == message.Item1)
        {
            if (message.Item3 && p.Data.IsDead)
            {
                var replacePlayer = PlayerControl.AllPlayerControls.GetFastEnumerator().FirstOrDefault(p => !p.Data.IsDead);
                AmongUsLLImpl.HudManagerBridge.Chat.AddChat(replacePlayer, message.Item2);
                return;
            }
            AmongUsLLImpl.HudManagerBridge.Chat.AddChat(p, message.Item2, false);
        }
    }
});
    static RemoteProcess<ValueTuple<GamePlayer, GamePlayer>> setShieldPlayer = new RemoteProcess<(Player, Player)>("DocterSetShield", delegate (ValueTuple<GamePlayer, GamePlayer>
    message, bool _)
    {
        if (message.Item2.TryGetAbility<Doctor.Ability>(out var a))
        {
            GameOperatorManager.Instance.Subscribe<GameUpdateEvent>(ev =>
            {
                if (!message.Item2.IsDead)
                {
                    HighlightHelpers.SetHighlight(message.Item1, new Color(0, 221, 255, 255));
                }
            }, a);
            GameOperatorManager.Instance.Subscribe<PlayerCheckKilledEvent>(ev =>
            {
                if (message.Item2.IsDead)
                {
                    return;
                }
                if (ev.Player == ev.Killer)
                {
                    return;
                }
                if (ev.Player == message.Item1 && (!DoctorShieldNoBlockGuessedOption || ev.PlayerState != PlayerStates.Guessed))
                {
                    ev.Result = KillResult.ObviousGuard;
                    TryKillShieldPlayer.Invoke((message.Item1, ev.Killer, true));
                }
            }, a, -1);
        }
        if (message.Item2.TryGetAbility<Doctor.UsurpedAbility>(out var ab))
        {
            GameOperatorManager.Instance.Subscribe<GameUpdateEvent>(ev =>
            {
                if (!message.Item2.IsDead)
                {
                    HighlightHelpers.SetHighlight(message.Item1, new Color(0, 221, 255, 255));
                }
            }, ab);
            GameOperatorManager.Instance.Subscribe<PlayerCheckKilledEvent>(ev =>
            {
                if (message.Item2.IsDead)
                {
                    return;
                }
                if (ev.Player == ev.Killer)
                {
                    return;
                }
                if (ev.Player == message.Item1 && (!DoctorShieldNoBlockGuessedOption || ev.PlayerState != PlayerStates.Guessed))
                {
                    ev.Result = KillResult.ObviousGuard;
                    TryKillShieldPlayer.Invoke((message.Item1, ev.Killer, true));
                }
            }, ab, -1);
        }
    });
    internal static RemoteProcess<ValueTuple<GamePlayer, GamePlayer, bool>> TryKillShieldPlayer = new RemoteProcess<ValueTuple<GamePlayer, GamePlayer, bool>>("TryKillShieldPlayer", delegate (ValueTuple<GamePlayer, GamePlayer, bool>
    message, bool _)
    {
        if (message.Item1.AmOwner || message.Item2.AmOwner)
        {
            if (message.Item3)
            {
                AmongUsUtil.PlayCustomFlash(Color.Red, 0.5f, 0.5f, 0.75f);
            }
        }
    });
    public static RemoteProcess<(GamePlayer sender, string message)> RpcSendSkillMessage = new("SendSkillMessage", (m, _) =>
    {
        if (m.sender.AmOwner || NebulaGameManager.Instance.CanSeeAllInfo)
        {
            AmongUsLLImpl.HudManagerBridge.Chat.AddChat(m.sender.VanillaPlayer, m.message);
        }
    });
    public static RemoteProcess<(string info, bool needTranslate)> RpcSendNoSenderSkillMessage = new("SendNoSenderSkillMessage", (m, _) =>
    {
        AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, m.needTranslate ? Language.Translate(m.info) : m.info);
    });
    internal static RemoteProcess<GamePlayer> SetVisible = new RemoteProcess<Player>("SetPlayerVisible", (p, _) =>
    {
        if (p == null || p.VanillaPlayer == null)
        {
            return;
        }
        p.VanillaPlayer.Visible = true;
    });
    public static void TaskPanelPosPatch(TaskPanelBehaviour __instance)
    {
        if (LogicDangerLevel != null && IntroShowImpostor)
        {
            __instance.closedPosition = new Vector3(__instance.closedPosition.x, 1.6f, __instance.closedPosition.z);
            __instance.openPosition = new Vector3(__instance.openPosition.x, 1.6f, __instance.openPosition.z);
            Vector3 vector3 = new Vector3(Mathn.SmoothStep(__instance.closedPosition.x, __instance.openPosition.x, __instance.timer), Mathn.SmoothStep(__instance.closedPosition.y, __instance.openPosition.y, __instance.timer), __instance.openPosition.z);
            __instance.transform.localPosition = AspectPosition.ComputePosition(AspectPosition.EdgeAlignments.LeftTop, vector3);
        }
    }
    [HarmonyPatch]
    public class CannotSeeOtherPatch
    {
        [HarmonyPatch(typeof(ImpostorBasicRuleOperator), "OnSetRole")]
        [HarmonyPatch(typeof(ImpostorBasicRuleOperator), "OnCheckCanKill")]
        [HarmonyPatch(typeof(ImpostorGameRule), "DecoratePlayerColor")]
        [HarmonyPrefix]
        public static bool BlockRun()
        {
            return !ImpostorCannotSeeOther;
        }

        [HarmonyPatch(typeof(RadioManager), "Register", typeof(RadioKind), typeof(string), typeof(Func<GamePlayer, bool>), typeof(ILifespan), typeof(Color))]
        [HarmonyPrefix]
        public static bool AllPlayerBlockRun()
        {
            return !AllPlayerCannotSeeOtherNameColor;
        }

        [HarmonyPatch(typeof(PlayerModInfo), "UpdateNameText")]
        [HarmonyPostfix]
        private static void AllPlayerBlockNameColor(TextMeshProHandler nameText)
        {
            if (AllPlayerCannotSeeOtherNameColor)
            {
                nameText.RequestChange(UnityEngine.Color.white);
            }
        }
        [HarmonyPatch(typeof(PlayerCheckCanKillLocalEvent), "get_CanKill")]
        [HarmonyPostfix]
        public static void PlayerCheckCanKillForcelyLocalEvent(ref bool __result)
        {
            if (AllPlayerCannotSeeOtherNameColor)
            {
                __result = true;
            }
        }
    }
    [NebulaRPCHolder]
    [HarmonyPatch]
    public class BetterStandardRoleAllocator
    {
        static List<(byte player, DefinedRole role, int chance)> RoleUpList = new List<(byte player, DefinedRole role, int chance)>();
        static List<(byte player, RoleCategory role, int chance)> CategoryUpList = new List<(byte player, RoleCategory role, int chance)>();
        public static void Reload()
        {
            RoleUpList.Clear();
            CategoryUpList.Clear();
        }
        public static void AddRoleUp(byte player, DefinedRole role, int chance = 100)
        {
            if (RoleUpList.Any(r => r.player == player) || CategoryUpList.Any(r => r.player == player))
            {
                PDebug.Log("Player " + player.ToString() + " Added UpList");
                return;
            }
            RoleUpList.Add(ValueTuple.Create(player, role, chance));
        }
        public static void AddCategoryUp(byte player, RoleCategory c, int chance = 100)
        {
            if (RoleUpList.Any(r => r.player == player) || CategoryUpList.Any(r => r.player == player))
            {
                PDebug.Log("Player " + player.ToString() + " Added UpList");
                return;
            }
            CategoryUpList.Add(ValueTuple.Create(player, c, chance));
        }
        public record BetterRoleChance(DefinedRole role, AllocationParameters? param = null)
        {
            public int count = (param ?? role.AllocationParameters)?.RoleCountSum ?? 0;
            public int left = (param ?? role.AllocationParameters)?.RoleCountSum ?? 0;
            public int cost = (param ?? role.AllocationParameters)?.TeamCost ?? 1;
            public int otherCost = (param ?? role.AllocationParameters)?.OtherCost ?? 1;
            public AllocationParameters? Param => param ?? role.AllocationParameters;
        }

        public static void CreateRolePool(out List<BetterRoleChance> impostorRoles, out List<BetterRoleChance> crewmateRoles, out List<BetterRoleChance> neutralRoles, out List<BetterRoleChance>[] customChances, out List<BetterRoleChance>[] allRoles)
        {
            customChances = AssignmentType.AllTypes.Select((AssignmentType t) => new List<BetterRoleChance>(from r in Nebula.Roles.Roles.AllRoles.Where(delegate (DefinedRole r)
            {
                if (t.Predicate(r.AssignmentStatus, r))
                {
                    AllocationParameters customAllocationParameters = r.GetCustomAllocationParameters(t);
                    return ((customAllocationParameters != null) ? customAllocationParameters.RoleCountSum : 0) > 0;
                }
                return false;
            })
                                                                                                            select new BetterRoleChance(r, r.GetCustomAllocationParameters(t))
                                                                                                            {
                                                                                                                cost = 1,
                                                                                                                otherCost = 0
                                                                                                            })).ToArray<List<BetterRoleChance>>();

            List<BetterRoleChance> GetRolePool(RoleCategory category)
            {
                return new List<BetterRoleChance>(from r in Nebula.Roles.Roles.AllRoles.Where(delegate (DefinedRole r)
                {
                    if (r.Category == category)
                    {
                        AllocationParameters allocationParameters = r.AllocationParameters;
                        return ((allocationParameters != null) ? allocationParameters.RoleCountSum : 0) > 0;
                    }
                    return false;
                })
                                                  select new BetterRoleChance(r, null)
                                                  {
                                                      cost = 1,
                                                      otherCost = 0
                                                  });
            }
            crewmateRoles = GetRolePool(RoleCategory.CrewmateRole);
            impostorRoles = GetRolePool(RoleCategory.ImpostorRole);
            neutralRoles = GetRolePool(RoleCategory.NeutralRole);
            List<List<BetterRoleChance>> list = new List<List<BetterRoleChance>>();
            list.Add(crewmateRoles);
            list.Add(impostorRoles);
            list.Add(neutralRoles);
            list.AddRange(customChances);
            allRoles = list.ToArray();
            foreach (IExclusiveAssignmentRule exclusiveOption in IExclusiveAssignmentRule.AllRules)
            {
                List<DefinedRole> cand = new List<DefinedRole>();
                List<BetterRoleChance>[] array = allRoles;
                for (int i = 0; i < array.Length; i++)
                {
                    foreach (BetterRoleChance chance2 in array[i])
                    {
                        if (exclusiveOption.Contains(chance2.role) && !cand.Contains(chance2.role))
                        {
                            cand.Add(chance2.role);
                        }
                    }
                }
                if (cand.Count > 1)
                {
                    cand.RemoveRandomOne<DefinedRole>();
                    foreach (List<BetterRoleChance> list2 in allRoles)
                    {
                        list2.RemoveAll(chance => cand.Contains(chance.role));
                    }
                }
            }
        }
        public static void CategoryAssign(IRoleAllocator allocator, IRoleTable rtable, int left, List<byte> main, List<byte> others, List<BetterRoleChance> rolePool, List<BetterRoleChance>[] allRolePool, Action<DefinedRole, byte>? onSelected = null)
        {
            RoleTable table = (rtable as RoleTable)!;
            if (left < 0) left = 15;
            void OnSelected(BetterRoleChance selected)
            {
                if (onSelected != null)
                {
                    onSelected.Invoke(selected.role, main[0]);
                    main.RemoveAt(0);
                }
                else
                {
                    var player = main[0];
                    table.SetRole(main[0], selected.role, selected.role.DefaultAssignableArguments);
                    main.RemoveAt(0);

                    selected.Param?.TeamAssignment?.Do(assignment =>
                    {
                        var param = assignment.Assigner.Invoke(selected.role, player);
                        table.SetRole(main[0], param.role, param.argument);
                        main.RemoveAt(0);
                    });

                    selected.Param?.OthersAssignment?.Do(assignment =>
                    {
                        var param = assignment.Assigner.Invoke(selected.role, player);
                        table.SetRole(others[0], param.role, param.argument);
                        others.RemoveAt(0);
                    });
                }

                left -= selected.cost;

                //割り当て済み役職を排除
                selected.left--;
                if (selected.left == 0) rolePool.Remove(selected);
            }

            bool left100Roles = true;
            while (main.Count > 0 && left > 0 && rolePool.Count > 0)
            {
                //コスト超過によって割り当てられない役職を弾く
                rolePool.RemoveAll(c =>
                {
                    if (main == others)
                        return c.cost + c.otherCost > left && c.cost + c.otherCost > main.Count;
                    else
                        return c.cost > left && c.cost > main.Count && c.otherCost > others.Count;
                });

                //100%割り当て役職が残っている場合
                if (left100Roles)
                {
                    var roles100 = rolePool.Where(r => r.Param!.GetRoleChance(r.count - r.left + 1) == 100f);
                    if (roles100.Any(r => true))
                    {
                        //役職を選択する
                        OnSelected(roles100.ToArray().Random());
                        continue;
                    }
                    else
                    {
                        left100Roles = false;
                    }
                }

                //100%役職がもう残っていない場合
                var sum = rolePool.Sum(r => r.Param!.GetRoleChance(r.count - r.left + 1));
                var random = System.Random.Shared.NextSingle() * sum;
                foreach (var r in rolePool)
                {
                    random -= r.Param!.GetRoleChance(r.count - r.left + 1);
                    if (random < 0f)
                    {
                        //役職を選択する
                        OnSelected(r);
                        break;
                    }
                }
            }
        }
        public static void SingleSetRole(IRoleAllocator allocator, IRoleTable rtable, byte player, List<byte> main, List<byte> others, BetterRoleChance targetRole, List<BetterRoleChance> rolePool, List<BetterRoleChance>[] allRolePool)
        {
            RoleTable table = (rtable as RoleTable)!;
            void OnSelected(BetterRoleChance selected)
            {
                table.SetRole(player, selected.role, selected.role.DefaultAssignableArguments);
                selected.Param?.TeamAssignment?.Do(assignment =>
                {
                    var param = assignment.Assigner.Invoke(selected.role, player);
                    table.SetRole(main[0], param.role, param.argument);
                    main.RemoveAt(0);
                });

                selected.Param?.OthersAssignment?.Do(assignment =>
                {
                    var param = assignment.Assigner.Invoke(selected.role, player);
                    table.SetRole(others[0], param.role, param.argument);
                    others.RemoveAt(0);
                });
                selected.left--;
                if (selected.left == 0) rolePool.Remove(selected);
            }
            OnSelected(targetRole);
        }
        [NebulaRPC]
        public static void SetVanillaRole(byte[] imps)
        {
            var impostors = imps.ToList();
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (impostors.Contains(player.PlayerId))
                {
                    DestroyableSingleton<RoleManager>.Instance.SetRole(player, RoleTypes.Impostor);
                }
                else
                {
                    DestroyableSingleton<RoleManager>.Instance.SetRole(player, RoleTypes.Crewmate);
                }
            }
            var ins = AmongUsLLImpl.HudManagerInstance;
            ins.HideGameLoader();
            ins.StartCoroutine(ins.CoShowIntro());
        }
        [HarmonyPatch(typeof(StandardRoleAllocator), "Assign"), HarmonyPrefix]
        public static bool Assign(StandardRoleAllocator __instance, List<byte> impostors, List<byte> others)
        {
            /*if (CanInvokeSpecialMeeting && DiagnosisIsActive && Mathn.Prob(DiagnosisChance / 100f))
            {
                RoleTable table = new();
                var customChances = AssignmentType.AllTypes.Select((AssignmentType t) => new List<BetterRoleChance>(from r in Nebula.Roles.Roles.AllRoles.Where(delegate (DefinedRole r)
                {
                    if (t.Predicate(r.AssignmentStatus, r))
                    {
                        AllocationParameters customAllocationParameters = r.GetCustomAllocationParameters(t)!;
                        return ((customAllocationParameters != null) ? customAllocationParameters.RoleCountSum : 0) > 0;
                    }
                    return false;
                })
                                                                                                                    select new BetterRoleChance(r, r.GetCustomAllocationParameters(t))
                                                                                                                    {
                                                                                                                        cost = 1,
                                                                                                                        otherCost = 0
                                                                                                                    })).ToArray<List<BetterRoleChance>>();
                //ロールプールを作る
                List<BetterRoleChance> GetRolePool(RoleCategory category) => new(Nebula.Roles.Roles.AllRoles.Where(r => r.Category == category && (r.AllocationParameters?.RoleCountSum ?? 0) > 0).Select(r => new BetterRoleChance(r) { cost = 1, otherCost = 0 }));
                List<BetterRoleChance> crewmateRoles = GetRolePool(RoleCategory.CrewmateRole);
                List<BetterRoleChance> neutralRoles = new List<BetterRoleChance>() { new BetterRoleChance(Violator.MyRole) { count = 1, left = 1, cost = 1, otherCost = 0 } };
                List<List<BetterRoleChance>> list = new();
                list.Add(crewmateRoles);
                list.Add(neutralRoles);
                list.AddRange(customChances);
                var allRoles = list.ToArray();
                List<byte> players = new List<byte>();
                players.AddRange(impostors);
                players.AddRange(others);
                if (DiagnosisChance == 100)
                {
                    table.SetRole(AmongUsLLImpl.LocalPlayer.PlayerId, Violator.MyRole);
                    players.Remove(AmongUsLLImpl.LocalPlayer.PlayerId);
                }
                else
                {
                    CategoryAssign(__instance, table, 1, players, players, neutralRoles, allRoles);
                }
                void AssignCustomAbilities(RoleCategory category)
                {
                    foreach (var assignmentType in AssignmentType.AllTypes)
                    {
                        if (assignmentType.IsActive && assignmentType.RelatedRole.Category == category)
                        {
                            var targetRole = assignmentType.RelatedRole;
                            List<byte> players = (from r in table.roles
                                                  where r.Item1 == targetRole
                                                  select r.Item3).ToList<byte>();
                            CategoryAssign(__instance, table, players.Count, players, players, customChances[assignmentType.Id], allRoles, delegate (DefinedRole role, byte player)
                            {
                                table.EditRole(player, (ValueTuple<DefinedRole, int[]> last) => (last.Item1, assignmentType.EditArguments(last.Item2, role)));
                            });
                        }
                    }
                }
                int left = GeneralConfigurations.AssignmentCrewmateOption;
                if (left < 0)
                {
                    left = 24;
                }
                CategoryAssign(__instance, table, left, players, players, crewmateRoles, allRoles);
                AssignCustomAbilities(RoleCategory.CrewmateRole);
                foreach (var p in players) table.SetRole(p, Crewmate.MyRole);
                foreach (var m in Nebula.Roles.Roles.AllAllocatableModifiers().OrderBy(im => im.AssignPriority)) m.TryAssign(table);
                GameOperatorManager.Instance?.Run<PreFixAssignmentEvent>(new PreFixAssignmentEvent(table), false, true);
                table.Determine();
            }
            else
            {*/
            try
            {
                RoleTable table = new();
                List<BetterRoleChance> impostorRoles;
                List<BetterRoleChance> crewmateRoles;
                List<BetterRoleChance> neutralRoles;
                List<BetterRoleChance>[] customChances;
                List<BetterRoleChance>[] allRoles;
                CreateRolePool(out impostorRoles, out crewmateRoles, out neutralRoles, out customChances, out allRoles);
                int impnums = GeneralConfigurations.AssignmentImpostorOption, neunums = GeneralConfigurations.AssignmentNeutralOption, crewnums = GeneralConfigurations.AssignmentCrewmateOption < 0 ? 24 : GeneralConfigurations.AssignmentCrewmateOption;
                foreach (var tuple in RoleUpList)
                {
                    try
                    {
                        if (allRoles.Any(r => r.Any(r => r.role.Id == tuple.role.Id)))
                        {
                            if (Mathn.Prob(tuple.chance / 100f))
                            {
                                if (impostors.Contains(tuple.player))
                                {
                                    if (tuple.role.Category != RoleCategory.ImpostorRole)
                                    {
                                        impostors.Remove(tuple.player);
                                        var otherplayer = others.Random();
                                        impostors.Add(otherplayer);
                                        others.Remove(otherplayer);
                                    }
                                    else
                                    { impostors.Remove(tuple.player); }
                                }
                                else if (others.Contains(tuple.player))
                                {
                                    if (tuple.role.Category == RoleCategory.ImpostorRole)
                                    {
                                        var imp = impostors.Random();
                                        impostors.Remove(imp);
                                        others.Add(imp);
                                    }
                                    others.Remove(tuple.player);
                                }
                                switch (tuple.role.Category)
                                {
                                    case RoleCategory.ImpostorRole:
                                        impnums--;
                                        break;
                                    case RoleCategory.NeutralRole:
                                        neunums--;
                                        break;
                                    case RoleCategory.CrewmateRole:
                                        crewnums--;
                                        break;
                                }
                                BetterRoleChance targetRoleChance = allRoles.FirstOrDefault(r => r.Any(r => r.role.Id == tuple.role.Id))!.FirstOrDefault(r => r.role.Id == tuple.role.Id)!;
                                var rolepool = targetRoleChance!.role.Category == RoleCategory.ImpostorRole ? impostorRoles : targetRoleChance.role
                                    .Category == RoleCategory.NeutralRole ? neutralRoles : crewmateRoles;
                                SingleSetRole(__instance, table, tuple.player, targetRoleChance.role.Category == RoleCategory.ImpostorRole ? impostors : others, others, targetRoleChance, rolepool, allRoles);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        PDebug.Log("RoleUpError:" + e.ToString());
                    }
                }
                foreach (var tuple in CategoryUpList)
                {
                    try
                    {
                        if (Mathn.Prob(tuple.chance / 100f))
                        {
                            var usedRoleList = tuple.role == RoleCategory.ImpostorRole ? impostorRoles : tuple.role == RoleCategory.NeutralRole ? neutralRoles : crewmateRoles;

                            if (impostors.Contains(tuple.player))
                            {
                                if (tuple.role != RoleCategory.ImpostorRole)
                                {
                                    impostors.Remove(tuple.player);
                                    var otherplayer = others.Random();
                                    impostors.Add(otherplayer);
                                    others.Remove(otherplayer);
                                }
                                else
                                { impostors.Remove(tuple.player); }
                            }
                            else if (others.Contains(tuple.player))
                            {
                                if (tuple.role == RoleCategory.ImpostorRole)
                                {
                                    var imp = impostors.Random();
                                    impostors.Remove(imp);
                                    others.Add(imp);
                                }
                                others.Remove(tuple.player);
                            }
                            switch (tuple.role)
                            {
                                case RoleCategory.ImpostorRole:
                                    usedRoleList = impostorRoles;
                                    impnums--;
                                    break;
                                case RoleCategory.NeutralRole:
                                    usedRoleList = neutralRoles;
                                    neunums--;
                                    break;
                                case RoleCategory.CrewmateRole:
                                    usedRoleList = crewmateRoles;
                                    crewnums--;
                                    break;
                            }
                            CategoryAssign(__instance, table, 1, [tuple.player], others, usedRoleList, allRoles);
                        }
                    }
                    catch (Exception e)
                    {
                        PDebug.Log("CategoryUpError:" + e.ToString());
                    }
                }
                void AssignCustomAbilities(RoleCategory category)
                {
                    foreach (var assignmentType in AssignmentType.AllTypes)
                    {
                        if (assignmentType.IsActive && assignmentType.RelatedRole.Category == category)
                        {
                            var targetRole = assignmentType.RelatedRole;
                            List<byte> players = (from r in table.roles
                                                  where r.Item1 == targetRole
                                                  select r.Item3).ToList<byte>();
                            CategoryAssign(__instance, table, players.Count, players, players, customChances[assignmentType.Id], allRoles, delegate (DefinedRole role, byte player)
                            {
                                table.EditRole(player, (ValueTuple<DefinedRole, int[]> last) => (last.Item1, assignmentType.EditArguments(last.Item2, role)));
                            });
                        }
                    }
                }
                CategoryAssign(__instance, table, impnums, impostors, others, impostorRoles, allRoles);
                AssignCustomAbilities(RoleCategory.ImpostorRole);
                CategoryAssign(__instance, table, neunums, others, others, neutralRoles, allRoles);
                AssignCustomAbilities(RoleCategory.NeutralRole);
                CategoryAssign(__instance, table, crewnums, others, others, crewmateRoles, allRoles);
                AssignCustomAbilities(RoleCategory.CrewmateRole);
                foreach (var p in impostors) table.SetRole(p, Impostor.MyRole);
                foreach (var p in others) table.SetRole(p, Crewmate.MyRole);
                foreach (var m in Nebula.Roles.Roles.AllAllocatableModifiers().OrderBy(im => im.AssignPriority)) m.TryAssign(table);
                GameOperatorManager.Instance?.Run<PreFixAssignmentEvent>(new PreFixAssignmentEvent(table), false, true);
                table.Determine();
            }
            catch (Exception e)
            {
                PDebug.Log("StandardRoleAssign Error:" + e.ToString());
            }
            Reload();
            return false;
        }
    }
    [HarmonyPatch]
    public class DiagnosisModePatchs
    {
        public static bool IsEnabled
        {
            get
            {
                return NebulaGameManager.Instance!.AllPlayerInfo.Any(p => p.Role is Violator.Instance);
            }
        }
        static RemoteProcess<int> SetExileMessageRpc = new("SetExileMessageRpc", (m, _) =>
        {
            ExileMessageType = m;
        });
        [HarmonyPatch(typeof(ExileControllerBeginPatch), "Postfix"), HarmonyPrefix]
        public static bool BlockNoSExile()
        {
            if (IsEnabled)
            {
                return false;
            }
            return true;
        }
        public static IEnumerable<RuntimeModifier> GetModifiers(GamePlayer player, bool isevil)
        {
            if (isevil)
            {
                foreach (var modifier in player.Modifiers)
                {
                    if (modifier.Modifier is Damned || modifier.Modifier is Desperate || modifier.Modifier is Disperser || modifier.Modifier is ExtraMission || modifier.Modifier is JailerModifier
                        || modifier.Modifier is LawyerClient || modifier.Modifier is MadmatePLUS || modifier.Modifier is MoranModifier || modifier.Modifier is Scrambler || modifier.Modifier
                        is SidekickModifier || modifier.Modifier is SkinnerDog || modifier.Modifier is Trilemma || modifier.Modifier is InsomniacsModifier || modifier.Modifier is DreamweaverModifier)
                    {
                        yield return modifier;
                    }
                }
            }
            else
            {
                foreach (var m in player.Modifiers) yield return m;
            }
        }
        public static string GetModifierInfo(string text, IEnumerable<RuntimeModifier> modifiers)
        {
            var newtext = text;
            foreach (var m in modifiers)
            {
                var t = m.OverrideRoleName(newtext, false, true);
                if (!string.IsNullOrEmpty(t))
                {
                    newtext = t;
                }
                m.DecorateNameConstantly(ref newtext, true, false);
            }
            return newtext;
        }
        [HarmonyPatch(typeof(ExileControllerBeginPatch), "Postfix"), HarmonyPostfix]
        public static void NosExilePos(ref ExileController.InitProperties init)
        {
            var instance = ExileController.Instance;
            instance.Text.UseRoleIcon();
            if (GamePlayer.LocalPlayer.TryGetAbility<BadSheriff.Ability>(out var ab))
            {
                if (!GamePlayer.LocalPlayer.IsDead)
                {
                    init.confirmImpostor = false;
                    instance.completeString = "......";
                    return;
                }
            }
            bool exiledJester = false;
            if (init.networkedPlayer != null)
            {
                if (MeetingHudExtension.IsObvious)
                {
                    instance.completeString = Language.Translate("game.meeting.obvious");
                }
                else if ((MeetingHudExtension.ExiledAllModCache?.Length ?? 0) > 1)
                {
                    instance.completeString = Language.Translate("game.meeting.multiple");
                }
                else if (GeneralConfigurations.ShowRoleOfExiled && GameOptionsManager.Instance.currentNormalGameOptions.ConfirmImpostor)
                {
                    GamePlayer player = NebulaGameManager.Instance!.GetPlayer(init.networkedPlayer.PlayerId);
                    if (player != null)
                    {
                        string roletext = player.Role.Role.DisplayName;
                        if (roletext != player.Role.DisplayName)
                        {
                            roletext += $" {player.Role.DisplayName}";
                        }
                        if (SnatcherExileUseUsurpRole && player.TryGetAbility<Snatcher.Ability>(out var ability) && ability.UsurpedRole != null)
                        {
                            roletext = ability.UsurpedRole.DisplayName;
                        }
                        if (ExileShowEvilModifier || ExileShowAllModifier)
                        {
                            roletext = GetModifierInfo(roletext, GetModifiers(player, ExileShowEvilModifier));
                        }

                        instance.completeString = Language.Translate("game.meeting.roleText").Replace("%PLAYER%", init.networkedPlayer.PlayerName).Replace("%ROLE%", roletext);
                        if (player.Role.Role is Nebula.Roles.Neutral.Jester)
                        {
                            exiledJester = true;
                        }
                    }
                }
            }
            if (!IsEnabled && !exiledJester)
            {
                if (GeneralConfigurations.ConfirmEjectTargetOption.GetValue() == 0)
                {
                    string confirmText = instance.ImpostorText.text;
                    int exiledBitFlag = 0;
                    foreach (GamePlayer exiled in MeetingHudExtension.ExiledAllModCache)
                    {
                        exiledBitFlag |= 1 << (int)exiled.PlayerId;
                    }
                    bool IsDead(GamePlayer p)
                    {
                        return p.IsDead || (exiledBitFlag & (1 << (int)p.PlayerId)) != 0;
                    }
                    int num = GamePlayer.AllPlayers.Count(p => !IsDead(p) && (p.IsImpostor || OutlawTeam.IsOutlaw(p)));
                    confirmText = Language.Translate((num > 1) ? "exile.confirmEject.impostor.p" : "exile.confirmEject.impostor.s").Replace("%NUM%", num.ToString());
                    GamePlayer localPlayer = GamePlayer.LocalPlayer;
                    int i = GeneralConfigurations.ConfirmEjectConditionOption.GetValue();
                    bool flag = true;
                    switch (i)
                    {
                        case 0:
                            flag = true;
                            break;
                        case 1:
                            flag = localPlayer.IsImpostor || OutlawTeam.IsOutlaw(localPlayer);
                            break;
                        case 2:
                            flag = localPlayer.Role.Role.IsKiller;
                            break;
                        case 3:
                            flag = !localPlayer.IsCrewmate;
                            break;
                    }
                    if (!flag)
                    {
                        confirmText = "";
                    }
                    instance.ImpostorText.text = confirmText;
                }
                if (ExileMessageType != 0)
                {
                    instance.completeString = Language.Translate("options.diagnosis.meeting.exile." + ExileMessageType.ToString());
                    instance.ImpostorText.text = "";
                }
            }
        }
        [HarmonyPatch(typeof(ExileController), "Begin"), HarmonyPostfix]
        public static void ExilePostfix(ExileController __instance, ref ExileController.InitProperties init)
        {
            if (!IsEnabled)
            {
                return;
            }
            ExileController.Instance.Text.UseRoleIcon();
            GameOperatorManager.Instance?.Run(new ExileSceneStartEvent(MeetingHudExtension.ExiledAllModCache!));
            bool lockedJester = false;
            if (init.networkedPlayer != null)
            {

                if (MeetingHudExtension.IsObvious)
                {
                    __instance.completeString = Language.Translate("game.meeting.obvious");
                }
                else if ((MeetingHudExtension.ExiledAllModCache?.Length ?? 0) > 1)
                {
                    __instance.completeString = Language.Translate("game.meeting.multiple");
                }
                else if (GeneralConfigurations.ShowRoleOfExiled && GameOptionsManager.Instance.currentNormalGameOptions.ConfirmImpostor)
                {
                    var player = NebulaGameManager.Instance!.GetPlayer(init.networkedPlayer.PlayerId);
                    if (player != null)
                    {
                        string text = player.Role.Role.DisplayName;
                        if (ExileShowEvilModifier || ExileShowAllModifier)
                        {
                            text = GetModifierInfo(text, GetModifiers(player, ExileShowEvilModifier));
                        }
                        __instance.completeString = Language.Translate("game.meeting.roleText").Replace("%PLAYER%", init.networkedPlayer.PlayerName).Replace("%ROLE%", text);
                        if (player.Role.Role == Nebula.Roles.Neutral.Jester.MyRole)
                        {
                            __instance.ImpostorText.text = Language.Translate("game.meeting.roleJesterText");
                            lockedJester = true;
                        }
                    }
                }
            }
            if (ExileMessageType != 0)
            {
                __instance.completeString = Language.Translate("options.diagnosis.meeting.exile." + ExileMessageType.ToString());
            }
            var texts = GameOperatorManager.Instance?.Run(new FixExileTextEvent(MeetingHudExtension.ExiledAllModCache!)).GetTexts();
            __instance.ImpostorText.rectTransform.pivot = new(0.5f, 1f);
            __instance.ImpostorText.rectTransform.sizeDelta = new(11.555f, 2f);
            __instance.ImpostorText.alignment = TMPro.TextAlignmentOptions.Top;
            if (init.confirmImpostor && !lockedJester)
            {
                string confirmText = __instance.ImpostorText.text;
                int exiledBitFlag = 0;
                foreach (PlayerControl exiled in MeetingHudExtension.ExiledAllModCache!)
                {
                    exiledBitFlag |= 1 << (int)exiled.PlayerId;
                }
                //bool IsDead(GamePlayer p) => p.IsDead || (exiledBitFlag & (1 << (int)p.PlayerId)) != 0;
                switch (GeneralConfigurations.ConfirmEjectTargetOption.GetValue())
                {
                    case 0:
                        {
                            int num = Violator.Instance.LeftImpostorNum;
                            confirmText = Language.Translate((num > 1) ? "exile.confirmEject.impostor.p" : "exile.confirmEject.impostor.s").Replace("%NUM%", num.ToString());
                            break;
                        }
                    case 1:
                        {
                            int num = Violator.Instance.LeftImpostorNum;
                            confirmText = Language.Translate((num > 1) ? "exile.confirmEject.killer.p" : "exile.confirmEject.killer.s").Replace("%NUM%", num.ToString());
                            break;
                        }
                    case 2:
                        {
                            int num = Violator.Instance.LeftImpostorNum;
                            confirmText = Language.Translate((num > 1) ? "exile.confirmEject.nonCrewmate.p" : "exile.confirmEject.nonCrewmate.s").Replace("%NUM%", num.ToString());
                            break;
                        }
                }
                Player localPlayer = Player.LocalPlayer!;
                int i = GeneralConfigurations.ConfirmEjectConditionOption.GetValue();
                bool flag = false;
                switch (i)
                {
                    case 0:
                        flag = true;
                        break;
                    case 1:
                        flag = localPlayer!.IsImpostor;
                        break;
                    case 2:
                        flag = localPlayer!.Role.Role.IsKiller;
                        break;
                    case 3:
                        flag = !localPlayer!.IsCrewmate;
                        break;
                    default:
                        break;
                }
                if (!flag)
                {
                    confirmText = "";
                }
                __instance.ImpostorText.text = confirmText;
            }
            if (init.networkedPlayer != null)
            {
                if (GeneralConfigurations.ShowRoleOfExiled && GameOptionsManager.Instance.currentNormalGameOptions.ConfirmImpostor)
                {
                    var role = NebulaGameManager.Instance!.GetPlayer(init.networkedPlayer.PlayerId)?.Role;
                    if (role != null)
                    {
                        if (role.Role == Nebula.Roles.Neutral.Jester.MyRole) __instance.ImpostorText.text = Language.Translate("game.meeting.roleJesterText");
                    }
                }
            }
            if (ExileMessageType != 0)
            {
                __instance.ImpostorText.text = "";
            }
            if (texts != null && texts.Count > 0)
            {
                if (!init.confirmImpostor) __instance.ImpostorText.text = "";
                init.confirmImpostor = true;

                __instance.ImpostorText.rectTransform.anchoredPosition3D += new UnityEngine.Vector3(0f, 0.1f, 0f);

                var text = __instance.ImpostorText.text;
                text = "<line-height=90%>" + text;
                texts.Do(str => text += "<br>" + str);
                __instance.ImpostorText.text = text;
            }
            else
            {
                __instance.ImpostorText.rectTransform.anchoredPosition3D += new UnityEngine.Vector3(0f, 0.19f, 0f);
            }
        }
        [HarmonyPatch(typeof(OracleSystem), "GetCurrentRolePool"), HarmonyPostfix]
        public static void OracleGetCurrentPoolPatch(ref object __result)
        {
            if (!DiagnosisModePatchs.IsEnabled)
            {
                return;
            }
            OracleSystem.RolePool set = new([], [], []);
            bool IsCrewmateRole(DefinedRole role) => role.Category == RoleCategory.CrewmateRole && !role.IsMadmate;
            NebulaGameManager.Instance!.AllPlayerInfo.Do(p =>
            {
                if (IsCrewmateRole(p.Role.ExternalRecognitionRole))
                    set.CrewmateRoles.Add(p.Role.ExternalRecognitionRole);
            });
            var addedlist = new List<DefinedRole>();
            for (int i = 0; i < AmongUsLLImpl.CurrentGameOptionsInstance.GetAdjustedNumImpostorsModded(PlayerControl.AllPlayerControls.Count); i++)
            {
                var role = new List<DefinedRole>(Nebula.Roles.Roles.AllRoles.Where(r => r.Category == RoleCategory.ImpostorRole && r.IsSpawnable)).Random();
                if (addedlist.Contains(role))
                {
                    i--;
                    continue;
                }
                set.ImpostorRoles.Add(role);
                addedlist.Add(role);
            }
            for (int i = 0; i < GeneralConfigurations.AssignmentNeutralOption; i++)
            {
                var role = new List<DefinedRole>(Nebula.Roles.Roles.AllRoles.Where(r => r.Category == RoleCategory.NeutralRole && r.IsSpawnable && !(r is GameMaster) && !(r is Violator) && !(r is Spectre) && !(r is SpectreFollower) && !(r is SpectreImmoralist))).Random();
                if (addedlist.Contains(role))
                {
                    i--;
                    continue;
                }
                set.NeutralRoles.Add(role);
                addedlist.Add(role);
            }
            __result = set;
        }
        [HarmonyPatch(typeof(OracleSystem), "GetExcludedRoles"), HarmonyPostfix]
        public static void OracleGetExcludedRolesPatch(GamePlayer oracle, ref HashSet<DefinedRole> __result)
        {
            __result.Add(GameMaster.MyRole);
            __result.Add(Violator.MyRole);
        }
        [HarmonyPatch(typeof(ModAbilityButtonImpl), "get_OnClick"), HarmonyPostfix]
        public static void OnUseButton(ModAbilityButtonImpl __instance, ref Action<ModAbilityButtonImpl> __result)
        {
            try
            {
                if (!IsEnabled)
                {
                    return;
                }
                if (currentKillTime > StartKillTime)
                {
                    var local = GamePlayer.LocalPlayer;
                    if (local == null)
                    {
                        return;
                    }
                    if (local.Role is Violator.Instance)
                    {
                        return;
                    }
                    List<GamePlayer> closeplayers = new List<GamePlayer>();
                    foreach (var p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                    {
                        if ((p.TruePosition - local.TruePosition).ToUnityVector().magnitude <= 1f)
                        {
                            closeplayers.Add(p);
                        }
                    }
                    Violator.Instance violator = null!;
                    var playerddd = NebulaGameManager.Instance?.AllPlayerInfo?.FirstOrDefault(p => p.Role is Violator.Instance);
                    if (playerddd != null)
                    {
                        violator = (playerddd!.Role as Violator.Instance)!;
                    }
                    if (violator == null)
                    {
                        return;
                    }
                    currentKillTime -= UnityEngine.Random.Range(2f, 6f);
                    NebulaManager.Instance.StartDelayAction(UnityEngine.Random.Range(3f, 11f), () =>
                    {
                        if (MeetingHud.Instance.AsBoolFast())
                        {
                            return;
                        }
                        if (Mathn.Prob(1f - (KillChance / 100f)))
                        {
                            return;
                        }
                        var deadplayer = local;
                        if (Mathn.Prob(0.4f))
                        {
                            deadplayer = closeplayers.Random();
                        }
                        closeplayers.Clear();
                        foreach (var p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                        {
                            if ((p.TruePosition - deadplayer.TruePosition).ToUnityVector().magnitude <= 2.7f && p.PlayerId != deadplayer.PlayerId && !(p.Role is Violator.Instance))
                            {
                                closeplayers.Add(p);
                            }
                        }
                        if (closeplayers.Count >= 1)
                        {
                            int killtype = UnityEngine.Random.Range(0, 3);
                            if (killtype == 0 && usebubblenum >= Bubblegun.maxBubblesOption)
                            {
                                killtype = UnityEngine.Random.Range(1, 3);
                            }
                            if (killtype == 0)
                            {
                                usebubblenum++;
                                SetUseBubbleNum.Invoke(usebubblenum);
                                if (deadplayer.IsDead)
                                {
                                    return;
                                }
                                bool isbait = deadplayer.Role.GetAbility<Bait.Ability>() != null || deadplayer.Role.GetAbility<Provocateur.Ability>() != null || deadplayer.Modifiers.Any(r => r is BaitM);
                                if (isbait)
                                {
                                    Bubblegun.RpcBubbleKill.Invoke((deadplayer, deadplayer, deadplayer.Position, 0));
                                }
                                else
                                {
                                    Bubblegun.RpcBubbleKill.Invoke((violator.MyPlayer, deadplayer, deadplayer.Position, 0));
                                }
                                closeplayers.Do(p =>
                                {
                                    if (p.IsDead)
                                    {
                                        return;
                                    }
                                    if (!(p.Role is Violator.Instance))
                                    {
                                        bool isbaits = p.Role.GetAbility<Bait.Ability>() != null || p.Role.GetAbility<Provocateur.Ability>() != null || p.Modifiers.Any(r => r is BaitM);
                                        Bubblegun.RpcBubbleKill.Invoke((isbaits ? p : violator.MyPlayer, p, p.Position, 0));
                                    }
                                });
                            }
                            else
                            {
                                if (killtype == 1)
                                {
                                    NebulaManager.Instance.StartDelayAction(1f, () =>
                                    {
                                        NebulaAsset.PlaySE(VanillaAsset.HnSTransformClip.Clip, deadplayer.VanillaPlayer.transform.position, 0.8f, Berserker.berserkSEStrengthOption, 0.8f, false);
                                    });
                                }
                                if (deadplayer.IsDead)
                                {
                                    return;
                                }
                                if (!(deadplayer.Role is Violator.Instance))
                                {
                                    bool isbait = deadplayer.Role.GetAbility<Bait.Ability>() != null || deadplayer.Role.GetAbility<Provocateur.Ability>() != null || deadplayer.Modifiers.Any(r => r is BaitM);
                                    if (isbait)
                                    {
                                        deadplayer.Suicide(killtype == 1 ? PlayerStates.Dead : PlayerState.Drill, killtype == 1 ? EventDetails.Kill : null, KillParameter.RemoteKill);
                                    }
                                    else
                                    {
                                        violator.MyPlayer.MurderPlayer(deadplayer, killtype == 1 ? PlayerStates.Dead : PlayerState.Drill, killtype == 1 ? EventDetails.Kill : null, KillParameter.RemoteKill);
                                    }
                                }
                                closeplayers.Do(p =>
                                {
                                    if (p.IsDead)
                                    {
                                        return;
                                    }
                                    if (p.Role is not Violator.Instance)
                                    {
                                        bool isb = p.Role.GetAbility<Bait.Ability>() != null || p.Role.GetAbility<Provocateur.Ability>() != null || p.Modifiers.Any(r => r is BaitM);
                                        if (isb)
                                        {
                                            p.Suicide(killtype == 1 ? PlayerStates.Dead : PlayerState.Drill, killtype == 1 ? EventDetails.Kill : null, KillParameter.RemoteKill);
                                        }
                                        else
                                        {
                                            violator.MyPlayer.MurderPlayer(p, killtype == 1 ? PlayerStates.Dead : PlayerState.Drill, killtype == 1 ? EventDetails.Kill : null, KillParameter.RemoteKill);
                                        }
                                    }
                                });
                            }
                        }
                        else
                        {
                            if (deadplayer.IsDead)
                            {
                                return;
                            }
                            if (!(deadplayer.Role is Violator.Instance))
                            {
                                bool isb = deadplayer.Role.GetAbility<Bait.Ability>() != null || deadplayer.Role.GetAbility<Provocateur.Ability>() != null || deadplayer.Modifiers.Any(r => r is BaitM);
                                if (isb)
                                {
                                    deadplayer.Suicide(PlayerStates.Dead, EventDetails.Kill, KillParameter.RemoteKill);
                                }
                                else
                                {
                                    violator.MyPlayer.MurderPlayer(deadplayer, PlayerStates.Dead, EventDetails.Kill, KillParameter.RemoteKill);
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        [HarmonyPatch(typeof(EmergencyMinigame), "Begin"), HarmonyPostfix]
        public static void BeginPost(EmergencyMinigame __instance)
        {
            try
            {
                isSpecial = false;
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        static Rect bgRect = new Rect(1, 500, 503, 503);
        [HarmonyPatch(typeof(EmergencyMinigame), "Update"), HarmonyPostfix]
        public static void EmergencyButtonUpdate(EmergencyMinigame __instance)
        {
            if (GamePlayer.LocalPlayer!.Role is Violator.Instance)
            {
                return;
            }
            if (!CanInvokeSpecialMeeting)
            {
                return;
            }
            if (Input.GetMouseButtonDown(1))
            {
                isSpecial = !isSpecial;
            }
            if (isSpecial)
            {
                var spr = __instance.transform.FindChild("Background").GetComponent<SpriteRenderer>();
                var stream = NebulaAPI.AddonAsset!.GetResource("SpecialMeetingButton.png")!.AsStream();
                var texture = GraphicsHelper.LoadTextureFromStream(stream!);
                var sp = Sprite.Create(texture, bgRect, UnityEngine.Vector2.one / 2);
                if (Background == null)
                {
                    Background = spr.sprite;
                }
                spr.sprite = sp;
                spr = __instance.transform.FindChild("Background").GetChild(0).GetComponent<SpriteRenderer>();
                sp = Sprite.Create(texture, bg2 == null ? spr.sprite.rect : bg2.rect, UnityEngine.Vector2.one / 2);
                if (bg2 == null)
                {
                    bg2 = spr.sprite;
                }
                spr.sprite = sp;
                spr = __instance.transform.FindChild("MeetingButton").GetChild(0).GetComponent<SpriteRenderer>();
                sp = Sprite.Create(texture, meetingbutton == null ? spr.sprite.rect : meetingbutton.rect, UnityEngine.Vector2.one / 2);
                if (meetingbutton == null)
                {
                    meetingbutton = spr.sprite;
                }
                spr.sprite = sp;
                if (__instance.state == 1)
                {
                    __instance.StatusText.text = Language.Translate("options.diagnosis.meeting.status");
                    __instance.NumberText.text = !usedSpecialMeeting ? "1" : "0";
                    __instance.ButtonActive = !usedSpecialMeeting;
                    __instance.ClosedLid.gameObject.SetActive(!__instance.ButtonActive);
                    __instance.OpenLid.gameObject.SetActive(__instance.ButtonActive);
                }
            }
            else
            {
                if (Background != null && bg2 != null && meetingbutton != null)
                {
                    __instance.transform.FindChild("Background").GetComponent<SpriteRenderer>().sprite = Background;
                    __instance.transform.FindChild("Background").GetChild(0).GetComponent<SpriteRenderer>().sprite = bg2;
                    __instance.transform.FindChild("MeetingButton").GetChild(0).GetComponent<SpriteRenderer>().sprite = meetingbutton;
                    if (__instance.state == 1)
                    {
                        int remainingEmergencies = AmongUsLLImpl.LocalPlayer.RemainingEmergencies;
                        __instance.StatusText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.EmergencyCount, new Il2CppReferenceArray<Il2CppSystem.Object>(new Il2CppSystem.Object[] { AmongUsLLImpl.LocalPlayer.Data.PlayerName }));
                        __instance.NumberText.text = remainingEmergencies.ToString();
                    }
                }
                if (__instance.state == 1)
                {
                    __instance.StatusText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.EmergencyCount, new Il2CppReferenceArray<Il2CppSystem.Object>(new Il2CppSystem.Object[] { AmongUsLLImpl.LocalPlayer.Data.PlayerName })) + Language.Translate("options.diagnosis.meetingbutton.tips");
                }
            }
        }
        [HarmonyPatch(typeof(EmergencyMinigame), "CallMeeting"), HarmonyPrefix]
        public static bool CallMeeting(EmergencyMinigame __instance)
        {
            if (GamePlayer.LocalPlayer!.Role is Violator.Instance)
            {
                return true;
            }
            if (isSpecial)
            {
                if (!Enumerable.Any<PlayerTask>(AmongUsLLImpl.LocalPlayer.myTasks.GetFastEnumerator(), new Func<PlayerTask, bool>(PlayerTask.TaskIsEmergency)) && !usedSpecialMeeting && __instance.ButtonActive)
                {
                    __instance.StatusText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.EmergencyRequested, new Il2CppReferenceArray<Il2CppSystem.Object>(Array.Empty<Il2CppSystem.Object>()));
                    if (Constants.ShouldPlaySfx())
                    {
                        AmongUsLLImpl.SoundManagerInstance.PlaySound(__instance.ButtonSound, false, 1f, null);
                    }
                    AmongUsLLImpl.LocalPlayer.CmdReportDeadBody(null);
                    __instance.ButtonActive = false;
                    VibrationManager.Vibrate(1f, 1f, 0.2f, VibrationManager.VibrationFalloff.None, null, false, "");
                    RpcStartSpecialMeeting.Invoke(true);
                }
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(PlayerModInfo), "UpdateNameText"), HarmonyPrefix]
        public static bool UpdateNameText(TextMeshPro nameText, bool onMeeting = false, bool showDefaultName = false)
        {
            if (isSpecialMeeting && onMeeting)
            {
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(MeetingHud), "UpdateTimerText"), HarmonyPostfix]
        public static void UpdateTimerText(MeetingHud __instance, StringNames key)
        {
            if (isSpecialMeeting && key == StringNames.MeetingVotingEnds)
            {
                var str = string.Format(DestroyableSingleton<TranslationController>.Instance.GetString(key, new Il2CppReferenceArray<Il2CppSystem.Object>(Array.Empty<Il2CppSystem.Object>())), "∞");
                __instance.TimerText.text = str;
            }
        }
        [HarmonyPatch(typeof(CheckForEndVotingPatch), "Prefix"), HarmonyPrefix]
        public static bool BlockNoS()
        {
            if (isSpecialMeeting)
            {
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(CrewmateGameRule), "CheckTaskWin"), HarmonyPrefix]
        public static bool CrewmateTaskCheck()
        {
            if (!IsEnabled)
            {
                return true;
            }
            int quota = 0;
            int completed = 0;
            foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo)
            {
                if (p.IsDisconnected) continue;

                if (!p.Tasks.IsCrewmateTask) continue;
                quota += p.Tasks.Quota;
                completed += p.Tasks.TotalCompleted;
            }
            if (quota > 0 && quota <= completed) NebulaAPI.CurrentGame?.TriggerGameEnd(NobodyAliveEnd, GameEndReason.Special);
            return false;
        }
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting)), HarmonyPrefix]
        public static bool CheckEndVotingPatch(MeetingHud __instance)
        {
            if (!isSpecialMeeting)
            {
                return true;
            }
            //投票が済んでない場合、なにもしない
            if (!__instance.playerStates.All((PlayerVoteArea ps) =>
            {
                if (ps.PlayerId == yesId || ps.PlayerId == noId)
                {
                    if (GamePlayer.GetPlayer(ps.PlayerId)!.IsDead)
                    {
                        return true;
                    }
                }
                return ps.AmDead || ps.DidVote || !MeetingHudExtension.HasVote(ps.PlayerId);
            }))
            {
                return false;
            }

            {
                Dictionary<byte, int> dictionary = ModCalculateVotes(__instance);
                KeyValuePair<byte, int> max = dictionary.MaxPairV2(out bool tie);

                List<byte> extraVotes = new();

                if (tie)
                {
                    Dictionary<byte, GamePlayer?> voteForMap = new();

                    foreach (var state in __instance.playerStates)
                    {
                        if (!state.DidVote || state.VotedForId == 254) continue;
                        voteForMap[state.PlayerId] = NebulaGameManager.Instance?.GetPlayer(state.VotedForId);
                    }

                    foreach (var target in GameOperatorManager.Instance?.Run(new MeetingTieVoteHostEvent(voteForMap))?.ExtraVotes ?? [])
                    {
                        dictionary.AddValueV2(target?.PlayerId ?? 253, 1);
                        extraVotes.Add(target?.PlayerId ?? 253);
                    }

                    //再計算する
                    max = dictionary.MaxPairV2(out tie);
                }

                bool ResultisYes = max.Key == yesId;
                GamePlayer exiled = null!;
                GamePlayer[] exiledAll = [];
                var violator = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => p.Role is Violator.Instance);
                var mask = BitMasks.AsPlayer();
                if (violator != null)
                {
                    if (!violator.IsDead)
                    {
                        mask.Add(violator);
                    }
                }
                if (!tie)
                {
                    if (IsEnabled)
                    {
                        if (ResultisYes)
                        {
                            SetExileMessageRpc.Invoke(1);
                            GameOperatorManager.Instance!.Subscribe<TaskPhaseStartEvent>(ev =>
                            {
                                NebulaAPI.CurrentGame!.TriggerGameEnd(NebulaGameEnd.CrewmateWin, GameEndReason.Special);
                            }, NebulaAPI.CurrentGame!);
                        }
                        else
                        {
                            SetExileMessageRpc.Invoke(2);
                            GameOperatorManager.Instance!.Subscribe<TaskPhaseStartEvent>(ev =>
                            {
                                NebulaAPI.CurrentGame!.TriggerGameEnd(NobodyAliveEnd, GameEndReason.Special, mask);
                            }, NebulaAPI.CurrentGame!);
                            exiledAll = GamePlayer.AllPlayers.Where(v => !v.IsDead && v.PlayerId != violator?.PlayerId).ToArray();
                            exiled = exiledAll.First();
                        }
                    }
                    else
                    {
                        if (ResultisYes)
                        {
                            SetExileMessageRpc.Invoke(1);
                            GameOperatorManager.Instance!.Subscribe<TaskPhaseStartEvent>(ev =>
                            {
                                NebulaAPI.CurrentGame!.TriggerGameEnd(NobodyAliveEnd, GameEndReason.Special);
                            }, NebulaAPI.CurrentGame!);
                            exiledAll = GamePlayer.AllPlayers.Where(v => !v.IsDead).ToArray();
                            exiled = exiledAll.First();
                        }
                        else
                        {
                            SetExileMessageRpc.Invoke(3);
                        }
                    }
                }
                if (tie)
                {
                    RpcStartSpecialMeeting.Invoke(false);
                }
                List<MeetingHud.VoterState> allStates = new();

                //記名投票分
                foreach (var state in __instance.playerStates)
                {
                    if (!state.DidVote) continue;

                    if (!MeetingHudExtension.WeightMap.TryGetValue((byte)state.PlayerId, out var vote)) vote = 1;

                    for (int i = 0; i < vote; i++)
                    {
                        allStates.Add(new MeetingHud.VoterState
                        {
                            VoterId = state.PlayerId,
                            VotedForId = state.VotedForId
                        });
                    }
                }

                //追加投票分
                foreach (var votedFor in extraVotes)
                {
                    allStates.Add(new MeetingHud.VoterState
                    {
                        VoterId = byte.MaxValue,
                        VotedForId = votedFor
                    });
                }

                //Debug.Log($"Exiled: ({string.Join(',', (exiledAll ?? []).Select(b => b.ToString()))})");
                MeetingModRpc.RpcModCompleteVoting.Invoke((allStates, new List<ValueTuple<byte, byte>>(), exiled?.PlayerId ?? byte.MaxValue, exiledAll?.Select(e => e.PlayerId).ToArray() ?? [], tie, false));
                //サーバー用、プレイヤーは全員このメッセージを無視する
                //__instance.RpcVotingComplete(allStates.ToArray(), Helpers.GetPlayer(exiled?.PlayerId)?.Data, tie);
            }
            return false;
        }
        /*[HarmonyPatch(typeof(MeetingHud), "UpdateButtons"), HarmonyPrefix]
        public static bool UpdateButtonBlock(MeetingHud __instance)
        {
            if (isSpecialMeeting)
            {
                if (AmongUsLLImpl.LocalPlayer.Data.IsDead && !__instance.hasForegroundForDeadBeenSet)
                {
                    __instance.SetForegroundForDead();
                }
                if (AmongUsLLImpl.AmongUsClientInstance.AmHost)
                {
                    for (int i = 0; i < __instance.playerStates.Length; i++)
                    {
                        var state = __instance.playerStates[i];
                        if (state.PlayerId == yesId || state.PlayerId == noId)
                        {
                            state.SetDead(false);
                            state.SetEnabled();
                        }
                    }
                }
                return false;
            }
            return true;
        }*/
        [HarmonyPatch(typeof(MeetingHudExtension), "CanVoteFor", typeof(byte)), HarmonyPostfix]
        public static void MeetingHudCanVoteFor(MeetingHud __instance, byte playerId, ref bool __result)
        {
            if (!isSpecialMeeting)
            {
                return;
            }
            if (playerId == yesId || playerId == noId)
            {
                __result = true;
            }
        }
        [HarmonyPatch(typeof(MeetingPlayerButtonManager), "Update"), HarmonyPostfix]
        public static void BlockMeetingAction(MeetingPlayerButtonManager __instance)
        {
            if (isSpecialMeeting)
            {
                __instance.ResetActions();
            }
        }
        public static int ExileMessageType = 0;
        public static Dictionary<byte, int> ModCalculateVotes(MeetingHud __instance)
        {
            Dictionary<byte, int> dictionary = new();

            List<string> log = new();
            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                var player = NebulaGameManager.Instance?.GetPlayer(playerVoteArea.PlayerId);
                if (player?.IsDead ?? true) continue;

                bool didVote = playerVoteArea.VotedForId != 252 && playerVoteArea.VotedForId != 255 && playerVoteArea.VotedForId != 254;
                if (!MeetingHudExtension.WeightMap.TryGetValue((byte)playerVoteArea.PlayerId, out var vote)) vote = 1;
                var ev = GameOperatorManager.Instance!.Run(new PlayerFixVoteHostEvent(player, didVote, NebulaGameManager.Instance?.GetPlayer(playerVoteArea.VotedForId), vote));

                if (ev.DidVote)
                {
                    dictionary.AddValueV2(ev.VoteTo?.PlayerId ?? PlayerVoteArea.SkippedVote, ev.Vote);
                    playerVoteArea.VotedForId = ev.VoteTo?.PlayerId ?? PlayerVoteArea.SkippedVote;
                    MeetingHudExtension.WeightMap[player.PlayerId] = ev.Vote;
                }
                else
                {
                    playerVoteArea.VotedForId = PlayerVoteArea.MissedVote;
                }
            }


            return dictionary;
        }


        static RemoteProcess<bool> RpcStartSpecialMeeting = new("RpcStartspecialMeeting", (m, _) =>
        {
            usedSpecialMeeting = m;
            isSpecialMeeting = m;
        });
        public static bool startmeeting;
        public static void OnSpecialMeetingUpdate()
        {
            if ((MeetingHud.Instance.state == MeetingHud.MeetingStates.NotVoted || MeetingHud.Instance.state == MeetingHud.MeetingStates.Voted) && !startmeeting)
            {
                startmeeting = true;
                NebulaManager.Instance.StartDelayAction(1f, () =>
                {
                    MeetingModRpc.RpcChangeVotingStyle.LocalInvoke((255, false, 10000000f, false, false));
                    MeetingHudExtension.CanShowPhotos = false;
                    foreach (var p in MeetingHud.Instance.playerStates) p.SetDisabled();
                    var sMeeting = UnityHelper.CreateObject<SpecialMeetingHud>("SpecialMeeting", MeetingHud.Instance.transform, UnityEngine.Vector3.zero);
                    sMeeting.Begin(() =>
                    {
                        int votemask = 0;
                        votemask |= 1 << yesId;
                        votemask |= 1 << noId;
                        MeetingModRpc.RpcChangeVotingStyle.LocalInvoke((votemask, false, 10000000f, false, false));
                    });
                });
            }
        }

        public static bool isSpecialMeeting;
        static bool isSpecial;
        public static bool usedSpecialMeeting;
        static Sprite? Background, bg2, meetingbutton;
        public static byte yesId = 1, noId = 1;
        static Image SpecialMeetingButton = NebulaAPI.AddonAsset.GetResource("SpecialMeetingButton.png")!.AsImage()!;
        /*static readonly RemoteProcess RpcJusticePlusMeeting = new("JusticePlusMeeting",
    _ => {
        MeetingModRpc.RpcChangeVotingStyle.LocalInvoke((255, false, 2000000f, true, false));
        MeetingHudExtension.CanShowPhotos = false;
        foreach (var p in MeetingHud.Instance.playerStates) p.SetDisabled();
        var sMeeting = UnityHelper.CreateObject<SpecialMeetingHud>("SpecialMeeting", MeetingHud.Instance.transform, Vector3.zero);
        sMeeting.Begin(() =>
        {
            int votemask = 0;
            votemask |= 1 << 1;
            votemask |= 1 << 2;
            MeetingModRpc.RpcChangeVotingStyle.LocalInvoke((votemask, false, 2000000f, true, false));
        });
    });*/
        public class SpecialMeetingHud : MonoBehaviour
        {
            static SpecialMeetingHud() => ClassInjector.RegisterTypeInIl2Cpp<SpecialMeetingHud>();
            static private readonly SpriteLoader meetingBackMaskSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.MeetingUIMask.png", 100f);
            static private readonly SpriteLoader meetingReticleSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeMeetingReticle.png", 100f);
            static private readonly Image meetingViewSprite = NebulaAPI.AddonAsset.GetResource("SpecialMeetingView.png")!.AsImage()!;
            static private readonly Image votingHolderLeftSprite = NebulaAPI.AddonAsset.GetResource("SpecialMeetingLeft.png")!.AsImage()!;
            static private readonly Image votingHolderRightSprite = NebulaAPI.AddonAsset.GetResource("SpecialMeetingRight.png")!.AsImage()!;
            static private readonly SpriteLoader votingHolderMaskSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeHolderMask.png", 120f);
            static private readonly SpriteLoader votingHolderFlashSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeHolderFlash.png", 120f);
            static private readonly SpriteLoader votingHolderBlurSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeHolderFlashBlur.png", 120f);

            SpriteRenderer? Background1, Background2, BackView, UpView;

            // 位置参数
            Vector3 leftPos = new Vector3(-2f, 0f, 0f);
            Vector3 rightPos = new Vector3(2f, 0f, 0f);
            Vector3 CenterPos = new Vector3(0f, 0f, 0f);
            Vector3 leftOutPos = new Vector3(-10f, 0f, 0f);
            Vector3 rightOutPos = new Vector3(10f, 0f, 0f);

            private IEnumerator CoAnimColor(SpriteRenderer renderer, Color color1, Color color2, float duration)
            {
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    renderer.color = UnityEngine.Color.Lerp(color1.ToUnityColor(), color2.ToUnityColor(), t / duration);
                    yield return null;
                }
                renderer.color = color2.ToUnityColor();
            }

            private IEnumerator CoAnimColorRepeat(SpriteRenderer renderer, Color color1, Color color2, float duration)
            {
                while (true)
                {
                    yield return CoAnimColor(renderer, color1, color2, duration);
                    yield return CoAnimColor(renderer, color2, color1, duration);
                }
            }
            public void Begin(Action onMeetingStart)
            {
                StartCoroutine(SetUpJusticeMeeting(MeetingHud.Instance, onMeetingStart).WrapToIl2Cpp());
            }

            private static string[] RandomTexts = ["(despired)", "terminus", "<revolt>", "solitary", "bona vacantia", "despotism", "pizza", "elitism", "suspicion", "justice", "outsider", "discrepancy", "purge", "uniformity", "conviction", "tribunal", "triumph", "heroism", "u - majority"];
            private static string[] RandomAltTexts = ["HERO", "victor", "supreme", "the one", "genius", "prodigy", "detective", "clairvoyant"];
            IEnumerator CoDisappearVotingArea(MeetingHud meetingHud, float duration)
            {
                var states = meetingHud.playerStates.OrderBy(i => Guid.NewGuid()).ToArray();
                var interval = duration / states.Length;
                foreach (var state in states)
                {
                    state.gameObject.SetActive(false);
                    yield return Effects.Wait(interval);
                }
            }

            IEnumerator CoAnimBackLine(GameObject parent)
            {
                var lineRenderer = UnityHelper.CreateObject<SpriteRenderer>("Line", parent.transform, new(0.01f, 0f, -18f));
                lineRenderer.sprite = VanillaAsset.FullScreenSprite;
                lineRenderer.color = UnityEngine.Color.black.AlphaMultiplied(0.85f);
                lineRenderer.transform.localScale = new(20f, 0f, 1f);

                float t = 0f;
                float p = 0f;
                while (t < 2f && parent)
                {
                    p += (1 - p).Delta(3.5f, 0.01f);
                    lineRenderer.transform.localScale = new(8.79f, p * 0.76f, 1f);
                    t += Time.deltaTime;
                    yield return null;
                }

                yield return Effects.Wait(1.25f);
                t = 0f;
                while (t < 1f && parent)
                {
                    p -= p.Delta(6.9f, 0.01f);
                    lineRenderer.transform.localScale = new(8.79f, p * 0.76f, 1f);
                    t += Time.deltaTime;
                    yield return null;
                }
                UnityEngine.Object.Destroy(parent);
            }

            IEnumerator CoAnimIntroText(GameObject parent)
            {
                var introText = UnityHelper.CreateObject<TextMeshNoS>("IntroText", parent.transform, new(0f, 0f, -18.5f));
                introText.Font = NebulaAsset.JusticeFont;
                introText.FontSize = 0.48f;
                introText.TextAlignment = Virial.Text.TextAlignment.Center;
                introText.Pivot = new(0.5f, 0.5f);
                introText.Text = "";
                introText.Material = UnityHelper.GetMeshRendererMaterial();
                introText.Color = JusticePlus.MyRole.UnityColor;
                /*IEnumerator CoAnimTextColor(TextMeshNoS tm, Color color1, Color color2, float duration)
                {
                    float t = 0f;
                    while (t < duration)
                    {
                        t += Time.deltaTime;
                        tm.Color = Color.Lerp(color1, color2, t / duration);
                        yield return null;
                    }
                    tm.Color = color2;
                }*/
                string completedText = "A special meeting begins...";
                for (int i = 0; i < completedText.Length; i++)
                {
                    introText.Text = completedText.Substring(0, i + 1);
                    yield return Effects.Wait(0.072f);
                }
                for (int i = 0; i < 3; i++)
                {
                    introText.gameObject.SetActive(false);
                    yield return Effects.Wait(0.04f);
                    introText.gameObject.SetActive(true);
                    yield return Effects.Wait(0.04f);
                }
            }

            IEnumerator CoPlayAlertFlash()
            {
                yield return Effects.Wait(0.15f);
                for (int i = 0; i < 3; i++)
                {
                    AmongUsUtil.PlayCustomFlash(Color.Red, 0.2f, 0.2f, 0.3f, 0.6f);
                    yield return Effects.Wait(1.0f + 0.3f);
                }
            }
            IEnumerator SetUpJusticeMeeting(MeetingHud meetingHud, Action onMeetingStart)
            {
                yesId = GameData.Instance.AllPlayers.GetFastEnumerator().FirstOrDefault(p => !p.Disconnected && !p.IsDead)!.PlayerId;
                noId = GameData.Instance.AllPlayers.GetFastEnumerator().FirstOrDefault(p => !p.Disconnected && p.PlayerId != yesId)!.PlayerId;
                meetingHud.TimerText.gameObject.SetActive(false);
                StartCoroutine(CoDisappearVotingArea(meetingHud, 2.3f).WrapToIl2Cpp());
                yield return Effects.Wait(0.1f);
                StartCoroutine(CoDisappearVotingArea(meetingHud, 2.3f).WrapToIl2Cpp());
                yield return Effects.Wait(0.9f);
                NebulaAsset.PlaySE(NebulaAudioClip.Justice2);
                //GameObject.Destroy(introObj);
                meetingHud.TimerText.gameObject.SetActive(true);
                PDebug.Log("SpecialMeetingStart");
                onMeetingStart.Invoke();

                //タイトルテキストが少し右にずれているので修正
                meetingHud.TitleText.transform.localPosition = new(-0.25f, 2.2f, -1f);
                meetingHud.TitleText.text = Language.Translate("options.diagnosis.meeting.title");
                string textid = string.Empty;
                switch (AmongUsLLImpl.Instance.MapId)
                {
                    case 0:
                    case 3:
                    case 4:
                        textid = "skeldandairship";
                        break;
                    case 1:
                    case 2:
                        textid = "miraandpolus";
                        break;
                    case 5:
                        textid = "fungle";
                        break;
                }
                meetingHud.TitleText.text += Environment.NewLine + Language.Translate("options.diagnosis.meeting.prefix").Replace("%TYPE%", Language.Translate("options.diagnosis.meeting." + textid));
                //背景を作る
                var backObj = UnityHelper.CreateObject<SortingGroup>("JusticeBackground", transform, new(0f, 0f, 7f));
                var mask = UnityHelper.CreateObject<SpriteMask>("JusticeMask", backObj.transform, UnityEngine.Vector3.zero);
                mask.transform.localScale = new(1.2f, 1f, 1f);
                mask.sprite = meetingBackMaskSprite.GetSprite();
                PDebug.Log("ShowAnimEnd");
                var backReticle = UnityHelper.CreateSpriteRenderer("JusticeBackReticle", backObj.transform, new(0f, 0f, -0.2f));
                backReticle.transform.localScale = new(0.69f, 0.69f, 1f);
                backReticle.sprite = meetingReticleSprite.GetSprite();
                backReticle.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

                var reticleText = NebulaAsset.InstantiateText("ReticleText", backObj.transform, new(0f, -1.9f, -0.2f), NebulaAsset.JusticeFont, 0.42f, Virial.Text.TextAlignment.Center, new(0.5f, 0.5f), "", new(0.7f, 0.7f, 0.7f, 0.4f));

                IEnumerator CoAnimText(string text)
                {
                    reticleText.Text = "";
                    reticleText.gameObject.SetActive(true);
                    yield return null;
                    string targetText = text;
                    for (int i = 1; i <= targetText.Length; i++)
                    {
                        reticleText.Text = targetText.Substring(0, i);
                        yield return Effects.Wait(0.08f);
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        reticleText.gameObject.SetActive(false);
                        yield return Effects.Wait(0.05f);
                        reticleText.gameObject.SetActive(true);
                        yield return Effects.Wait(0.05f);
                    }

                    yield return Effects.Wait(5f + System.Random.Shared.NextSingle() * 8f);

                    for (int i = 0; i < 3; i++)
                    {
                        reticleText.gameObject.SetActive(false);
                        yield return Effects.Wait(0.05f);
                        reticleText.gameObject.SetActive(true);
                        yield return Effects.Wait(0.05f);
                    }
                    reticleText.gameObject.SetActive(false);
                    yield return Effects.Wait(0.6f + System.Random.Shared.NextSingle() * 0.6f);
                }
                IEnumerator CoRepeatAnimText()
                {
                    string[] texts = [];
                    int index = 0;
                    while (true)
                    {
                        if (index == texts.Length)
                        {
                            texts = RandomTexts.OrderBy(_ => Guid.NewGuid()).ToArray();
                            index = 0;
                        }

                        yield return CoAnimText(texts[index++]);
                    }
                }
                StartCoroutine(CoRepeatAnimText().WrapToIl2Cpp());
                PDebug.Log("GenBackView");

                BackView = UnityHelper.CreateSpriteRenderer("JusticeBackView", backObj.transform, new(0f, 0f, -0.2f));
                BackView.transform.localScale = new(1f, 1f, 1f);
                BackView.sprite = meetingViewSprite.GetSprite();
                BackView.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                IEnumerator CoAnimView()
                {
                    BackView.color = new(0.5f, 0.5f, 0.5f, 0.4f);

                    while (true)
                    {
                        yield return Effects.Wait(5.5f + System.Random.Shared.NextSingle() * 3f);

                        switch (System.Random.Shared.Next(3))
                        {
                            case 0:
                                yield return CoAnimColor(BackView, new(1f, 0.3f, 0.3f, 0.5f), new(0.5f, 0.5f, 0.5f, 0.5f), 0.8f);
                                break;
                            case 1:
                                yield return CoAnimColor(BackView, new(0.9f, 0.3f, 0.3f, 0.5f), new(0.5f, 0.5f, 0.5f, 0.5f), 0.3f);
                                yield return Effects.Wait(0.2f);
                                yield return CoAnimColor(BackView, new(1f, 0.3f, 0.3f, 0.5f), new(0.5f, 0.5f, 0.5f, 0.5f), 1.4f);
                                break;
                            case 2:
                                yield return CoAnimColor(BackView, new(0.5f, 0.5f, 0.5f, 0.5f), new(1f, 1f, 1f, 0.75f), 0.2f);
                                yield return CoAnimColor(BackView, new(1f, 1f, 1f, 0.75f), new(0.5f, 0.5f, 0.5f, 0.5f), 0.8f);
                                break;
                        }
                    }
                }
                PDebug.Log("CoAnimView");
                StartCoroutine(CoAnimView().WrapToIl2Cpp());
                PDebug.Log("AnimViewEnd");
                meetingHud.playerStates.Do(p => p.gameObject.SetActive(false));

                var boardPassGame = VanillaAsset.MapAsset[2].CommonTasks.FirstOrDefault(p => p.MinigamePrefab.name == "BoardingPassGame")?.MinigamePrefab.TryCast<BoardPassGame>();
                void SpawnVotingArea(bool yes, Vector3 localPos, Virial.Media.Image holder)
                {
                    IEnumerator CoSpawnPlayerArea(bool isyes)
                    {
                        yield return Effects.Wait(1.3f);
                        PDebug.Log("GenSpecialMeetingPlayerArea");
                        var back = UnityHelper.CreateSpriteRenderer("JusticePlayerArea", transform, localPos + new Vector3(0f, 0f, 6f));
                        back.gameObject.AddComponent<SortingGroup>();
                        back.transform.localScale = new(0.6f, 0.6f, 1f);
                        back.sprite = holder.GetSprite();
                        //back.material = HatManager.Instance.PlayerMaterial;
                        yield return StartCoroutine(CoAnimColor(back, new(1f, 1f, 1f, 0f), new(1f, 1f, 1f, 1f), 0.5f).WrapToIl2Cpp());
                        try
                        {
                            var mask = UnityHelper.CreateObject<SortingGroup>("Masked", back.transform, new(0f, 0f, -0.5f));
                            var maskRenderer = UnityHelper.CreateObject<SpriteMask>("Mask", mask.transform, UnityEngine.Vector3.zero);
                            maskRenderer.sprite = votingHolderMaskSprite.GetSprite();
                            PDebug.Log("SetVoteAreaPos");
                            PlayerVoteArea playerVoteArea = MeetingHud.Instance.playerStates.FirstOrDefault(p => p.PlayerId == (isyes ? yesId : noId))!;
                            if (playerVoteArea != null)
                            {
                                //playerVoteArea.transform.localScale = new Vector2(0.85f, 0.85f);
                                playerVoteArea.Background.sprite = AmongUsLLImpl.ShipStatusInstance.CosmeticsCache.GetNameplate("1111111").Image;
                                playerVoteArea.PlayerIcon.gameObject.SetActive(false);
                                playerVoteArea.LevelNumberText.transform.parent.gameObject.SetActive(false);
                                playerVoteArea.NameText.text = Language.Translate("options.diagnosis.meeting." + (isyes ? "yes" : "no"));
                                playerVoteArea.ColorBlindName.text = "";//Language.Translate("options.diagnosis.meeting." + (isyes ? "yes" : "no"));
                                playerVoteArea.SetDead(false);
                                playerVoteArea.gameObject.SetActive(true);
                                playerVoteArea.gameObject.transform.localPosition = localPos + new Vector3(-0.22f, 0f, -0.9f);
                            }
                        }
                        catch (Exception E)
                        {
                            PDebug.Log(E);
                        }
                    }
                    StartCoroutine(CoSpawnPlayerArea(yes).WrapToIl2Cpp());
                }
                SpawnVotingArea(true, leftPos, votingHolderLeftSprite);
                SpawnVotingArea(false, rightPos, votingHolderRightSprite);

                yield break;
            }
        }
    }
    static HNSModules.NormalGameLogicHnSDangerLevel? LogicDangerLevel;
    static HNSModules.NormalGameLogicHnSMusic? LogicMusic;
    [NebulaPreprocess(PreprocessPhase.PostFixStructure)]
    public class HNSModules
    {
        public static void Preprocess(NebulaPreprocessor preprocessor)
        {
            PDebug.Log("Add HNS Modules");
            DIManager.Instance.RegisterGeneralModule<IGameModeStandard>(() =>
            {
                if (!IntroShowImpostor || !CrewmateHasEchoSkill)
                {
                    LogicDangerLevel = null;
                    return null;
                }
                LogicDangerLevel = new NormalGameLogicHnSDangerLevel().RegisterPermanently();
                return LogicDangerLevel;
            });
            DIManager.Instance.RegisterGeneralModule<IGameModeFreePlay>(() =>
            {
                if (!IntroShowImpostor || !CrewmateHasEchoSkill)
                {
                    LogicDangerLevel = null;
                    return null;
                }
                LogicDangerLevel = new NormalGameLogicHnSDangerLevel().RegisterPermanently();
                return LogicDangerLevel;
            });
            DIManager.Instance.RegisterGeneralModule<IGameModeStandard>(() =>
            {
                if (!IntroShowImpostor || !CrewmateHasEchoSkill)
                {
                    LogicMusic = null;
                    return null;
                }
                LogicMusic = new NormalGameLogicHnSMusic().RegisterPermanently();
                return LogicMusic;
            });
            DIManager.Instance.RegisterGeneralModule<IGameModeFreePlay>(() =>
            {
                if (!IntroShowImpostor || !CrewmateHasEchoSkill)
                {
                    LogicMusic = null;
                    return null;
                }
                LogicMusic = new NormalGameLogicHnSMusic().RegisterPermanently();
                return LogicMusic;
            });
            DIManager.Instance.RegisterGeneralModule<IGameModeStandard>(() =>
            {
                if (!CanSeePlayerState)
                {
                    return null;
                }
                return new CrewmateStateTrackerTrigger().RegisterPermanently();
            });
            DIManager.Instance.RegisterGeneralModule<IGameModeFreePlay>(() =>
            {
                if (!CanSeePlayerState)
                {
                    return null;
                }
                return new CrewmateStateTrackerTrigger().RegisterPermanently();
            });
        }
        public class NormalGameLogicHnSDangerLevel : IModule, IGameOperator
        {
            public NormalGameLogicHnSDangerLevel()
            {
            }
            void OnRoleSet(PlayerRoleSetEvent ev)
            {
                if (ev.Role.Role.Category == RoleCategory.ImpostorRole)
                {
                    impostors?.Add(ev.Player.VanillaPlayer);
                }
            }
            void OnGameStart(GameStartEvent ev)
            {
                firstMusicActivation = true;
                if (!GamePlayer.LocalPlayer!.IsImpostor)
                {
                    dangerMeter = AmongUsLLImpl.HudManagerInstance.DangerMeter;
                    dangerMeter.gameObject.SetActive(true);
                }
                impostors = new List<PlayerControl>();
                foreach (var player in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                {
                    if (player.IsImpostor)
                    {
                        impostors.Add(player.VanillaPlayer);
                    }
                }
                scaryMusicDistance = 55f * AmongUsLLImpl.CurrentGameOptionsInstance.GetFloat(FloatOptionNames.PlayerSpeedMod);
                veryScaryMusicDistance = 15f * AmongUsLLImpl.CurrentGameOptionsInstance.GetFloat(FloatOptionNames.PlayerSpeedMod);
                if (scaryMusicDistance < veryScaryMusicDistance)
                {
                    float num = veryScaryMusicDistance;
                    float num2 = scaryMusicDistance;
                    scaryMusicDistance = num;
                    veryScaryMusicDistance = num2;
                }
            }

            void OnPlayerDisconnect(PlayerDisconnectEvent ev)
            {
                if (ev.Player.IsImpostor)
                {
                    List<PlayerControl> list = impostors!;
                    if (list == null)
                    {
                        return;
                    }
                    list.Remove(ev.Player.VanillaPlayer);
                }
            }

            void OnGameEnd(GameEndEvent ev)
            {
                impostors = null;
            }

            void Update(GameUpdateEvent ev)
            {
                try
                {
                    PlayerControl localPlayer = AmongUsLLImpl.LocalPlayer;
                    if (impostors == null || localPlayer == null)
                    {
                        return;
                    }
                    if (impostors.Count <= 0)
                    {
                        return;
                    }
                    float num = float.MaxValue;
                    foreach (PlayerControl playerControl in impostors)
                    {
                        if (!(playerControl == null))
                        {
                            float sqrMagnitude = (playerControl.transform.position - localPlayer.transform.position).sqrMagnitude;
                            if (sqrMagnitude < scaryMusicDistance && num > sqrMagnitude)
                            {
                                num = sqrMagnitude;
                            }
                        }
                    }
                    if (nomusictime < ImpostorCannotMoveTime)
                    {
                        nomusictime += Time.deltaTime;
                        dangerLevel1 = 0f;
                        dangerLevel2 = 0f;
                    }
                    else
                    {
                        if (firstMusicActivation)
                        {
                            firstMusicActivation = false;
                            firstCrossfadeCountdown = 3f;
                            LogicMusic!.SetMusicCrossfadeSpeed(0.6f);
                        }
                        if (firstCrossfadeCountdown > 0f)
                        {
                            firstCrossfadeCountdown -= Time.deltaTime;
                            if (firstCrossfadeCountdown <= 0f)
                            {
                                LogicMusic!.SetMusicCrossfadeSpeed(5f);
                            }
                        }
                        dangerLevel1 = Mathn.Clamp01((scaryMusicDistance - num) / (scaryMusicDistance - veryScaryMusicDistance));
                        dangerLevel2 = Mathn.Clamp01((veryScaryMusicDistance - num) / veryScaryMusicDistance);
                    }
                    UpdateDangerMeter();
                    UpdateDangerMusic();
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
            }

            private void UpdateDangerMusic()
            {
                var localPlayer = GamePlayer.LocalPlayer;
                if (localPlayer != null && localPlayer.IsDead)
                {
                    LogicMusic!.ResetMusic();
                    return;
                }
                LogicMusic!.SetMusicValues(dangerLevel1, dangerLevel2);
            }

            private void UpdateDangerMeter()
            {
                if (dangerMeter == null)
                {
                    return;
                }
                dangerMeter.SetDangerValue(dangerLevel1, dangerLevel2);
            }
            private DangerMeter? dangerMeter;
            private List<PlayerControl>? impostors;
            private float scaryMusicDistance;
            private float veryScaryMusicDistance;
            private float dangerLevel1;
            private float dangerLevel2;
            private bool firstMusicActivation;
            private float firstCrossfadeCountdown;
        }
        public class NormalGameLogicHnSMusic : IModule, IGameOperator
        {
            bool started;
            public NormalGameLogicHnSMusic()
            {
                musicCollection = GameManagerCreator.Instance.HideAndSeekManagerPrefab.MusicCollection;
                started = false;
            }

            void OnGameStart(GameStartEvent ev)
            {
                musicCollection = GameManagerCreator.Instance.HideAndSeekManagerPrefab.MusicCollection;
                InitMusic();
                ResetMusic();
                StartMusicWithIntro();
                started = true;
            }

            void OnGameEnd(GameEndEvent ev)
            {
                ResetMusic();
            }
            private void InitMusic()
            {
                if (!HasHNSMusic)
                {
                    return;
                }
                if (normalSource == null)
                {
                    normalSource = AmongUsLLImpl.SoundManagerInstance.GetNamedSfxSource(musicNames[LogicHnSMusic.HideAndSeekMusicTrack.Normal]);
                }
                normalSource.outputAudioMixerGroup = AmongUsLLImpl.SoundManagerInstance.MusicChannel;
                normalSource.clip = musicCollection!.NormalMusic;
                normalSource.loop = true;
                if (taskSource == null)
                {
                    taskSource = AmongUsLLImpl.SoundManagerInstance.GetNamedSfxSource(musicNames[LogicHnSMusic.HideAndSeekMusicTrack.Task]);
                }
                taskSource.outputAudioMixerGroup = AmongUsLLImpl.SoundManagerInstance.MusicChannel;
                taskSource.volume = 0f;
                taskSource.clip = musicCollection.TaskMusic;
                taskSource.loop = true;
                if (dangerLevel1Source == null)
                {
                    dangerLevel1Source = AmongUsLLImpl.SoundManagerInstance.GetNamedSfxSource(musicNames[LogicHnSMusic.HideAndSeekMusicTrack.DangerLevel1]);
                }
                dangerLevel1Source.outputAudioMixerGroup = AmongUsLLImpl.SoundManagerInstance.MusicChannel;
                dangerLevel1Source.volume = 0f;
                dangerLevel1Source.clip = musicCollection.DangerLevel1Music;
                dangerLevel1Source.loop = true;
                if (dangerLevel2Source == null)
                {
                    dangerLevel2Source = AmongUsLLImpl.SoundManagerInstance.GetNamedSfxSource(musicNames[LogicHnSMusic.HideAndSeekMusicTrack.DangerLevel2]);
                }
                dangerLevel2Source.outputAudioMixerGroup = AmongUsLLImpl.SoundManagerInstance.MusicChannel;
                dangerLevel2Source.volume = 0f;
                dangerLevel2Source.clip = musicCollection.DangerLevel2Music;
                dangerLevel2Source.loop = true;
                normalSource.Play();
                taskSource.Play();
                dangerLevel1Source.Play();
                dangerLevel2Source.Play();
                SyncMusic();
            }

            public void StartMusicWithIntro()
            {
                try
                {
                    if (!HasHNSMusic)
                    {
                        return;
                    }
                    if (!GamePlayer.LocalPlayer.IsImpostor)
                    {
                        return;
                    }
                    AudioClip audioClip = musicCollection!.ImpostorShortMusic;
                    try
                    {
                        var totalTask = 0;
                        foreach (var p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                        {
                            if (!p.IsDisconnected && p.Tasks.IsCrewmateTask)
                            {
                                totalTask += p.Tasks.Quota;
                            }
                        }
                        audioClip = totalTask < 45 ? musicCollection.ImpostorShortMusic : musicCollection.ImpostorLongMusic;
                        if (AprilFoolsMode.ShouldHorseAround())
                        {
                            audioClip = musicCollection.ImpostorRanchMusic;
                        }
                    }
                    catch (Exception e)
                    {
                        PDebug.Log(e);
                    }
                    ImpostorMusic = AmongUsLLImpl.SoundManagerInstance.PlaySound(audioClip, true, 1f, AmongUsLLImpl.SoundManagerInstance.MusicChannel);
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
            }

            public void SetTaskState(bool isDoingTask)
            {
                this.isDoingTask = isDoingTask;
            }
            AudioSource? ImpostorMusic;
            void Update(GameUpdateEvent ev)
            {
                try
                {
                    if (started && ImpostorMusic == null && GamePlayer.LocalPlayer!.IsImpostor)
                    {
                        ImpostorMusic = AmongUsLLImpl.SoundManagerInstance.PlaySound(musicCollection!.ImpostorShortMusic, true, 1f, AmongUsLLImpl.SoundManagerInstance.MusicChannel);
                    }
                    if (normalSource == null || taskSource == null || dangerLevel1Source == null || dangerLevel2Source == null)
                    {
                        return;
                    }
                    if (Time.unscaledTime > lastMusicSyncTime + 1f)
                    {
                        SyncMusic();
                    }
                    normalSource.volume = Mathn.Lerp(normalSource.volume, normalVolume, Time.fixedDeltaTime * musicLerpSpeed);
                    taskSource.volume = Mathn.Lerp(taskSource.volume, taskVolume, Time.fixedDeltaTime * musicLerpSpeed);
                    dangerLevel1Source.volume = Mathn.Lerp(dangerLevel1Source.volume, dangerLevel1Volume, Time.fixedDeltaTime * musicLerpSpeed);
                    dangerLevel2Source.volume = Mathn.Lerp(dangerLevel2Source.volume, dangerLevel2Volume, Time.fixedDeltaTime * musicLerpSpeed);
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
            }

            private void SyncMusic()
            {
                taskSource!.timeSamples = normalSource!.timeSamples;
                dangerLevel1Source!.timeSamples = normalSource!.timeSamples;
                dangerLevel2Source!.timeSamples = normalSource!.timeSamples;
                lastMusicSyncTime = Time.unscaledTime;
            }

            public void ResetMusic()
            {
                SetMusicValues(0f, 0f);
            }

            public void SetMusicCrossfadeSpeed(float lerpSpeed)
            {
                musicLerpSpeed = lerpSpeed;
            }

            public void SetMusicValues(float dangerLevel1, float dangerLevel2)
            {
                if (GamePlayer.LocalPlayer != null && GamePlayer.LocalPlayer.IsImpostor)
                {
                    return;
                }
                if (normalSource == null || taskSource == null || dangerLevel1Source == null || dangerLevel2Source == null)
                {
                    return;
                }
                normalVolume = (isDoingTask ? 0f : 1f);
                taskVolume = (isDoingTask ? 1f : 0f);
                dangerLevel1Volume = 0f;
                dangerLevel2Volume = 0f;
                if (dangerLevel1 > 0f)
                {
                    dangerLevel1Volume = dangerLevel1;
                    if (isDoingTask)
                    {
                        taskVolume = 1f - dangerLevel1;
                    }
                    else
                    {
                        normalVolume = 1f - dangerLevel1;
                    }
                }
                if (dangerLevel2 > 0f)
                {
                    dangerLevel2Volume = dangerLevel2;
                    dangerLevel1Volume = 1f - dangerLevel2;
                }
            }

            private HideAndSeekMusicCollection? musicCollection;

            private float lastMusicSyncTime;

            private bool isDoingTask;

            private float normalVolume;

            private float taskVolume;

            private float dangerLevel1Volume;

            private float dangerLevel2Volume;

            private AudioSource? normalSource;

            private AudioSource? taskSource;

            private AudioSource? dangerLevel1Source;

            private AudioSource? dangerLevel2Source;

            private float musicLerpSpeed = 5f;

            private readonly Dictionary<LogicHnSMusic.HideAndSeekMusicTrack, string> musicNames = new Dictionary<LogicHnSMusic.HideAndSeekMusicTrack, string>
    {
        {
            LogicHnSMusic.HideAndSeekMusicTrack.Normal,
            "HnS_Music_Normal"
        },
        {
            LogicHnSMusic.HideAndSeekMusicTrack.Task,
            "HnS_Music_Task"
        },
        {
            LogicHnSMusic.HideAndSeekMusicTrack.DangerLevel1,
            "HnS_Music_DangerLevel1"
        },
        {
            LogicHnSMusic.HideAndSeekMusicTrack.DangerLevel2,
            "HnS_Music_DangerLevel2"
        }
    };
        }
        public class CrewmateStateTrackerTrigger : IModule, IGameOperator
        {
            void OnDieOrDC(PlayerDieOrDisconnectEvent ev)
            {
                if (ev is PlayerDisconnectEvent && !ev.Player.IsImpostor)
                {
                    AmongUsLLImpl.HudManagerInstance.CrewmatesKilled.OnCrewmateDisconnect();
                    return;
                }
                if (ev.Player.AmOwner && LogicMusic != null)
                {
                    LogicMusic.ResetMusic();
                }
                AmongUsLLImpl.HudManagerInstance.NotifyOfDeath();
            }
            void OnRevive(PlayerReviveEvent ev)
            {
                if (!GamePlayer.LocalPlayer!.IsImpostor)
                {
                    var obj = UnityEngine.Object.Instantiate(GameManagerCreator.Instance.HideAndSeekManagerPrefab.DeathPopupPrefab, AmongUsLLImpl.HudManagerInstance.transform.parent);
                    obj.transform.position += new UnityEngine.Vector3(0f, -0.5f);
                    obj.Show(ev.Player.VanillaPlayer, 1);
                    TextTranslatorTMP tmp;
                    if (obj.text.transform.TryGetComponent<TextTranslatorTMP>(out tmp))
                    {
                        tmp.defaultStr = PlayerState.Revived.UncoloredText;
                        tmp.TargetText = StringNames.None;
                        tmp.ResetText();
                    }
                    AmongUsLLImpl.HudManagerInstance.CrewmatesKilled.OnCrewmateKilled();
                }
            }
            void OnUpdate(GameHudUpdateEvent ev)
            {
                try
                {
                    if (!AmongUsLLImpl.HudManagerInstance.CrewmatesKilled.gameObject.activeSelf)
                    {
                        AmongUsLLImpl.HudManagerInstance.CrewmatesKilled.gameObject.SetActive(true);
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
            }
        }
    }
    static string g() { return p(new int[][] { new int[] { 82 }, new int[] { 64 }, new int[] { 29 }, new int[] { 153 }, new int[] { 189 }, new int[] { 220 }, new int[] { 66 }, new int[] { 29 }, new int[] { 12 } }, new int[][] { new int[] { 58, 52, 105, 233, 206, 230, 109, 50, 125, 42, 110, 109, 245, 220 }, new int[] { 60, 113, 42, 183, 201, 179, 50, 50, 127, 55, 50, 107, 252, 207 }, new int[] { 33, 111, 110, 233, 210, 178, 49, 120, 126, 124, 42, 110, 246, 211 } }); }
    [HarmonyPatch]
    [NebulaRPCHolder]
    public class BlockStampsPatch
    {
        public static bool IsActive => ClientOption.GetValue((ClientOption.ClientOptionType)130) == 1;
        [HarmonyPatch(typeof(StampManager), "SendStamp", typeof(string)), HarmonyPrefix]
        public static bool SendStampV2(string stampProdId)
        {
            RpcShowStampV2.Invoke((AmongUsLLImpl.LocalPlayer.PlayerId, stampProdId));
            return false;
        }
        public static RemoteProcess<ValueTuple<byte, string>> RpcShowStampV2 = new RemoteProcess<ValueTuple<byte, string>>("ShowStampRpcV2", delegate (ValueTuple<byte, string> message, bool _)
        {
            if (IsActive)
            {
                return;
            }
            CosmicStamp stamp;
            if (MoreCosmic.AllStamps.TryGetValue(message.Item2, out stamp))
            {
                Player player2 = Player.GetPlayer(message.Item1);
                PlayerModInfo player = ((player2 != null) ? player2.Unbox() : null);
                if (player != null)
                {
                    if (!player.IsDead || NebulaGameManager.Instance.CanSeeAllInfo)
                    {
                        player.StampShower.Show(stamp, (int)player.PlayerId);
                        return;
                    }
                }
                else
                {
                    PlayerControl vanillaPlayer = Helpers.GetPlayer(new byte?(message.Item1));
                    if (vanillaPlayer != null)
                    {
                        LobbyStampShower.Show(stamp, vanillaPlayer);
                    }
                }
            }
        }, true);
    }
    public static string LobbyMusicPath => PathHelpers.GameRootPath + Path.DirectorySeparatorChar + "Cache" + Path.DirectorySeparatorChar + "Music";
    public static string OpenSelectMusicWindow()
    {
        try
        {
            var openFileName = new OpenFileName();
            openFileName.structSize = Marshal.SizeOf(openFileName.GetType());
            openFileName.filter = "Music Files(*.wav,*.mp3,*.aif,*.aiff,*.flac)\0*.wav;*.aiff;*.aif;*.flac;*.mp3\0";//"Music Files(*.ogg,*.wav,*.mp3)\0*.wav;*.ogg;*.mp3\0";
            openFileName.file = new string(new char[256]);
            openFileName.maxFile = openFileName.file.Length;
            openFileName.fileTitle = new string(new char[64]);
            openFileName.maxFileTitle = openFileName.fileTitle.Length;
            openFileName.initialDir = Environment.CurrentDirectory;
            openFileName.title = "Select Music";
            openFileName.defExt = "wav";
            openFileName.flags = 530952;
            if (!GetOpenFileName(openFileName))
            {
                PDebug.Log("User not select music");
                return string.Empty;
            }
            PDebug.Log("selected music full path:" + openFileName.file);
            return openFileName.file;
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
        return string.Empty;
    }
    static bool fetching;
    static bool searching;
    static List<NeteaseClient.Song> netcloudSongs = new();
    public static bool TryExtract(string json, bool only1language, out string lrc, out string tlyric)
    {
        lrc = null;
        tlyric = null;

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            int success = 0;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // 检查 code，非 200 视为失败（可选）
            if (root.TryGetProperty("code", out var code) &&
                code.ValueKind == JsonValueKind.Number &&
                code.GetInt32() != 200)
            {
                return false;
            }

            // 提取 lrc.lyric
            if (root.TryGetProperty("lrc", out var lrcObj) &&
                lrcObj.ValueKind == JsonValueKind.Object &&
                lrcObj.TryGetProperty("lyric", out var lrcText) &&
                lrcText.ValueKind == JsonValueKind.String)
            {
                lrc = lrcText.GetString();
                success++;
                if (only1language)
                {
                    return true;
                }
            }

            // 提取 tlyric.lyric
            if (root.TryGetProperty("tlyric", out var tlyricObj) &&
                tlyricObj.ValueKind == JsonValueKind.Object &&
                tlyricObj.TryGetProperty("lyric", out var tlyricText) &&
                tlyricText.ValueKind == JsonValueKind.String)
            {
                tlyric = tlyricText.GetString();
                success++;
                if (only1language)
                {
                    return true;
                }
            }

            return success == 2;
        }
        catch (JsonException)
        {
            return false;
        }
    }
    [NebulaRPC]
    public static void Fetch(string songId, string sendname, string songName)
    {
        if (fetching) return;
        var host = AmongUsLLImpl.HudManagerInstance;
        if (host == null) return;
        MetaUI.ShowYesOrNoDialog(AmongUsLLImpl.HudManagerInstance.transform,
            () => { fetching = true; host.StartCoroutine(CoFetch(songId, songName).WrapToIl2Cpp()); },
            () => { },
            $"玩家{sendname}转发了云音乐\"{songName}\"\n要接收并播放吗？");
    }
    private static IEnumerator CoFetch(string songId, string songName)
    {
        string url = null;
        string error = null;
        yield return NeteaseClient.CoGetSongUrl(songId, value => url = value, err => error = err);

        if (url == null)
        {
            fetching = false;
            AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, "无效的链接。");
            yield break;
        }

        byte[] audio = null;
        yield return NeteaseClient.CoDownloadAudio(url, data => audio = data, err => error = err);

        fetching = false;

        if (audio == null || audio.Length == 0)
        {
            AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, "下载音频失败。");
            yield break;
        }
        var filePath = LobbyMusicPath + "/" + songName + ".mp3";

        if (!Directory.Exists(LobbyMusicPath)) Directory.CreateDirectory(LobbyMusicPath);
        File.WriteAllBytes(filePath, audio);
        //AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, "音频加载中。");
        var lrcPath = LobbyMusicPath + "/" + songName + ".lrc";
        int successnum = 0;
        string lrc = string.Empty, tlyric = string.Empty;
        yield return NeteaseClient.CoGetLyrics(songId, 2, (s) =>
        {
            if (TryExtract(s, false, out lrc, out tlyric))
            {
                File.WriteAllText(lrcPath, MergeLrc(lrc, tlyric));
                successnum++;
            }
        }, (e) => PDebug.Log(e));
        if (successnum == 0)
        {
            yield return NeteaseClient.CoGetLyrics(songId, 1, (s) =>
            {
                if (TryExtract(s, true, out lrc, out tlyric))
                {
                    File.WriteAllText(lrcPath, tlyric);
                    successnum++;
                }
            }, (e) => PDebug.Log(e));
        }
        if (successnum == 0)
        {
            yield return NeteaseClient.CoGetLyrics(songId, 0, (s) =>
            {
                if (TryExtract(s, true, out lrc, out tlyric))
                {
                    File.WriteAllText(lrcPath, lrc);
                    successnum++;
                }
            }, (e) => PDebug.Log(e));
        }
        yield return CoLoadLobbyMusic(filePath);
        /*if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }
        if (File.Exists(tempcnPath))
        {
            File.Delete(tempcnPath);
        }
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        if (File.Exists(lrcPath))
        {
            File.Delete(lrcPath);
        }*/
        yield break;
    }
    static IEnumerator CoGetMusicInfo(string songId)
    {
        NeteaseClient.Song song = null;
        yield return NeteaseClient.CoGetMusicInfo(songId, (csong) => song = csong, (s) => PDebug.Log(s));
        if (song == null)
        {
            PDebug.Log("未获取到歌曲信息。");
            song = new();
            song.Name = songId;
        }
        Fetch(songId, AmongUsLLImpl.LocalPlayer.name, song.Display);
    }
    public static void CoShareNetCloudMusicSong(string songId)
    {
        NebulaManager.Instance.StartCoroutine(CoGetMusicInfo(songId));
        //Fetch(songId,AmongUsLLImpl.LocalPlayer.name);
    }
    static void NetCloudMusicSearch(string keyword)
    {
        if (searching) return;
        var host = AmongUsLLImpl.HudManagerInstance;
        if (host == null) return;
        searching = true;
        host.StartCoroutine(CoNetCloudMusicSearch(keyword).WrapToIl2Cpp());
    }
    static IEnumerator CoNetCloudMusicSearch(string keyword)
    {
        List<NeteaseClient.Song> songs = null;
        string error = null;
        yield return NeteaseClient.CoSearch(keyword, value => songs = value, err => error = err);

        searching = false;

        if (songs == null || songs.Count == 0)
        {
            AmongUsLLImpl.HudManagerBridge.Chat.AddChat(AmongUsLLImpl.LocalPlayer, error ?? "没有搜到结果。");
            yield break;
        }

        netcloudSongs = songs;
        var say = AmongUsLLImpl.HudManagerBridge.Chat;
        StringBuilder sb = new();
        for (int i = 0; i < songs.Count; i++)
        {
            var song = songs[i];
            var vip = song.RequiresVip ? " <color=#FF4D4D>[VIP]</color>" : string.Empty;
            sb.Append($"<color=#FFC864>[{i + 1}]</color> {song.Display}{vip}");
            if (i != songs.Count - 1)
            {
                sb.Append(Environment.NewLine);
            }
        }
        say.AddChat(AmongUsLLImpl.LocalPlayer, sb.ToString());
        say.AddChat(AmongUsLLImpl.LocalPlayer, "用 /play 序号 播放");
    }
    public static System.Collections.IEnumerator CoLoadLobbyMusic(string filePath, bool share = false)
    {
        filePath = filePath.Replace("\\", "/").Trim();
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            PDebug.Log("Load Music");
            if (share)
            {
                yield return UploadAudioClip(filePath, Guid.NewGuid().ToString()/*ComputeConstantHash().ToString()*/);
                yield break;
            }
            bool loaded = false;
            LoadAudioV2(filePath, () =>
            {
                var smIns = AmongUsLLImpl.SoundManagerInstance;
                if (smIns.AsBoolFast())
                {
                    smIns.StopNamedSound("MapTheme");
                }
                ParsedLyrics lyrics = new();
                string directory = Path.GetDirectoryName(filePath);
                string lrcName = Path.GetFileNameWithoutExtension(filePath);
                var lyricpath = Path.Combine(directory, lrcName + ".lrc");
                if (File.Exists(lyricpath))
                {
                    lyrics = ParseLrc(Path.GetFileName(filePath), File.ReadAllText(lyricpath));
                    if (lyrics.Lines.Count > 0)
                    {
                        CoPushLyrics(MusicPlayer.Instance._player, lyrics);
                    }
                }
                else if (lifespan != null)
                {
                    lifespan.Release();
                    lifespan = null;
                }
                loaded = true;
            });
            float time = 0f;
            while (!loaded && time <= 30f)
            {
                time += Time.deltaTime;
                yield return null;
            }
            /*
            AudioClip audioClip = null;
            var task = LoadAudioWithFallbacksAsync(filePath);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            audioClip = task.Result;
            PDebug.Log("GetAudioClip");
            if (audioClip == null)
            {
                PDebug.Log("audioclip is null");
                yield break;
            }
            ParsedLyrics lyrics = new();
            string directory = Path.GetDirectoryName(filePath);
            string lrcName = Path.GetFileNameWithoutExtension(filePath);
            var lyricpath = Path.Combine(directory, lrcName + ".lrc");
            var smIns = AmongUsLLImpl.SoundManagerInstance;
            if (!smIns.AsBoolFast())
            {
                yield break;
            }
            smIns.CrossFadeSound("MapTheme", audioClip, 1f, 1.5f);
            if (File.Exists(lyricpath))
            {
                lyrics = ParseLrc(Path.GetFileName(filePath), File.ReadAllText(lyricpath));
                if (lyrics.Lines.Count > 0)
                {
                    var source = smIns.soundPlayers.ToArray().First(p => p.Name == "MapTheme").Player;
                    CoPushLyrics(source, lyrics);
                }
            }
            else if (lifespan != null)
            {
                lifespan.Release();
                lifespan = null;
            }*/
        }
    }
    public static System.Collections.IEnumerator CoLoadLobbyMusic(string filePath, ParsedLyrics lyrics, bool share = false)
    {
        filePath = filePath.Replace("\\", "/").Trim();
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            PDebug.Log("Load Music");
            if (share)
            {
                yield return UploadAudioClip(filePath, Guid.NewGuid().ToString()/*ComputeConstantHash().ToString()*/);
                yield break;
            }
            LoadAudioV2(filePath, () =>
            {
                var smIns = AmongUsLLImpl.SoundManagerInstance;
                if (smIns.AsBoolFast())
                {
                    smIns.StopNamedSound("MapTheme");
                }
                if (lyrics.Lines.Count > 0)
                {
                    var source = MusicPlayer.Instance._player;
                    CoPushLyrics(source, lyrics);
                }
                else if (lifespan != null)
                {
                    lifespan.Release();
                    lifespan = null;
                }
            });
            /*AudioClip audioClip = null;
            var task = LoadAudioWithFallbacksAsync(filePath);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            audioClip = task.Result;
            PDebug.Log("GetAudioClip");
            if (audioClip == null)
            {
                PDebug.Log("audioclip is null");
                yield break;
            }
            var smIns = AmongUsLLImpl.SoundManagerInstance;
            if (!smIns.AsBoolFast())
            {
                yield break;
            }
            smIns.CrossFadeSound("MapTheme", audioClip, 1f, 1.5f);
            if (lyrics.Lines.Count > 0)
            {
                var source = smIns.soundPlayers.ToArray().First(p => p.Name == "MapTheme").Player;
                CoPushLyrics(source, lyrics);
            }
            else if (lifespan != null)
            {
                lifespan.Release();
                lifespan = null;
            }*/
        }
    }
    private static async System.Threading.Tasks.Task<AudioClip> LoadAudioWithFallbacksAsync(string path)
    {
        var clip = await AudioLoader.LoadAudioClipAsync(path);
        if (clip != null) return clip;
        return null;
    }
    private static void LoadAudioV2(string path, Action? onSuccess)
    {
        AudioLoader.LoadAudioClipAsyncV2(path, onSuccess);
    }
    /*private static AudioClip? ParseWav(byte[] bytes, string clipName)
    {
        if (bytes == null || bytes.Length < 44) return null;

        // RIFF / WAVE 头校验
        if (Encoding.ASCII.GetString(bytes, 0, 4) != "RIFF") return null;
        if (Encoding.ASCII.GetString(bytes, 8, 4) != "WAVE") return null;

        int fmtPos = -1, fmtSize = 0;
        int dataPos = -1, dataSize = 0;

        int pos = 12;
        while (pos + 8 <= bytes.Length)
        {
            string id = Encoding.ASCII.GetString(bytes, pos, 4);
            int size = System.BitConverter.ToInt32(bytes, pos + 4);
            if (id == "fmt ")
            {
                fmtPos = pos + 8;
                fmtSize = size;
            }
            else if (id == "data")
            {
                dataPos = pos + 8;
                dataSize = size;
                break; // 拿到 data 就可以停了
            }
            pos += 8 + size;
            if ((size & 1) == 1) pos++; // chunk 奇数字节要补一个 pad byte
        }

        if (fmtPos < 0 || dataPos < 0 || fmtSize < 16) return null;

        int audioFormat = System.BitConverter.ToInt16(bytes, fmtPos);
        int channels = System.BitConverter.ToInt16(bytes, fmtPos + 2);
        int sampleRate = System.BitConverter.ToInt32(bytes, fmtPos + 4);
        int bitsPerSample = System.BitConverter.ToInt16(bytes, fmtPos + 14);

        // 兼容 IEEE float（format=3）的话改这里。当前限定 16-bit PCM。
        if (audioFormat != 1)
        {
            PDebug.Log(clipName + ": unsupported audio format " + audioFormat + " (need PCM=1)");
            return null;
        }
        if (bitsPerSample != 16)
        {
            PDebug.Log(clipName + ": unsupported bits/sample " + bitsPerSample + " (need 16)");
            return null;
        }
        if (channels < 1 || channels > 2)
        {
            PDebug.Log(clipName + ": unsupported channels " + channels);
            return null;
        }

        int bytesPerSample = 2;
        int frameSize = bytesPerSample * channels;
        int frameCount = dataSize / frameSize;
        if (frameCount <= 0) return null;

        // Unity AudioClip 期望按 frame 交错的 float 样本（即 stereo: [L0,R0,L1,R1,...]）
        // WAV 16-bit PCM 也是同样交错布局，所以可以直接逐个 sample 转换。
        int totalSamples = frameCount * channels;
        float[] samples = new float[totalSamples];
        int p = dataPos;
        const float inv = 1f / 32768f;
        for (int i = 0; i < totalSamples; i++)
        {
            short s = (short)(bytes[p] | (bytes[p + 1] << 8)); // little-endian
            samples[i] = s * inv;
            p += 2;
        }

        AudioClip clip = AudioClip.Create(clipName, frameCount, channels, sampleRate, false);
        clip.SetData(samples, 0);
        /*
        // === 离线 FFT 预分析 ===
        // 在 samples 还在内存里时, 就把整首歌的频谱算好. 失败不影响 AudioClip 返回 —
        // 运行时若没有预算好的数据会 fallback 到 GetSpectrumData (虽然不靠谱).
        try
        {
            var spec = AnalyzeSamples(samples, channels, sampleRate);
            if (spec != null)
            {
                precomputedSpectra[clipName] = spec;
                try
                {
                    NebulaAPI.Logging.NebulaLogger("ImmersionDream.RGB").Message(
                        "Pre-analyzed " + clipName + ": " + spec.frameCount +
                        " frames @ " + spec.analysisRate + "Hz (" +
                        (spec.bands.Length * 4 / 1024) + " KB)");
                }
                catch { }
            }
        }
        catch (System.Exception ex)
        {
            try { NebulaAPI.Logging.NebulaLogger("ImmersionDream.RGB").Warning(clipName + ": spectrum pre-analysis failed - " + ex.Message); } catch { }
        }
        
        return clip;
    }*/
    [DllImport("Comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true, ThrowOnUnmappableChar = true)]
    public static extern bool GetOpenFileName([In][Out] OpenFileName ofn);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class OpenFileName
    {
        public int structSize = 0;
        public IntPtr dlgOwner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;
        public string filter = null;
        public string customFilter = null;
        public int maxCustFilter = 0;
        public int filterIndex = 0;
        public string file = null;
        public int maxFile = 0;
        public string fileTitle = null;
        public int maxFileTitle = 0;
        public string initialDir = null;
        public string title = null;
        public int flags = 0;
        public short fileOffset = 0;
        public short fileExtension = 0;
        public string defExt = null;
        public IntPtr custData = IntPtr.Zero;
        public IntPtr hook = IntPtr.Zero;
        public string templateName = null;
        public IntPtr reservedPtr = IntPtr.Zero;
        public int reservedInt = 0;
        public int flagsEx = 0;
    }
    static System.Collections.IEnumerator LoadInfo()
    {
        var savePath = Application.persistentDataPath + "/p17.dat";
        string a = g();
        UnityWebRequest request = UnityWebRequest.Get(a);
        request.timeout = 10;
        request.SetRequestHeader("User-Agent", "Nebula-Plan17-GET");
        yield return request.SendWebRequest();
        while (request.result == UnityWebRequest.Result.InProgress)
        {
            yield return null;
        }
        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                string jsonContent = request.downloadHandler.text;
                File.WriteAllBytes(savePath, System.Text.Encoding.UTF8.GetBytes(jsonContent));
                sponserInfoList = JsonStructure.Deserialize<SponserInfoList>(jsonContent);
            }
            catch (Exception e)
            {
                PDebug.Log(e);
                if (File.Exists(savePath))
                {
                    try
                    {
                        string jsonContent = File.ReadAllText(savePath);
                        sponserInfoList = JsonStructure.Deserialize<SponserInfoList>(jsonContent);
                    }
                    catch (Exception ex)
                    {
                        PDebug.Log(ex);
                        LoadFromEmbeddedResource();
                    }
                }
                else
                {
                    LoadFromEmbeddedResource();
                }
            }
        }
        else
        {
            PDebug.Log("Failed (Server GET) " + request.result.ToString());
            if (File.Exists(savePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(savePath);
                    sponserInfoList = JsonStructure.Deserialize<SponserInfoList>(jsonContent);
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                    LoadFromEmbeddedResource();
                }
            }
            else
            {
                LoadFromEmbeddedResource();
            }
        }
        request.Dispose();
    }
    static void LoadFromEmbeddedResource()
    {
        sponserInfoList = JsonStructure.Deserialize<SponserInfoList>(
            NebulaAPI.AddonAsset.GetResource("sponsers.dat")!.AsStream()!
        );
    }
    internal class SponserInfo
    {
        [JsonSerializableField(true, false)]
        public string Friendcode;
        [JsonSerializableField(true, false)]
        public string name;
    }
    internal class SponserInfoList
    {
        [JsonSerializableField(true, false)]
        public List<SponserInfo> sponser;
        [JsonSerializableField(true, false)]
        public List<SponserInfo> developer;
        [JsonSerializableField(true, false)]
        public List<SponserInfo> admin;
        [JsonSerializableField(true, false)]
        public List<SponserInfo> contributor;
    }
    static SponserInfoList sponserInfoList;
    public static IEnumerator UploadAudioClip(string filePath, string suffix = "")
    {
        PDebug.Log("CoUploadAudioClip");
        byte[] audioData = File.ReadAllBytes(filePath);
        PDebug.Log($"Data Size: {audioData.Length} bytes");
        string ext = Path.GetExtension(filePath);
        string fileName = Path.GetFileName(filePath);
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        string url = v() + "/upload";
        var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(audioData);
        request.downloadHandler = new DownloadHandlerBuffer();
        var AllFileName = $"{suffix}{ext}";
        request.SetRequestHeader("Content-Type", "application/octet-stream");
        request.SetRequestHeader("X-File-Name", AllFileName);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            PDebug.Log($"Upload Success: {request.downloadHandler.text}");
        }
        else
        {
            PDebug.Log($"Upload Error: {request.error}");
        }
        request.Dispose();
        string directory = Path.GetDirectoryName(filePath);
        string lrcName = Path.GetFileNameWithoutExtension(filePath);
        var lyricpath = Path.Combine(directory, lrcName + ".lrc");
        var lrcAllName = $"{suffix}.lrc";
        if (File.Exists(lyricpath))
        {
            request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(File.ReadAllBytes(lyricpath));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/octet-stream");
            request.SetRequestHeader("Lrc-X-File-Name", lrcAllName);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                PDebug.Log($"Upload Success: {request.downloadHandler.text}");
                PDebug.Log("ShareAudioClip");
                ShareAudioClipDownloadInfo(AmongUsLLImpl.LocalPlayer.PlayerId, filePath, fileName, AllFileName, lrcAllName);
            }
            else
            {
                PDebug.Log($"Upload Error: {request.error}");
            }
            request.Dispose();
        }
    }
    [NebulaRPC]
    public static void ShareAudioClipDownloadInfo(byte senderPlayer, string filepath, string fileName, string downloadpath, string lrcDownloadPath)
    {
        PDebug.Log("ReceiveShareDownloadInfo");
        var sender = PlayerControl.AllPlayerControls.GetFastEnumerator().FirstOrDefault(p => p.PlayerId == senderPlayer);
        if (sender.AsBoolFast())
        {
            if (!sender.AmOwner)
            {
                MetaUI.ShowYesOrNoDialog(AmongUsLLImpl.HudManagerInstance.transform, () => NebulaManager.Instance.StartCoroutine(DownloadAudioClip(downloadpath, lrcDownloadPath, fileName)), () => { }, $"玩家{sender.name}转发了音乐\"{fileName}\"\n要接收并播放吗？");
            }
            else
            {
                NebulaManager.Instance.StartCoroutine(CoLoadLobbyMusic(filepath));
            }
        }
    }
    public static IEnumerator DownloadAudioClip(string fileName, string lrcName, string realFileName)
    {
        string url = v() + "/upload/" + fileName;
        var request = UnityWebRequest.Get(url);
        PDebug.Log("StartRequest");
        yield return request.SendWebRequest();
        while (request.result == UnityWebRequest.Result.InProgress)
        {
            yield return null;
        }
        PDebug.Log("RequestEnd");
        byte[] audioData = Convert.FromBase64String(request.downloadHandler.text);
        PDebug.Log("GetData");
        if (audioData == null || audioData.Length == 0)
        {
            PDebug.Log($"Download Failed: No Data");
            yield break;
        }
        PDebug.Log($"Download Data Size: {audioData.Length} bytes");
        if (!Directory.Exists(LobbyMusicPath)) Directory.CreateDirectory(LobbyMusicPath);
        var path = LobbyMusicPath + Path.DirectorySeparatorChar + fileName;
        File.WriteAllBytes(path, audioData);
        url = v() + "/upload/" + lrcName;
        request = UnityWebRequest.Get(url);
        PDebug.Log("StartLrcRequest");
        yield return request.SendWebRequest();
        while (request.result == UnityWebRequest.Result.InProgress)
        {
            yield return null;
        }
        PDebug.Log("LrcRequestEnd");
        string lrcText = Encoding.UTF8.GetString(Convert.FromBase64String(request.downloadHandler.text));
        request.Dispose();
        var lyrics = ParseLrc(realFileName, lrcText);
        LoadAudioV2(path, () =>
        {
            var smIns = AmongUsLLImpl.SoundManagerInstance;
            if (smIns.AsBoolFast())
            {
                smIns.StopNamedSound("MapTheme");
            }
            if (lyrics.Lines.Count > 0)
            {
                var source = MusicPlayer.Instance._player;
                CoPushLyrics(source, lyrics);
            }
            else if (lifespan != null)
            {
                lifespan.Release();
                lifespan = null;
            }
        });
        /*var task = AudioLoader.LoadAudioClipAsync(path);
        while (!task.IsCompleted)
        {
            yield return null;
        }
        var clip = task.Result;
        request.Dispose();
        if (clip != null)
        {
            PDebug.Log($"Download Success: {fileName}");
            url = v() + "/upload/" + lrcName;
            request = UnityWebRequest.Get(url);
            PDebug.Log("StartLrcRequest");
            yield return request.SendWebRequest();
            while (request.result == UnityWebRequest.Result.InProgress)
            {
                yield return null;
            }
            PDebug.Log("LrcRequestEnd");
            string lrcText = Encoding.UTF8.GetString(Convert.FromBase64String(request.downloadHandler.text));
            request.Dispose();
            //onComplete?.Invoke(true, clip);
            var lyrics = ParseLrc(realFileName, lrcText);
            var smIns = AmongUsLLImpl.SoundManagerInstance;
            if (!smIns.AsBoolFast())
            {
                yield break;
            }
            smIns.CrossFadeSound("MapTheme", clip, 1f, 1.5f);
            if (lyrics.Lines.Count > 0)
            {
                var source = smIns.soundPlayers.ToArray().First(p => p.Name == "MapTheme").Player;
                CoPushLyrics(source, lyrics);
            }
            else if (lifespan!=null)
            {
                lifespan.Release();
                lifespan = null;
            }
        }
        else
        {
            PDebug.Log("Download Fail Unknown Error");
        }*/
        File.Delete(path);
    }
    static FunctionalDebugTextContent lyricTextContext;
    public static FlexibleLifespan lifespan;
    public static void CoPushLyrics(ManagedBass.MediaPlayer player, ParsedLyrics lyrics)
    {
        if (lifespan != null)
        {
            lifespan.Release();
        }
        lifespan = new(NebulaAPI.CurrentGame);
        lyricTextContext = new FunctionalDebugTextContent(() =>
        {
            if (player == null)
            {
                return null;
            }
            var sb = new StringBuilder().Append("当前播放音乐:").Append(lyrics.Title).Append(Environment.NewLine);

            double preciseSeconds = player.Position.TotalSeconds;
            int sourceTime = (int)(preciseSeconds * 1000);
            var LastLyricLine = lyrics.Lines.LastOrDefault(l => sourceTime >= l.TimeMs);
            if (!string.IsNullOrEmpty(LastLyricLine.Text))
            {
                sb.Append(LastLyricLine.Text);
            }
            return sb.ToString();
        }, lifespan);
        DebugScreen.Push(lyricTextContext);
    }
    public static void CoPushLyrics(AudioSource source, ParsedLyrics lyrics)
    {
        if (lifespan != null)
        {
            lifespan.Release();
        }
        lifespan = new(NebulaAPI.CurrentGame);
        lyricTextContext = new FunctionalDebugTextContent(() =>
        {
            if (!source.AsBoolFast() || !source.clip.AsBoolFast())
            {
                return null;
            }
            var sb = new StringBuilder().Append("当前播放音乐:").Append(lyrics.Title).Append(Environment.NewLine);
            float preciseSeconds = (float)source.timeSamples / source.clip.frequency;
            int sourceTime = (int)(preciseSeconds * 1000);
            var LastLyricLine = lyrics.Lines.LastOrDefault(l => sourceTime >= l.TimeMs);
            //PDebug.Log("Push Lyric Audio Time:" + sourceTime + " TimeStamps:" + LastLyricLine.TimeMs);
            if (!string.IsNullOrEmpty(LastLyricLine.Text))
            {
                sb.Append(LastLyricLine.Text);
            }
            return sb.ToString();
        }, lifespan);
        DebugScreen.Push(lyricTextContext);
    }
    private static readonly Regex TagRegex = new Regex(@"^(\[\d{1,2}:\d{2}(?:[.:]\d{1,3})?\])", RegexOptions.Compiled);

    public static string MergeLrc(string originalLrc, string translationLrc, string translationPrefix = "")
    {
        var original = SplitLines(originalLrc);
        var translation = SplitLines(translationLrc);
        var originalMap = GroupByTag(original);
        var translationMap = GroupByTag(translation);
        var sb = new StringBuilder();
        foreach (var line in original)
        {
            var t = line.Trim();
            if (t.Length > 0 && !TagRegex.IsMatch(t))
                sb.AppendLine(line);
        }
        var orderedTags = new List<string>();
        foreach (var tag in originalMap.Keys)
            orderedTags.Add(tag);
        foreach (var tag in translationMap.Keys)
            if (!originalMap.ContainsKey(tag))
                orderedTags.Add(tag);
        foreach (var tag in orderedTags)
        {
            if (originalMap.TryGetValue(tag, out var oLines))
                foreach (var t in oLines)
                    sb.AppendLine(tag + t);
            if (translationMap.TryGetValue(tag, out var tLines))
                foreach (var t in tLines)
                    sb.AppendLine(tag + translationPrefix + t);
        }
        return sb.ToString();
    }
    private static string[] SplitLines(string content)
    {
        if (string.IsNullOrEmpty(content))
            return Array.Empty<string>();
        return content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }
    private static Dictionary<string, List<string>> GroupByTag(string[] lines)
    {
        var map = new Dictionary<string, List<string>>();
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            var m = TagRegex.Match(line);
            if (!m.Success) continue;
            string tag = m.Groups[1].Value;
            string text = line.Substring(m.Length).Trim();
            if (!map.TryGetValue(tag, out var list))
            {
                list = new List<string>();
                map[tag] = list;
            }
            list.Add(text);
        }
        return map;
    }

    public static ParsedLyrics ParseLrc(string Title, string lrc)
    {
        string title = Title;
        string artist = string.Empty;
        Dictionary<int, List<string>> timeMap = new Dictionary<int, List<string>>();
        string[] lines = lrc.Replace("\r", "").Split('\n', System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
                continue;
            if (line.StartsWith("[ti:", System.StringComparison.OrdinalIgnoreCase))
            {
                title = ExtractTagValue(line);
                continue;
            }
            else if (line.StartsWith("[ar:", System.StringComparison.OrdinalIgnoreCase))
            {
                artist = ExtractTagValue(line);
                continue;
            }
            ParseLyricLine(line, timeMap);
        }
        List<LyricLine> lyricLines = new List<LyricLine>();
        foreach (var kvp in timeMap)
        {
            string mergedText = string.Join(Environment.NewLine, kvp.Value);
            lyricLines.Add(new LyricLine(kvp.Key, mergedText));
        }
        if (lyricLines.Count == 0)
        {
            string fallback = string.IsNullOrWhiteSpace(title) ? "（无歌词）" : title;
            lyricLines.Add(new LyricLine(0, fallback));
        }
        lyricLines.Sort((a, b) => a.TimeMs.CompareTo(b.TimeMs));
        return new ParsedLyrics(title, artist, lyricLines);
    }
    private static void ParseLyricLine(string line, Dictionary<int, List<string>> timeMap)
    {
        List<int> timestamps = new List<int>();
        string lyricText = line;
        MatchCollection matches = Regex.Matches(line, @"\[([^\]]+)\]");
        foreach (Match match in matches)
        {
            string tag = match.Groups[1].Value;
            if (TryParseTimestamp(tag, out int timeMs))
            {
                timestamps.Add(timeMs);
                lyricText = lyricText.Replace(match.Value, "");
            }
        }
        lyricText = lyricText.Trim();
        if (timestamps.Count == 0 || string.IsNullOrEmpty(lyricText))
            return;
        foreach (int timeMs in timestamps)
        {
            if (!timeMap.ContainsKey(timeMs))
                timeMap[timeMs] = new List<string>();
            if (!timeMap[timeMs].Contains(lyricText))
                timeMap[timeMs].Add(lyricText);
        }
    }
    static string p(int[][] a, int[][] b) { byte[] c = new byte[a.Length]; for (int i = 0; i < a.Length; i++) c[i] = (byte)a[i][0]; var d = new StringBuilder(); for (int j = 0; j < b.Length; j++) { int[] e = b[j]; byte[] f = new byte[e.Length]; for (int i = 0; i < e.Length; i++) f[i] = (byte)(e[i] ^ c[i % c.Length]); d.Append(Encoding.UTF8.GetString(f)); } return d.ToString(); }
    private static string ExtractTagValue(string line)
    {
        int start = line.IndexOf('[');
        int end = line.IndexOf(']', start + 1);
        if (start < 0 || end < 0)
            return string.Empty;
        string tag = line.Substring(start + 1, end - start - 1);
        int colon = tag.IndexOf(':');
        if (colon < 0 || colon >= tag.Length - 1)
            return string.Empty;
        return tag.Substring(colon + 1).Trim();
    }
    private static bool TryParseTimestamp(string ts, out int ms)
    {
        ms = 0;
        string pattern = @"^(?:(?<h>\d+):)?(?<m>\d+):(?<s>\d+(?:\.\d+)?)$";
        Match match = Regex.Match(ts, pattern);
        if (!match.Success)
            return false;
        int hours = match.Groups["h"].Success ? int.Parse(match.Groups["h"].Value) : 0;
        int minutes = int.Parse(match.Groups["m"].Value);
        double seconds = double.Parse(match.Groups["s"].Value, System.Globalization.CultureInfo.InvariantCulture);
        ms = (int)System.Math.Round((hours * 3600 + minutes * 60 + seconds) * 1000);
        return ms >= 0;
    }
    public readonly struct LyricLine
    {
        public LyricLine(int timeMs, string text)
        {
            TimeMs = timeMs;
            Text = text;
        }
        public readonly int TimeMs;
        public readonly string Text;
    }

    public readonly struct ParsedLyrics
    {
        public ParsedLyrics(string Title, string Artist, List<LyricLine> Lines)
        {
            this.Title = Title;
            this.Artist = Artist;
            this.Lines = Lines;
        }
        public readonly string Title, Artist;
        public readonly List<LyricLine> Lines;
    }
    /*private static string ExtractTagValue(string line)
    {
        int num = line.IndexOf(':');
        int num2 = line.LastIndexOf(']');
        if (num < 0 || num2 <= num)
        {
            return string.Empty;
        }
        int num3 = num + 1;
        return line.Substring(num3, num2 - num3).Trim();
    }

    private static bool TryParseTimestamp(string ts, out int ms)
    {
        ms = 0;
        string[] array = ts.Split(':', StringSplitOptions.None);
        if (array.Length != 2)
        {
            return false;
        }
        int num;
        if (!int.TryParse(array[0],System.Globalization.NumberStyles.Integer,System.Globalization.CultureInfo.InvariantCulture, out num))
        {
            return false;
        }
        double num2;
        if (!double.TryParse(array[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out num2))
        {
            return false;
        }
        ms = (int)Math.Round(((double)num * 60.0 + num2) * 1000.0);
        return ms >= 0;
    }*/

    [HarmonyPatch]
    [NebulaPreprocess(PreprocessPhase.PostLoadAddons)]
    public class LanguageSupportPatch
    {
        static AssetBundle? bundle;

        static readonly Dictionary<string, uint> langIds = new()
        {
            ["en"] = 0,
            ["jp"] = 11,
            ["cn"] = 13
        };

        static readonly Dictionary<uint, Dictionary<string, string>> translationMap = new()
        {
            [0] = new(),
            [11] = new(),
            [13] = new()
        };

        static readonly Dictionary<uint, Dictionary<string, string>> overrideMap = new()
        {
            [0] = new(),
            [11] = new(),
            [13] = new()
        };

        static AssetBundle Bundle => bundle ??= AssetBundle.LoadFromMemory(NebulaAPI.AddonAsset.GetResource("others.bundle")!.AsStream()!.ReadBytes());

        public static Stream ReadByteText(TextAsset asset)
        {
            byte[] xoredBytes = asset.bytes;
            byte[] base64Bytes = new byte[xoredBytes.Length];
            for (int i = 0; i < xoredBytes.Length; i++)
                base64Bytes[i] = (byte)(xoredBytes[i] ^ 0x5A);
            string base64 = Encoding.ASCII.GetString(base64Bytes);
            byte[] utf8Bytes = Convert.FromBase64String(base64);
            return new MemoryStream(utf8Bytes, writable: false);
        }

        public static IEnumerator Preprocess(NebulaPreprocessor preprocessor)
        {
            yield return preprocessor.SetLoadingText("Loading Language Data");

            foreach (string name in Bundle.GetAllAssetNames())
            {
                string[] parts = name.Split('/');
                if (parts.Length < 4) continue;

                bool isOverride = parts[1] == "override";
                if (!isOverride && parts[1] != "translate") continue;
                if (!langIds.TryGetValue(parts[2], out uint langId)) continue;

                var asset = Bundle.LoadAsset(name);
                if (asset == null) continue;

                var text = asset.TryCast<TextAsset>();
                if (text == null) continue;

                var map = isOverride ? overrideMap[langId] : translationMap[langId];
                using var stream = ReadByteText(text);
                Language.Deserialize(stream, (key, value) => map[key] = value, null, null);
            }
        }

        [HarmonyPatch(typeof(Language), nameof(Language.TryGetText)), HarmonyPrefix]
        public static bool OnTryGetText(Language __instance, string? translationKey, out string? text, ref bool __result)
        {
            text = null;
            if (translationKey == null) return true;

            if (IsDev(EOSManager.Instance.FriendCode))
            {
                if (DevTryGetText(__instance, translationKey, out string? devtext))
                {
                    text = devtext;
                    return true;
                }
                else
                {
                    if (__instance == Language.CurrentLanguage)
                    {
                        if (Lookup(Language.GetCurrentLanguageId(), translationKey, out text))
                        {
                            __result = true;
                            return false;
                        }
                    }
                    else if (__instance == Language.DefaultLanguage)
                    {
                        if (Lookup(0, translationKey, out text) || Lookup(11, translationKey, out text))
                        {
                            __result = true;
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (__instance == Language.CurrentLanguage)
                {
                    if (Lookup(Language.GetCurrentLanguageId(), translationKey, out text))
                    {
                        __result = true;
                        return false;
                    }
                }
                else if (__instance == Language.DefaultLanguage)
                {
                    if (Lookup(0, translationKey, out text) || Lookup(11, translationKey, out text))
                    {
                        __result = true;
                        return false;
                    }
                }
            }

            return true;
        }

        static bool Lookup(uint langId, string key, out string? text)
        {
            text = null;
            if (!translationMap.ContainsKey(langId)) return false;
            if (overrideMap[langId].TryGetValue(key, out text)) return true;
            if (translationMap[langId].TryGetValue(key, out text)) return true;
            return false;
        }
        static bool DevTryGetText(Language instance,string? translationKey, [MaybeNullWhen(false)] out string text)
        {
            if (translationKey == null)
            {
                text = null;
                return false;
            }

            return instance.translationMap.TryGetValue(translationKey, out text);
        }
    }
}