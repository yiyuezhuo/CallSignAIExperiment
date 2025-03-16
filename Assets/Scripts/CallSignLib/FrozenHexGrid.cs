using System;
using System.Collections.Generic;

namespace CallSignLib
{
    public class FrozenHexGrid
{
    public class Hex
    {
        public int x; // odd-q (shove odd columns by 1/2 row) col
        public int y; // ... row
        public List<Hex> neighbors = new();
        public (int, int, int) toCube()
        {
            var q = x;
            var r = y - (x - x % 2) / 2;
            var s = -q - r;
            return (q, r, s);
        }
        public int Distance(Hex other)
        {
            (var q1, var r1, var s1) = toCube();
            (var q2, var r2, var s2) = other.toCube();
            return (Math.Abs(q1-q2) + Math.Abs(r1-r2) + Math.Abs(s1-s2)) / 2;
        }
    }

    public Dictionary<(int, int), Hex> hexMap = new();

    public static FrozenHexGrid Make(DynamicHexGrid dynamicGrid)
    {
        var hexMap = new Dictionary<(int, int), Hex>();
        for(var x=0; x<dynamicGrid.width; x++)
        {
            for(var y=0; y<dynamicGrid.height; y++)
            {
                if(!dynamicGrid.excludeSet.Contains((x,y)))
                {
                    hexMap[(x, y)] = new Hex(){x=x, y=y};
                }
            }
        }
        foreach(var hex in hexMap.Values)
        {
            foreach(var nei in dynamicGrid.GetNeighbors(hex.x, hex.y))
            {
                hex.neighbors.Add(hexMap[nei]);
            }
        }
        return new(){hexMap=hexMap};
    }

    public static FrozenHexGrid Make() => Make(new());
}

}