using Plana.Roles.Crewmate;
using Plana.Roles.Impostor;
using Virial.Runtime;
using IEnumerator = System.Collections.IEnumerator;
using Vector2 = UnityEngine.Vector2;

namespace Plana.Core;

[NebulaPreprocess(PreprocessPhase.PostRoles)]
public class Prism : NebulaSyncStandardObject
{
    static public readonly bool special = (DateTime.Now.Month == 10 && DateTime.Now.Day >= 20) || (DateTime.Now.Month == 11 && DateTime.Now.Day <= 20);
    static Image PrismImage = NebulaAPI.AddonAsset.GetResource(special ? "SpecialPrism.png" : "Prism.png")!.AsImage(115f)!;
    static MultiImage SpawnImage = NebulaAPI.AddonAsset.GetResource("PrismSpwanAnim.png")!.AsMultiImage(5, 2, 100f)!;
    static MultiImage SpawnChargeImage = NebulaAPI.AddonAsset.GetResource("PrismSpawnChargeAnim.png")!.AsMultiImage(5, 1, 100f)!;
    public const float extendLen = 100f;
    public const string MyTag = "Prism";
    PrismString? mirror;
    static Prism()
    {
        NebulaSyncObject.RegisterInstantiater(MyTag, (float[] args) => new Prism(new Virial.Compat.Vector2(args[0], args[1]), (int)args[2], args.Length > 3 && args[3] > 0f));
    }
    public static Prism Place(Vector2 pos, int linkId, bool ghost) => (Prism)NebulaSyncObject.LocalInstantiate(MyTag, [pos.x, pos.y, linkId, ghost ? 1f : 0f]).SyncObject!;
    public static Prism PlaceGlobal(Vector2 pos, int linkId) => (Prism)NebulaSyncObject.RpcInstantiate(MyTag, [pos.x, pos.y, linkId, 0f]).SyncObject!;

    public void ScheduleGlobal(int linkId, Vector2 pos)
    {
        NebulaGameManager.Instance!.Scheduler.Schedule(RPCScheduler.RPCTrigger.AfterMeeting,
            NebulaSyncObject.RpcInstantiateDef.GetInvoker((ObjectId, TagHash, new float[] { pos.x, pos.y, linkId, 0f }, true)));
    }

    public Prism(Virial.Compat.Vector2 pos, int linkId, bool ghost) : base(pos, NebulaSyncStandardObject.ZOption.Back, true, PrismImage.GetSprite(), ghost)
    {
        MyRenderer.transform.localScale = Vector2.one * 0.45f;
        MyTransform.SetWorldZ(-2.49f);
        NebulaManager.Instance.StartCoroutine(CoSpawnAnim(MyTransform).WrapToIl2Cpp());
        if (linkId == 0) return;
        Prism? other = NebulaSyncObject.GetObject<Prism>(linkId);
        if (other == null) return;
        other.Color = Virial.Color.White;
        if (other.argumentsCache is { Length: > 3 }) other.argumentsCache[3] = 0f;
        mirror = new PrismString(pos.ToUnityVector(), other.Position.ToUnityVector()).Register(this);
    }

    public override void OnReleased()
    {
        mirror?.Inactivate();
        base.OnReleased();
    }

    static IEnumerator CoSpawnAnim(Transform parent)
    {
        SpriteRenderer main = UnityHelper.SimpleAnimator(parent, new UnityEngine.Vector3(0f, 0f, -1.5f), 0.1f, i => SpawnImage.GetSprite(i % 5));
        SpriteRenderer sub = UnityHelper.SimpleAnimator(main.transform, new UnityEngine.Vector3(0f, 0f, 0.5f), 0.1f, i => SpawnImage.GetSprite(5 + i % 5));
        sub.material.shader = NebulaAsset.MultiplyShader;
        UnityHelper.SimpleAnimator(main.transform, UnityEngine.Vector3.zero, 0.1f, i => SpawnChargeImage.GetSprite(i % 5));
        float angle = 0f;
        float duration = 0.6f;
        while (duration > 0f)
        {
            angle -= 2f;
            main.transform.localEulerAngles = new UnityEngine.Vector3(0f, 0f, angle);
            yield return Effects.Wait(0.1f);
            duration -= 0.1f;
        }
        if (main) GameObject.Destroy(main.gameObject);
    }

