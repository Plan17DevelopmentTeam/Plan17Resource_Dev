using Plana.Core;
using Vector2 = UnityEngine.Vector2;

namespace Plana.Roles.Impostor;

public class Prismkiller : DefinedSingleAbilityRoleTemplate<Prismkiller.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, IAssignableDocument
{
    private const float alpha = 0.5f;
    private const int usesPerRound = 2;

    private Prismkiller() : base("prismkiller", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [PrismCooldown, PullCooldown, VisibleNextRound, PersistentShards])
    {
        ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImages/SpecialPrismkiller.png")!.AsImage(115f)!;
    }
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Prismkiller.PNG")!.AsImage();
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CannotLoad;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player);
    public static readonly FloatConfiguration PrismCooldown = NebulaAPI.Configurations.Configuration("options.role.prismkiller.prismCooldown", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    public static readonly FloatConfiguration PullCooldown = NebulaAPI.Configurations.Configuration("options.role.prismkiller.pullCooldown", (5f, 40f, 5f), 25f, FloatConfigurationDecorator.Second);
    public static readonly BoolConfiguration VisibleNextRound = NebulaAPI.Configurations.Configuration("options.role.prismkiller.visibleNextRound", true);
    public static readonly BoolConfiguration PersistentShards = NebulaAPI.Configurations.Configuration("options.role.prismkiller.persistentShards", true);
    static public Prismkiller MyRole = new Prismkiller();
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(Ability.PlaceIcon, "role.prismkiller.ability.prism");
        yield return new(Ability.PullIcon, "role.prismkiller.ability.pull");
    }

    [NebulaRPC]
    public static void RpcPull(byte targetId, float x, float y)
    {
        GamePlayer? player = GamePlayer.GetPlayer(targetId);
        if (player == null) return;
        player.VanillaPlayer.NetTransform.SnapTo(new Vector2(x, y));
    }

    public class Ability : AbstractPlayerAbility, IPlayerAbility
    {
        public static Image PlaceIcon = NebulaAPI.AddonAsset.GetResource(Prism.special ? "SpecialPrismkillerButton.PNG" : "PrismButton.png")!.AsImage(115f)!;
        public static Image PullIcon = NebulaAPI.AddonAsset.GetResource(Prism.special ? "SpecialTradeButton.PNG" : "WarpButton_impostorVer.png")!.AsImage(115f)!;
        private const float extendLen = 100f;
        int leftUses = usesPerRound;
        int pending;
        List<int> mirrorIds = new();
        Vector2 mirrorA, mirrorB;
        bool hasMirror;
        ModAbilityButton? placeButton;
        ModAbilityButton? pullButton;
        ObjectTracker<GamePlayer>? tracker;
        Dictionary<byte, PlayerDisplay> mirrors = new();
        List<GameObject> shown = new();

        public Ability(GamePlayer player) : base(player)
        {
            if (!AmOwner) return;
            placeButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.Ability, PrismCooldown, "prism", PlaceIcon, _ => MyPlayer.CanMove, _ => !MyPlayer.IsDead && leftUses > 0);
            placeButton.SetLabelType(ModAbilityButton.LabelType.Impostor).ShowUsesIcon(0, leftUses.ToString());
            placeButton.OnClick = button =>
            {
                PlacePrism();
                button.UpdateUsesIcon(leftUses.ToString());
                button.StartCoolDown();
            };
            tracker = new ObjectTrackerUnityImpl<GamePlayer, PlayerControl>(
                MyPlayer.VanillaPlayer,
                AmongUsLLImpl.Instance.VanillaKillDistance,
                () => PlayerControl.AllPlayerControls.GetFastEnumerator().Where(p => p),
                p => p.PlayerId != MyPlayer.PlayerId && !p.IsDead && !p.Logic.InVent && Phantom(p) != null,
                null,
                p => p.GetModInfo(),
                p => { var ghost = Phantom(p.GetModInfo()); return ghost == null ? Array.Empty<Vector2>() : new[] { ghost.Value }; },
                p => PhantomRenderer(p.GetModInfo()),
                Palette.ImpostorRed,
                true,
                true).Register(this) as ObjectTracker<GamePlayer>;
            pullButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.SecondaryAbility, PullCooldown, "prismkiller.pull", PullIcon,
                _ => tracker!.CurrentTarget != null && MyPlayer.CanMove, _ => !MyPlayer.IsDead && hasMirror);
            pullButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
            pullButton.OnClick = button =>
            {
                GamePlayer? target = tracker!.CurrentTarget;
                if (target == null) return;
                Vector2 from = target.TruePosition.ToUnityVector();
                Vector2? ghost = Reflect(from);
                if (ghost == null) return;
                RpcPull(target.PlayerId, ghost.Value.x, ghost.Value.y);
                if (PersistentShards) GlassShard.Spawn(from);

                button.StartCoolDown();
            };
            GameOperatorManager.Instance?.RegisterOnReleased(() =>
            {
                foreach (var mirror in mirrors.Values) UnityEngine.Object.Destroy(mirror.gameObject);
            }, this);
        }
        [Local]
        void OnUpdate(GameUpdateEvent ev)
        {
            if (!hasMirror || MyPlayer.IsDead || MeetingHud.Instance != null)
            {
                mirrors.Values.Do(m => m.gameObject.SetActive(false));
                return;
            }
            shown.Clear();
            foreach (var p in GamePlayer.AllPlayers)
            {
                if (p.IsDead || p.Logic.InVent) continue;
                if (p.IsInvisible && p.Role.Role == Plana.Roles.Impostor.NinjaFix.MyRole) continue;
                var mirrored = Reflect(p.TruePosition.ToUnityVector());
                if (mirrored != null) shown.Add(ShowPhantom(p, mirrored.Value));
            }
            foreach (var mirror in mirrors.Values) if (!shown.Contains(mirror.gameObject)) mirror.gameObject.SetActive(false);
            foreach (var mirror in shown)
                foreach (var renderer in mirror.GetComponentsInChildren<SpriteRenderer>(true)) renderer.color = renderer.color with { a = alpha };
        }
        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            leftUses = usesPerRound;
            placeButton?.UpdateUsesIcon(leftUses.ToString());
            ClearPending();
        }
        [Local]
        void OnOutfitChanged(PlayerOutfitChangeEvent ev)
        {
            if (!mirrors.TryGetValue(ev.Player.PlayerId, out var mirror)) return;
            mirror.UpdateFromPlayerOutfit(ev.Player.VanillaPlayer, false, false);
            mirror.Cosmetics.nameText.text = ev.Player.PlayerName;
        }
        Vector2? Reflect(Vector2 worldPos)
        {
            Vector2 lensVec = mirrorB - mirrorA;
            Vector2 lensDir = lensVec.normalized;
            float along = Vector2.Dot(worldPos - mirrorA, lensDir);
            if (along < -extendLen || along > lensVec.magnitude + extendLen) return null;
            Vector2 foot = mirrorA + lensDir * along;
            Vector2 mirrored = 2f * foot - worldPos;
            if (Vector2.Distance(mirrored, worldPos) < 0.5f) return null;
            return mirrored;
        }
        Vector2? Phantom(GamePlayer player) => hasMirror ? Reflect(player.TruePosition.ToUnityVector()) : null;
        SpriteRenderer PhantomRenderer(GamePlayer player)
        {
            if (mirrors.TryGetValue(player.PlayerId, out var mirror)) return mirror.Cosmetics.currentBodySprite.BodySprite;
            return player.VanillaPlayer.cosmetics.currentBodySprite.BodySprite;
        }
        void PlacePrism()
        {
            Vector2 pos = MyPlayer.TruePosition.ToUnityVector();
            var enemy = PrismSystem.FindCandidate(MyPlayer, true);
            if (enemy != null)
            {
                MakeCrossMirror(enemy.Value, pos);
                return;
            }
            if (pending != 0)
            {
                Prism? first = NebulaSyncObject.GetObject<Prism>(pending);
                if (first != null)
                {
                    MakeMirror(first, pos);
                    leftUses--;
                    return;
                }
                pending = 0;
            }
            Prism placed = Prism.Place(pos, 0, true);
            pending = placed.ObjectId;
            PrismSystem.SharePrism(placed.ObjectId, MyPlayer.PlayerId, true, pos.x, pos.y);
            leftUses--;
        }
        void MakeMirror(Prism first, Vector2 pos)
        {
            RemoveMirror();
            Vector2 aPos = first.Position.ToUnityVector();
            Prism second = Prism.Place(pos, first.ObjectId, false);
            pending = 0;
            mirrorIds.Add(first.ObjectId);
            mirrorIds.Add(second.ObjectId);
            mirrorA = aPos;
            mirrorB = pos;
            hasMirror = true;
            if (VisibleNextRound)
            {
                first.ScheduleGlobal(0, aPos);
                second.ScheduleGlobal(first.ObjectId, pos);
            }
            PrismSystem.ConnectPrisms(first.ObjectId, second.ObjectId, MyPlayer.PlayerId, aPos.x, aPos.y, pos.x, pos.y);
        }
        void MakeCrossMirror((int id, byte owner, bool killer, Vector2 pos) enemy, Vector2 pos)
        {
            Vector2 enemyPos = enemy.pos;
            PrismSystem.DestroyMirrorOf(enemy.owner);
            RemoveMirror();
            PrismSystem.DropLocalPrism(enemy.id);
            PrismSystem.RemovePrism(enemy.id);
            Prism a = Prism.PlaceGlobal(enemyPos, 0);
            Prism b = Prism.PlaceGlobal(pos, a.ObjectId);
            mirrorIds.Add(a.ObjectId);
            mirrorIds.Add(b.ObjectId);
            mirrorA = enemyPos;
            mirrorB = pos;
            hasMirror = true;
            PrismSystem.ConnectMixedPrisms(a.ObjectId, b.ObjectId, enemy.owner, MyPlayer.PlayerId, enemy.killer, enemyPos.x, enemyPos.y, pos.x, pos.y, enemy.id, 0);
        }
        void ClearPending()
        {
            if (pending == 0) return;
            NebulaSyncObject.RpcDestroy(pending);
            PrismSystem.RemovePrism(pending);
            pending = 0;
        }
        public void RemoveMirror()
        {
            foreach (int id in mirrorIds)
            {
                NebulaSyncObject.RpcDestroy(id);
                PrismSystem.RemovePrism(id);
            }
            mirrorIds.Clear();
            hasMirror = false;
        }
        public void SetOwnPair(int aId, int bId, float ax, float ay, float bx, float by)
        {
            pending = 0;
            mirrorIds.Clear();
            mirrorIds.Add(aId);
            mirrorIds.Add(bId);
            mirrorA = new Vector2(ax, ay);
            mirrorB = new Vector2(bx, by);
            hasMirror = true;
        }
        public void SetMixedPair(int aId, int bId, float ax, float ay, float bx, float by, int consumedId)
        {
            if (pending == consumedId) pending = 0;
            mirrorIds.Clear();
            mirrorIds.Add(aId);
            mirrorIds.Add(bId);
            mirrorA = new Vector2(ax, ay);
            mirrorB = new Vector2(bx, by);
            hasMirror = true;
            leftUses = 0;
            placeButton?.UpdateUsesIcon("0");
        }
        GameObject ShowPhantom(GamePlayer player, Vector2 target)
        {
            if (!mirrors.TryGetValue(player.PlayerId, out var mirror))
            {
                mirror = VanillaAsset.GetPlayerDisplay();
                mirror.name = "PrismPhantom";
                UnityHelper.DoForAllChildren(mirror.gameObject, child => { child.tag = "Untagged"; child.layer = LayerExpansion.GetPlayersLayer(); }, true);
                foreach (var renderer in mirror.GetComponentsInChildren<SpriteRenderer>(true)) renderer.maskInteraction = SpriteMaskInteraction.None;
                mirror.UpdateFromPlayerOutfit(player.VanillaPlayer, false, false);
                mirror.Cosmetics.ToggleName(true);
                mirror.Cosmetics.nameText.transform.parent.localPosition = new UnityEngine.Vector3(0f, 1f, -0.5f);
                mirror.Cosmetics.nameText.text = player.PlayerName;
                mirror.Cosmetics.nameText.color = new UnityEngine.Color(1f, 1f, 1f, alpha);
                mirrors[player.PlayerId] = mirror;
            }
            mirror.gameObject.SetActive(true);
            Vector2 delta = target - (Vector2)mirror.transform.position;
            mirror.transform.position = target.AsVector3(target.y / 1000f);
            if (delta.sqrMagnitude > 0.0001f)
            {
                var flip = mirror.Cosmetics.FlipX;
                if (delta.x < -0.01f) mirror.Cosmetics.SetFlipX(true);
                else if (delta.x > 0.01f) mirror.Cosmetics.SetFlipX(false);
                if (!mirror.Animations.IsPlayingRunAnimation() || flip != mirror.Cosmetics.FlipX) mirror.Animations.PlayRunAnimation();
                if (!mirror.Cosmetics.IsSkinPlayingRunAnim()) mirror.Cosmetics.AnimateSkinRun();
            }
            else if (mirror.Animations.IsPlayingRunAnimation() || !mirror.Animations.IsPlayingSomeAnimation())
            {
                mirror.Animations.PlayIdleAnimation();
                mirror.Cosmetics.AnimateSkinIdle();
            }
            return mirror.gameObject;
        }
    }

    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    public class GlassShard : NebulaSyncStandardObject
    {
        public const string MyTag = "PrismShard";
        static Sprite? shardSprite;
        static GlassShard()
        {
            NebulaSyncObject.RegisterInstantiater(MyTag, (float[] args) => new GlassShard(new Vector2(args[0], args[1])));
        }
        public GlassShard(Vector2 pos) : base(pos, NebulaSyncStandardObject.ZOption.Back, true, GetShardSprite(), false)
        {
            MyRenderer.transform.localScale = Vector2.one * 0.55f;
        }
        public static void Spawn(Vector2 pos) => NebulaSyncObject.RpcInstantiate(MyTag, [pos.x, pos.y]);
        static Sprite GetShardSprite()
        {
            if (shardSprite != null) return shardSprite;
            string name = Prism.special ? "SpecialBrokenMirror.PNG" : "GlassShard.png";
            var res = NebulaAPI.AddonAsset.GetResource(name) ?? NebulaAPI.AddonAsset.GetResource("Prism.PNG");
            return shardSprite = res!.AsImage(115f)!.GetSprite();
        }
    }
}
