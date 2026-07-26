using Nebula.Behavior;
using Nebula.Patches;
using Nebula.Roles.Complex;
using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Neutral;
using Virial.Helpers;
using Virial.Runtime;
using Nebula.Extensions;
using Nebula.Modules.Cosmetics;

namespace Plana.Roles.Impostor;

    [NebulaRPCHolder]
public class Pollyanna : DefinedSingleAbilityRoleTemplate<Pollyanna.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, IAssignableDocument
{
    private Pollyanna() : base("pollyanna", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [SkillCooldownOption,SkillDurationOption])
    {
    }
    static public FloatConfiguration SkillCooldownOption = NebulaAPI.Configurations.Configuration("options.role.pollyanna.skillCooldown", (0f, 120f, 2.5f), 20f, FloatConfigurationDecorator.Second);
  
    static public FloatConfiguration SkillDurationOption = NebulaAPI.Configurations.Configuration("options.role.pollyanna.skillDuration", (0f, 120f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }
    static public Pollyanna MyRole = new Pollyanna();
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Pollyanna.png")!.AsImage();

    //bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(Ability.SkillIcon, "role.pollyanna.ability.skill");
        yield return new(Ability.GuessIcon, "role.pollyanna.ability.guess");
    }


    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    public static void LoadPatch(Harmony harmony)
    {
        PDebug.Log("Pollyanna Patch");
        harmony.Patch(typeof(KillOverlay).GetMethod("ShowMeeting"), new HarmonyMethod(typeof(Pollyanna).GetMethod("ShowMeetingOutfitPatch")));
        harmony.Patch(typeof(PoolablePlayer).GetMethod("UpdateFromPlayerOutfit"), null, new HarmonyMethod(typeof(Pollyanna).GetMethod("FixHalfAlpha")));
        PDebug.Log("Done");
    }
    public static void FixHalfAlpha(PoolablePlayer __instance, NetworkedPlayerInfo.PlayerOutfit outfit, PlayerMaterial.MaskType maskType, bool isDead, bool includePet, Action OnSkinLoaded = null, bool forceAlive = false)
    {
        if (!MyRole.IsSpawnableInSomeForm())
        {
            return;
        }
        __instance.cosmetics.SetSkin(outfit.SkinId, outfit.ColorId, OnSkinLoaded);
        __instance.cosmetics.SetHatColor(Palette.White);
        __instance.cosmetics.SetVisorAlpha(Palette.White.a);
    }
    public static void ShowMeetingOutfitPatch(ref NetworkedPlayerInfo.PlayerOutfit playerOutfit)
    {
        var outfit = playerOutfit;
        if (NebulaGameManager.Instance.AllPlayerInfo.Any(p => p.TryGetAbility<Ability>(out var ability) && ability.RemoteKillPlayers.Any(p=>p.VanillaPlayer .Data.DefaultOutfit==outfit)))
        {
            playerOutfit = NebulaGameManager.Instance.UnknownOutfit.outfit;
            var allDeadBodies = Helpers.AllDeadBodies(true);
            allDeadBodies.Do(d =>
            {
                if (NebulaGameManager.Instance.AllPlayerInfo.Any(p => p.TryGetAbility<Ability>(out var ability) && ability.RemoteKillPlayers.Any(p => p.PlayerId == d.ParentId)))
                {
                    d.ParentId = 255;
                }
            });
        }
    }
    public static RemoteProcess<(GamePlayer polly,GamePlayer AlivePlayer, GamePlayer DeadPlayer,bool needTransport,bool needUpdateSwap,bool needUpdateKillList)> RpcSetOutfit = new("RpcPollyannaKill", (m,_) =>
    {
        try
        {
            GamePlayer d = m.DeadPlayer, a = m.AlivePlayer;
            var mc = DynamicPalette.PlayerColors[d.PlayerId];
            DynamicPalette.PlayerColors[d.PlayerId] = DynamicPalette.PlayerColors[a.PlayerId];
            DynamicPalette.PlayerColors[a.PlayerId] = mc;
            var sc = DynamicPalette.ShadowColors[d.PlayerId];
            DynamicPalette.ShadowColors[d.PlayerId] = DynamicPalette.ShadowColors[a.PlayerId];
            DynamicPalette.ShadowColors[a.PlayerId] = sc;
            if (DynamicPalette.ColorNameDic.TryGetValue(d.PlayerId, out var nameDicDead) && DynamicPalette.ColorNameDic.TryGetValue(a.PlayerId, out var nameDicAlive))
            {
                DynamicPalette.ColorNameDic[d.PlayerId] = nameDicAlive;
                DynamicPalette.ColorNameDic[a.PlayerId] = nameDicDead;
            }
            var alivePlayerOutfit = a.DefaultOutfit;
            var deadPlayerOutfit = d.DefaultOutfit;
            var cid = alivePlayerOutfit.outfit.ColorId;
            alivePlayerOutfit.outfit.ColorId = deadPlayerOutfit.outfit.ColorId;
            deadPlayerOutfit.outfit.ColorId = cid;
            a.VanillaPlayer.RawSetOutfit(deadPlayerOutfit.outfit, PlayerOutfitType.Default);
            a.VanillaPlayer.Data.SetOutfit(PlayerOutfitType.Default, deadPlayerOutfit.outfit);
            a.Unbox().DefaultOutfit = new(deadPlayerOutfit, "", -99, true);
            d.VanillaPlayer.RawSetOutfit(alivePlayerOutfit.outfit, PlayerOutfitType.Default);
            d.VanillaPlayer.Data.SetOutfit(PlayerOutfitType.Default, alivePlayerOutfit.outfit);
            d.Unbox().DefaultOutfit = new(alivePlayerOutfit, "", -99, true);
            if (MeetingHud.Instance.AsBoolFast())
            {
                var aliveArea = MeetingHud.Instance.playerStates.FirstOrDefault(area => area.TargetPlayerId == a.PlayerId);
                var deadArea = MeetingHud.Instance.playerStates.FirstOrDefault(area => area.TargetPlayerId == d.PlayerId);
                aliveArea.SetCosmetics(d.VanillaPlayer.Data);
                deadArea.SetCosmetics(a.VanillaPlayer.Data);
                UnityEngine.Vector3 aliveAreaPos = aliveArea.transform.position, deadAreaPos = deadArea.transform.position;
                aliveArea.transform.position = deadAreaPos;
                deadArea.transform.position = aliveAreaPos;
                bool AliveDidReportState = aliveArea.DidReport;
                aliveArea.SetDead(deadArea.DidReport, false, false);
                deadArea.SetDead(AliveDidReportState, true, false);
            }
            PDebug.Log("TranslatePhase");
            if (m.needTransport && !MeetingHud.Instance.AsBoolFast())
            {
                var alivePos = a.Position.ToUnityVector();
                var deadPos = d.Position.ToUnityVector();
                a.VanillaPlayer.NetTransform.SnapTo(deadPos);
                d.VanillaPlayer.NetTransform.SnapTo(alivePos);
            }
            if (m.needUpdateSwap)
            {
                RpcUpdateSwapDic.LocalInvoke((m.polly, m.AlivePlayer, m.DeadPlayer));
            }
            if (m.needUpdateKillList)
            {
                RpcUpdateRemoteKillList.LocalInvoke((m.polly, m.DeadPlayer));
            }
        }catch(Exception e)
        {
            PDebug.Log(e);
        }
    });
    public static RemoteProcess<(GamePlayer polla, GamePlayer key, GamePlayer value)> RpcUpdateSwapDic = new("RpcUpdateSwapDic", (m, _) =>
    {
        if (m.polla.TryGetAbility<Ability>(out var ability)&&!m.polla.AmOwner)
        {
            ability.SwapedPlayer.Add((m.key, m.value));
        }
    });
    public static RemoteProcess<(GamePlayer polla, GamePlayer dead)> RpcUpdateRemoteKillList = new("RpcUpdateRemoteKillList", (m, _) =>
    {
        if (m.polla.TryGetAbility<Ability>(out var ability) && !m.polla.AmOwner)
        {
            ability.RemoteKillPlayers.Add(m.dead);
        }
    });
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility, IBindPlayer, IGameOperator, ILifespan
    {
        public static Image GuessIcon = NebulaAPI.AddonAsset.GetResource("PollyannaGuessIcon.png")!.AsImage(115f)!;
        public static Image SkillIcon = NebulaAPI.AddonAsset.GetResource("PollyannaSkill.png")!.AsImage(115f)!;
        ModAbilityButton killButton, markedButton;
        GamePlayer skillSelectedPlayer;
        
        void OnDeadBodyGenerated(DeadBodyInstantiateEvent ev)
        {
            if (ev.Killer==MyPlayer&&RemoteKillPlayers.Any(p=>p==ev.DeadBody.Player))
            {
                ev.DeadBody.VanillaDeadBody.bodyRenderers.Do(d => PlayerMaterial.SetColors(NebulaGameManager.Instance.UnknownOutfit.outfit.ColorId, d));
                PlayerMaterial.SetColors(NebulaGameManager.Instance.UnknownOutfit.outfit.ColorId, ev.DeadBody.VanillaDeadBody.bloodSplatter);
                //ev.DeadBody.VanillaDeadBody.ParentId = 255;
            }
        }
        void OnGameEnd(GameEndEvent ev)
        {
            for (int i=1;i<=SwapedPlayer.Count;i++)
            {
                var tuple = SwapedPlayer[SwapedPlayer.Count - i];
                var outfit1 = tuple.Item1.Unbox().DefaultOutfit;
                var outfit2 = tuple.Item2.Unbox().DefaultOutfit;
                tuple.Item1.Unbox().DefaultOutfit = outfit2;
                tuple.Item2.Unbox().DefaultOutfit = outfit1;
            }
        }
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var myTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, ObjectTrackers.LocalKillablePredicate, Palette.ImpostorRed, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption, false);
                killButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, true, false, VirtualKeyInput.Kill, null, MyPlayer.TeamKillCooldown, "kill", null, (ModAbilityButton _) => myTracker.CurrentTarget != null, (ModAbilityButton _) => !MyPlayer.IsDead, false).SetLabelType(ModAbilityButton.LabelType.Impostor);
                killButton.OnClick = (button) =>
                {
                    var player = myTracker.CurrentTarget;
                    var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEventAbstractPlayerEvent(MyPlayer, player));
                    if (!(cancelable?.IsCanceled ?? false))
                    {
                        if (markedButton != null && markedButton.IsInEffect)
                        {
                            using (RPCRouter.CreateSection("RpcSetOutfitKill"))
                            {
                                RpcSetOutfit.Invoke((MyPlayer,skillSelectedPlayer, player, true,true,true));
                                SwapedPlayer.Add((skillSelectedPlayer, player));
                                markedButton.InterruptEffect();
                                markedButton.StartCoolDown();
                                RemoteKillPlayers.Add(player);
                                MyPlayer.MurderPlayer(player!, PatchManager.NoHope, EventDetail.Kill, KillParameter.RemoteKill, KillCondition.TargetAlive);
                                skillSelectedPlayer.VanillaPlayer.ShowFailedMurder();
                            }
                            skillSelectedPlayer = null;
                            if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                            return;
                        }
                        MyPlayer.MurderPlayer(player!, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                    }
                    if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                killButton.OnBroken = (button) =>
                {
                    Snatcher.RewindKillCooldown();
                };
                killButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                killButton.SetAsUsurpableButton(this);
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
                ObjectTracker<IPlayerlike> markTracker = ObjectTrackers.ForPlayerlike(this, null, base.MyPlayer, (IPlayerlike p) => ObjectTrackers.PlayerlikeStandardPredicate(p) && !MyPlayer.CanKill(p.RealPlayer), Palette.ImpostorRed, false, false);
                markedButton = NebulaAPI.Modules.AbilityButton(this, true, false, 0, false).BindKey(VirtualKeyInput.Ability);
                markedButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, SkillCooldownOption).Start();
                markedButton.EffectTimer = NebulaAPI.Modules.Timer(this, SkillDurationOption);
                markedButton.SetLabel("PollyannaMark");
                markedButton.SetImage(SkillIcon);
                markedButton.Visibility = button => !MyPlayer.IsDead;
                PoolablePlayer? markIcon = null;
                markedButton.OnClick = (button) =>
                 {
                     button.StartEffect();
                     PDebug.Log("PollyUseSkill");
                     skillSelectedPlayer = markTracker.CurrentTarget == null ? MyPlayer : markTracker.CurrentTarget.RealPlayer;
                     PDebug.Log("GetSelectedPlayer");
                     markIcon = (button as ModAbilityButtonImpl)!.GeneratePlayerIcon(skillSelectedPlayer);
                 };
                markedButton.OnEffectEnd = button =>
                {
                    if (markIcon)
                    {
                        UnityEngine.Object.Destroy(markIcon?.gameObject);
                    }
                    markIcon = null;
                };
            }
        }
        bool IPlayerAbility.HideKillButton => true;
        ValueTuple<GamePlayer, DefinedRole> selectedPlayer = new();
        MetaScreen? screen;
        public List<ValueTuple<GamePlayer, GamePlayer>> SwapedPlayer = new();
        public List<GamePlayer> RemoteKillPlayers=new();
        [EventPriority(-1000)]
        void OnSpecialKillPlayer(PlayerCheckKilledEvent ev)
        {
            if (ev.PlayerState==PatchManager.NoHope)
            {
                ev.Result = KillResult.Kill;
            }
        }
        [Local]
        void ShowSelectRole(PlayerSetFakeRoleNameEvent ev)
        {
            if (selectedPlayer.Item1!=null&&ev.Player==selectedPlayer.Item1&&!NebulaGameManager.Instance.CanSeeAllInfo)
            {
                ev.Append(selectedPlayer.Item2.DisplayColoredName);
            }
        }
        void ShowSwapedPlayer(PlayerDecorateNameEvent ev)
        {
            if (NebulaGameManager.Instance.CanSeeAllInfo||AmOwner)
            {
                foreach (var tuple in SwapedPlayer)
                {
                    if (ev.Player == tuple.Item1)
                    {
                        ev.Name += $"({tuple.Item2.PlayerName})".Color(UnityEngine.Color.gray);
                        continue;
                    }
                    if (ev.Player == tuple.Item2)
                    {
                        ev.Name += $"({tuple.Item1.PlayerName})".Color(UnityEngine.Color.gray);
                        continue;
                    }
                }
            }
        }
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            selectedPlayer.Item1 = null;
            bool guessed = false;
            NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>()?.RegisterMeetingAction(new MeetingPlayerAction(GuessIcon, state =>
            {
                var p = state.MyPlayer;
                screen=GuesserSystem.DoomsayerOpenGuessWindow((r) =>
                {
                    if (AmongUsLLImpl.LocalPlayer.Data.IsDead)
                    {
                        return;
                    }
                    if (MeetingHud.Instance.state != MeetingHud.VoteStates.Voted && MeetingHud.Instance.state != MeetingHud.VoteStates.NotVoted)
                    {
                        return;
                    }
                    if (!MeetingHudExtension.CanUseAbilityFor(p, true))
                    {
                        return;
                    }
                    if (selectedPlayer.Item1 == null)
                    {
                        selectedPlayer = ValueTuple.Create(p, r);
                        screen?.CloseScreen();
                        screen = null;
                        return;
                    }
                    if (selectedPlayer.Item1.Role.Role == selectedPlayer.Item2 && p.Role.Role == r)
                    {
                        RemoteKillPlayers.Add(p);
                        RpcUpdateRemoteKillList.Invoke((MyPlayer, p));
                        MyPlayer.MurderPlayer(p, PatchManager.NoHope, EventDetail.Kill, KillParameter.MeetingKill, KillCondition.BothAlive, result =>
                        {
                            if (result == KillResult.Kill)
                            {
                                RpcSetOutfit.Invoke((MyPlayer,selectedPlayer.Item1, p,false,true,false));
                            }
                        });
                    }
                    else
                    {
                        AmongUsUtil.PlayCustomFlash(Virial.Color.Red, 0.4f, 1f, 0.75f);
                    }
                    //selectedPlayer.Item1 = null;
                    guessed = true;
                    screen?.CloseScreen();
                    screen = null;
                });
            },
            (p) => !guessed && !p.MyPlayer.IsDead&&(selectedPlayer.Item1==null?p.MyPlayer.IsImpostor:p.MyPlayer!=selectedPlayer.Item1&&!p.MyPlayer.IsImpostor) && !p.MyPlayer.AmOwner && !AmongUsLLImpl.LocalPlayer.Data.IsDead && GameOperatorManager.Instance.Run<PlayerCanGuessPlayerLocalEvent>(new PlayerCanGuessPlayerLocalEvent(NebulaAPI.CurrentGame.LocalPlayer, p.MyPlayer, true)).CanGuess));
        }
    }
}
