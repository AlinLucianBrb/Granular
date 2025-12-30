using UnityEngine;
using Unity.Mathematics;

public class BlockPainter : MonoBehaviour
{
    [Header("Brush")]
    public int brushRadius = 2;          // in cells
    public int minRadius = 1;
    public int maxRadius = 30;
    public float scrollSensitivity = 10f; // bigger = faster size changes

    [Header("Selected Block (1-4)")]
    public byte paintId = (byte)Blocks.Sand; // default

    void HandleHotkeys()
    {
        // 1-4 block selection
        if (Input.GetKeyDown(KeyCode.Alpha1)) paintId = (byte)Blocks.Smoke;
        if (Input.GetKeyDown(KeyCode.Alpha2)) paintId = (byte)Blocks.Water;
        if (Input.GetKeyDown(KeyCode.Alpha3)) paintId = (byte)Blocks.Sand;
        if (Input.GetKeyDown(KeyCode.Alpha4)) paintId = (byte)Blocks.Stone;

        // Scroll wheel brush size
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0f)
        {
            int delta = Mathf.RoundToInt(scroll * scrollSensitivity);
            if (delta == 0) delta = scroll > 0 ? 1 : -1;

            brushRadius = Mathf.Clamp(brushRadius + delta, minRadius, maxRadius);
        }
    }

    public void Paint(in WorldSettings s)
    {
        HandleHotkeys();

        bool paint = Input.GetMouseButton(0);
        bool erase = Input.GetMouseButton(1);
        if (!paint && !erase) return;

        // screen -> world pixel (full-screen OnRenderImage assumption)
        int x = (int)(Input.mousePosition.x / Screen.width * s.width);
        int y = (int)(Input.mousePosition.y / Screen.height * s.height);

        x = math.clamp(x, 0, s.width - 1);
        y = math.clamp(y, 0, s.height - 1);

        byte idToWrite = erase ? (byte)Blocks.Clear : paintId;

        int r = brushRadius;
        int rr = r * r;

        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (dx * dx + dy * dy > rr) continue; // circle brush

                int px = x + dx;
                int py = y + dy;

                if ((uint)px >= (uint)s.width || (uint)py >= (uint)s.height)
                    continue;

                int2 p = new int2(px, py);
                int index = WorldPosToBlockIndex(p, in s);

                var b = World.instance.blocksData[index];

                b.id = idToWrite;
                b.posLast = b.pos;
                b.pos = p;              // keep consistent
                b.repeat = 0;
                b.lastMovedTick = 0;    // or s.tick if you want "no move this tick"

                World.instance.blocksData[index] = b;
            }
    }

    public static int WorldPosToBlockIndex(int2 pos, in WorldSettings s)
    {
        int cs = s.chunkSize;

        int2 chunk = new int2(pos.x / cs, pos.y / cs);
        int inChunk = (pos.y - chunk.y * cs) * cs + (pos.x - chunk.x * cs);

        return (chunk.y * s.noChunks.x + chunk.x) * (cs * cs) + inChunk;
    }
}
