using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 引入场景管理命名空间

public class Reload_GM : MonoBehaviour
{
    // 指定要重新加载的场景名称
    [SerializeField] private string sceneName;

    // 绑定到按钮的点击事件
    public void ReloadScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName); // 重新加载指定场景
        }
        else
        {
            Debug.LogError("未指定场景名称！");
        }
    }
}
