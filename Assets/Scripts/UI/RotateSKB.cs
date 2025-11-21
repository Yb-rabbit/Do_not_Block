using UnityEngine;

public class RotateSKB : MonoBehaviour
{
    public Transform skyboxCamera;
    public float rotationSpeed = 0.5f;

    [Header("锁定轴（保持初始值不变）")]
    public bool lockX = true;
    public bool lockY = false;
    public bool lockZ = true;

    private Vector3 _initialEuler;

    void Start()
    {
        if (skyboxCamera != null)
        {
            _initialEuler = skyboxCamera.eulerAngles;
        }
    }

    void Update()
    {
        if (skyboxCamera == null) return;

        // 仅围绕 Y 轴累加旋转（水平旋转），防止累积误差可直接构造新欧拉角
        float newY = skyboxCamera.eulerAngles.y + rotationSpeed * Time.deltaTime;

        Vector3 euler = skyboxCamera.eulerAngles;
        euler.y = newY;

        if (lockX) euler.x = _initialEuler.x;
        if (lockY) euler.y = _initialEuler.y; // 若锁定Y则不会转动
        if (lockZ) euler.z = _initialEuler.z;

        skyboxCamera.rotation = Quaternion.Euler(euler);
    }
}