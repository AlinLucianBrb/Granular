using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

public struct WorldSettings
{
    public int chunkSize;
    public int2 noChunks;
    public int width;
    public int height;
    public uint tick;
}

[BurstCompile]
public struct BlockUpdateJob : IJobParallelFor
{
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<BlockData> blockDataArray;

    public bool2 result;
    public WorldSettings settings;

    public void Execute(int i)
    {
        if (blockDataArray[i].id == 0) return;

        int chunkArea = settings.chunkSize * settings.chunkSize;
        int chunkIndex = i / chunkArea;
        int chunkX = chunkIndex % settings.noChunks.x;
        int chunkY = chunkIndex / settings.noChunks.x;

        if (((chunkX & 1) == 0) == result.x &&
            ((chunkY & 1) == 0) == result.y)
        {
            if (settings.tick != blockDataArray[i].lastMovedTick)
            {
                blockDataArray[i].Update(ref blockDataArray, in settings);
            }
        }
    }
}

public struct BlockData
{
    public byte id;
    public int2 pos;
    public int2 posLast;
    public int repeat;
    public uint lastMovedTick;

    public BlockData(byte id, int2 pos)
    {
        this.id = id;
        this.pos = pos;
        this.posLast = pos;
        this.repeat = 0;
        this.lastMovedTick = 0;
    }

    public void Update(ref NativeArray<BlockData> blockDataArray, in WorldSettings s)
    {
        int2 posDown = new int2(pos.x, pos.y - 1);
        int2 posUp = new int2(pos.x, pos.y + 1);
        int2 posLeft = new int2(pos.x - 1, pos.y);
        int2 posRight = new int2(pos.x + 1, pos.y);
        int2 posLeftDown = new int2(pos.x - 1, pos.y - 1);
        int2 posRightDown = new int2(pos.x + 1, pos.y - 1);
        int2 posLeftUp = new int2(pos.x - 1, pos.y + 1);
        int2 posRightUp = new int2(pos.x +1, pos.y + 1);

        bool borderDown = pos.y > 0;
        bool borderUp = pos.y < s.height - 1;
        bool borderLeft = pos.x > 0;
        bool borderRight = pos.x < s.width - 1;
        bool borderLeftDown = borderLeft && borderDown;
        bool borderRightDown = borderRight && borderDown;
        bool borderLeftUp = borderLeft && borderUp;
        bool borderRightUp = borderRight && borderUp;

        bool leftFirst = (((pos.x * 73856093) ^ (pos.y * 19349663) ^ (repeat * 83492791)) & 1) == 0;

        if (id >= (byte)Blocks.Stone)
        {
            CheckAndExecuteMove(ref blockDataArray, borderDown, posDown, in s, (byte)Blocks.Stone - 1);
            return;
        }

        if (id == (byte)Blocks.Sand)
        {
            if (CheckAndExecuteMove(ref blockDataArray, borderDown, posDown, in s)) return;

            if (leftFirst)
            {
                if (CheckAndExecuteMove(ref blockDataArray, borderLeftDown, posLeftDown, in s)) return;
                if (CheckAndExecuteMove(ref blockDataArray, borderRightDown, posRightDown, in s)) return;
            }
            else
            {
                if (CheckAndExecuteMove(ref blockDataArray, borderRightDown, posRightDown, in s)) return;
                if (CheckAndExecuteMove(ref blockDataArray, borderLeftDown, posLeftDown, in s)) return;
            }

            return;
        }

        if (id == (byte)Blocks.Water)
        {
            int waterCheck = (byte)Blocks.Water - 1;

            if (CheckAndExecuteMove(ref blockDataArray, borderDown, posDown, in s, waterCheck)) return;

            if (leftFirst)
            {
                if (CheckAndExecuteMove(ref blockDataArray, borderLeftDown, posLeftDown, in s, waterCheck)) return;
                if (CheckAndExecuteMove(ref blockDataArray, borderRightDown, posRightDown, in s, waterCheck)) return;
                if (CheckAndExecuteMove(ref blockDataArray, borderLeft, posLeft, in s, waterCheck)) return;
                if (CheckAndExecuteMove(ref blockDataArray, borderRight, posRight, in s, waterCheck)) return;
            }
            else
            {
                if (CheckAndExecuteMove(ref blockDataArray, borderRightDown, posRightDown, in s, waterCheck)) return;
                if (CheckAndExecuteMove(ref blockDataArray, borderLeftDown, posLeftDown, in s, waterCheck)) return;
                if (CheckAndExecuteMove(ref blockDataArray, borderRight, posRight, in s, waterCheck)) return;
                if (CheckAndExecuteMove(ref blockDataArray, borderLeft, posLeft, in s, waterCheck)) return;
            }

            return;
        }

        if (id == (byte)Blocks.Smoke)
        {
            int smokeCheck = (byte)Blocks.Smoke - 1;

            if (CheckAndExecuteMove(ref blockDataArray, borderUp, posUp, in s, smokeCheck)) return;

            if(leftFirst)
            {
                if (CheckAndExecuteMove(ref blockDataArray, borderLeftUp, posLeftUp, in s, smokeCheck)) return;
                if (CheckAndExecuteMove(ref blockDataArray, borderRightUp, posRightUp, in s, smokeCheck)) return;
                if (CheckAndExecuteMove(ref blockDataArray, borderLeft, posLeft, in s, smokeCheck)) return;
                if (CheckAndExecuteMove(ref blockDataArray, borderRight, posRight, in s, smokeCheck)) return;
            }
            else
            {
                if (CheckAndExecuteMove(ref blockDataArray, borderRightUp, posRightUp, in s, smokeCheck)) return;
                if (CheckAndExecuteMove(ref blockDataArray, borderLeftUp, posLeftUp, in s, smokeCheck)) return;
                if (CheckAndExecuteMove(ref blockDataArray, borderRight, posRight, in s, smokeCheck)) return;
                if (CheckAndExecuteMove(ref blockDataArray, borderLeft, posLeft, in s, smokeCheck)) return;
            }
        }
    }

