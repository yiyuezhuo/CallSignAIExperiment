using System;
using System.Collections.Generic;
using System.Linq;

namespace GameAlgorithms
{

// public class Node
// {
//     public int id;
//     public 
// }

public class SimpleGraph
{
    public int[] nodes; // Node id
    public int[][] neighbors; // Node id => neighbors

    public int[] GetDistanceField(int startNodeId)
    {
        var distArr = new int[nodes.Length];
        for(int i=0; i<nodes.Length; i++)
            distArr[i] = int.MaxValue;

        var activeList = new List<int>(){startNodeId};
        var nextActiveList = new List<int>();
        var n = 0;

        while(activeList.Count > 0)
        {
            foreach(var id in activeList)
            {
                if(distArr[id] < int.MaxValue)
                    continue;
                distArr[id] = n;
                foreach(var nei in neighbors[id])
                {
                    nextActiveList.Add(nei);
                }
            }
            activeList = nextActiveList;
            nextActiveList = new();
            
            n++;
        }
        return distArr;
    }

    public static void MinMerge(int[] main, int[] other)
    {
        for(int i=0; i<main.Length; i++)
            main[i] = Math.Min(main[i], other[i]);
    }

    public int[] ZerosField() => new int[nodes.Length];

    // public static void DistanceFieldToLinearDecayField
}


}