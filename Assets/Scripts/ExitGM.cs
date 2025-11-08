using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor; // 仅在编辑器模式下引入
#endif

public class ExitGM : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    // 绑定到退出按钮的点击事件
    public void ExitGame()
    {
#if UNITY_EDITOR
        // 如果在编辑器中，退出游戏模式
        EditorApplication.isPlaying = false;
#else
        // 如果是构建后的应用程序，退出游戏
        Application.Quit();
#endif
    }
}
