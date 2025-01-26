using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GenerateImg : MonoBehaviour
{
    //[HideInInspector]
    [SerializeField]
    private Camera _camera;
    private int _downResFactor = 1;
    [SerializeField]
    private Material _material;

    private string _globalTextureName = "_GlobalRefractionTex";

    private void GenerateRT()
    {
        _camera = GetComponent<Camera>();

        if (_camera.targetTexture != null)
        {
            RenderTexture temp = _camera.targetTexture;

            _camera.targetTexture = null;
            DestroyImmediate(temp);
        }

        _camera.targetTexture = new RenderTexture(_camera.pixelWidth >> _downResFactor, _camera.pixelHeight >> _downResFactor, 16);
        _camera.targetTexture.filterMode = FilterMode.Bilinear;

        _material.SetTexture(_globalTextureName, _camera.targetTexture);        
    }

    private void FixedUpdate()
    {
        GenerateRT();
    }
}