    public class PrismString : IGameOperator
    {
        static readonly Image laserNodeImage = NebulaAPI.AddonAsset.GetResource("PrismNodeBack.png")!.AsImage(100f)!;
        static readonly Image laserNodeBackImage = NebulaAPI.AddonAsset.GetResource("PrismNodeBack.png")!.AsImage(100f)!;
        static readonly MultiImage laserImage = NebulaAPI.AddonAsset.GetResource("PrismLaserBack.png")!.AsMultiImage(1, 4, 100f)!;
        static readonly MultiImage laserBackImage = NebulaAPI.AddonAsset.GetResource("PrismLaserBack.png")!.AsMultiImage(1, 4, 100f)!;

        private SpriteRenderer node1Renderer;
        private SpriteRenderer node1BackRenderer;
        private SpriteRenderer node2Renderer;
        private SpriteRenderer node2BackRenderer;
        private SpriteRenderer edgeFrontRenderer;
        private SpriteRenderer edgeBackRenderer;
        private Vector2 pos1, pos2;
        private bool initialized = false;

        public PrismString(Vector2 a, Vector2 b)
        {
            pos1 = a;
            pos2 = b;
        }

        void OnFirstUpdate()
        {
            node1Renderer = UnityHelper.CreateSpriteRenderer("Node1", null, UnityEngine.Vector3.zero);
            node1BackRenderer = UnityHelper.CreateSpriteRenderer("Back", node1Renderer.transform, new UnityEngine.Vector3(0f, 0f, 0.01f));
            node1Renderer.sprite = laserNodeImage.GetSprite();
            node1BackRenderer.sprite = laserNodeBackImage.GetSprite();
            node1BackRenderer.material.shader = NebulaAsset.MultiplyShader;

            node2Renderer = UnityHelper.CreateSpriteRenderer("Node2", null, UnityEngine.Vector3.zero);
            node2BackRenderer = UnityHelper.CreateSpriteRenderer("Back", node2Renderer.transform, new UnityEngine.Vector3(0f, 0f, 0.01f));
            node2Renderer.sprite = laserNodeImage.GetSprite();
            node2BackRenderer.sprite = laserNodeBackImage.GetSprite();
            node2BackRenderer.material.shader = NebulaAsset.MultiplyShader;

            edgeFrontRenderer = UnityHelper.SimpleAnimator(null, new UnityEngine.Vector3(0f, 0f, -2.48f), 0.1f, i => laserImage.GetSprite(i % 4));
            edgeBackRenderer = UnityHelper.SimpleAnimator(edgeFrontRenderer.transform, new UnityEngine.Vector3(0f, 0f, 0.01f), 0.1f, i => laserBackImage.GetSprite(i % 4));
            edgeBackRenderer.material.shader = NebulaAsset.MultiplyShader;
            edgeFrontRenderer.drawMode = SpriteDrawMode.Tiled;
            edgeBackRenderer.drawMode = SpriteDrawMode.Tiled;

            initialized = true;
        }

        void OnHudUpdate(GameHudUpdateEvent ev)
        {
            if (!initialized) OnFirstUpdate();

            edgeFrontRenderer.transform.localScale = new UnityEngine.Vector3(1f, 0.2f + System.Random.Shared.NextSingle() * 0.9f, 1f);

            node1Renderer.transform.position = pos1.AsVector3(-2.5f);
            node1Renderer.transform.localScale = UnityEngine.Vector3.one * (System.Random.Shared.NextSingle() * 0.2f + 0.35f);
            node1Renderer.transform.localEulerAngles = new UnityEngine.Vector3(0f, 0f, System.Random.Shared.NextSingle() * 360f);
            node2Renderer.transform.position = pos2.AsVector3(-2.5f);
            node2Renderer.transform.localScale = UnityEngine.Vector3.one * (System.Random.Shared.NextSingle() * 0.2f + 0.35f);
            node2Renderer.transform.localEulerAngles = new UnityEngine.Vector3(0f, 0f, System.Random.Shared.NextSingle() * 360f);

            Vector2 posDiff = pos1 - pos2;
            UnityEngine.Vector3 center = AmongUsLLImpl.HudManagerBridge.MyTransform.position;
            if (Vector2.Distance(center, pos1) < Vector2.Distance(center, pos2)) posDiff = -posDiff;

            edgeFrontRenderer.transform.position = ((pos1 + pos2) * 0.5f).AsVector3(-2.48f);
            edgeFrontRenderer.transform.localEulerAngles = new UnityEngine.Vector3(0f, 0f, Mathn.Atan2(posDiff.y, posDiff.x).RadToDeg());
            edgeFrontRenderer.size = new Vector2(posDiff.magnitude + extendLen * 2f, 0.4f);
            edgeBackRenderer.size = edgeFrontRenderer.size;
        }

