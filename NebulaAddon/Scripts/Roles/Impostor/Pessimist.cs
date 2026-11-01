using Il2CppSystem.Text.Json;
using NAudio.CoreAudioApi;
using Nebula.Roles.Complex;
using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Neutral;
using Virial.Helpers;

namespace Plana.Roles.Impostor;

public class Pessimist : DefinedRoleTemplate, DefinedRole, HasCitation, IAssignableDocument
{
    private Pessimist() : base("pessimist", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [KillCoolDownOption,ReplaceRoleMethod,SidekickCoolDownOption,ReplaceDoneSendMessage,ReplaceFailSendMessage])
    {
        ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImages/MrPessimist.png")!.AsImage(115f)!;
    }
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }
    static IRelativeCooldownConfiguration KillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.pessimist.killCoolDown", CoolDownType.Relative, new ValueTuple<float, float, float>(0f, 60f, 2.5f), 30f, new ValueTuple<float, float, float>(-40f, 40f, 2.5f), +5f, new ValueTuple<float, float, float>(0.125f, 2f, 0.125f), 1.125f, null, null);
    static ValueConfiguration<int> ReplaceRoleMethod = NebulaAPI.Configurations.Configuration("options.role.pessimist.replaceMethod", ["options.role.pessimist.onlyTaskPhase", "options.role.pessimist.onlyMeeting", "options.role.pessimist.unlimit"], 2);
    static FloatConfiguration SidekickCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.pessimist.sidekickcooldown", (0f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second,()=>ReplaceRoleMethod.GetValue()!=1);
    static BoolConfiguration ReplaceDoneSendMessage = NebulaAPI.Configurations.Configuration("options.role.pessimist.replacedoneSendMessage", true,()=>ReplaceRoleMethod.GetValue()!=0);
    static BoolConfiguration ReplaceFailSendMessage = NebulaAPI.Configurations.Configuration("options.role.pessimist.replacefailSendMessage", true,()=>ReplaceRoleMethod.GetValue()!=0);
    static public Pessimist MyRole = new Pessimist();
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Pessimist.png")!.AsImage();

    bool IAssignableDocument.HasTips => true;
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CannotLoad;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public static RemoteProcess<GamePlayer> RpcClearMyExtendedRoleInfo = new("RpcClearPessimistExtendedRoleInfo", (m, _) =>
    {
        if (m.Role is Pessimist.Instance instance)
        {
            instance.ExtendedRoleInstance?.Inactivate();
            instance.MyExtendedRole = null;
            instance.ExtendedRoleInstance = null;
        }
    });

    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        public Instance(GamePlayer player) : base(player)
        {
        }
        static Virial.Media.Image? GuessIcon = NebulaAPI.AddonAsset.GetResource("pessimistGuess.png")!.AsImage(115f);
        static Virial.Media.Image? ReplaceIcon = NebulaAPI.AddonAsset.GetResource("pessimistReplace.png")!.AsImage(115f);
        IEnumerable<DefinedAssignable> RuntimeAssignable.AssignableOnHelp => MyExtendedRole == null ? [MyRole] : [MyRole, MyExtendedRole];
        IEnumerable<IPlayerAbility> RuntimeAssignable.MyAbilities => ExtendedRoleInstance == null ? Array.Empty<IPlayerAbility>() : ExtendedRoleInstance.MyAbilities!;
        DefinedRole RuntimeRole.Role => MyRole;
        bool HasAnyImpostorKillByMyPlayer()
        {
            return NebulaGameManager.Instance!.AllPlayerInfo.Any(p => p.IsImpostor && p.MyKiller == MyPlayer);
        }
        
        [OnlyMyPlayer]
        void OnKillPlayer(PlayerKillPlayerEvent ev)
        {
            if (ev.Dead.IsImpostor && MyExtendedRole==null&&ExtendedRoleInstance==null)
            {
                if (ev.Dead.Role.Role==MyRole)
                {
                    return;
                }
                MyExtendedRole = ev.Dead.Role.Role;

                ExtendedRoleInstance = MyExtendedRole.CreateInstance(MyPlayer, MyExtendedRole.DefaultAssignableArguments ?? []);
                ExtendedRoleInstance.OnActivated();
                ExtendedRoleInstance.Register(this);

                if (ev.Dead.AmOwner)
                {
                    ev.Dead.SetRole(Nebula.Roles.Impostor.Impostor.MyRole);
                }
                if (sidekickButton==null)
                {
                    return;
                }
                sidekickButton.StartCoolDown();
                (sidekickButton as ModAbilityButtonImpl)!.PlayFlashOnce();
            }
        }

        MetaScreen? LastGuesserWindow;
        [OnlyMyPlayer,Local]
        void OnMeetingDead(PlayerDieEvent ev)
        {
            LastGuesserWindow?.CloseScreen();
        }
        static public MetaScreen OpenGuessWindow(Action<DefinedRole> onSelected)
        {
            if (ClientOption.GetValue((ClientOption.ClientOptionType)125) == 1)
            {
                AmongUsLLImpl.SoundManagerInstance.PlaySound(PatchManager.GetSound("Gunload"), false, 1f);
            }
            return MeetingRoleSelectWindow.OpenRoleSelectWindow(!PatchManager.GuesserCannotGuessNoSpawnableRole ? Nebula.Roles.Roles.AllRoles : null, (r) => r.CanBeGuess&&!r.IsSystemRole, GamePlayer.LocalPlayer?.IsTrueCrewmate ?? false, "", onSelected);
        }
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (ReplaceRoleMethod.GetValue() != 0)
            {
                CurrentGuessWrong = false;
                NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>()?.RegisterMeetingAction(new(GuessIcon,
                    state =>
                    {
                        var p = state.MyPlayer;
                        LastGuesserWindow = OpenGuessWindow((r) =>
                        {
                            try
                            {
                                if (AmongUsLLImpl.LocalPlayer.Data.IsDead) return;
                                if (!(MeetingHud.Instance.state == MeetingHud.MeetingStates.Voted || MeetingHud.Instance.state == MeetingHud.MeetingStates.NotVoted)) return;
                                if (!MeetingHudExtension.CanUseAbilityForLocal(p, true))
                                {
                                    return;
                                }
                                PDebug.Log("CheckDGuessCorrect");
                                if (GuesserSystem.CheckGuess(p, r))
                                {
                                    if (!p.IsDead)
                                    {
                                        GuesserSystem.GuessMessageRPC.Invoke(new ValueTuple<GamePlayer, GamePlayer, int>(GamePlayer.LocalPlayer, p, r.Id));
                                        if (ReplaceDoneSendMessage)
                                        {
                                            PatchManager.RpcSendNoSenderSkillMessage.Invoke(("role.pessimist.replacedone",true));
                                        }

                                        if (MyPlayer.TryGetModifier<DreamweaverModifier.Instance>(out var _))
                                        {
                                            MyPlayer.RemoveModifier(DreamweaverModifier.MyRole);
                                            p.AddModifier(DreamweaverModifier.MyRole);
                                        }
                                        if (MyPlayer.TryGetModifier<InsomniacsModifier.Instance>(out var _))
                                        {
                                            MyPlayer.RemoveModifier(InsomniacsModifier.MyRole);
                                            p.AddModifier(InsomniacsModifier.MyRole);
                                        }

                                        p.SetRole(MyExtendedRole!, ExtendedRoleInstance?.RoleArguments);

                                        RpcClearMyExtendedRoleInfo.Invoke(MyPlayer);
                                    }
                                }
                                else
                                {
                                    GuesserSystem.GuessMessageRPC.Invoke(new ValueTuple<GamePlayer, GamePlayer, int>(GamePlayer.LocalPlayer, p, r.Id));
                                    AmongUsUtil.PlayCustomFlash(Virial.Color.Red, 0.4f, 1f, 0.75f);
                                    CurrentGuessWrong = true;
                                    if (ReplaceFailSendMessage)
                                    {
                                        PatchManager.RpcSendNoSenderSkillMessage.Invoke(("role.pessimist.replacefail",true));
                                    }
                                }
                                if (LastGuesserWindow) LastGuesserWindow.CloseScreen();
                                LastGuesserWindow = null!;

                            }
                            catch (Exception e)
                            {
                                PDebug.Log(e);
                            }
                        }
                        );
                    },
                    p => MyExtendedRole!=null&&MyPlayer.CanKill(p.MyPlayer)&&!CurrentGuessWrong && !p.MyPlayer.IsDead && !p.MyPlayer.AmOwner && !AmongUsLLImpl.LocalPlayer.Data.IsDead && GameOperatorManager.Instance!.Run(new PlayerCanGuessPlayerLocalEvent(NebulaAPI.CurrentGame!.LocalPlayer, p.MyPlayer, true)).CanGuess
                    ));
            }
        }
        bool CurrentGuessWrong;
        string? RuntimeAssignable.OverrideRoleName(string lastRoleName, bool isShort, bool canSeeAllInfo)
        {
            if ((AmOwner || canSeeAllInfo) && MyExtendedRole != null && ExtendedRoleInstance != null)
            {
                var suffix = isShort ? $"{ExtendedRoleInstance.GetRoleIconTagSmall()}{ExtendedRoleInstance.DisplayColoredShort}" : ExtendedRoleInstance.DisplayColoredName;
                return $"{lastRoleName} ({suffix})";
            }
            return lastRoleName;
        }


        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                if (ReplaceRoleMethod.GetValue() != 1)
                {
                    ObjectTracker<IPlayerlike> mySidekickTracker = ObjectTrackers.ForPlayerlike(this, null, base.MyPlayer, (IPlayerlike p) => ObjectTrackers.PlayerlikeStandardPredicate(p) &&MyPlayer.CanKill(p.RealPlayer), Palette.ImpostorRed, false, false);
                    sidekickButton = NebulaAPI.Modules.InteractButton<IPlayerlike>(this, base.MyPlayer, mySidekickTracker, new PlayerInteractParameter(true, false, true), false, true, VirtualKeyInput.SidekickAction, null, SidekickCoolDownOption, "pessimist.replace",ReplaceIcon, delegate (IPlayerlike p, ModAbilityButton button)
                    {
                        p.RealPlayer.SetRole(MyExtendedRole!,MyExtendedRole!.DefaultAssignableArguments);
                        RpcClearMyExtendedRoleInfo.Invoke(MyPlayer);
                    }, (ModAbilityButton _) => !MyPlayer.IsDived, (ModAbilityButton _) => !MyPlayer.IsDead && MyExtendedRole != null, false);
                }
                ObjectTracker<IPlayerlike> myKillTracker = ObjectTrackers.ForPlayerlike(this, null, base.MyPlayer, (IPlayerlike p) => ObjectTrackers.PlayerlikeStandardPredicate(p) && (MyPlayer.CanKill(p.RealPlayer) || (MyExtendedRole == null && ExtendedRoleInstance == null)), Palette.ImpostorRed, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption);
                var killButton = NebulaAPI.Modules.PlayerlikeKillButton(this, MyPlayer, new PlayerInteractParameter(false, true, true), true, Virial.Compat.VirtualKeyInput.Kill, null,
                    KillCoolDownOption.Cooldown, "kill", ModAbilityButton.LabelType.Impostor, null!,
                    (target, _) =>
                    {
                        var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEvent(MyPlayer, target));
                        if (!(cancelable?.IsCanceled ?? false))
                        {
                            //キャンセルされなければキルを実行する
                            MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                        }

                        //クールダウンをリセットする
                        if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    },
                    myKillTracker,
                    _ => !MyPlayer.IsDived,
                    _ => MyPlayer.IsAlive
                    );
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
        }
        bool RuntimeRole.CheckGuessAbility(DefinedRole abilityRole)
        {
            return abilityRole == MyExtendedRole || abilityRole == Pessimist.MyRole;
        }
        bool RuntimeRole.HasVanillaKillButton => false;
        public DefinedRole? MyExtendedRole { get; set; }
        public RuntimeRole? ExtendedRoleInstance { get; set; }
        ModAbilityButton? killButton,sidekickButton;
    }
}
