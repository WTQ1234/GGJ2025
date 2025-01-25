using UnityEngine;

[ExecuteInEditMode]
public class PP_Controller : MonoBehaviour
{
    public Material mat;

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        Graphics.Blit(src, dst, mat);        
    }
}
