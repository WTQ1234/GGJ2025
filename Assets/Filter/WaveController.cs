using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveController : MonoBehaviour
{
    private float Magnitude;
    private Camera m_Camera;

    [SerializeField]
    [Range(0, 1f)]
    private float Intensity;
    [SerializeField]
    [Range(0.2f,5)]
    private float Speed;

    private PP_Controller controller;
    private float Timer;

    private void Start()
    {
        m_Camera = Camera.main;
        controller = m_Camera.GetComponent<PP_Controller>();
        Timer = 0;        
    }

    private void Update()
    {
        Timer += Time.deltaTime;
        controller.mat.SetFloat("_Magnitude", Mathf.Sin(Timer * Speed) * Intensity * 0.04f);
    }
}
