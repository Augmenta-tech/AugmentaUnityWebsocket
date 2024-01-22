using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.VFX;

public class PCLVFXConverter : MonoBehaviour  
{
    VisualEffect vfx;
    PObject pObject;

    [SerializeField] 
    [HideInInspector]
    RenderTexture posTex;

    public ComputeShader posToTexCompute;
    ComputeBuffer posBuffer;

    void Start()
    {
        vfx = GetComponent<VisualEffect>();
        if (!vfx.HasTexture("Positions")) Debug.LogWarning("VFX Graph should have a Positions Texture property");
        pObject = GetComponent<PObject>();

    }

    void Update()
    {
        int bufferCount = pObject.points.Length * 3;
        if (vfx == null || pObject == null || bufferCount == 0) return;

        if (!vfx.HasTexture("Positions")) return;

        if (posBuffer != null) posBuffer.Dispose();
        posBuffer = new ComputeBuffer(bufferCount, sizeof(float));

        if (posTex != null) posTex.Release();
        posTex = new RenderTexture(pObject.points.Length, 1, 1, RenderTextureFormat.ARGBFloat);
        posTex.enableRandomWrite = true;
        posToTexCompute.SetTexture(0, "PositionMap", posTex);

        posBuffer.SetData(pObject.points);
        posToTexCompute.SetBuffer(0, "PositionBuffer", posBuffer);
        posToTexCompute.Dispatch(0, 64, 1, 1);
        vfx.SetTexture("Positions", posTex);
    }
}
