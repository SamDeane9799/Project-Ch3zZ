using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridPosition
{
    public int x;
    public int y;
    public int direction_X;
    public int direction_Y;

    public GridPosition(int _x, int _y, int directionX, int directionY)
    {
        x = _x;
        y = _y;
        direction_X = directionX;
        direction_Y = directionY;
    }

    public bool IsEqual(GridPosition pos)
    {
        return x == pos.x && y == pos.y;
    }
}
