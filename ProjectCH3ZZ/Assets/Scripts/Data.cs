using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Data
{
    public static List<short[]> rollChancesByLevel = new List<short[]>()
    {
        new short[5]{100, 0, 0, 0, 0 },
        new short[5]{75, 25, 0, 0, 0 },
        new short[5]{60, 30, 10, 0, 0 },
        new short[5]{40, 35, 20, 5, 0 },
        new short[5]{25, 35, 30, 10, 0 },
        new short[5]{19, 30, 35, 15, 1 },
        new short[5]{14, 20, 35, 25, 6 },
        new short[5]{10, 15, 25, 35, 15 }
    };
    public static List<short> requiredXP = new List<short>()
    {
        2, 6, 16, 20, 32, 50, 66
    };
    public static List<ITEMNAME> tierOneItems = new List<ITEMNAME>()
    {
        ITEMNAME.jaggedBlade,
        ITEMNAME.siphoner
    };
    public static short itemSpriteSideLength = 50;
}
