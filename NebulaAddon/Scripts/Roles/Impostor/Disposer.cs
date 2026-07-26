using Iced.Intel;
using Nebula.Player;
using Plana.Core;
using Virial.Helpers;

namespace Plana.Roles.Impostor;
[NebulaRPCHolder]
public class Disposer : DefinedSingleAbilityRoleTemplate<Disposer.Ability>, HasCitation,DefinedRole, IAssignableDocument
{
    private Disposer() : base("disposer", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [SpawnDeadbodiesNum])
    {
    }
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.Allowed;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player,arguments.GetAsBool(0));
    public static void LoadPatch(Harmony harmony)
    {
        PDebug.Log("Disposer Patch");
        harmony.Patch(typeof(KillOverlay).GetMethod("ShowMeeting"), new HarmonyMethod(typeof(Disposer).GetMethod("ShowMeetingOutfitPatch")));
        PDebug.Log("Done");
    }
    public static void ShowMeetingOutfitPatch(ref NetworkedPlayerInfo.PlayerOutfit playerOutfit)
    {
        if (NebulaGameManager.Instance.AllPlayerInfo.Any(p=>p.TryGetAbility<Disposer.Ability>(out var ability)&&ability.killPlayer))
        {
            playerOutfit = NebulaGameManager.Instance.UnknownOutfit.outfit;
        }
    }
    public static readonly RemoteProcess<(GamePlayer,GamePlayer,UnityEngine.Vector2)> CoSpawnDeadBody = new("DisposerSpawnDeadBodyRPC", (m, _) =>
    {
        DeadBody deadBody = UnityEngine.Object.Instantiate<DeadBody>(GameManager.Instance.deadBodyPrefab[0]);
        deadBody.transform.position = m.Item3.AsVector3(m.Item3.y/1000f);
        deadBody.ParentId =m.Item1.PlayerId;
        deadBody.enabled = true;
        deadBody.bodyRenderers.Do(delegate (SpriteRenderer b)
        {
            PlayerMaterial.SetColors(NebulaGameManager.Instance.UnknownOutfit.outfit.ColorId, b);
        });
        PlayerMaterial.SetColors(NebulaGameManager.Instance.UnknownOutfit.outfit.ColorId,deadBody.bloodSplatter);
        GameOperatorManager.Instance?.Run(new DeadBodyInstantiateEvent(m.Item1, ModSingleton<DeadBodyManager>.Instance.RegisterDeadBody(deadBody, DeadBodyManager.GenerateId(m.Item2.PlayerId, NebulaGameManager.Instance.KillRequestHandler.availableId++, 0), m.Item1), m.Item2, m.Item1.PlayerState));
    });
    public static readonly IntegerConfiguration SpawnDeadbodiesNum = NebulaAPI.Configurations.Configuration("options.role.disposer.deadbodiesnum", (0, 20), 4);
    static public Disposer MyRole = new Disposer();
    bool IAssignableDocument.HasTips => true;
    public class Ability : AbstractPlayerUsurpableAbility,IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player,bool IsUsurped) : base(player,IsUsurped)
        {
        }
        public List<DeadBody> deadBodies = new();
        void OnDeadBodyGenerated(DeadBodyInstantiateEvent ev)
        {
            if (ev.Killer==MyPlayer&&ev.DeadBody.Player!=MyPlayer)
            {
                foreach (var deadbody in Helpers.AllDeadBodies())
                {
                    deadbody.bodyRenderers.Do(d => PlayerMaterial.SetColors(NebulaGameManager.Instance.UnknownOutfit.outfit.ColorId, d));
                    deadBodies.Add(deadbody);
                }
            }
        }
        void OnPreStartMeeting(MeetingPreStartEvent ev)
        {
            if (killPlayer)
            {
                foreach (var deadbody in deadBodies)
                {
                    if (deadbody==null)
                    {
                        continue;
                    }
                    var obj = deadbody.gameObject;
	                UnityEngine.Object.Destroy(deadbody);
                    GameOperatorManager.Instance?.Subscribe<MeetingEndEvent>(ev =>
                    {
                        //deadbody.gameObject.AddComponent<DeadBody>();
                        UnityEngine.Object.Destroy(obj);
                    }, new GameObjectLifespan(obj));
                }
            }
        }
        float refreshTime;
        public bool killPlayer;
        [EventPriority(-1)]
        void OnMeetingUpdate(MeetingStartEvent ev)
        {
            if (!MeetingHud.Instance.AsBoolFast())
            {
                return;
            }
            if (MeetingHud.Instance.playerStates == null) return;
            if (NebulaGameManager.Instance.CanSeeAllInfo) return;
            /*if (refreshTime>0)
            {
                refreshTime -= Time.deltaTime;
                return;
            }
            refreshTime = 1f;*/
            if (killPlayer)
            {
                MeetingHud.Instance.playerStates.Do(p =>
                {
                    var player = GamePlayer.GetPlayer(p.TargetPlayerId);
                    if (player?.IsDead??false)
                    {
                        p.Background.sprite = AmongUsLLImpl.ShipStatusInstance.CosmeticsCache.GetNameplate("11111111111111").Image;
                        p.PlayerIcon.UpdateFromPlayerOutfit(NebulaGameManager.Instance.UnknownOutfit.outfit, PlayerMaterial.MaskType.ComplexUI, player.IsDead, false);
                        p.PlayerIcon.ToggleName(false);
                        p.NameText.text = "???";
                        p.LevelNumberText.text = "???";
                        p.ColorBlindName.text = "???";
                        p.ColorBlindName.gameObject.SetActive(false);
                    }
                });
            }
        }
        [EventPriority(EventPriority.Low)]
        void DecorateName(PlayerDecorateNameEvent ev)
        {
            if (NebulaGameManager.Instance.CanSeeAllInfo) return;
            if (!MeetingHud.Instance.AsBoolFast()) return;
            if (killPlayer && ev.Player.IsDead)
            {
                ev.Name = "???";
            }
        }
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            deadBodies = new();
        }
        void OnTaskStart(TaskPhaseStartEvent ev)
        {
            killPlayer = false;
        }
        [OnlyMyPlayer]
        void OnKillGlobal(PlayerKillPlayerEvent ev)
        {
            if (ev.Dead==MyPlayer)
            {
                return;
            }
            killPlayer = true;
        }
        [Local, OnlyMyPlayer]
        void OnKillLocal(PlayerKillPlayerEvent ev)
        {
            if (ev.Dead == MyPlayer)
            {
                return;
            }
            if (MeetingHud.Instance.AsBoolFast())
            {
                return;
            }    
            //var list = new List<UnityEngine.Vector2>();
            /*HashSet<Vent> vents = UnityEngine.Object.FindObjectsOfType<Vent>().ToHashSet<Vent>();
            vents.Do(vent =>
            {
                UnityEngine.Vector2 size = vent.GetComponent<BoxCollider2D>().size;
                UnityEngine.Vector3 destination = vent.transform.position;
                destination.y += 0.3636f;
                list.Add(new UnityEngine.Vector2(destination.x, destination.y));
            });
            byte mapId = AmongUsLLImpl.Instance.MapId;
            NebulaPreSpawnLocation[] cand = NebulaPreSpawnLocation.Locations[(int)mapId];
            if (cand.Length == 0)
            {
                cand = NebulaPreSpawnLocation.Locations[(int)mapId].Where((NebulaPreSpawnLocation l) => l.VanillaIndex != null).ToArray<NebulaPreSpawnLocation>();
            }
            cand.Do(p => list.Add(p.Position!.Value));*/
            using (RPCRouter.CreateSection("SpawnDeadBody"))
            {
                for (int i = 0; i < SpawnDeadbodiesNum; i++)
                {
                    //var pos = list.Random();
                    var pos = ev.Dead.TruePosition.ToUnityVector();
                    pos.x += (UnityEngine.Random.Range(1, 201) - 100) / 100f;
                    pos.y += (UnityEngine.Random.Range(1, 201) - 100) / 100f;
                    CoSpawnDeadBody.Invoke((ev.Dead,MyPlayer, pos));
                }
            }
        }
    }
}
