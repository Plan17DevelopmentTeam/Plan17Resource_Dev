
namespace Plana.Roles.Crewmate;

[NebulaRPCHolder]
public class BadSheriff : DefinedSingleAbilityRoleTemplate<BadSheriff.Ability>, HasCitation, DefinedRole, IAssignableDocument
{
    private BadSheriff() : base("badsheriff", Virial.Color.ImpostorColor, RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [KillCoolDownOption, NumOfShotsOption, CanKillHidingPlayerOption, SealAbilityUntilReportingDeadBodiesOption, UnsealOnReportButtonTurningOnOption,ShareKillPlayerRoleToAllPlayer,ShareKillPlayerRoleToNonCrewmatePlayer])
    {
        //ConfigurationHolder?.AddTags(ConfigurationTags.TagBeginner);
        //ConfigurationHolder!.Illustration = new NebulaSpriteLoader("Assets/NebulaAssets/Sprites/Configurations/Sheriff.png");
        ConfigurationHolder?.ScheduleAddRelated(() => [Sheriff.MyRole.ConfigurationHolder!,Vanity.MyRole.ConfigurationHolder!]);
        Vanity.MyRole.ConfigurationHolder?.ScheduleAddRelated(() => [ConfigurationHolder]);
        Sheriff.MyRole.ConfigurationHolder?.ScheduleAddRelated(() => [ConfigurationHolder]);
    }

