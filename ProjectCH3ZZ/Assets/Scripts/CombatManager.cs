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
                    if (c.target_Position.x < 0) c.target_Position = FindTarget(c, c.target.grid_Position, c.range);
                    if (c.target_Position.x > 0)
                    {
                        GridSpace next = FindNextSpace(c.grid_Position, c.target.grid_Position);
                        if (next == null)
                        {
                            c.ResetTargetPosition();
                        }
                        else
                        {
                            c.next_Space = next;
                            c.visited_Spaces.Enqueue(next);
                            next.AddCharacter(c);
                        }
                    }
                }
            }
        }
    }

    private Vector2 FindTarget(Character character, Vector2 target, short range)
    {
        int o_X = (int)character.grid_Position.x;
        int o_Y = (int)character.grid_Position.y;
        int x = (int)(target.x - character.grid_Position.x);
        int y = (int)(target.y - character.grid_Position.y);
        if (x < 0) x += range;
        else if (x > 0) x -= range;
        if (y < 0) y += range;
        else if (y > 0) y -= range;

        if (grid[x + o_X, y + o_Y].combat_Unit != null)
        {
            GridPosition position_One = null;
            GridPosition position_Two = null;
            //Test in the y direction
            if (IsOptimal(target, new Vector2(o_X + x, o_Y + y - 1), range))
            {
                if (grid[o_X + x, o_Y + y - 1].combat_Unit == null)
                {
                    return grid[o_X + x, o_Y + y - 1].grid_Position;
                }
                position_One = new GridPosition(o_X + x, o_Y + y - 1, 0, 1);
            }
            if (IsOptimal(target, new Vector2(o_X + x, o_Y + y + 1), range))
            {
                if (grid[o_X + x, o_Y + y + 1].combat_Unit == null)
                {
                    return grid[o_X + x, o_Y + y + 1].grid_Position;
                }
                if (position_One.x > 0)
                    position_One = new GridPosition(o_X + x, o_Y + y + 1, 0, -1);
                position_Two = new GridPosition(o_X + x, o_Y + y + 1, 0, -1);
            }
            //Test in the x direction
            if (IsOptimal(target, new Vector2(o_X + x - 1, o_Y + y), range))
            {
                if (grid[o_X + x - 1, o_Y + y].combat_Unit == null)
                {
                    return grid[o_X + x - 1, o_Y + y].grid_Position;
                }
                if (position_One.x > 0)
                    position_One = new GridPosition(o_X + x - 1, o_Y + y, 1, 0);
                position_Two = new GridPosition(o_X + x - 1, o_Y + y, 1, 0);
            }
            if (IsOptimal(target, new Vector2(o_X + x + 1, o_Y + y), range))
            {
                if (grid[o_X + x + 1, o_Y + y].combat_Unit == null)
                {
                    return grid[o_X + x + 1, o_Y + y].grid_Position;
                }
                if (position_One.x > 0)
                    position_One = new GridPosition(o_X + x + 1, o_Y + y, -1, 0);
                position_Two = new GridPosition(o_X + x + 1, o_Y + y, -1, 0);
            }

            while (!position_One.IsEqual(position_Two))
            {
                Vector2 possible_Space = GetNextOptimalSpace(position_One, target, range);
                if (possible_Space.x >= 0) return possible_Space;
                possible_Space = GetNextOptimalSpace(position_Two, target, range);
                if (possible_Space.x >= 0) return possible_Space;
            }
        }

        return grid[x + o_X, y + o_Y].grid_Position;
    }

    //Target is the position of the enemy, desired is the 
    //desired position of the character
    private bool IsOptimal(Vector2 target, Vector2 desired, short range)
    {
        return Mathf.Abs(target.x - desired.x) <= range && Mathf.Abs(target.y - desired.y) <= range;
    }

    private Vector2 GetNextOptimalSpace(GridPosition pos, Vector2 target, short range)
    {
        if (IsOptimal(target, new Vector2(pos.x, pos.y - 1), range) && -1 != pos.direction_Y)
        {
            if (grid[pos.x, pos.y - 1].combat_Unit == null)
            {
                return grid[pos.x, pos.y - 1].grid_Position;
            }
            pos = new GridPosition(pos.x, pos.y - 1, 0, 1);
        }
        if (IsOptimal(target, new Vector2(pos.x, pos.y + 1), range) && 1 != pos.direction_Y)
        {
            if (grid[pos.x, pos.y + 1].combat_Unit == null)
            {
                return grid[pos.x, pos.y + 1].grid_Position;
            }
            pos = new GridPosition(pos.x, pos.y + 1, 0, -1);
        }
        //Test in the x direction
        if (IsOptimal(target, new Vector2(pos.x - 1, pos.y), range) && -1 != pos.direction_X)
        {
            if (grid[pos.x - 1, pos.y].combat_Unit == null)
            {
                return grid[pos.x - 1, pos.y].grid_Position;
            }
            pos = new GridPosition(pos.x - 1, pos.y, 1, 0);
        }
        if (IsOptimal(target, new Vector2(pos.x + 1, pos.y), range) && 1 != pos.direction_X)
        {
            if (grid[pos.x + 1, pos.y].combat_Unit == null)
            {
                return grid[pos.x + 1, pos.y].grid_Position;
            }
            pos = new GridPosition(pos.x + 1, pos.y, -1, 0);
        }
        return new Vector2(-1, -1);
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
            else
            {
                return null;
            }
        }

        grid[o_X, o_Y].combat_Unit = null;
        return grid[x, y];
    }
}
