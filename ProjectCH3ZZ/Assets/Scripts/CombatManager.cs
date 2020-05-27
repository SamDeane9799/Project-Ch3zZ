using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    private Player main_Player;
    private Player other_Player;
    private GridSpace[,] grid;

    //Setup the grid for combat between the two players
    public void SetCombat(Player main, Player other)
    {
        main_Player = main;
        other_Player = other;
        for (short i = 0; i < 4; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                grid[j, i] = main_Player.grid[j, i];
                grid[j, i].SetGridPosition(new Vector2 ( j, i ));
            }
        }
        for (short i = 0; i < 4; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                grid[j, i + 4] = other_Player.grid[j, i];
                grid[j, i + 4].SetGridPosition(new Vector2(j, i + 4));
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        grid = new GridSpace[8, 8];
    }

    // Update is called once per frame
    void Update()
    {
        SimulateCombat(main_Player.field_Units, other_Player.field_Units);
        SimulateCombat(other_Player.field_Units, main_Player.field_Units);
    }

    private void SimulateCombat(List<Character> fielded_Units, List<Character> enemy_Units)
    {
        //Loop through the first player's units and determine what they should do 
        foreach (Character c in fielded_Units)
        {
            if (c.target == null)
            {
                float shortestDistance = 0;
                foreach (Character e in enemy_Units)
                {
                    float distance = Vector3.Distance(c.transform.position, e.transform.position);
                    if ( distance < shortestDistance )
                    {
                        shortestDistance = distance;
                        c.target = e;
                    }
                }
            }
            if ((int)Vector2.Distance(c.grid_Position, c.grid_Position) <= c.range)
            {
                c.CastUltimate();
                c.Attack();
            }
            else
            {
                if (!c.Moving())
                {
                    if (c.target == null) c.target_Position = FindTarget(c.grid_Position, c.target.grid_Position);
                    if (c.target != null)
                    {
                        GridSpace next = FindNextSpace(c.grid_Position, c.target.grid_Position);
                        if (next == null)
                        {
                            c.target = null;
                        }
                        else
                        {
                            c.next_Space = next;
                        }
                    }
                }
            }
        }
    }

    private Vector2 FindTarget(Vector2 occupied, Vector2 target)
    {
        return grid[0, 0].grid_Position;
    }

    private GridSpace FindNextSpace(Vector2 occupied, Vector2 target)
    {
        int o_X = (int)occupied.x;
        int o_Y = (int)occupied.y;
        int x = (int)(target.x - occupied.x);
        int y = (int)(target.y - occupied.y);
        x = (x / Mathf.Abs(x)) + o_X;
        y = (y / Mathf.Abs(y)) + o_Y;

        if (grid[x, y].combat_Unit != null)
        {
            //Look in the x direction
            if (grid[x, o_Y].combat_Unit == null)
            {
                y = o_Y;
            }
            //Look in the y direction
            else if (grid[o_X, y].combat_Unit == null)
            {
                x = o_X;
            }
            //Look in alternate y directions
            else if (o_X != x && o_Y == y)
            {
                if (y - 1 >= 0)
                {
                    if (grid[x, y - 1].combat_Unit == null) y--;
                }
                else if (y + 1 < 8)
                {
                    if (grid[x, y + 1].combat_Unit == null) y++;
                }
            }
            //Look in alternate x directions
            else if (o_Y != y && o_X == x)
            {
                if (y - 1 >= 0)
                {
                    if (grid[x - 1, y].combat_Unit == null) x--;
                }
                else if (y + 1 < 8)
                {
                    if (grid[x + 1, y].combat_Unit == null) x++;
                }
            }
            return null;
        }

        grid[o_X, o_Y].combat_Unit = null;
        return grid[x, y];
    }
}