        public void Inactivate()
        {
            if (node1Renderer) GameObject.Destroy(node1Renderer.gameObject);
            if (node2Renderer) GameObject.Destroy(node2Renderer.gameObject);
            if (edgeFrontRenderer) GameObject.Destroy(edgeFrontRenderer.gameObject);
        }

        void IGameOperator.OnReleased() => Inactivate();
    }
}

[NebulaPreprocess(PreprocessPhase.PostRoles)]
public class PrismSystem : AbstractModule<Game>, IGameOperator
{
    static readonly Dictionary<int, (byte owner, bool killer, Vector2 pos, bool linked)> entries = new();

    static void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.DIManager.RegisterModule<Game>(() => new PrismSystem());
    }
    PrismSystem() => this.RegisterPermanently();

    void OnGameStart(GameStartEvent ev) => entries.Clear();

    [NebulaRPC]
    public static void SharePrism(int id, byte ownerId, bool isKiller, float x, float y) => entries[id] = (ownerId, isKiller, new Vector2(x, y), false);

    [NebulaRPC]
    public static void RemovePrism(int id)
    {
        entries.Remove(id);
    }

    [NebulaRPC]
    public static void DropLocalPrism(int id) => NebulaSyncObject.LocalDestroy(id);

    [NebulaRPC]
    public static void ConnectPrisms(int aId, int bId, byte ownerId, float ax, float ay, float bx, float by)
    {
        if (entries.TryGetValue(aId, out var a)) entries[aId] = (a.owner, a.killer, a.pos, true);
        if (entries.TryGetValue(bId, out var b)) entries[bId] = (b.owner, b.killer, b.pos, true);
        GamePlayer? player = GamePlayer.GetPlayer(ownerId);
        if (player == null) return;
        if (player.TryGetAbility<Prismologist.Ability>(out var scholar)) scholar.ApplyOwnPair(aId, bId, ax, ay, bx, by);
        else if (player.TryGetAbility<Prismkiller.Ability>(out var killer)) killer.SetOwnPair(aId, bId, ax, ay, bx, by);
    }

    [NebulaRPC]
    public static void ConnectMixedPrisms(int aId, int bId, byte aOwner, byte bOwner, bool aKiller, float ax, float ay, float bx, float by, int aOldId, int bOldId)
    {
        entries[aId] = (aOwner, aKiller, new Vector2(ax, ay), true);
        entries[bId] = (bOwner, !aKiller, new Vector2(bx, by), true);
        GamePlayer? a = GamePlayer.GetPlayer(aOwner);
        if (a != null)
        {
            if (a.TryGetAbility<Prismologist.Ability>(out var scholar)) scholar.ApplyMixedPair(aId, bId, ax, ay, bx, by, aOldId);
            else if (a.TryGetAbility<Prismkiller.Ability>(out var killer)) killer.SetMixedPair(aId, bId, ax, ay, bx, by, aOldId);
        }
        GamePlayer? b = GamePlayer.GetPlayer(bOwner);
        if (b != null)
        {
            if (b.TryGetAbility<Prismologist.Ability>(out var scholar)) scholar.ApplyMixedPair(aId, bId, ax, ay, bx, by, bOldId);
            else if (b.TryGetAbility<Prismkiller.Ability>(out var killer)) killer.SetMixedPair(aId, bId, ax, ay, bx, by, bOldId);
        }
    }

    public static void DestroyMirrorOf(byte playerId)
    {
        GamePlayer? player = GamePlayer.GetPlayer(playerId);
        if (player == null) return;
        if (player.TryGetAbility<Prismologist.Ability>(out var scholar)) scholar.DestroyMirror();
        else if (player.TryGetAbility<Prismkiller.Ability>(out var killer)) killer.RemoveMirror();
    }

    public static (int id, byte owner, bool killer, Vector2 pos)? FindCandidate(GamePlayer me, bool isKiller)
    {
        var candidates = entries.Where(e => !e.Value.linked && e.Value.owner != me.PlayerId && e.Value.killer != isKiller).ToArray();
        if (candidates.Length == 0) return null;
        var pick = candidates[System.Random.Shared.Next(candidates.Length)];
        return (pick.Key, pick.Value.owner, pick.Value.killer, pick.Value.pos);
    }
}
