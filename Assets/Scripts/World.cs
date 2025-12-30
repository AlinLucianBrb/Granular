using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

enum Blocks
{
    Clear = 0,
    Smoke = 1,
    Water = 2,
    Sand = 3,
    Stone = 4,
    COUNT = 5,
    INVALID,
}

public class World : Singleton<World>
{
    public int chunkSize = 16;
    public int2 noChunks = new int2(64,36);
    public WorldSettings worldSettings = new WorldSettings();

    public NativeArray<BlockData> blocksData;

    private uint tick;

    BlockPainter painter;

    public override void Awake()
    {
        painter = new BlockPainter();

        worldSettings.chunkSize = chunkSize; 
        worldSettings.noChunks = noChunks;
        worldSettings.width = noChunks.x * chunkSize;
        worldSettings.height = noChunks.y * chunkSize;

        blocksData = new NativeArray<BlockData>(chunkSize * noChunks.y * chunkSize * noChunks.x, Allocator.Persistent);

        UnityEngine.Random.InitState(System.Environment.TickCount);
        float offsetX = UnityEngine.Random.Range(-100000f, 100000f);
        float offsetY = UnityEngine.Random.Range(-100000f, 100000f);
        float scale = UnityEngine.Random.Range(0.002f, 0.08f);

        int curPos = 0;
        for (int j = 0; j < noChunks.y; j++)
        {
            for (int i = 0; i < noChunks.x; i++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        float worldX = i * chunkSize + x;
                        float worldY = j * chunkSize + y;

                        float perlinNoise = Mathf.PerlinNoise(worldX * scale + offsetX, worldY * scale + offsetY);

                        if (perlinNoise > 0.6f && perlinNoise < 1f)
                        {
                            blocksData[curPos] = new BlockData((byte)Blocks.Stone, new int2(i * chunkSize + x, j * chunkSize + y));
                        }
                        else if (perlinNoise < 0.6f && perlinNoise > 0.4f)
                        {
                            blocksData[curPos] = new BlockData((byte)Blocks.Sand, new int2(i * chunkSize + x, j * chunkSize + y));
                        }
                        else if (perlinNoise < 0.35f && perlinNoise > 0.3f)
                        {
                            blocksData[curPos] = new BlockData((byte)Blocks.Water, new int2(i * chunkSize + x, j * chunkSize + y));
                        }
                        else
                        {
                            blocksData[curPos] = new BlockData((byte)Blocks.Clear, new int2(i * chunkSize + x, j * chunkSize + y));
                        }

                        curPos++;
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (GameManager.gameState == GameManager.GameState.Pause)
            return;

        tick++;
        worldSettings.tick = tick;

        painter.Paint(worldSettings);

        bool2 result;

        bool2[] test = { new bool2(true, true), new bool2(true, false), new bool2(false, true), new bool2(false, false) };

        for (int i = 0; i < 4; i++)
        {       
            result = test[i];

            BlockUpdateJob blockUpdateJob = new BlockUpdateJob()
            {
                blockDataArray = blocksData,
                result = result,
                settings = worldSettings
            };

            JobHandle jobHandle = blockUpdateJob.Schedule(blocksData.Length, chunkSize * chunkSize);
            jobHandle.Complete();     
        }
    }

    private void OnDestroy()
    {
        blocksData.Dispose();
    }
}
