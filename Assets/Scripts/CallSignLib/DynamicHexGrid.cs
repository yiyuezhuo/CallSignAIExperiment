using System.Collections.Generic;

namespace CallSignLib
{
public class DynamicHexGrid
{
    public int height = 5;
    public int width = 6;
    public HashSet<(int, int)> excludeSet = new(){
        (1, 4), (2,4), (3,4), (5,4)
    };

    // static List<(int, int)> oddOffsets = new()
    static List<(int, int)> evenOffsets = new()
    {
        (0, -1), (1, -1), (1, 0), (0, 1), (-1, 0), (-1, -1) 
    };

    // static List<(int, int)> evenOffsets = new()
    static List<(int, int)> oddOffsets = new()
    {
        (0, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0)
    };

    public IEnumerable<(int, int)> GetNeighbors(int x, int y)
    {
        var even = x %2 == 0;
        var offsets = even ? evenOffsets : oddOffsets;
        foreach(var offset in offsets)
        {
            var newX = x + offset.Item1;
            var newY = y + offset.Item2;
            if(IsInBound(newX, newY))
                yield return (newX, newY);
        }
    }

    public bool IsInBound(int x, int y)
    {
        if(x < 0 || y < 0 || x >= width || y >= height)
            return false;
        if(excludeSet.Contains((x,y)))
            return false;
        return true;
    }
}

}