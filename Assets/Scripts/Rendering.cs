using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System;

[BurstCompile]
public struct RenderingUpdateJob : IJobParallelFor
{
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<uint> texture;
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<BlockData> blockDataArray;

    public int chunkSize;
    public int2 noChunks;

    public void Execute(int i)
    {
        int chunk = (i / (chunkSize * chunkSize));
        int chunkPos = i - (chunkSize * chunkSize) * chunk;
        int x = chunkPos % chunkSize;
        int y = chunkPos / chunkSize;

        x = (chunk % noChunks.x) * chunkSize + x;
        y = (chunk / noChunks.x) * chunkSize + y;

        int arrayPos = y * chunkSize * noChunks.x + x;

        texture[arrayPos] = blockDataArray[i].id;
    }
}

public class Rendering : MonoBehaviour
{
    NativeArray<uint> textureNativeArray;
    ComputeBuffer textureBuffer;

    public ComputeShader computeShader;

    // Multi-pass RTs
    RenderTexture baseRT;
    RenderTexture smokeRT;
    RenderTexture smokeTempRT;
    RenderTexture smokeBlurRT;
    RenderTexture waterRT;
    RenderTexture finalRT;

    int width, height;

    // Kernels
    int kShade;
    int kBlurH;
    int kBlurV;
    int kComposite;

    // Thread group size (must match [numthreads(8,8,1)])
    const int TX = 8;
    const int TY = 8;

    void Start()
    {
        width = World.instance.chunkSize * World.instance.noChunks.x;
        height = World.instance.chunkSize * World.instance.noChunks.y;

        // Find kernels by name (your compute shader must have these)
        kShade = computeShader.FindKernel("Shade");
        kBlurH = computeShader.FindKernel("BlurH");
        kBlurV = computeShader.FindKernel("BlurV");
        kComposite = computeShader.FindKernel("Composite");

        textureNativeArray = new NativeArray<uint>(World.instance.blocksData.Length, Allocator.Persistent);

        // IMPORTANT: stride must be sizeof(uint), not sizeof(uint) * 4
        textureBuffer = new ComputeBuffer(textureNativeArray.Length, sizeof(uint));
    }

    void Update()
    {
        if (GameManager.gameState == GameManager.GameState.Pause)
            return;

        RenderingUpdateJob renderingUpdateJob = new RenderingUpdateJob()
        {
            blockDataArray = World.instance.blocksData,
            texture = textureNativeArray,
            chunkSize = World.instance.chunkSize,
            noChunks = World.instance.noChunks
        };

        JobHandle jobHandle = renderingUpdateJob.Schedule(World.instance.blocksData.Length, 128);
        jobHandle.Complete();
    }

    void EnsureRT(ref RenderTexture rt, int w, int h, RenderTextureFormat format, FilterMode filter)
    {
        if (rt != null && rt.width == w && rt.height == h && rt.format == format)
            return;

        if (rt != null) rt.Release();

        rt = new RenderTexture(w, h, 0, format);
        rt.enableRandomWrite = true;
        rt.filterMode = filter;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.Create();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        int w = width;
        int h = height;

        // Allocate RTs
        // Base + final color
        EnsureRT(ref baseRT, w, h, RenderTextureFormat.ARGB32, FilterMode.Point);
        EnsureRT(ref finalRT, w, h, RenderTextureFormat.ARGB32, FilterMode.Point);

        // Masks (single channel float)
        EnsureRT(ref smokeRT, w, h, RenderTextureFormat.RFloat, FilterMode.Point);
        EnsureRT(ref smokeTempRT, w, h, RenderTextureFormat.RFloat, FilterMode.Point);
        EnsureRT(ref smokeBlurRT, w, h, RenderTextureFormat.RFloat, FilterMode.Point);
        EnsureRT(ref waterRT, w, h, RenderTextureFormat.RFloat, FilterMode.Point);

        // Upload ids
        textureBuffer.SetData(textureNativeArray);

        int groupsX = (w + TX - 1) / TX;
        int groupsY = (h + TY - 1) / TY;

        // Common uniforms
        computeShader.SetInt("Width", w);
        computeShader.SetInt("Height", h);

        // -------------------------
        // Pass A: Shade -> baseRT + smokeRT + waterRT
        // -------------------------
        computeShader.SetBuffer(kShade, "Ids", textureBuffer);
        computeShader.SetTexture(kShade, "BaseOut", baseRT);
        computeShader.SetTexture(kShade, "SmokeOut", smokeRT);
        computeShader.SetTexture(kShade, "WaterOut", waterRT);

        computeShader.Dispatch(kShade, groupsX, groupsY, 1);

        // -------------------------
        // Pass B: BlurH smokeRT -> smokeTempRT
        // -------------------------
        computeShader.SetTexture(kBlurH, "InTex", smokeRT);
        computeShader.SetTexture(kBlurH, "OutTex", smokeTempRT);

        computeShader.Dispatch(kBlurH, groupsX, groupsY, 1);

        // -------------------------
        // Pass C: BlurV smokeTempRT -> smokeBlurRT
        // -------------------------
        computeShader.SetTexture(kBlurV, "InTex", smokeTempRT);
        computeShader.SetTexture(kBlurV, "OutTex", smokeBlurRT);

        computeShader.Dispatch(kBlurV, groupsX, groupsY, 1);

        // -------------------------
        // Pass D: Composite -> finalRT
        // -------------------------
        computeShader.SetTexture(kComposite, "BaseIn", baseRT);
        computeShader.SetTexture(kComposite, "SmokeBlur", smokeBlurRT);
        computeShader.SetTexture(kComposite, "WaterMask", waterRT);
        computeShader.SetTexture(kComposite, "Result", finalRT);

        computeShader.Dispatch(kComposite, groupsX, groupsY, 1);

        // Output
        EnsureRT(ref finalRT, w, h, RenderTextureFormat.ARGB32, FilterMode.Bilinear);
        Graphics.Blit(finalRT, destination);
    }

    private void OnDestroy()
    {
        if (textureNativeArray.IsCreated) textureNativeArray.Dispose();
        if (textureBuffer != null) textureBuffer.Dispose();

        if (baseRT != null) baseRT.Release();
        if (smokeRT != null) smokeRT.Release();
        if (smokeTempRT != null) smokeTempRT.Release();
        if (smokeBlurRT != null) smokeBlurRT.Release();
        if (waterRT != null) waterRT.Release();
        if (finalRT != null) finalRT.Release();
    }
}
