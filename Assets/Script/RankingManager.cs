using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class RankingRecord
{
    public int stage;
    public int steps;

    public RankingRecord(int stage, int steps)
    {
        this.stage = stage;
        this.steps = steps;
    }
}

[Serializable]
public class RankingData
{
    public List<RankingRecord> records = new List<RankingRecord>();
}

public static class RankingManager
{
    private const string RankingKey = "OfflineRanking";
    private const int MaxRankingCount = 10;

    public static int SaveResult(int stage, int steps)
    {
        // 保存しない場合は -1
        if (stage <= 0 || steps <= 0) return -1;

        RankingData data = LoadData();

        RankingRecord newRecord = new RankingRecord(stage, steps);
        data.records.Add(newRecord);

        // いったん全件をランキング順に並べる
        List<RankingRecord> sortedRecords = data.records
            .OrderByDescending(r => r.stage)
            .ThenByDescending(r => r.steps)
            .ToList();

        // 今回追加した記録が何位かを取得
        int newRankIndex = sortedRecords.IndexOf(newRecord);

        // 上位10件だけ保存
        data.records = sortedRecords
            .Take(MaxRankingCount)
            .ToList();

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(RankingKey, json);
        PlayerPrefs.Save();

        // 10位以内なら 1〜10 を返す
        if (newRankIndex >= 0 && newRankIndex < MaxRankingCount)
        {
            return newRankIndex + 1;
        }

        // 10位以内に入らなければ -1
        return -1;
    }

    public static List<RankingRecord> GetRecords()
    {
        RankingData data = LoadData();

        return data.records
            .OrderByDescending(r => r.stage)
            .ThenByDescending(r => r.steps)
            .Take(MaxRankingCount)
            .ToList();
    }

    public static string GetRankingText()
    {
        List<RankingRecord> records = GetRecords();

        if (records.Count == 0)
        {
            return "NO DATA";
        }

        string text = "";

        for (int i = 0; i < records.Count; i++)
        {
            RankingRecord r = records[i];

            string rank = (i + 1).ToString().PadLeft(2);
            string stage = r.stage.ToString().PadLeft(3);
            string steps = r.steps.ToString().PadLeft(4);

            text += $"{rank}.  STAGE {stage}   STEPS {steps}";

            if (i < records.Count - 1)
                text += "\n";
        }

        return text;
    }

    public static void ClearRanking()
    {
        PlayerPrefs.DeleteKey(RankingKey);
        PlayerPrefs.Save();
    }

    private static RankingData LoadData()
    {
        string json = PlayerPrefs.GetString(RankingKey, "");

        if (string.IsNullOrEmpty(json))
        {
            return new RankingData();
        }

        RankingData data = JsonUtility.FromJson<RankingData>(json);

        if (data == null || data.records == null)
        {
            return new RankingData();
        }

        return data;
    }
}