public class StageDifficulty
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

    public static StageDifficulty GetDifficulty(int stage)
    {
        if (stage >= 30)
        {
            // STAGE 30〜：穴4個 / クリスタル3個
            // 穴が増えて難しいので、宝箱は通常出現にしておく
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

        // STAGE 1〜9：穴3個 / クリスタル2個
        return new StageDifficulty(3, 2, 1.0f);
    }
}