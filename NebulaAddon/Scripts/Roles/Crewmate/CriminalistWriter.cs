using Nebula.Patches;
using Nebula.Roles.Scripts;
using Plana.Core;
using Plana.Roles.Impostor;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using Virial.Helpers;
using Color = UnityEngine.Color;
using IEnumerator = System.Collections.IEnumerator;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Plana.Roles.Crewmate;

[NebulaRPCHolder]
public class CriminalistWriter : DefinedSingleAbilityRoleTemplate<CriminalistWriter.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, IAssignableDocument
{

    private CriminalistWriter() : base("criminalistWriter",new(60,91,163), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [MeetingSkillCount,EveryMeetingSkillCount,UseSkillFailCanUseNextSkill,LightRange]) 
    {
        ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImages/CriminalistWriter.png")!.AsImage(115f)!;
    }

    static IntegerConfiguration MeetingSkillCount = NebulaAPI.Configurations.Configuration("options.role.criminalistWriter.meetingskillcount", (1,20),3);
    static IntegerConfiguration EveryMeetingSkillCount= NebulaAPI.Configurations.Configuration("options.role.criminalistWriter.everymeetingskillcount", (1,20),3);
    static BoolConfiguration UseSkillFailCanUseNextSkill = NebulaAPI.Configurations.Configuration("options.role.criminalistWriter.useskillfailcanusenextskill", true);
    static FloatConfiguration LightRange= NebulaAPI.Configurations.Configuration("options.role.criminalistWriter.lightrange", (-1f,5f,0.125f),1.75f,FloatConfigurationDecorator.Ratio);
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0),arguments.Get(1,MeetingSkillCount));

    static public CriminalistWriter MyRole = new CriminalistWriter();
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/CriminalistWriter.png")!.AsImage();

    public static RemoteProcess<(GamePlayer, int)> UpdateCWArguments = new("UpdateCWArgumentsRPC", (m, _) =>
    {
        if (m.Item1.TryGetAbility<Ability>(out var a))
        {
            a.initialSkillCount = m.Item2;
        }
    });
    bool IAssignableDocument.HasTips => true;

    public static void LoadPatch(Harmony harmony)
    {
        PDebug.Log("CriminalistWriter Patch");
        harmony.Patch(typeof(PlayerVoteArea).GetMethod(nameof(PlayerVoteArea.CanBeHighlighted)), null, new HarmonyMethod(typeof(CriminalistWriter).GetMethod("WriterCanBeHighlightedPatch")));
        harmony.Patch(typeof(MeetingHudExtension).GetMethod("CanVoteFor",new Type[] {typeof(byte)}), null, new HarmonyMethod(typeof(CriminalistWriter).GetMethod("WriterCanUseSkillFor")));
        PDebug.Log("Done");
    }
    public static void WriterCanUseSkillFor(byte playerId,ref bool __result)
    {
        if (NebulaAPI.CurrentGame != null && GamePlayer.LocalPlayer!.Role.GetAbility<CriminalistWriter.Ability>() != null && !MeetingHudExtension.CanVote)
        {
            if (!__result)
            {
                Player player = Player.GetPlayer(playerId);
                if (player != null && MeetingHudExtension.CanUseAbilityForByMask(playerId))
                {
                    __result = true;
                }
            }
        }
    }
    public static void WriterCanBeHighlightedPatch(PlayerVoteArea __instance,ref bool __result)
    {
        if (NebulaAPI.CurrentGame!=null&&GamePlayer.LocalPlayer!.Role.GetAbility<CriminalistWriter.Ability>()!=null&&!MeetingHudExtension.CanVote)
        {
            if (!__result && __instance.AmDead && !AmongUsLLImpl.LocalPlayer.Data.IsDead && __instance.Parent != null && __instance.Parent.CurrentState != MeetingHud.MeetingStates.Discussion)
            {
                __result = true;
            }
        }
    }
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static Virial.Media.Image? UnSkillButton = NebulaAPI.AddonAsset.GetResource("CWriterUnSkill.png")!.AsImage(115f)!;
        static Virial.Media.Image? InSkillButton = NebulaAPI.AddonAsset.GetResource("CWriterInSkill.png")!.AsImage(115f)!;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt(),initialSkillCount];
        public Ability(GamePlayer player, bool isUsurped, int initialskillCount) : base(player,isUsurped)
        {
            initialSkillCount = initialskillCount;
            if (AmOwner)
            {
                GameOperatorManager.Instance?.RegisterOnReleased(() =>
                {
                    MeetingHudExtension.CanVote = true;
                }, this);
                string prefix = Language.Translate("role.criminalistWriter.leftskillprefix");
                Helpers.TextHudContent("FixerText", this, delegate (TextMeshPro tmPro)
                {
                    tmPro.text = prefix + ": " + initialSkillCount;
                }, true);
            }
        }
        [EventPriority(999)]
        [Local]
        void LightUpdate(LightRangeUpdateEvent ev)
        {
            ev.LightRange = LightRange;
        }
        GamePlayer? deadPlayer, alivePlayer;
        public int initialSkillCount;
        FlexibleLifespan lifespan;
        Dictionary<byte, SpriteRenderer> highlights;
        bool isKilled;
        bool killedCrew;

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            deadPlayer = alivePlayer = null;
            useskillfail = false;
            if (MyPlayer.IsDead) return;
            if (initialSkillCount<=0)
            {
                return;
            }
            ev.CanVote = false;
            if (lifespan != null && !lifespan.IsDeadObject)
            {
                lifespan.Release();
            }
            lifespan = new FlexibleLifespan(this);
            
            highlights = new Dictionary<byte, SpriteRenderer>();
            foreach (var pva in MeetingHud.Instance.playerStates)
            {
                var highlight = GameObject.Instantiate(pva.HighlightedFX, pva.transform);
                highlight.enabled = false;
                highlight.color = UnityEngine.Color.red;
                highlight.transform.localPosition = new(-0.0155f, 0.0127f, -2.8f);
                highlights[pva.PlayerId] = highlight;
                PDebug.Log("AddHightlight:" + pva.PlayerId);
            }
            void UpdateHighlight(byte player,GamePlayer nPlayer)
            {
                try
                {
                    PDebug.Log("WriterUpdateHightlight");
                    if (deadPlayer == null)
                    {
                        PDebug.Log("DPlayer select");
                        if (nPlayer.IsDead)
                        {
                            deadPlayer = nPlayer;
                            var highlight = highlights[player];
                            highlight.color = UnityEngine.Color.red;
                            highlight.enabled = true;
                        }
                    }
                    else if (deadPlayer.PlayerId == player)
                    {
                        PDebug.Log("Unselect dplayer");
                        deadPlayer = null;
                        alivePlayer = null;
                        highlights[player].enabled = false;
                        highlights.Do(entry => entry.Value.enabled = false);
                    }
                    else if (alivePlayer == null && deadPlayer.PlayerId != player)
                    {
                        PDebug.Log("alive Player select");
                        if (nPlayer.IsAlive)
                        {
                            alivePlayer = nPlayer;
                            var highlight = highlights[player];
                            highlight.color = new Color32(60, 91, 163, 255);
                            highlight.enabled = true;
                        }
                    }
                    else if (alivePlayer != null && alivePlayer.PlayerId == player)
                    {
                        PDebug.Log("UnSelect AliverPlayer");
                        alivePlayer = null;
                        highlights[player].enabled = false;
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
            }

            var skipButton = MeetingHud.Instance.SkipVoteButton;
            GameObject binder = UnityHelper.CreateObject("WriterButton", MeetingHud.Instance.SkipVoteButton.transform.parent, MeetingHud.Instance.SkipVoteButton.transform.localPosition, null);
            GameOperatorManager.Instance?.Subscribe<GameUpdateEvent>(delegate (GameUpdateEvent ev)
                {
                    if (binder==null)
                    {
                        return;
                    }
                    binder.gameObject.SetActive(!this.MyPlayer.IsDead && MeetingHud.Instance.CurrentState == MeetingHud.MeetingStates.NotVoted&& binder);
                }, lifespan, 100);
            GameOperatorManager.Instance?.Subscribe<InvokeVoteAlternateEvent>(ev =>
            {
                if (ev.Player == MyPlayer)
                {
                    return;
                }
                if (IsUsurped)
                {
                    MeetingHudExtension.CanVote = true;
                    UnityEngine.Object.Destroy(binder);
                    highlights.Do(entry => entry.Value.gameObject.SetActive(false));
                    return;
                }
                UpdateHighlight(ev.Player.PlayerId, ev.Player);
                VanillaAsset.PlaySelectSE();
            }, lifespan);
            int meetingCount = EveryMeetingSkillCount;
            SpriteRenderer leftRenderer = UnityHelper.CreateObject<SpriteRenderer>("Writer-UseSkill", binder.transform, new Vector3(2.1f, 0.15f), null);
            leftRenderer.sprite = InSkillButton.GetSprite();
            PassiveButton passiveButton = leftRenderer.gameObject.SetUpButton(true, new SpriteRenderer[0], null, null);
            passiveButton.OnMouseOver.AddListener(delegate
            {
                leftRenderer.color = new Color32(146, 164, 204, 255);
            });
            passiveButton.OnMouseOut.AddListener(delegate
            {
                leftRenderer.color = UnityEngine.Color.white;
            });
            passiveButton.OnClick.AddListener(delegate
            {
                if (IsUsurped)
                {
                    MeetingHudExtension.CanVote = true;
                    UnityEngine.Object.Destroy(binder);
                    highlights.Do(entry => entry.Value.gameObject.SetActive(false));
                    return;
                }
                if (alivePlayer!=null&&deadPlayer!=null)
                {
                    MetaUI.ShowYesOrNoDialog(AmongUsLLImpl.HudManagerInstance.transform, () =>
                    {
                        initialSkillCount--;
                        meetingCount--;
                        UpdateCWArguments.Invoke((MyPlayer, initialSkillCount));
                        //useSkill;
                        if (deadPlayer.MyKiller != null && deadPlayer.MyKiller.PlayerId == alivePlayer.PlayerId)
                        {
                            MyPlayer.MurderPlayer(alivePlayer, PatchManager.CriminologistKill, EventDetails.Kill, KillParameter.MeetingKill,KillCondition.BothAlive);

                            if (!alivePlayer.IsTrueCrewmate)
                            {
                                new StaticAchievementToken("criminalistWriter.common2");
                            }
                            else
                            {
                                killedCrew = true;
                            }

                            PatchManager.RpcSendSkillMessage.Invoke((MyPlayer, Language.Translate("role.criminalistWriter.CorrectSkill").Replace("%PLAYER%", alivePlayer.PlayerName).Replace("%DEADPLAYER%", deadPlayer.PlayerName)));
                        }
                        else
                        {
                            PatchManager.RpcSendSkillMessage.Invoke((MyPlayer, Language.Translate("role.criminalistWriter.WrongSkill").Replace("%PLAYER%", alivePlayer.PlayerName).Replace("%DEADPLAYER%", deadPlayer.PlayerName)));
                            if (!UseSkillFailCanUseNextSkill)
                            {
                                useskillfail = true;
                                MeetingHudExtension.CanVote = true;
                                UnityEngine.Object.Destroy(binder);
                                highlights.Do(entry => entry.Value.gameObject.SetActive(false));
                                MeetingHudExtension.ReflectVotingMask();
                            }
                        }
                        deadPlayer = null;
                        alivePlayer = null;
                        isKilled = true;
                        new StaticAchievementToken("criminalistWriter.common1");
                        highlights.Do(entry => entry.Value.enabled = false);
                        if (initialSkillCount <= 0 || meetingCount <= 0)
                        {
                            MeetingHudExtension.CanVote = true;
                            UnityEngine.Object.Destroy(binder);
                            highlights.Do(entry => entry.Value.gameObject.SetActive(false));
                            MeetingHudExtension.ReflectVotingMask();
                        }
                    }, () => { }, Language.Translate("role.criminalistWriter.confirmUseSkill"));
                }
                else
                {
                    try
                    {
                        MeetingHudExtension.CanVote = !MeetingHudExtension.CanVote;
                        if (MeetingHudExtension.CanVote)
                        {
                            leftRenderer.sprite = UnSkillButton?.GetSprite();
                            highlights.Do(entry => entry.Value.gameObject.SetActive(false));
                        }
                        else
                        {
                            leftRenderer.sprite = InSkillButton?.GetSprite();
                            highlights.Do(entry => entry.Value.gameObject.SetActive(true));
                        }
                    }
                    catch(Exception e)
                    {
                        PDebug.Log(e);
                    }
                    MeetingHudExtension.ReflectVotingMask();
                }
            });
            leftRenderer.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.6f);
        }

        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            var killed = GamePlayer.AllPlayers.Where(p => p.MyKiller == MyPlayer);
            bool won = ev.CheckWin(MyPlayer);
            bool crewWinEnd = ev.EndState.EndCondition == NebulaGameEnds.CrewmateGameEnd.Get();

            if (won && !crewWinEnd && isKilled)
                new StaticAchievementToken("criminalistWriter.common3");

            if (won && crewWinEnd && killedCrew)
                new StaticAchievementToken("criminalistWriter.challenge3");

            if (MyPlayer.IsDead && GamePlayer.AllPlayers.Where(p => p.IsImpostor).All(p => p.IsDead && p.PlayerState == PatchManager.CriminologistKill && p.MyKiller == MyPlayer))
                new StaticAchievementToken("criminalistWriter.challenge2");

            if (killed.Count() >= 5 && killed.Count(p => p.PlayerState == PatchManager.CriminologistKill) >= 2)
                new StaticAchievementToken("criminalistWriter.challenge1");
        }


        bool useskillfail;
    }
}
