using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneEntity_Base : MonoBehaviour
{
    public Transform homeTrans;
    public bool isSetBaseAlready = false;
    public BubbleData bubbleData;

    public void Update()
    {
        float scalePercent = bubbleData.currentGas / bubbleData.maxGas;
        scalePercent = Mathf.Clamp01(scalePercent);  // 0~1之间
        homeTrans.localScale = Vector3.one * Mathf.Lerp(0.1f, 1.0f, scalePercent);
    }

    public float SetBase(float oxygenInput)
    {
        if (isSetBaseAlready)
        {
            return 0;
        }
        // TODO 取消掉描边


        // 设置一个
        homeTrans.gameObject.SetActive(true);

        var leftGas = bubbleData.TryAddGas(oxygenInput);
        return leftGas;
    }
}
