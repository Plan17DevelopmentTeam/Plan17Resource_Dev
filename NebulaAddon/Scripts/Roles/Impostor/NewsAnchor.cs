using Nebula.Modules.Cosmetics;
using Nebula.Patches;
using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Modifier;
using Sentry;
using Virial.Helpers;
using GUI = Nebula.Modules.GUIWidget.NebulaGUIWidgetEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Plana.Roles.Impostor;

[NebulaRPCHolder]
public class NewsAnchor : DefinedSingleAbilityRoleTemplate<NewsAnchor.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, IAssignableDocument
{
    private NewsAnchor() : base("newsAnchor", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [SkillNumOption,GuessNumOption,EveryMeetingGuessNumOption,ForceUseSkillMeetingNumOption,AnchorCannotGuessCoAnchorOption])
    {                
        ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImages/NewsAnchor.png")!.AsImage(115f)!;
    }
    public static IntegerConfiguration SkillNumOption = NebulaAPI.Configurations.Configuration("options.role.newsAnchor.skillnum", (1, 16), 1);
    public static IntegerConfiguration GuessNumOption = NebulaAPI.Configurations.Configuration("options.role.newsAnchor.guessNum", (1, 20), 4);
    public static IntegerConfiguration EveryMeetingGuessNumOption = NebulaAPI.Configurations.Configuration("options.role.newsAnchor.everyGuessNum", (1, 20), 2);
    public static IntegerConfiguration ForceUseSkillMeetingNumOption = NebulaAPI.Configurations.Configuration("options.role.newsAnchor.forceSkillMeetingNum", (1, 20), 2);
    public static BoolConfiguration AnchorCannotGuessCoAnchorOption = NebulaAPI.Configurations.Configuration("options.role.newsAnchor.anchorCannotGuessCoAnchor", false);
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }
    
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/newsAnchor.png")!.AsImage();
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0),arguments.Get(1,SkillNumOption),arguments.Get(2,GuessNumOption));
    public static void LoadPatch(Harmony harmony)

    {
        PDebug.Log("NewsAnchorPatch");
        harmony.Patch(typeof(PlayerModInfo).GetMethod(typeof(IPlayerlike).FullName + ".get_CanBeTarget", AccessTools.all), null, new HarmonyMethod(typeof(NewsAnchor).GetMethod("FixCanBeTarget")));
        PDebug.Log("Done");
    }
    public static void FixCanBeTarget(GamePlayer __instance,ref bool __result)
    {
        var newsAnchor = NebulaGameManager.Instance.AllPlayerInfo.FirstOrDefault(p => p.TryGetAbility<NewsAnchor.Ability>(out var ab) && ab.CoAnchorPlayer == __instance);
        if (newsAnchor!=null&&newsAnchor.TryGetAbility<Ability>(out var ab)&&ab.CoAnchorPlayer==__instance)
        {
            __result = false;
        }
    }
    static public NewsAnchor MyRole = new NewsAnchor();
    public static RemoteProcess<string> RpcSendChat = new("SendNewsChat",
(string message, bool _) =>
{
    var senderPlayer=PlayerControl.AllPlayerControls.ToArray().Where(p=>!p.Data.IsDead).Shuffle().Random();
    var messagename = "News Send".Color("red");
    var name = senderPlayer.name;
    senderPlayer.SetName(messagename);
    AmongUsLLImpl.HudManagerBridge.Chat.AddChat(senderPlayer, message, false);
    senderPlayer.SetName(name);
});
    public static RemoteProcess<(GamePlayer newsAnchor, bool isinNews, GamePlayer coanchor,bool containAbilityOwner)> RpcUpdateNewsAnchorInfo = new("RpcUpdateNewsAnchorInfo", (m, _) =>
    {
        if (m.newsAnchor.TryGetAbility<Ability>(out var ab) && (m.containAbilityOwner||!ab.AmOwner))
        {
            ab.IsInNewsPhase = m.isinNews;
            ab.CoAnchorPlayer = m.coanchor;
            if (ab.IsInNewsPhase&&ab.CoAnchorPlayer!=null)
            {
                ab.newsHud=new NewsPhaseMeetingHud(ab);
                MeetingHud.Instance.ResetPlayerState();
            }
            else if (!ab.IsInNewsPhase&&ab.CoAnchorPlayer==null&&ab.newsHud!=null)
            {
                ab.newsHud.Release();
                ab.newsHud = null;
                MeetingHud.Instance.ResetPlayerState();
            }
        }
    });
    public static RemoteProcess<(GamePlayer newsAnchor, int leftSkill,int leftGuess,int everyMeetingGuess)> RpcUpdateNewsAnchorNumInfo = new("RpcUpdateNewsAnchorNumInfo", (m, _) =>
    {
        if (m.newsAnchor.TryGetAbility<Ability>(out var ab))
        {
            ab.leftSkill = m.leftSkill;
            ab.leftGuess = m.leftGuess;
            ab.leftMeetingGuess = m.everyMeetingGuess;
        }
    });
    //bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(Ability.SkillIcon, "role.newsAnchor.ability.skill");
        yield return new(Ability.VoteIcon, "role.newsAnchor.ability.vote");
    }


    static OutfitDefinition.OutfitId CoAnchorOutfitId = new OutfitDefinition.OutfitId(-2, -2);
    public static OutfitDefinition CoAnchorOutfit = new OutfitDefinition(CoAnchorOutfitId, new NetworkedPlayerInfo.PlayerOutfit
    {
        PlayerName = "Co-Anchor",
        ColorId = (int)NebulaPlayerTab.CamouflageColorId,
        HatId = "hat_pk05_Burthat",
        SkinId = "skin_None",
        VisorId = "visor_EmptyVisor",
        PetId = "pet_EmptyPet"

    }, Array.Empty<OutfitTag>());
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        public static Image SkillIcon = NebulaAPI.AddonAsset.GetResource("newsAnchorSkill.png")!.AsImage(115f)!;
        public static Image VoteIcon = NebulaAPI.AddonAsset.GetResource("newsAnchorVote.png")!.AsImage(125f)!;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt(),leftSkill,leftGuess];
        public Ability(GamePlayer player, bool isUsurped,int leftskill,int leftGuess) : base(player, isUsurped)
        {
            leftSkill = leftskill;
            this.leftGuess = leftGuess;
            meetingnum = 0;
            forceSkilled = false;
            if (AmOwner)
            {
                string prefix = Language.Translate("roles.newsanchor.leftskill");
                Helpers.TextHudContent("NewsAnchorText", this, delegate (TextMeshPro tmPro)
                {
                    tmPro.text = prefix + "" + leftSkill.ToString();
                }, true);
            }
        }
        public bool IsInNewsPhase;
        public GamePlayer CoAnchorPlayer;
        public NewsPhaseMeetingHud newsHud;
        public int leftSkill;
        public int leftGuess;
        public int leftMeetingGuess;
        public int meetingnum;
        public bool forceSkilled;
        void OnMeetingStart(MeetingStartEvent ev)
        {
            meetingnum++;
            leftMeetingGuess = EveryMeetingGuessNumOption;
            if (!AmOwner)
            {
                return;
            }
            if (MyPlayer.IsAlive && leftSkill > 0 && !forceSkilled && meetingnum > ForceUseSkillMeetingNumOption)
            {
                leftSkill--;
                IsInNewsPhase = true;
                CoAnchorPlayer = MyPlayer;
                using (RPCRouter.CreateSection("RpcUpdateInfoNewsAnchor"))
                {
                    RpcUpdateNewsAnchorInfo.Invoke((MyPlayer, IsInNewsPhase, CoAnchorPlayer, false));
                    RpcUpdateNewsAnchorNumInfo.Invoke((MyPlayer, leftSkill, leftGuess, leftMeetingGuess));
                }
                NewsPhaseMeetingHud.isInAniming = true;
                MeetingHud.Instance.StartCoroutine(CoWaitAnimGenerateHud());
                forceSkilled = true;
                return;
            }
            System.Collections.IEnumerator CoWaitAnimGenerateHud()
            {
                if (MeetingHud.Instance.state == MeetingHud.VoteStates.Animating)
                {
                    yield return null;
                }
                yield return ManagedEffects.Wait(5f);
                newsHud = new NewsPhaseMeetingHud(this);
                RpcUpdateNewsAnchorInfo.Invoke((MyPlayer, IsInNewsPhase, CoAnchorPlayer, false));
                MeetingHud.Instance.ResetPlayerState();
                MeetingHud.Instance.UpdatePlayerState();
            }
            if (IsInNewsPhase && CoAnchorPlayer != null)
            {
                NewsPhaseMeetingHud.isInAniming = true;
                MeetingHud.Instance.StartCoroutine(CoWaitAnimGenerateHud());
                //MeetingModRpc.RpcChangeVotingStyle.Invoke((1 << 31, true, 120f, false, false));
                return;
            }
            NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>()?.RegisterMeetingAction(new(SkillIcon,
   p =>
   {
       if (MeetingHudExtension.CanInvokeSomeAction)
       {
           if (!MyPlayer.TryGetAbility<Ability>(out var a))
           {
               return;
           }
           leftSkill--;
           IsInNewsPhase = true;
           CoAnchorPlayer = p.MyPlayer;
           newsHud = new NewsPhaseMeetingHud(this);
           using (RPCRouter.CreateSection("RpcUpdateInfoNewsAnchor"))
           {
               RpcUpdateNewsAnchorInfo.Invoke((MyPlayer, IsInNewsPhase, CoAnchorPlayer,false));
               RpcUpdateNewsAnchorNumInfo.Invoke((MyPlayer, leftSkill, leftGuess,leftMeetingGuess));
           }
           MeetingHud.Instance.ResetPlayerState();
           MeetingHud.Instance.UpdatePlayerState();
           //MeetingModRpc.RpcChangeVotingStyle.Invoke((1 << 31, true, 120f, false, false));
       }
   },
   p => MyPlayer.TryGetAbility<Ability>(out var i)&&leftSkill>0&&!p.MyPlayer.IsDead &&(!p.MyPlayer.TryGetModifier<PMini.Instance>(out var mini)||!mini.IsMini())&& !MyPlayer.IsDead&&(!NebulaGameManager.Instance?.AllPlayerInfo.Any(p=>p.TryGetAbility<Ability>(out var ab)&&ab.IsInNewsPhase)??true&&!MeetingHudExtension.CanSkip)
   ));
        }
        [Local]
        [EventPriority(-1000)]
        void DecorateCoAnchor(PlayerDecorateNameEvent ev)
        {
            if (ev.Player==CoAnchorPlayer&&!NebulaGameManager.Instance.CanSeeAllInfo)
            {
                ev.Name += $"({ev.Player.PlayerName})".Color(UnityEngine.Color.gray);
            }
        }
        [EventPriority(-500)]
        void DecorateCoAnchorGlobal(PlayerDecorateNameEvent ev)
        {
            if (ev.Player == CoAnchorPlayer && !NebulaGameManager.Instance.CanSeeAllInfo&&!MyPlayer.AmOwner&&(MeetingHud.Instance&&(newsHud!=null&&!NewsPhaseMeetingHud.isInAniming)))
            {
                ev.Name = "Co-Anchor";
            }
        }
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (CoAnchorPlayer!=null)
            {
                CoAnchorPlayer.AddOutfit(new OutfitCandidate(CoAnchorOutfit, "CoAnchor", 20, true, null));
            }
        }
        void OnMeetingPreStart(MeetingPreStartEvent ev)
        {
            if (CoAnchorPlayer!=null)
            {
                CoAnchorPlayer.RemoveOutfitByTag("CoAnchor");
            }
        }
        void OnPlayerDie(PlayerDieOrDisconnectEvent ev)
        {
            if (ev.Player==MyPlayer || ev.Player==CoAnchorPlayer)
            {
                if (newsHud != null)
                {
                    newsHud.Release();
                    newsHud = null;
                }
                CoAnchorPlayer?.RemoveOutfitByTag("CoAnchor");
                IsInNewsPhase = false;
                CoAnchorPlayer = null;
            }
        }
        void OnMeetingUpdate(GameUpdateEvent ev)
        {
            if (newsHud!=null&&!NewsPhaseMeetingHud.isInAniming&&MeetingHud.Instance&&!MeetingHudExtension.CanSkip)
            {
                MeetingHud.Instance.playerStates.Do(v => v.gameObject.SetActive(!IsInNewsPhase));
            }
        }
        void IGameOperator.OnReleased()
        {
            if (newsHud != null)
            {
                newsHud.Release();
                newsHud = null;
            }
            CoAnchorPlayer?.RemoveOutfitByTag("CoAnchor");
            IsInNewsPhase = false;
            CoAnchorPlayer = null;
        }
    }
    public class NewsPhaseMeetingHud : FlexibleLifespan,IGameOperator
    {
        PlayerVoteArea? fakeArea;
        public static bool isInAniming;
        System.Collections.IEnumerator CoShowVotingArea(MeetingHud meetingHud, float duration, bool isappear)
        {
            if (!MeetingHud.Instance.AsBoolFast())
            {
                yield break;
            }
            MeetingHudExtension.CanVote = isappear;
            var states = meetingHud.playerStates.OrderBy(i => Guid.NewGuid()).ToArray();
            var interval = duration / states.Length;
            foreach (var state in states)
            {
                state.gameObject.SetActive(isappear);
                yield return Effects.Wait(interval);
            }
            PlayerVoteArea pva = UnityEngine.Object.Instantiate<PlayerVoteArea>(MeetingHud.Instance.MeetingIntro.VoteAreaPrefab, MeetingHud.Instance.transform);
            pva.transform.localPosition = UnityEngine.Vector3.zero;
            pva.SetMaskLayer(255);
            pva.Background.sprite = AmongUsLLImpl.ShipStatusInstance.CosmeticsCache.GetNameplate("nameplate_NoPlate").Image;
            pva.PlayerIcon.UpdateFromPlayerOutfit(NewsAnchor.CoAnchorOutfit.outfit, PlayerMaterial.MaskType.ComplexUI, false, false, null, false);
            pva.PlayerIcon.ToggleName(false);
            pva.NameText.text = "Co-Anchor";
            pva.LevelNumberText.transform.parent.gameObject.SetActive(false);
            pva.SetDead(false,false, false);
            if (fakeArea)
            {
                UnityEngine.Object.Destroy(fakeArea);
                fakeArea = null;
            }
            fakeArea = pva;
            isInAniming = false;
        }
        MetaScreen screen;
        void OnClickArea()
        {
            if (screen!=null)
            {
                screen.CloseScreen();
            }
            screen=OpenWindow();
        }
        void IGameOperator.OnReleased()
        {
            try
            {
                //PatchManager.CheckCanUseChat();
                if (!MeetingHudExtension.CanSkip)
                {
                    isInAniming = true;
                    MeetingHud.Instance.StartCoroutine(CoShowVotingArea(MeetingHud.Instance, UnityEngine.Random.Range(1.5f, 2.5f), true));
                }
                MeetingHudExtension.CanSkip = true;
                if (fakeArea)
                {
                    UnityEngine.Object.Destroy(fakeArea);
                    fakeArea = null;
                }
                if (binder)
                {
                    UnityEngine.Object.Destroy(binder);
                    binder = null;
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        GameObject binder;
        NewsAnchor.Ability ability;
        public NewsPhaseMeetingHud(NewsAnchor.Ability ab)
        {
            MeetingHudExtension.CanSkip = false;
            ability = ab;
            if ((GamePlayer.LocalPlayer.TryGetAbility<NewsAnchor.Ability>(out var a) && a.IsInNewsPhase) || (NebulaGameManager.Instance!.AllPlayerInfo.Any(player => player.TryGetAbility<NewsAnchor.Ability>(out var a2) && a2.IsInNewsPhase && a2.CoAnchorPlayer == GamePlayer.LocalPlayer)))
            {
                var c = new Virial.Color(138, 138, 138, 255);
                PrivateChatManager.Instance.RegisterPublicChannel(c,c, "NewsAnchor", Language.Translate("chat.newstext"), this,() => MeetingHud.Instance,true, (s) => NewsAnchor.RpcSendChat.Invoke(s));
            }
            //PatchManager.CheckCanUseChat();
            this.RegisterSelf();
            MeetingHud.Instance.StartCoroutine(CoShowVotingArea(MeetingHud.Instance, UnityEngine.Random.Range(1.5f, 2.5f), false));
            binder = UnityHelper.CreateObject("NewsAnchor-OpenWindow", MeetingHud.Instance.SkipVoteButton.transform.parent, MeetingHud.Instance.SkipVoteButton.transform.localPosition, null);
            GameOperatorManager instance = GameOperatorManager.Instance;
            if (instance != null)
            {
                instance.Subscribe<GameUpdateEvent>(delegate (GameUpdateEvent ev)
                {
                    binder.gameObject.SetActive(!GamePlayer.LocalPlayer.IsDead && (MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.NotVoted || (MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.Voted&&ability.leftGuess>0)));
                }, new GameObjectLifespan(binder), 100);
            }
            MeetingHud.Instance.ResetPlayerState();
            SpriteRenderer leftRenderer = UnityHelper.CreateObject<SpriteRenderer>("NewsAnchor-OpenWindow", binder.transform, new Vector3(1.77f, 0.18f), null);
            leftRenderer.sprite = NewsAnchor.Ability.VoteIcon.GetSprite();
            PassiveButton passiveButton = leftRenderer.gameObject.SetUpButton(true, new SpriteRenderer[0], null, null);
            passiveButton.OnMouseOver.AddListener(delegate
            {
                leftRenderer.color = UnityEngine.Color.green;
            });
            passiveButton.OnMouseOut.AddListener(delegate
            {
                leftRenderer.color = UnityEngine.Color.white;
            });
            passiveButton.OnClick.AddListener(delegate
            {
                if (MeetingHudExtension.CanInvokeSomeAction)
                {
                    OnClickArea();
                }
            });
            leftRenderer.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.6f);
            isInAniming = true;
        }
        MetaScreen OpenWindow()
        {
            try
            {
                var window = MetaScreen.GenerateWindow(new Vector2(7.5f, 4.5f), AmongUsLLImpl.HudManagerInstance.transform, new Vector3(0, 0, -400f), true, false);
                //widget.Append(new MetaWidgetOld.Text(TextAttributeOld.TitleAttr) { RawText = Language.Translate("role.metaRole.ui.roles") });
                var roleMaskedTittleAttr = GUI.API.GetAttribute(Virial.Text.AttributeAsset.MetaRoleButton);
                var roleTitleAttr = new Virial.Text.TextAttribute(roleMaskedTittleAttr) { Font = GUI.API.GetFont(Virial.Text.FontAsset.Gothic), Size = new(1.1f, 0.22f) };
                void SetWidget(int tab)
                {
                    List<GUIWidget> widgets = new List<GUIWidget>();
                    bool hasVote = false, hasGuess = false;
                    if (MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)
                    {
                        hasVote = true;
                        widgets.Add(GUI.API.LocalizedButton(Virial.Media.GUIAlignment.Center, roleTitleAttr, "roles.newsanchor.tabs.vote", (button) => SetWidget(0), color: tab == 0 ? Virial.Color.Yellow : null));
                    }
                    if (ability.leftGuess > 0&&(!AnchorCannotGuessCoAnchorOption||!ability!.AmOwner))
                    {
                        hasGuess = true;
                        widgets.Add(GUI.API.LocalizedButton(Virial.Media.GUIAlignment.Center, roleTitleAttr, "roles.newsanchor.tabs.guess", (button) => SetWidget(1), color: tab == 1 ? Virial.Color.Yellow : null));
                    }
                    var holder = GUI.API.VerticalHolder(GUIAlignment.Center, GUI.API.HorizontalHolder(Virial.Media.GUIAlignment.Center,widgets));
                    GUIWidget inner = GUIEmptyWidget.Default;
                    if (tab == 0&&hasVote)
                    {
                        inner = GUI.API.Arrange(Virial.Media.GUIAlignment.Center, NebulaGameManager.Instance.AllPlayerInfo.Select(p => GUI.API.RawButton(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, p.Unbox().ColoredDefaultName, button =>
                        {
                            if (MeetingHud.Instance.state!=MeetingHud.VoteStates.NotVoted)
                            {
                                return;
                            }
                            MeetingHud.Instance.state = MeetingHud.VoteStates.Voted;
                            int vote = GameOperatorManager.Instance?.Run<PlayerVoteCastLocalEvent>(new PlayerVoteCastLocalEvent(Player.LocalPlayer, NebulaGameManager.Instance.GetPlayer(p.PlayerId), 1), false, true).Vote ?? 1;
                            MeetingHud.Instance.ModCastVote(AmongUsLLImpl.LocalPlayer.PlayerId, p.PlayerId, vote);
                            window.CloseScreen();
                            AmongUsLLImpl.SoundManagerInstance.PlaySound(MeetingHud.Instance.VoteLockinSound, false, 1f, null);
                        })), 4);
                    }
                    else if (hasGuess)
                    {
                        inner = GUI.API.VerticalHolder(GUIAlignment.Center,[GUI.API.Arrange(Virial.Media.GUIAlignment.Center, NebulaGameManager.Instance.AllPlayerInfo.Select(p => GUI.API.RawButton(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, p.Unbox().ColoredDefaultName, button =>
                        {
                            if (ability.CoAnchorPlayer==p)
                            {
                                ability.MyPlayer.MurderPlayer(ability.CoAnchorPlayer, PlayerStates.Dead, EventDetails.Kill, KillParameter.MeetingKill, KillCondition.BothAlive);
                                RpcSendChat.Invoke(Language.Translate("role.newsAnchor.guessCorrectText").Replace("%ROLE%", p.Role.DisplayColoredName));
                            }
                            else
                            {
                                GamePlayer.LocalPlayer?.Suicide(PlayerStates.Suicide, EventDetails.Kill, KillParameter.MeetingKill);
                            }
                            ability.leftGuess--;
                            ability.leftMeetingGuess--;
                            RpcUpdateNewsAnchorNumInfo.Invoke((ability.MyPlayer, ability.leftSkill, ability.leftGuess,ability.leftMeetingGuess));
                            window.CloseScreen();
                        })), 4),GUI.API.RawText(GUIAlignment.Center, NebulaAPI.GUI.GetAttribute(AttributeParams.StandardBold),Language.Translate("roles.newsanchor.leftguess") + ability.leftMeetingGuess+$"({ability.leftGuess})")]);
                    }
                    var widget = GUI.API.VerticalHolder(Virial.Media.GUIAlignment.Center, holder,
                        GUI.API.VerticalMargin(0.15f),
                        GUI.API.ScrollView(Virial.Media.GUIAlignment.Center, new(7.4f, 3f), null, inner, out _));
                    window.SetWidget(widget, out _);
                }
                SetWidget(0);
                return window;
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
            return null;
        }
        void OnMeetingEnd(ExileSceneStartEvent ev)
        {
            Release();
        }
    }
}
