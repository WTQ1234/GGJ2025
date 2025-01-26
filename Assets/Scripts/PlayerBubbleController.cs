using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;


public class PlayerBubbleController : MonoBehaviour
{
    public Player player;
    public GameObject BubbleParent;
    public UIComp_AbilityChoose UIComp_AbilityChoose;

    // 不同的变异的能量槽 和   每个怪物变异的能量，提供的食物能量
    [ShowInInspector]
    public Dictionary<string, float> dic_Trans = new Dictionary<string, float>();   // 0-1
    [ShowInInspector]
    public Dictionary<string, BubbleAbility> dic_Ability = new Dictionary<string, BubbleAbility>();

    [ShowInInspector]
    public Dictionary<string, Image> dic_ui = new Dictionary<string, Image>();

    //public static string cur_bubble_name;

    public void Awake()
    {
        foreach (Transform child in BubbleParent.transform)
        {
            child.gameObject.SetActive(false);
            dic_Ability.Add(child.name, child.GetComponent<BubbleAbility>());
            if (child.name == "Bubble_Default")
            {
                dic_Trans.Add(child.name, 1);
            }
            else
            {
                dic_Trans.Add(child.name, 1);
            }

            var uicomp = UIComp_AbilityChoose.transform.Find(child.name).Find(child.name);
            //Debug.Log(uicomp);
            //Debug.Log(child.name);
            dic_ui.Add(child.name, uicomp.GetComponent<Image>());
            uicomp.GetComponent<Button>().onClick.AddListener(() => TrnsTo(child.name));
        }
        RefreshBubblePower();
        TrnsTo("Bubble_Default");
    }

    // 
    public void AddBubblePower(string name, float value)
    {
        dic_Trans[name] += value;
        RefreshBubblePower();
    }

    public void RefreshBubblePower()
    {
        foreach (var kv in dic_Trans)
        {
            var image = dic_ui[kv.Key];
            image.fillAmount = kv.Value;
            image.GetComponent<Button>().enabled = DetectBubbleTrans(kv.Key);
        }
    }

    public void RefreshBubbleScale(BubbleData bubbleData)
    {
        // 根据当前气体，改变气泡大小（示例：从 minGas->maxGas 做一个插值，或简单按比例放缩）
        float scalePercent = bubbleData.currentGas / bubbleData.maxGas;
        scalePercent = Mathf.Clamp01(scalePercent);  // 0~1之间
        BubbleParent.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 1.0f, scalePercent);

        // 如果被吸收者气体低于或等于最小值，则销毁
        if (bubbleData.currentGas <= bubbleData.minGas)
        {
            BubbleParent.SetActive(false);
        }
        else
        {
            BubbleParent.SetActive(true);
        }
    }

    public bool DetectBubbleTrans(string name) {
        if (dic_Trans.ContainsKey(name))
        {
            if (dic_Trans[name] >= 1)
            {
                return true;
            }
        }
        return false;
    }


    [Button]
    public void TrnsTo(string name)
    {
        if (dic_Trans.ContainsKey(name))
        {
            if (dic_Trans[name] >= 1)
            {
                var trans = BubbleParent.transform.Find(name);
                if (trans != null)
                {
                    foreach (Transform child in BubbleParent.transform)
                    {
                        child.gameObject.SetActive(false);
                        //player.abilityController.UnRegisterMobAbility(dic_Ability[name]);
                    }
                    trans.gameObject.SetActive(true);
                    //player.abilityController.RegisterMobAbility(dic_Ability[name]);
                    GlobalVarManager.cur_bubble_name = name;
                    //cur_bubble_name = name;
                }
            }
        }
    }
}