    bool CheckAndExecuteMove(ref NativeArray<BlockData> blockDataArray, bool border, int2 posMove, in WorldSettings s, int checkWith = 2)
    {
        if (repeat < 100 && border && blockDataArray[GetPosInBlockArray(posMove, in s)].id <= checkWith)
        {
            Swap(ref blockDataArray, pos, posMove, in s);
            return true;
        }
        return false;
    }

    void Swap(
        ref NativeArray<BlockData> blockDataArray,
        int2 firstPos,
        int2 secondPos,
        in WorldSettings s)
    {
        int first = GetPosInBlockArray(firstPos, in s);
        int second = GetPosInBlockArray(secondPos, in s);

        BlockData a = blockDataArray[first];
        BlockData b = blockDataArray[second];

        int2 aOldPos = a.pos;

        a.posLast = aOldPos;
        a.pos = b.pos;
        a.lastMovedTick = s.tick;

        if (a.posLast.Equals(secondPos))
            a.repeat++;
        else
            a.repeat = 0;

        b.posLast = b.pos;
        b.pos = aOldPos;

        blockDataArray[first] = b;
        blockDataArray[second] = a;
    }

    public static int GetPosInBlockArray(int2 pos, in WorldSettings s)
    {
        int cs = s.chunkSize;
        int2 chunk = new int2(pos.x / cs, pos.y / cs);
        int inChunk = (pos.y - chunk.y * cs) * cs + (pos.x - chunk.x * cs);
        return (chunk.y * s.noChunks.x + chunk.x) * (cs * cs) + inChunk;
    }

    public static int GetPosInChunk(int2 pos, in WorldSettings s)
    {
        return (pos.y % s.chunkSize) * s.chunkSize + (pos.x % s.chunkSize);
    }

    public static int2 GetChunkPos(int2 pos, in WorldSettings s)
    {
        return new int2(pos.x / s.chunkSize, pos.y / s.chunkSize);
    }
}
