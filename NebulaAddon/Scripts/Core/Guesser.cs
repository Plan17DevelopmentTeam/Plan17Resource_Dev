using Nebula.Roles;
using Nebula.Roles.Complex;
using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Impostor;
using Plana.Roles.Modifier;
using Plana.Roles.Neutral;
using Virial.Assignable;

namespace Plana.Core;


[NebulaRPCHolder]
public static class GuesserSystem
{
    private static void GuessedLockTargets(GamePlayer __instance, TextMeshProHandler roleText, bool inMeeting)
    {
        if (inMeeting)
        {
            if (playerGuess.TryGetValue(__instance.PlayerId, out var roleid))
            {
                roleText.RequestChange(roleText.currentText+"(".Color(UnityEngine.Color.white) + Language.Translate("guessLockedText").Replace("%LOCKEDTEXT2%", Language.Translate("guessLockedText2")).Color(NebulaTeams.ImpostorTeam.Color.ToUnityColor()).Replace("%ROLE%", Nebula.Roles.Roles.AllRoles[roleid].DisplayColoredName) + ")".Color(UnityEngine.Color.white));
            }
        }
    }
    private record MisguessedExtraDeadInfo(GamePlayer To, DefinedRole Role) : GamePlayer.ExtraDeadInfo(PlayerStates.Misguessed)
    {
        public override string ToStateText() => To.PlayerName + " as " + Role.DisplayColoredName;
    }
   public static RemoteProcess<(GamePlayer guesser, GamePlayer to, int roleid)> RpcShareExtraInfo = new("ShareExInfoGuessOfGuesserEX",
        (message, _) => {
            message.guesser.PlayerStateExtraInfo = new MisguessedExtraDeadInfo(message.to, Nebula.Roles.Roles.GetRole(message.roleid));
        }
    );
    static TextAttributeOld ButtonAttribute = new TextAttributeOld(TextAttributeOld.BoldAttr) { Size = new(1.3f, 0.3f), Alignment = TMPro.TextAlignmentOptions.Center, FontMaterial = VanillaAsset.StandardMaskedFontMaterial }.EditFontSize(2f, 1f, 2f);
    static TextAttributeOld ModifierButtonAttribute = new TextAttributeOld(TextAttributeOld.BoldAttr) { Size = new(1.04f, 0.3f), Alignment = TMPro.TextAlignmentOptions.Center, FontMaterial = VanillaAsset.StandardMaskedFontMaterial }.EditFontSize(1.7f, 0.8f, 2f);
    public static MetaScreen LastGuesserWindow = null!;

