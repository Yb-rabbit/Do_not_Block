using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Reload_GM : MonoBehaviour
{
    // 目标场景名
    [SerializeField] private string sceneName;

    [Header("行为选项")]
    [Tooltip("false=直接切换(卸载原场景, 下次进入即为初始化); true=以叠加方式加载并可对原场景做进一步处理")]
    [SerializeField] private bool loadAdditively = false;

    [Tooltip("仅在 Additive 模式下有效：是否在切到目标场景后对原场景进行初始化处理")]
    [SerializeField] private bool reinitializeOriginal = true;

    [Tooltip("仅在 Additive+初始化 下有效：初始化后是否让原场景保持加载(预热)，以便快速切回。会禁用其根物体避免干扰")]
    [SerializeField] private bool keepOriginalLoaded = false;

    // 绑定到按钮
    public void ReloadScene()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("未指定场景名称！");
            return;
        }

        if (!loadAdditively)
        {
            // 简单模式：直接切换。原场景被卸载，日后再加载即为初始状态
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            return;
        }

        // 进阶模式：以 Additive 方式加载然后处理原场景
        StartCoroutine(LoadAdditiveAndInitOriginal());
    }

    private IEnumerator LoadAdditiveAndInitOriginal()
    {
        var originalScene = SceneManager.GetActiveScene();

        // 1) 加载目标场景(叠加)
        var loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return loadOp;

        var target = SceneManager.GetSceneByName(sceneName);
        if (!target.IsValid())
        {
            Debug.LogError($"加载场景失败: {sceneName}");
            yield break;
        }

        // 2) 切换激活场景到目标场景
        SceneManager.SetActiveScene(target);

        if (!reinitializeOriginal)
        {
            yield break;
        }

        // 3) 卸载原场景 => 原场景将处于“未加载=初始干净”状态
        yield return SceneManager.UnloadSceneAsync(originalScene);

        if (!keepOriginalLoaded)
        {
            // 不预热：保持未加载，下次需要再加载即可
            yield break;
        }

        // 4) 预热原场景：重新加载为初始化状态，并禁用其根物体避免与目标场景冲突
        var reloadOp = SceneManager.LoadSceneAsync(originalScene.name, LoadSceneMode.Additive);
        yield return reloadOp;

        var reloaded = SceneManager.GetSceneByName(originalScene.name);
        if (reloaded.IsValid())
        {
            SetSceneRootActive(reloaded, false); // 禁用根物体，避免脚本/渲染干扰
        }
    }

    private static void SetSceneRootActive(Scene scene, bool active)
    {
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            roots[i].SetActive(active);
        }
    }
}
