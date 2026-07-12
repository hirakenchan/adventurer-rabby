using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager I { get; private set; }

    [SerializeField] private Transform treasurePickup; // 宝箱（treasure）
    private Vector2Int treasureCell = new Vector2Int(-999, -999);
    private bool hasTreasureCell = false;

    [Header("Scene Objects (Drop in Inspector)")]
    [SerializeField] private Transform goalMarker;       // 魔法陣（親）
    [SerializeField] private Transform[] keyPickups;     // クリスタルを入れる（STAGE10以降は3個使うので Size=3 推奨）

    [Header("Grid")]
    public Vector2Int startCell = new Vector2Int(0, 0);
    public Vector2Int goalCell = new Vector2Int(3, 3);  // ランダムONなら毎回上書きされます

    [Header("Door / Clear Conditions")]
    public bool requireKey = true;
    [SerializeField] private int requiredKeyCount = 2;  // 現在ステージのクリスタル必要個数（難易度設定で上書き）
    public int requiredSwitches = 0;

    [Header("Goal Lock")]
    public bool lockGoalTileUntilUnlocked = false; // ゴールは踏めるけど、IsUnlockedでしかクリアしないならfalse推奨

    [Header("Randomize")]
    public bool randomizeGoalAndKey = true;

    [Header("Random Missing Tiles")]
    [SerializeField] private bool randomRemoveTiles = true;
    [SerializeField, Range(0, 8)] private int removeTileCount = 3; // 現在ステージの穴数（難易度設定で上書き）
    [SerializeField] private bool avoidStartNeighbors = true; // スタート隣を欠けさせない（詰み率が下がる）

    [Header("Ensure Solvable")]
    [SerializeField] private bool ensureSolvable = true;
    [SerializeField, Range(1, 2000)] private int maxGenerateTries = 500;

    // 状態
    public int KeyCount { get; private set; }
    public int SwitchCount { get; private set; }
    public bool IsUnlocked => (!requireKey || KeyCount >= requiredKeyCount) && SwitchCount >= requiredSwitches;

    public event Action ConditionsChanged;

    private readonly Dictionary<Vector2Int, FragileTile> tileMap = new();         // 現在有効なタイル
    private readonly Dictionary<Vector2Int, FragileTile> allTileMap = new();      // 全タイル（復元用）
    private readonly List<Vector2Int> keyCells = new();                           // 今回のクリスタルセル
    private readonly List<Transform> activeKeyPickups = new();                    // 今回使っているクリスタルオブジェクト
    private readonly List<Vector2Int> removedCells = new();                       // 欠けセル

    private int boardW = 4;
    private int boardH = 4;

    // ステージごとの難易度設定
    private class StageDifficulty
    {
        public int holeCount;
        public int crystalCount;
        public float treasureSpawnRate;

        public StageDifficulty(int holeCount, int crystalCount, float treasureSpawnRate)
        {
            this.holeCount = holeCount;
            this.crystalCount = crystalCount;
            this.treasureSpawnRate = treasureSpawnRate;
        }
    }

    private StageDifficulty GetDifficultyForCurrentStage()
    {
        int stage = FloorManager.I != null ? FloorManager.I.GetCurrentFloor() : 1;

        if (stage >= 30)
        {
            // STAGE 30〜：穴4個 / クリスタル3個
            return new StageDifficulty(4, 3, 1.0f);
        }

        if (stage >= 20)
        {
            // STAGE 20〜29：穴3個 / クリスタル3個 / 宝箱少なめ
            return new StageDifficulty(3, 3, 0.5f);
        }

        if (stage >= 10)
        {
            // STAGE 10〜19：穴3個 / クリスタル3個
            return new StageDifficulty(3, 3, 1.0f);
        }

        // STAGE 1〜9：穴2個 / クリスタル2個
        return new StageDifficulty(2, 2, 1.0f);
    }

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        BuildTileMap();
    }

    private void Start()
    {
        if (randomizeGoalAndKey)
            GenerateLayoutWithRetry();
        else
            PlaceOnTile(goalMarker, goalCell);

        ConditionsChanged?.Invoke();
    }

    private void BuildTileMap()
    {
        tileMap.Clear();
        allTileMap.Clear();

        var tiles = FindObjectsOfType<FragileTile>().ToList();
        if (tiles.Count == 0)
        {
            Debug.LogError("FragileTile が見つかりません。タイルに FragileTile を付けているか確認してください。");
            return;
        }

        // 盤面の列・行を自動抽出
        float Q(float v) => Mathf.Round(v * 1000f) / 1000f;

        var xs = tiles.Select(t => Q(t.transform.position.x)).Distinct().OrderBy(v => v).ToList();
        var ys = tiles.Select(t => Q(t.transform.position.y)).Distinct().OrderBy(v => v).ToList();

        boardW = xs.Count;
        boardH = ys.Count;

        foreach (var t in tiles)
        {
            float xq = Q(t.transform.position.x);
            float yq = Q(t.transform.position.y);

            int cx = xs.IndexOf(xq);
            int cy = ys.IndexOf(yq);

            var cell = new Vector2Int(cx, cy);
            t.SetCell(cell);

            tileMap[cell] = t;
            allTileMap[cell] = t;

            // 念のため有効化（欠けリトライで何度もON/OFFするので）
            t.gameObject.SetActive(true);
        }

        if (xs.Count * ys.Count != tiles.Count)
        {
            Debug.LogWarning($"タイル配置が格子になっていないかもです（列{xs.Count} × 行{ys.Count} ≠ タイル{tiles.Count}）。");
        }
    }

    // ========= Public API =========
    public bool TryGetTile(Vector2Int cell, out FragileTile tile) => tileMap.TryGetValue(cell, out tile);

    public bool CanEnter(Vector2Int cell)
    {
        if (lockGoalTileUntilUnlocked && cell == goalCell && !IsUnlocked) return false;

        return tileMap.TryGetValue(cell, out var t) && !t.IsCollapsed && !t.IsCollapsing;
    }

    public Vector3 GetWorldPosition(Vector2Int cell)
    {
        if (tileMap.TryGetValue(cell, out var t)) return t.transform.position;
        return Vector3.zero;
    }

    public void OnPlayerMoved(Vector2Int from, Vector2Int to)
    {
        StepManager.I?.AddStep(); // 移動成功時に1増やす

        if (tileMap.TryGetValue(from, out var prev))
            prev.Collapse();

        if (tileMap.TryGetValue(to, out var next))
            next.OnPlayerArrived();

        if (to == goalCell && IsUnlocked)
        {
            GameClearManager.I?.Clear();
            return;
        }

        if (!CanStillClearFrom(to))
        {
            GameClearManager.I?.GameOver();
        }
    }

    public void ObtainKey()
    {
        KeyCount++;
        ConditionsChanged?.Invoke();
    }

    public void ActivateSwitch()
    {
        SwitchCount++;
        ConditionsChanged?.Invoke();
    }

    // ========= Generation =========
    private void GenerateLayoutWithRetry()
    {
        StageDifficulty difficulty = GetDifficultyForCurrentStage();

        Debug.Log(
            $"[BoardManager] STAGE={FloorManager.I?.GetCurrentFloor()} / holes={difficulty.holeCount} / crystals={difficulty.crystalCount} / treasureRate={difficulty.treasureSpawnRate}"
        );

        removeTileCount = difficulty.holeCount;

        int availableCrystalObjects = keyPickups != null ? keyPickups.Count(k => k != null) : 0;
        requiredKeyCount = Mathf.Min(difficulty.crystalCount, availableCrystalObjects);

        if (requireKey && availableCrystalObjects < difficulty.crystalCount)
        {
            Debug.LogWarning($"クリスタルオブジェクトが不足しています。必要数={difficulty.crystalCount}, 設定済み={availableCrystalObjects}。keyPickups の Size を増やして、3個目のクリスタルを入れてください。");
        }

        for (int attempt = 0; attempt < maxGenerateTries; attempt++)
        {
            RestoreAllTiles();
            HideAllPickups();

            // 1) ゴールを決める（start以外）
            var cells = tileMap.Keys.ToList();
            cells.Remove(startCell);
            if (cells.Count == 0) break;

            goalCell = cells[UnityEngine.Random.Range(0, cells.Count)];

            // 2) クリスタルを配置（start/goalと被らない）
            keyCells.Clear();
            activeKeyPickups.Clear();
            var keyCandidates = tileMap.Keys.Where(c => c != startCell && c != goalCell).ToList();

            int nKeys = requireKey ? requiredKeyCount : 0;
            nKeys = Mathf.Min(nKeys, keyCandidates.Count);

            int placedKeyCount = 0;
            if (keyPickups != null)
            {
                for (int i = 0; i < keyPickups.Length && placedKeyCount < nKeys; i++)
                {
                    if (keyPickups[i] == null) continue;

                    var c = keyCandidates[UnityEngine.Random.Range(0, keyCandidates.Count)];
                    keyCandidates.Remove(c);
                    keyCells.Add(c);

                    keyPickups[i].gameObject.SetActive(true);
                    PlaceOnTile(keyPickups[i], c);
                    activeKeyPickups.Add(keyPickups[i]);

                    placedKeyCount++;
                }
            }

            // 実際に置けた数を必要数にする（不足時の保険）
            requiredKeyCount = requireKey ? placedKeyCount : 0;

            // 3) 魔法陣を配置
            PlaceOnTile(goalMarker, goalCell);

            // 4) 宝箱を配置（start/goal/keysと被らない）
            hasTreasureCell = false;
            treasureCell = new Vector2Int(-999, -999);

            bool shouldSpawnTreasure = treasurePickup != null && UnityEngine.Random.value < difficulty.treasureSpawnRate;

            if (shouldSpawnTreasure)
            {
                var treasureCandidates = tileMap.Keys
                    .Where(c => c != startCell && c != goalCell && !keyCells.Contains(c))
                    .ToList();

                if (treasureCandidates.Count > 0)
                {
                    treasureCell = treasureCandidates[UnityEngine.Random.Range(0, treasureCandidates.Count)];
                    hasTreasureCell = true;

                    treasurePickup.gameObject.SetActive(true);
                    PlaceOnTileWorldOnly(treasurePickup, treasureCell);
                }
            }
            else if (treasurePickup != null)
            {
                treasurePickup.gameObject.SetActive(false);
            }

            // 5) 欠けタイル
            ApplyRandomMissingTiles();

            // 6) 解けるかチェック（解けなければやり直し）
            if (!ensureSolvable || IsSolvable())
            {
                // 成功：開始時状態リセット
                KeyCount = 0;
                SwitchCount = 0;
                return;
            }
        }

        Debug.LogWarning("解ける配置が見つからなかったので、最後の配置のまま開始します（条件を緩めると安定します）。");
        KeyCount = 0;
        SwitchCount = 0;
    }

    private void RestoreAllTiles()
    {
        // 欠けを全部戻す
        removedCells.Clear();

        tileMap.Clear();
        foreach (var kv in allTileMap)
        {
            tileMap[kv.Key] = kv.Value;
            if (kv.Value != null) kv.Value.gameObject.SetActive(true);
        }

        // 配置のたびに条件をリセット（拾った状態が残らないように）
        KeyCount = 0;
        SwitchCount = 0;
    }

    private void HideAllPickups()
    {
        if (keyPickups != null)
        {
            foreach (var key in keyPickups)
            {
                if (key != null)
                    key.gameObject.SetActive(false);
            }
        }

        if (treasurePickup != null)
            treasurePickup.gameObject.SetActive(false);
    }

    private void ApplyRandomMissingTiles()
    {
        if (!randomRemoveTiles || removeTileCount <= 0) return;

        // start/goal/クリスタル/宝箱を除外
        var candidates = tileMap.Keys
            .Where(c => c != startCell && c != goalCell && !keyCells.Contains(c) && (!hasTreasureCell || c != treasureCell))
            .ToList();

        if (avoidStartNeighbors)
            candidates.RemoveAll(c => IsNeighbor(startCell, c));

        int count = Mathf.Min(removeTileCount, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            var c = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            candidates.Remove(c);
            removedCells.Add(c);
        }

        foreach (var cell in removedCells)
        {
            if (tileMap.TryGetValue(cell, out var tile))
            {
                tile.gameObject.SetActive(false);
                tileMap.Remove(cell);
            }
        }
    }

    private void PlaceOnTileWorldOnly(Transform obj, Vector2Int cell)
    {
        if (obj == null) return;
        if (!tileMap.TryGetValue(cell, out var tile)) return;

        // 親は変えない（タイルが消えても宝箱は消えない）
        obj.position = tile.transform.position;
    }

    private bool IsNeighbor(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;

    private void PlaceOnTile(Transform obj, Vector2Int cell)
    {
        if (obj == null) return;
        if (!tileMap.TryGetValue(cell, out var tile)) return;

        // 親子付けしてもサイズが変わらないように（ワールド維持）
        obj.SetParent(tile.transform, true);
        obj.position = tile.transform.position;
    }

    // ========= Solvability Check =========
    // 「崩れる床」＝一度踏んだセルへ戻れない、という前提で
    // start → (必要クリスタル回収) → goal に到達できるルートが1つでもあればOK
    private bool IsSolvable()
    {
        // start/goalが欠けてたら即アウト（通常は除外してるけど保険）
        if (!tileMap.ContainsKey(startCell)) return false;
        if (!tileMap.ContainsKey(goalCell)) return false;

        // 盤面をbitに変換（4x4想定。intなので最大30セル程度まで対応）
        int w = boardW;
        int h = boardH;

        int CellBit(Vector2Int c) => 1 << (c.y * w + c.x);

        int allMask = 0;
        foreach (var c in tileMap.Keys)
            allMask |= CellBit(c);

        int startBit = CellBit(startCell);
        int goalBit = CellBit(goalCell);

        // クリスタル位置。3個以上にも対応。
        int[] keyBits = new int[keyCells.Count];
        for (int i = 0; i < keyBits.Length; i++)
            keyBits[i] = CellBit(keyCells[i]);

        // 隣接マスク
        int[] neigh = new int[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int idx = y * w + x;
            int m = 0;
            if (x > 0)     m |= 1 << (y * w + (x - 1));
            if (x < w - 1) m |= 1 << (y * w + (x + 1));
            if (y > 0)     m |= 1 << ((y - 1) * w + x);
            if (y < h - 1) m |= 1 << ((y + 1) * w + x);
            neigh[idx] = m;
        }

        int required = requireKey ? requiredKeyCount : 0;
        required = Mathf.Clamp(required, 0, keyBits.Length);

        int KeyMaskAt(int posBit)
        {
            int km = 0;
            for (int i = 0; i < keyBits.Length; i++)
                if (posBit == keyBits[i]) km |= (1 << i);
            return km;
        }

        int PopCount(int v)
        {
            int c = 0;
            while (v != 0) { v &= (v - 1); c++; }
            return c;
        }

        var memo = new Dictionary<long, bool>(4096);

        bool Dfs(int posBit, int visitedMask, int gotMask)
        {
            // ゴール到達＆条件達成
            if (posBit == goalBit && PopCount(gotMask) >= required) return true;

            int posIdx = BitToIndex(posBit);
            long key = visitedMask | ((long)posIdx << 20) | ((long)gotMask << 28);

            if (memo.TryGetValue(key, out var cached)) return cached;

            int nextMask = neigh[posIdx] & allMask & ~visitedMask;

            while (nextMask != 0)
            {
                int nb = nextMask & -nextMask;
                nextMask -= nb;

                // ロック中はゴールに入れない
                if (lockGoalTileUntilUnlocked && nb == goalBit && PopCount(gotMask) < required)
                    continue;

                int newGot = gotMask | KeyMaskAt(nb);
                if (Dfs(nb, visitedMask | nb, newGot))
                {
                    memo[key] = true;
                    return true;
                }
            }

            memo[key] = false;
            return false;
        }

        int initGot = KeyMaskAt(startBit);
        return Dfs(startBit, startBit, initGot);
    }

    private int BitToIndex(int bit)
    {
        int index = 0;
        while (bit > 1)
        {
            bit >>= 1;
            index++;
        }
        return index;
    }

    private bool CanStillClearFrom(Vector2Int currentCell)
    {
        // 現在地が有効タイルでなければアウト
        if (!tileMap.TryGetValue(currentCell, out var currentTile)) return false;
        if (currentTile.IsCollapsed || currentTile.IsCollapsing) return false;

        // まだ使えるタイルだけを対象にする
        var usableCells = new HashSet<Vector2Int>(
            tileMap
                .Where(kv => kv.Value != null && !kv.Value.IsCollapsed && !kv.Value.IsCollapsing)
                .Select(kv => kv.Key)
        );

        if (!usableCells.Contains(currentCell)) return false;
        if (!usableCells.Contains(goalCell)) return false;

        // スイッチは今使っていない前提。必要数が残っていたら到達不可扱い
        if (SwitchCount < requiredSwitches) return false;

        int requiredKeys = requireKey ? requiredKeyCount : 0;
        int alreadyGotKeys = KeyCount;

        if (alreadyGotKeys >= requiredKeys)
        {
            // すでにクリスタル条件を満たしているなら、ゴールに行けるかだけ見る
            return CanReachGoalOnly(currentCell, usableCells);
        }

        // 残っているクリスタルの位置を集める
        var remainingKeyCells = new List<Vector2Int>();

        for (int i = 0; i < activeKeyPickups.Count && i < keyCells.Count; i++)
        {
            Transform keyPickup = activeKeyPickups[i];
            if (keyPickup == null) continue;

            // まだ表示されているクリスタルだけ対象
            if (keyPickup.gameObject.activeSelf && usableCells.Contains(keyCells[i]))
            {
                remainingKeyCells.Add(keyCells[i]);
            }
        }

        int needMoreKeys = requiredKeys - alreadyGotKeys;

        if (remainingKeyCells.Count < needMoreKeys)
            return false;

        var visited = new HashSet<Vector2Int>();
        visited.Add(currentCell);

        return DfsCanClear(currentCell, visited, 0, remainingKeyCells, usableCells, alreadyGotKeys, requiredKeys);
    }

    private bool CanReachGoalOnly(Vector2Int currentCell, HashSet<Vector2Int> usableCells)
    {
        var visited = new HashSet<Vector2Int>();
        visited.Add(currentCell);

        return DfsReachGoal(currentCell, visited, usableCells);
    }

    private bool DfsReachGoal(Vector2Int pos, HashSet<Vector2Int> visited, HashSet<Vector2Int> usableCells)
    {
        if (pos == goalCell) return true;

        foreach (var next in GetNeighbors(pos))
        {
            if (!usableCells.Contains(next)) continue;
            if (visited.Contains(next)) continue;

            visited.Add(next);

            if (DfsReachGoal(next, visited, usableCells))
                return true;

            visited.Remove(next);
        }

        return false;
    }

    private bool DfsCanClear(
        Vector2Int pos,
        HashSet<Vector2Int> visited,
        int gotMask,
        List<Vector2Int> remainingKeyCells,
        HashSet<Vector2Int> usableCells,
        int alreadyGotKeys,
        int requiredKeys
    )
    {
        int totalGot = alreadyGotKeys + CountBits(gotMask);

        // ゴールに到達して、必要なクリスタル数を満たしていればクリア可能
        if (pos == goalCell && totalGot >= requiredKeys)
            return true;

        foreach (var next in GetNeighbors(pos))
        {
            if (!usableCells.Contains(next)) continue;
            if (visited.Contains(next)) continue;

            int newGotMask = gotMask;

            for (int i = 0; i < remainingKeyCells.Count; i++)
            {
                if (next == remainingKeyCells[i])
                {
                    newGotMask |= (1 << i);
                    break;
                }
            }

            int newTotalGot = alreadyGotKeys + CountBits(newGotMask);

            // ゴールロックONの場合、クリスタル未達成ならゴールには入れない
            if (lockGoalTileUntilUnlocked && next == goalCell && newTotalGot < requiredKeys)
                continue;

            visited.Add(next);

            if (DfsCanClear(next, visited, newGotMask, remainingKeyCells, usableCells, alreadyGotKeys, requiredKeys))
                return true;

            visited.Remove(next);
        }

        return false;
    }

    private IEnumerable<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        yield return cell + Vector2Int.up;
        yield return cell + Vector2Int.down;
        yield return cell + Vector2Int.left;
        yield return cell + Vector2Int.right;
    }

    private int CountBits(int value)
    {
        int count = 0;

        while (value != 0)
        {
            value &= value - 1;
            count++;
        }

        return count;
    }
}