    static public MetaScreen OpenGuessWindow(int leftGuessPerMeeting, int leftGuess,int safeGuess, Action<DefinedRole> onSelected)
    {
        if (ClientOption.GetValue((ClientOption.ClientOptionType)125) == 1)
        {
            AmongUsLLImpl.SoundManagerInstance.PlaySound(PatchManager.GetSound("Gunload"), false, 1f);
        }
        var sb = new StringBuilder(Language.Translate("role.guesser.leftGuess")).Append(":");
        if (leftGuessPerMeeting < leftGuess)
            sb.Append(leftGuessPerMeeting).Append(" (").Append(leftGuess).Append(")");
        else
            sb.Append(leftGuess);
        if (safeGuess>0)
        {
            sb.Append(" (").Append(Language.Translate("role.guesser.leftSafeGuess")).Append(":").Append(safeGuess).Append(")");
        }
        return MeetingRoleSelectWindow.OpenRoleSelectWindow(!PatchManager.GuesserCannotGuessNoSpawnableRole?Nebula.Roles.Roles.AllRoles:null,(r)=>r.CanBeGuess&&!r.IsSystemRole, GamePlayer.LocalPlayer?.FeelBeTrueCrewmate??false, sb.ToString(), onSelected);
    }
    static public MetaScreen DoomsayerOpenGuessWindow(Action<DefinedRole> onSelected)
    {
        if (ClientOption.GetValue((ClientOption.ClientOptionType)125) == 1)
        {
            AmongUsLLImpl.SoundManagerInstance.PlaySound(PatchManager.GetSound("Gunload"), false, 1f);
        }
        return MeetingRoleSelectWindow.OpenRoleSelectWindow(!PatchManager.GuesserCannotGuessNoSpawnableRole ? Nebula.Roles.Roles.AllRoles : null, (r) => r.CanBeGuess&&!r.IsSystemRole, GamePlayer.LocalPlayer?.FeelBeTrueCrewmate ?? false, "", onSelected);
    }
    internal static DividedSpriteLoader Icons = DividedSpriteLoader.FromResource("Nebula.Resources.MeetingButtons.png", 115f, 100, 110, true);
    internal static Image DoubleGuessIcon = NebulaAPI.AddonAsset.GetResource("DoubleGuess.png").AsImage(115f);
    public static RemoteProcess<ValueTuple<GamePlayer, GamePlayer, int>> GuessMessageRPC = new RemoteProcess<ValueTuple<GamePlayer, GamePlayer, int>>("GuessMessage", delegate
    (ValueTuple<GamePlayer, GamePlayer, int> message, bool _)//MyPlayer Target RoleId PlaySound
    {
        if (AmongUsLLImpl.LocalPlayer.Data.IsDead)
        {
            AmongUsLLImpl.HudManagerBridge.Chat.AddChat(message.Item1.VanillaPlayer, Language.Translate("role.guesser.guessText").Replace("%NAME%", message.Item2.Name).Replace("%ROLE%", Nebula.Roles.Roles.GetRole(message.Item3).DisplayColoredName));
        }
    });
    public static RemoteProcess<ValueTuple<GamePlayer, GamePlayer, int>> DoomsayerGuessMessageRPC = new RemoteProcess<ValueTuple<GamePlayer, GamePlayer, int>>("DoomsayerGuessMessage", delegate
(ValueTuple<GamePlayer, GamePlayer, int> message, bool _)//MyPlayer Target RoleId PlaySound
    {
        if (AmongUsLLImpl.LocalPlayer.Data.IsDead)
        {
            AmongUsLLImpl.HudManagerBridge.Chat.AddChat(message.Item1.VanillaPlayer, Language.Translate("role.doomsayer.guessText").Replace("%NAME%", message.Item2.Name).Replace("%ROLE%", Nebula.Roles.Roles.GetRole(message.Item3).DisplayColoredName));
        }
    });
    public static bool CheckGuess(GamePlayer target, DefinedRole r)
    {
        if (target == null)
        {
            return false;
        }
        if (target.Role.CheckGuessAbility(r))
        {
            return true;
        }
        return false;
    }
    public static Dictionary<byte, int> playerGuess = new Dictionary<byte, int>();
    static public void OnMeetingStart(int leftGuess, Action guessDecrementer, bool isDoubleGuess = false, Func<bool>? guessIf = null, bool isLI = false, int safeGuessnum = 0,Action safeGuessAction=null)
    {
        bool awareOfUsurpation = false;
        bool safeGuessFail = false;
        playerGuess = new Dictionary<byte, int>();
        int leftGuessPerMeeting = isLI?99:Nebula.Roles.Complex.Guesser.NumOfGuessPerMeetingOption;
        PDebug.Log("RegisterGuesserUI");
        NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>()?.RegisterMeetingAction(new(isDoubleGuess ? DoubleGuessIcon : Icons.AsLoader(0),
            state =>
            {
                var p = state.MyPlayer;
                try
                {
                    PDebug.Log("StartGenGuessUI");
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    {
                        if (AmongUsLLImpl.LocalPlayer.Data.IsDead) return;
                        if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;
                        if (!MeetingHudExtension.CanUseAbilityFor(p, true))
                        {
                            return;
                        }
                        if (playerGuess.TryGetValue(p.PlayerId, out int roleid))
                        {
                            if (CheckGuess(p,Nebula.Roles.Roles.GetRole(roleid)))
                            {
                                if (!p.IsDead)
                                {
                                    GamePlayer.LocalPlayer.MurderPlayer(p!, PlayerState.Guessed, EventDetail.Guess, KillParameter.MeetingKill, KillCondition.BothAlive, (result) =>
                                    {
                                        GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, roleid));
                                    });
                                    guessDecrementer();
                                    leftGuess--;
                                    leftGuessPerMeeting--;
                                    if (GamePlayer.LocalPlayer.TryGetAbility<SpeechEater.Ability>(out var ability))
                                    {
                                        if (ability.SelectPlayer != null && ability.SelectPlayer.PlayerId == p.PlayerId)
                                        {
                                            new StaticAchievementToken("SpeechEater.challenge1");
                                        }
                                    }
                                    return;
                                }
                            }
                            else if (isDoubleGuess&&GamePlayer.LocalPlayer.TryGetAbility<SpeechEater.Ability>(out var ability))
                            {
                                if (ability == null)
                                {
                                    PDebug.Log("No Found SpeechEater Ability");
                                    return;
                                }
                                if (!ability.GuessNoDead)
                                {
                                    GamePlayer.LocalPlayer.MurderPlayer(GamePlayer.LocalPlayer, PlayerState.Misguessed, EventDetail.Missed, KillParameter.WithOverlay | KillParameter.WithAssigningGhostRole,KillCondition.BothAlive, (result) =>
                                    {
                                        GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, roleid));
                                    });
                                    RpcShareExtraInfo.Invoke((GamePlayer.LocalPlayer, p!, roleid));
                                    new StaticAchievementToken("SpeechEater.another1");
                                    guessDecrementer();
                                    leftGuess--;
                                    leftGuessPerMeeting--;
                                    return;
                                }
                                AmongUsUtil.PlayCustomFlash(Virial.Color.Red, 0.4f, 1f, 0.75f);
                                GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, roleid));
                                ability.GuessNoDead = false;
                                guessDecrementer();
                                leftGuess--;
                                leftGuessPerMeeting--;
                                return;
                            }
                            else if (safeGuessnum>0)
                            {
                                AmongUsUtil.PlayCustomFlash(Virial.Color.Red, 0.4f, 1f, 0.75f);
                                GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, roleid));
                                safeGuessFail = true;
                                safeGuessnum--;
                                safeGuessAction();
                                guessDecrementer();
                            }
                            else
                            {
                                GamePlayer.LocalPlayer.MurderPlayer(GamePlayer.LocalPlayer, PlayerState.Misguessed, EventDetail.Missed, KillParameter.WithOverlay | KillParameter.WithAssigningGhostRole, KillCondition.BothAlive, (result) =>
                                {
                                    GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, roleid));
                                });
                                RpcShareExtraInfo.Invoke((GamePlayer.LocalPlayer, p!, roleid));
                                guessDecrementer();
                                leftGuess--;
                                leftGuessPerMeeting--;
                                return;
                            }
                            PDebug.Log("TryGuessed");
                        }
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
                LastGuesserWindow = OpenGuessWindow(leftGuessPerMeeting, leftGuess,safeGuessnum, (r) =>
                {
                    try
                    {
                        if (AmongUsLLImpl.LocalPlayer.Data.IsDead) return;
                        if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;
                        if (!MeetingHudExtension.CanUseAbilityFor(p, true))
                        {
                            return;
                        }

                        PDebug.Log("CheckGuessCorrect");
                        if (guessIf?.Invoke() ?? true)
                        {
                            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))&&r is DefinedRole)
                            {
                                PDebug.Log("addTryGuessed");
                                if (!playerGuess.TryGetValue(p.PlayerId, out int roleid))
                                {
                                    playerGuess.Add(p.PlayerId, r.Id);
                                }
                                goto il_2;
                            }
                            if (CheckGuess(p,r))
                            {
                                if (!p.IsDead)
                                {
                                    try
                                    {
                                        PDebug.Log("GuessCorrect");
                                        GamePlayer.LocalPlayer.MurderPlayer(p!, PlayerState.Guessed, EventDetail.Guess, KillParameter.WithOverlay|KillParameter.WithAssigningGhostRole, KillCondition.BothAlive, (result) =>
                                        {
                                            GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, r.Id));
                                        });
                                        if (GamePlayer.LocalPlayer.TryGetAbility<SpeechEater.Ability>(out var ability))
                                        {
                                            if (ability.SelectPlayer != null && ability.SelectPlayer.PlayerId == p.PlayerId)
                                            {
                                                new StaticAchievementToken("SpeechEater.challenge1");
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        PDebug.Log(e);
                                    }
                                }
                            }
                            else if (isDoubleGuess&&GamePlayer.LocalPlayer.TryGetAbility<SpeechEater.Ability>(out var ability))
                            {
                                if (ability == null)
                                {
                                    PDebug.Log("No Found SpeechEater Ability");
                                    return;
                                }
                                if (!ability.GuessNoDead)
                                {
                                    PDebug.Log("DoubleGuessWrong2");
                                    GamePlayer.LocalPlayer.MurderPlayer(GamePlayer.LocalPlayer, PlayerState.Misguessed, EventDetail.Missed, KillParameter.WithOverlay|KillParameter.WithAssigningGhostRole, KillCondition.BothAlive, (result) =>
                                    {
                                        GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, r.Id));
                                    });
                                    RpcShareExtraInfo.Invoke((GamePlayer.LocalPlayer, p!, r.Id));
                                    new StaticAchievementToken("SpeechEater.another1");
                                    goto il_1;
                                }
                                PDebug.Log("DoubleGuessWrong1");
                                AmongUsUtil.PlayCustomFlash(Virial.Color.Red, 0.4f, 1f, 0.75f);
                                GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, r.Id));
                                ability.GuessNoDead = false;
                            }
                            else if (safeGuessnum>0)
                            {
                                AmongUsUtil.PlayCustomFlash(Virial.Color.Red, 0.4f, 1f, 0.75f);
                                GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, r.Id));
                                safeGuessFail = true;
                                safeGuessnum--;
                                safeGuessAction();
                            }
                            else
                            {
                                PDebug.Log("GuessWrong");
                                GamePlayer.LocalPlayer.MurderPlayer(GamePlayer.LocalPlayer, PlayerState.Misguessed, EventDetail.Missed, KillParameter.WithOverlay | KillParameter.WithAssigningGhostRole, KillCondition.BothAlive, (result) =>
                                {
                                    GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, r.Id));
                                });
                                RpcShareExtraInfo.Invoke((GamePlayer.LocalPlayer, p!, r.Id));
                            }
                        }
                        else
                        {
                            NebulaAsset.PlaySE(NebulaAudioClip.ButtonBreaking, false, 1f, 1f);
                            awareOfUsurpation = true;
                        }
                    il_1:;
                        PDebug.Log("GuessEnd");
                        guessDecrementer.Invoke();
                        leftGuess--;
                        leftGuessPerMeeting--;
                    il_2:;
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
            p => !awareOfUsurpation&&!safeGuessFail&&!p.MyPlayer.IsDead && !p.MyPlayer.AmOwner && leftGuess > 0 && leftGuessPerMeeting > 0 && !AmongUsLLImpl.LocalPlayer.Data.IsDead && GameOperatorManager.Instance!.Run(new PlayerCanGuessPlayerLocalEvent(NebulaAPI.CurrentGame!.LocalPlayer, p.MyPlayer, true)).CanGuess
            ));

        /*
        List<GameObject> guessIcons = new();

        foreach (var playerVoteArea in MeetingHud.Instance.playerStates)
        {
            if (playerVoteArea.AmDead || playerVoteArea.TargetPlayerId == AmongUsLLImpl.LocalPlayer.PlayerId) continue;

            GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, playerVoteArea.transform);
            guessIcons.Add(targetBox);
            targetBox.name = "ShootButton";
            targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = targetSprite.GetSprite();
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();


            var player = NebulaGameManager.Instance?.GetModPlayerInfo(playerVoteArea.TargetPlayerId);
            button.OnClick.AddListener(() =>
            {
                if (AmongUsLLImpl.LocalPlayer.Data.IsDead) return;
                if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;

                LastGuesserWindow = OpenGuessWindow(leftGuessPerMeeting, leftGuess, (r) =>
                {
                    if (AmongUsLLImpl.LocalPlayer.Data.IsDead) return;
                    if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;

                    if (player?.Role.Role == r)
                        AmongUsLLImpl.LocalPlayer.ModMeetingKill(player!.MyControl, true, PlayerState.Guessed, EventDetail.Guess);
                    else
                        AmongUsLLImpl.LocalPlayer.ModMeetingKill(AmongUsLLImpl.LocalPlayer, true, PlayerState.Misguessed, EventDetail.Missed);

                    //のこり推察数を減らす
                    guessDecrementer.Invoke();
                    leftGuess--;
                    leftGuessPerMeeting--;

                    if (leftGuess <= 0 || leftGuessPerMeeting <= 0) foreach (var obj in guessIcons) GameObject.Destroy(obj);
                    

                    if (LastGuesserWindow) LastGuesserWindow.CloseScreen();
                    LastGuesserWindow = null!;
                });
            });
        }
        */
    }
    static bool DGuessWrong;
    static RemoteProcess<(GamePlayer,GamePlayer)> KillShowAnimRpc = new RemoteProcess<(GamePlayer, GamePlayer)>("KillShowAnimRpc", delegate ((GamePlayer, GamePlayer)
        message, bool _)
    {
        if (!message.Item1.AmOwner&&!message.Item2.AmOwner)
        {
            AmongUsLLImpl.HudManagerInstance.KillOverlay.ShowKillAnimation(message.Item2.VanillaPlayer.Data, message.Item2.VanillaPlayer.Data);
        }
    });
    public static List<GamePlayer> doomsayerGuessKillPlayers = new List<GamePlayer>();
    static GamePlayer lastguesswrong;
    static public void DoomsayerOnMeetingStart(Action guessCorrectAction, bool cankillPlayer, bool showanim = true, bool guessfailSuicide = false, bool guessfailCannotGuess = true, int guessPerMeeting = 99)
    {
        DGuessWrong = false;
        int leftGuessPerMeeting =guessPerMeeting;
        playerGuess = new Dictionary<byte, int>();
        NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>()?.RegisterMeetingAction(new(Icons.AsLoader(0),
            state =>
            {
                var p = state.MyPlayer;
                try
                {
                    PDebug.Log("StartGenDGuessUI");
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    {
                        if (AmongUsLLImpl.LocalPlayer.Data.IsDead) return;
                        if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;
                        if (!MeetingHudExtension.CanUseAbilityFor(p, true))
                        {
                            return;
                        }
                        if (playerGuess.TryGetValue(p.PlayerId, out int roleid))
                        {
                            leftGuessPerMeeting--;
                            if (CheckGuess(p,Nebula.Roles.Roles.GetRole(roleid)))
                            {
                                if (!p.IsDead)
                                {
                                    doomsayerGuessKillPlayers.Add(p);
                                    if (doomsayerGuessKillPlayers.Count((GamePlayer player) => player.IsImpostor) >= 3)
                                    {
                                        new StaticAchievementToken("doomsayer.challenge1");
                                    }
                                    guessCorrectAction();
                                    if (cankillPlayer)
                                    {
                                        GamePlayer.LocalPlayer.MurderPlayer(p!, PlayerState.Guessed, EventDetail.Guess, KillParameter.WithOverlay | KillParameter.WithAssigningGhostRole, KillCondition.BothAlive, null);
                                    }
                                    if (showanim)
                                    {
                                        KillShowAnimRpc.Invoke((GamePlayer.LocalPlayer, p));
                                    }
                                    DoomsayerGuessMessageRPC.Invoke(new ValueTuple<GamePlayer, GamePlayer, int>(GamePlayer.LocalPlayer, p, roleid));
                                    return;
                                }
                            }
                            else
                            {
                                if (lastguesswrong != null && lastguesswrong == p)
                                {
                                    new StaticAchievementToken("doomsayer.another1");
                                }
                                if (guessfailSuicide)
                                {
                                    GamePlayer.LocalPlayer.MurderPlayer(GamePlayer.LocalPlayer, PlayerState.Misguessed, EventDetail.Missed, KillParameter.WithOverlay | KillParameter.WithAssigningGhostRole, KillCondition.BothAlive, (result) =>
                                    {
                                        DoomsayerGuessMessageRPC.Invoke(new ValueTuple<GamePlayer, GamePlayer, int>(GamePlayer.LocalPlayer, p, roleid));
                                    });
                                    RpcShareExtraInfo.Invoke((GamePlayer.LocalPlayer, p!, roleid));
                                    return;
                                }
                                DoomsayerGuessMessageRPC.Invoke(new ValueTuple<GamePlayer, GamePlayer, int>(GamePlayer.LocalPlayer, p, roleid));
                                AmongUsUtil.PlayCustomFlash(Virial.Color.Red, 0.4f, 1f, 0.75f);
                                lastguesswrong = p;
                                DGuessWrong = true;
                                return;
                            }
                            PDebug.Log("DTryGuessed");
                        }
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
                LastGuesserWindow = DoomsayerOpenGuessWindow((r) =>
                {
                    try
                    {
                        if (AmongUsLLImpl.LocalPlayer.Data.IsDead) return;
                        if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;
                        if (!MeetingHudExtension.CanUseAbilityFor(p, true))
                        {
                            return;
                        }
                        PDebug.Log("CheckDGuessCorrect");
                        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        {
                            PDebug.Log("DaddTryGuessed");
                            if (!playerGuess.TryGetValue(p.PlayerId, out int roleid))
                            {
                                playerGuess.Add(p.PlayerId, r.Id);
                            }
                            if (LastGuesserWindow) LastGuesserWindow.CloseScreen();
                            LastGuesserWindow = null!;
                            return;
                        }
                        if (CheckGuess(p,r))
                        {
                            if (!p.IsDead)
                            {
                                doomsayerGuessKillPlayers.Add(p);
                                if (doomsayerGuessKillPlayers.Count((GamePlayer player) => player.IsImpostor) >= 3)
                                {
                                    new StaticAchievementToken("doomsayer.challenge1");
                                }
                                guessCorrectAction();
                                if (cankillPlayer)
                                {
                                      GamePlayer.LocalPlayer.MurderPlayer(p!, PlayerState.Guessed, EventDetail.Guess, KillParameter.WithOverlay|KillParameter.WithAssigningGhostRole, KillCondition.BothAlive, null);
                                }
                                if (showanim)
                                {
                                    KillShowAnimRpc.Invoke((GamePlayer.LocalPlayer, p));
                                }
                                DoomsayerGuessMessageRPC.Invoke(new ValueTuple<GamePlayer, GamePlayer, int>(GamePlayer.LocalPlayer, p, r.Id));
                            }
                        }
                        else
                        {
                            if (lastguesswrong!=null&&lastguesswrong==p)
                            {
                                new StaticAchievementToken("doomsayer.another1");
                            }
                            if (guessfailSuicide)
                            {
                                GamePlayer.LocalPlayer.MurderPlayer(GamePlayer.LocalPlayer, PlayerState.Misguessed, EventDetail.Missed, KillParameter.WithOverlay | KillParameter.WithAssigningGhostRole, KillCondition.BothAlive, (result) =>
                                {
                                    DoomsayerGuessMessageRPC.Invoke(new ValueTuple<GamePlayer, GamePlayer, int>(GamePlayer.LocalPlayer, p, r.Id));
                                });
                                RpcShareExtraInfo.Invoke((GamePlayer.LocalPlayer, p!, r.Id));
                                leftGuessPerMeeting--;
                                return;
                            }
                            DoomsayerGuessMessageRPC.Invoke(new ValueTuple<GamePlayer, GamePlayer, int>(GamePlayer.LocalPlayer, p, r.Id));
                            AmongUsUtil.PlayCustomFlash(Virial.Color.Red ,0.4f, 1f, 0.75f);
                            DGuessWrong = true;
                            lastguesswrong = p;
                        }
                        leftGuessPerMeeting--;
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
            p => !doomsayerGuessKillPlayers.Contains(p.MyPlayer) &&leftGuessPerMeeting>0&&!p.MyPlayer.IsDead && !p.MyPlayer.AmOwner && (!guessfailCannotGuess||!DGuessWrong) && !AmongUsLLImpl.LocalPlayer.Data.IsDead && GameOperatorManager.Instance!.Run(new PlayerCanGuessPlayerLocalEvent(NebulaAPI.CurrentGame!.LocalPlayer, p.MyPlayer, true)).CanGuess
            ));
    }

    static public void OnDead()
    {
        if (LastGuesserWindow) LastGuesserWindow.CloseScreen();
        LastGuesserWindow = null!;
    }
    public static void OnGameEnd(Player myInfo)
    {
        NebulaGameManager instance = NebulaGameManager.Instance;
        int num = ((instance != null) ? instance.AllPlayerInfo.Count((Player p) => p.PlayerState == PlayerState.Guessed && p.MyKiller == myInfo) : 0);
        if (num >= 1)
        {
            new StaticAchievementToken("guesser.common1");
        }
        if (num >= 3)
        {
            new StaticAchievementToken("guesser.challenge");
        }
    }
}