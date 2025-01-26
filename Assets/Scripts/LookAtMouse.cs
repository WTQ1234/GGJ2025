using UnityEngine;

public class LookAtMouse : MonoBehaviour
{
    void Update()
    {
        // 获取鼠标在屏幕上的位置
        Vector3 mouseScreenPosition = Input.mousePosition;

        // 将鼠标屏幕坐标转换为世界坐标
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);

        // 确保 Z 轴与物体保持一致
        mouseWorldPosition.z = transform.position.z;

        // 计算物体指向鼠标位置的方向向量
        Vector3 direction = mouseWorldPosition - transform.position;

        // 计算出需要旋转的角度
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 设置物体旋转角度（Z轴旋转）
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
