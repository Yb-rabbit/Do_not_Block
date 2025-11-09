using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Beat
{
    public float time; // 音符出现时间
    public int column; // 音符所在列
}

[System.Serializable]
public class RhythmGenerator : MonoBehaviour
{
    public float bpm = 120f; // 每分钟节拍数
    public int columns = 4; // 列数
    public string outputFileName = "GeneratedRhythm"; // 输出文件名

    public FourBeat3 fourBeat3; // 引用 FourBeat3 脚本

    private float nextBeatTime = 0f; // 下一拍的时间

    void Start()
    {
        if (fourBeat3 != null)
        {
            fourBeat3.SetRhythmGenerator(this); // 将 RhythmGenerator 传递给 FourBeat3
        }
        else
        {
            Debug.LogError("FourBeat3 未正确设置！");
        }
    }

    public Beat GenerateNextBeat()
    {
        // 动态生成下一拍的节奏数据
        int column = Random.Range(0, columns); // 随机选择一列
        Beat beat = new Beat
        {
            time = nextBeatTime,
            column = column
        };

        // 更新下一拍的时间
        nextBeatTime += 60f / bpm;

        return beat;
    }
}
