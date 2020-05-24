using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Data
{
    public static List<int[]> rollChancesByLevel = new List<int[]>()
    {
        new int[5]{100, 0, 0, 0, 0 },
        new int[5]{75, 25, 0, 0, 0 },
        new int[5]{60, 30, 10, 0, 0 },
        new int[5]{40, 35, 20, 5, 0 },
        new int[5]{25, 35, 30, 10, 0 },
        new int[5]{19, 30, 35, 15, 1 },
        new int[5]{14, 20, 35, 25, 6 },
        new int[5]{10, 15, 25, 35, 15 }
    };
    public static List<int> requiredXP = new List<int>()
    {
        2, 6, 16, 20, 32, 50, 66
    };
}
