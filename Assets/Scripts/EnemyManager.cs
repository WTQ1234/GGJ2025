using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HRL;
using Sirenix.OdinInspector;

public class EnemyManager : MonoSingleton<EnemyManager>
{
    public List<Enemy> EnemiesList;
    //public Transform startTransform;
    //public float radious = 10;

    public float time;
    private float _time;

    void Start()
    {
        _time = 0;
        //for(int i = 0; i < EnemiesList.Count; i++)
        //{
        //    ObjectPoolManager.Instance.InitObjectPool(EnemiesList[i].gameObject);
        //}

        Check();
        Check();
    }

    void Update()
    {
        _time += Time.deltaTime;
        if (_time > time)
        {
            _time = 0;
            Check();
            Check();
        }
    }

    [Button]
    void Check()
    {
        // 获取 BoxCollider2D 区域
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            Debug.LogError("BoxCollider2D not found on this GameObject!");
            return;
        }

        // 在 BoxCollider2D 的区域内随机生成位置
        Vector3 randomPointInBox = GetRandomPointInBox(boxCollider);

        // 从敌人列表中随机选取一个敌人类型
        Enemy p = EnemiesList[Random.Range(0, EnemiesList.Count)];

        // 使用对象池或实例化生成敌人
        // GameObject enemy = ObjectPoolManager.Instance.GetObject(p.gameObject);
        GameObject enemy = GameObject.Instantiate<GameObject>(p.gameObject);
        enemy.SetActive(true);

        // 设置敌人的位置和初始旋转
        enemy.transform.position = randomPointInBox;
        enemy.transform.rotation = Quaternion.identity;
    }

    /// <summary>
    /// 从 BoxCollider2D 的范围内随机获取一个点
    /// </summary>
    private Vector3 GetRandomPointInBox(BoxCollider2D boxCollider)
    {
        // 获取 BoxCollider2D 的中心和大小
        Vector2 center = boxCollider.bounds.center;
        Vector2 size = boxCollider.size;

        // 根据 BoxCollider2D 的旋转和缩放计算随机点
        float randomX = Random.Range(-size.x / 2, size.x / 2);
        float randomY = Random.Range(-size.y / 2, size.y / 2);

        // 返回随机点，保持与 Z 轴一致
        return new Vector3(center.x + randomX, center.y + randomY, 0f);
    }

    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        //Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(startTransform.position, radious);
    }
}