    Citation? HasCitation.Citation => PCitations.PlanaANDKC;

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(Sheriff.buttonSprite, "role.badsheriff.ability.kill");
        if (SealAbilityUntilReportingDeadBodiesOption) yield return new(ButtonEffect.LockedImage, "role.badsheriff.ability.seal");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%VENT%", CanKillHidingPlayerOption ? Language.Translate("role.badsheriff.ability.kill.vent") : "");
    }
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/BadSheriff.png")!.AsImage();
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0), arguments.Get(1, NumOfShotsOption));
    //AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    static internal readonly IRelativeCooldownConfiguration KillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.badsheriff.killCoolDown", CoolDownType.Relative, (10f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), +5f, (0.125f, 2f, 0.125f), 1.25f);
    static internal readonly IntegerConfiguration NumOfShotsOption = NebulaAPI.Configurations.Configuration("options.role.badsheriff.numOfShots", (0, 15), 0,null, val => val == 0 ? Language.Translate("options.noLimit") : val.ToString());
    static internal readonly BoolConfiguration CanKillHidingPlayerOption = NebulaAPI.Configurations.Configuration("options.role.badsheriff.canKillHidingPlayer", false);
    static internal readonly BoolConfiguration SealAbilityUntilReportingDeadBodiesOption = NebulaAPI.Configurations.Configuration("options.role.badsheriff.sealAbilityUntilReportingDeadBodies", false);
    static internal readonly BoolConfiguration UnsealOnReportButtonTurningOnOption = NebulaAPI.Configurations.Configuration("options.role.badsheriff.unsealOnReportButtonTurningOn", false, () => SealAbilityUntilReportingDeadBodiesOption);
    static internal readonly BoolConfiguration ShareKillPlayerRoleToAllPlayer = NebulaAPI.Configurations.Configuration("options.role.badsheriff.shareKillPlayerRoleToAllPlayer", false);
    static internal readonly BoolConfiguration ShareKillPlayerRoleToNonCrewmatePlayer = NebulaAPI.Configurations.Configuration("options.role.badsheriff.shareKillPlayerRoleToNonCrewmatePlayer", true,()=>!ShareKillPlayerRoleToAllPlayer);
    bool DefinedRole.IsMadmate => true;
    static public readonly BadSheriff MyRole = new();
    public static RemoteProcess<(GamePlayer, int)> UpdateBadSheriffArgumentsRpc = new("UpdateBadSheriffArgumentRpc", (m, _) =>
    {
        if (m.Item1.TryGetAbility<Ability>(out var a))
        {
            a.leftShots = m.Item2;
        }
    });
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        private ModAbilityButtonImpl? killButton = null;
        public static Image MeetingInfoIcon = NebulaAPI.AddonAsset.GetResource("badsheriff.MeetingInfo.png")!.AsImage(135f)!;

        static internal Image KillButtonSprite => Sheriff.buttonSprite;
        public int leftShots;

        int killCount;
        bool killedImpostor;

        public Ability(GamePlayer player, bool isUsurped, int shots) : base(player, isUsurped)
        {
            leftShots = shots;
            if (AmOwner)
            {
                player.Tasks.Unbox().BecomeToOutsider();
                var killTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, ObjectTrackers.PlayerlikeKillablePredicate(MyPlayer), null, CanKillHidingPlayerOption);
                killButton = new ModAbilityButtonImpl(isArrangedAsKillButton: MyPlayer.IsCrewmate).KeyBind(MyPlayer.IsCrewmate ? Virial.Compat.VirtualKeyInput.Kill : Virial.Compat.VirtualKeyInput.Ability).Register(this);
                TextMeshPro leftText = null;
                if (NumOfShotsOption != 0)
                {
                    leftText=killButton.ShowUsesIcon(0);
                    leftText.text = leftShots.ToString();
                }
                killButton.SetSprite(KillButtonSprite.GetSprite());
                SpriteRenderer? lockSprite = null;
                if (SealAbilityUntilReportingDeadBodiesOption && !NebulaGameManager.Instance!.AllPlayerInfo.Any(p => p.IsDead))
                {
                    lockSprite = killButton.VanillaButton.AddLockedOverlay();

                    if (UnsealOnReportButtonTurningOnOption)
                    {
                        GameOperatorManager.Instance?.Subscribe<GameUpdateEvent>(ev =>
                        {
                            if (lockSprite != null && HudManager.InstanceExists && HudManager.Instance.ReportButton.canInteract)
                            {
                                killButton.PlayFlashOnce();
                                GameObject.Destroy(lockSprite!.gameObject);
                                lockSprite = null;
                            }
                        }, this);
                    }
                }
                killButton.Availability = (button) => killTracker.CurrentTarget != null && MyPlayer.CanMove && !MyPlayer.WillDie && lockSprite == null;
                killButton.Visibility = (button) => !MyPlayer.IsDead && (NumOfShotsOption==0||leftShots > 0);
                killButton.OnClick = (button) => {
                    var target = killTracker.CurrentTarget;
                    if (CanKill(target.RealPlayer))
                    {
                        if (target.RealPlayer.IsTrueCrewmate)
                        {
                            new StaticAchievementToken("badsheriff.common1");
                            killCount++;
                        }

                        if (target.RealPlayer.IsImpostor)
                        {
                            killedImpostor = true;
                        }

                        if (target.RealPlayer.TryGetAbility<Sheriff.Ability>(out var ab))
                        {
                            target.RealPlayer.MurderPlayer(MyPlayer, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                            new StaticAchievementToken("badsheriff.another1");
                            return;
                        }
                        if (target.RealPlayer.Role is SchrodingerCat.Instance)
                        {
                            MyPlayer.Suicide(PlayerState.Misfired, PlayerState.Suicide, Virial.Game.KillParameter.NormalKill);
                            NebulaGameManager.Instance?.GameStatistics.RpcRecordEvent(GameStatistics.EventVariation.Kill, EventDetail.Misfire, MyPlayer.VanillaPlayer, target!.RealPlayer.VanillaPlayer);
                            Sheriff.RpcShareExtraInfo.Invoke((MyPlayer, target.RealPlayer));
                            target.RealPlayer.SetRole(MyRole, MyPlayer.Role.RoleArguments);
                            return;
                        }
                        MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                    }
                    else
                    {
                        MyPlayer.MurderPlayer(target,PlayerState.Misfired, EventDetail.Kill,KillParameter.RemoteKill,KillCondition.TargetAlive);
                        MyPlayer.Suicide(PlayerState.Misfired, PlayerState.Suicide, Virial.Game.KillParameter.NormalKill);
                        NebulaGameManager.Instance?.GameStatistics.RpcRecordEvent(GameStatistics.EventVariation.Kill, EventDetail.Misfire, MyPlayer.VanillaPlayer, target!.RealPlayer.VanillaPlayer);
                        Sheriff.RpcShareExtraInfo.Invoke((MyPlayer, target.RealPlayer));;
                    }
                    button.StartCoolDown();

                    leftText?.text = (--leftShots).ToString();
                    UpdateBadSheriffArgumentsRpc.Invoke((MyPlayer, leftShots));
                };
                killButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, KillCoolDownOption.GetCooldown(MyPlayer.TeamKillCooldown)).SetAsKillCoolTimer();
                killButton.CoolDownTimer.Start();
                killButton.SetLabel("kill");
                killButton.OnMeeting = button =>
                {
                    if (lockSprite && NebulaGameManager.Instance!.AllPlayerInfo.Any(p => p.IsDead))
                    {
                        GameObject.Destroy(lockSprite!.gameObject);
                        lockSprite = null;
                    }
                };
                killButton.RelatedAbility = this;
            }
        }
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt(), leftShots];
        private bool CanKill(GamePlayer target)
        {
            return target.Role.Role.Category == RoleCategory.CrewmateRole;
        }
        List<byte> killplayers = new();
        [OnlyMyPlayer]
        void OnKill(PlayerKillPlayerEvent ev)
        {
            if (ev.Dead.PlayerId==MyPlayer.PlayerId)
            {
                return;
            }
            if (ev.Dead.PlayerState==PlayerState.Cursed)
            {
                return;
            }
            if (AmOwner&&!killplayers.Contains(ev.Dead.PlayerId))
            {
                killplayers.Add(ev.Dead!.PlayerId);
            }
            if (ShareKillPlayerRoleToAllPlayer || (ShareKillPlayerRoleToNonCrewmatePlayer && (GamePlayer.LocalPlayer.Role.Role.Category != RoleCategory.CrewmateRole||GamePlayer.LocalPlayer.IsMadmate)))
            {
                HideAndSeekDeathPopup hideAndSeekDeathPopup = UnityEngine.Object.Instantiate<HideAndSeekDeathPopup>(GameManagerCreator.Instance.HideAndSeekManagerPrefab.DeathPopupPrefab, DestroyableSingleton<HudManager>.Instance.transform.parent);
                hideAndSeekDeathPopup.nameplate.gameObject.SetActive(false);
                hideAndSeekDeathPopup.text.enabled = true;
                hideAndSeekDeathPopup.transform.localPosition = UnityEngine.Vector3.zero;
                hideAndSeekDeathPopup.StartCoroutine(hideAndSeekDeathPopup.AnimateCoroutine());
                TextTranslatorTMP tmp;
                if (hideAndSeekDeathPopup.text.transform.TryGetComponent<TextTranslatorTMP>(out tmp))
                {
                    tmp.defaultStr = Language.Translate("role.badSheriff.onCrewmateKill").Replace("%ROLE%", ev.Dead.Role.Role.DisplayName);
                    tmp.TargetText = StringNames.None;
                    tmp.ResetText();
                }
            }
        }
        [OnlyMyPlayer]
        private void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd == NebulaGameEnd.ImpostorWin);
        }
        [OnlyMyPlayer]
        private void BlockWins(PlayerBlockWinEvent ev)
        {
            ev.SetBlockedIf(ev.GameEnd == NebulaGameEnd.CrewmateWin);
        }

        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (MyPlayer.IsAlive && killCount >= 3)
            {
                new StaticAchievementToken("badsheriff.challenge1");
            }

            if (ev.EndState.EndCondition == NebulaGameEnds.ImpostorGameEnd.Get() && ev.CheckWin(MyPlayer))
            {
                if (!GamePlayer.AllPlayers.Any(p =>p.IsImpostor && p.IsAlive))
                {
                    new StaticAchievementToken("badsheriff.challenge2");
                }

                if (killedImpostor)
                {
                    new StaticAchievementToken("badsheriff.another2");
                }
            }
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            foreach (var p in killplayers)
            {
                var player = GamePlayer.GetPlayer(p);
                var sb = new StringBuilder().Append(Language.Translate("role.badsheriff.playername")).Append(":").Append(player.ColoredName).Append(Environment.NewLine).Append(Language.Translate("role.badsheriff.message.role")).Append(":").Append(player.Role.DisplayColoredName);
                NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay(NebulaAPI.GUI.VerticalHolder(GUIAlignment.Left, [
                    NebulaAPI.GUI.LocalizedText(GUIAlignment.Left,NebulaAPI.GUI.GetAttribute(AttributeAsset.OverlayTitle),"role.badsheriff.message.header"),
                    NebulaAPI.GUI.RawText(GUIAlignment.Center,NebulaAPI.GUI.GetAttribute(AttributeAsset.OverlayContent),sb.ToString())
                    ]), BadSheriff.Ability.MeetingInfoIcon, MyRole.RoleColor, false);
            }
            killplayers.Clear();
        }
        void OnSheriffKill(SheriffCheckKillEvent ev)
        {
            if (ev.Player.IsModMadmate())
            {
                ev.CanKill = false;
            }
            else if (ev.Target.IsModMadmate())
            {
                ev.CanKill = true;
            }
        }
        void OnCollactorCheckTeam(CollatorCheckTeamEvent ev)
        {
            if (ev.Target.PlayerId == MyPlayer.PlayerId)
            {
                var value = NebulaAPI.Configurations.Configuration("options.role.collator.madmateIsClassifiedAs", new string[] { "options.role.collator.madmateIsClassifiedAs.impostor", "options.role.collator.madmateIsClassifiedAs.crewmate" }, 0, null, null).GetValue();
                ev.Team = value == 0 ? NebulaTeams.ImpostorTeam : NebulaTeams.CrewmateTeam;
            }
        }
    }
}
