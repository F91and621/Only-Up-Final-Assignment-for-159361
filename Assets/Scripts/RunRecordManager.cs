using System;
using System.Collections.Generic;
using UnityEngine;

public class RunRecord
{
    public string id;
    public string date;
    public float duration;
}

// Saves up to 10 records.
public static class RunRecordManager
{
    const int MaxRecords = 10;
    const string CountKey = "OnlyUpRecordCount";
    const string CurrentKey = "OnlyUpCurrentRecord";

    public static string CurrentId
    {
        get { return PlayerPrefs.GetString(CurrentKey, ""); }
    }

    public static void AddRecord(float duration)
    {
        List<RunRecord> records = LoadRecords();
        RunRecord record = new RunRecord();
        record.id = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        record.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        record.duration = duration;
        records.Add(record);

        if (records.Count > MaxRecords)
            records.RemoveAt(0);

        SaveRecords(records);
        PlayerPrefs.SetString(CurrentKey, record.id);
        PlayerPrefs.Save();
    }

    public static List<RunRecord> GetSortedRecords()
    {
        List<RunRecord> records = LoadRecords();
        records.Sort((a, b) => a.duration.CompareTo(b.duration));
        return records;
    }

    public static string FormatTime(float duration)
    {
        int minutes = Mathf.FloorToInt(duration / 60f);
        int seconds = Mathf.FloorToInt(duration % 60f);
        int hundredths = Mathf.FloorToInt((duration * 100f) % 100f);
        return minutes.ToString("00") + ":" + seconds.ToString("00")
            + "." + hundredths.ToString("00");
    }

    static List<RunRecord> LoadRecords()
    {
        List<RunRecord> records = new List<RunRecord>();
        int count = PlayerPrefs.GetInt(CountKey, 0);

        for (int i = 0; i < count; i++)
        {
            RunRecord record = new RunRecord();
            record.id = PlayerPrefs.GetString("RecordId" + i, "");
            record.date = PlayerPrefs.GetString("RecordDate" + i, "");
            record.duration = PlayerPrefs.GetFloat("RecordTime" + i, 0f);
            records.Add(record);
        }

        return records;
    }

    static void SaveRecords(List<RunRecord> records)
    {
        PlayerPrefs.SetInt(CountKey, records.Count);
        for (int i = 0; i < records.Count; i++)
        {
            PlayerPrefs.SetString("RecordId" + i, records[i].id);
            PlayerPrefs.SetString("RecordDate" + i, records[i].date);
            PlayerPrefs.SetFloat("RecordTime" + i, records[i].duration);
        }
    }
}
