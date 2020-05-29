using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public Player main_Player;
    public Player other_Player;
    private GridSpace[,] grid;

    float combat_Timer;

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
        SetCombat(main_Player, other_Player);
    }

    // Update is called once per frame
    void Update()
    {
        if (combat_Timer >= 5)
        {
            main_Player.in_Combat = true;
            other_Player.in_Combat = true;
            SimulateCombat(main_Player.field_Units, other_Player.field_Units);
        }
        combat_Timer += Time.deltaTime;
        //SimulateCombat(other_Player.field_Units, main_Player.field_Units);
    }

    private void SimulateCombat(List<Character> fielded_Units, List<Character> enemy_Units)
    {
        //Loop through the first player's units and determine what they should do 
        foreach (Character c in fielded_Units)
        {
            if (c.target == null)
            {
                float shortestDistance = float.MaxValue;
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
            if (!c.Moving() && Vector2.Distance(c.grid_Position, c.target.grid_Position) > c.range)
            {
                FindTarget(c);
            }
            else
            {
                c.CastUltimate();
                c.Attack();
            }
        }
    }

    //Target is the position of the enemy, desired is the 
    //desired position of the character
    private bool IsOptimal(Vector2 target, Vector2 desired, short range)
    {
        return Mathf.Abs(target.x - desired.x) <= range && Mathf.Abs(target.y - desired.y) <= range;
    }

    private bool IsValid(int x, int y)
    {
        if (x >= 0 && x < 8 && y >= 0 && y < 8)
            return grid[x, y].combat_Unit == null;
        return false;
    }

    private int CalculateHeuristic(Vector2 target, Vector2 desired)
    {
        return (int)(Mathf.Max(Mathf.Abs(target.x - desired.x), Mathf.Abs(target.y - desired.y)));
    }

    private void UpdateSpace(int x, int y, float f, float g, float h, Vector2 parent)
    {
        grid[x, y].f = f;
        grid[x, y].g = g;
        grid[x, y].h = h;
        grid[x, y].parent_Position = parent;
    }

    private void CalculatePath(Character character, Vector2 target)
    {
        Stack<GridSpace> path = new Stack<GridSpace>();
        int x = (int)target.x;
        int y = (int)target.y;

        while (!(grid[x, y].parent_Position.x == x && grid[x, y].parent_Position.y == y))
        {
            path.Push(grid[x, y]);
            int t_X = (int)grid[x, y].parent_Position.x;
            int t_Y = (int)grid[x, y].parent_Position.y;
            x = t_X;
            y = t_Y;
        }

        grid[(int)character.grid_Position.x, (int)character.grid_Position.y].combat_Unit = null;
        character.next_Space = path.Pop();
        character.next_Space.AddCombatCharacter(character);
        character.path = path;
    }

    private void AddElement(List<GridSpace> openList, GridSpace element)
    {
        for (int i = 0; i < openList.Count; i++)
        {
            if (element.f < openList[i].f)
            {
                openList.Insert(i, element);
                return;
            }
        }
        openList.Add(element);
    }

    private void FindTarget(Character character)
    {
        for (short i = 0; i < 8; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                grid[j, i].ResetCosts();
            }
        }

        List<GridSpace> openList = new List<GridSpace>();
        bool[,] closedList = new bool[8,8];

        GridSpace space = grid[(int)character.grid_Position.x, (int)character.grid_Position.y];
        space.f = (int)Vector2.Distance(character.grid_Position, character.target.grid_Position);
        space.g = 0;
        space.h = 0;
        space.parent_Position = space.grid_Position;
        openList.Add(space);

        while(openList.Count > 0)
        {
            space = openList[0];
            openList.RemoveAt(0);
            int x = (int)space.grid_Position.x;
            int y = (int)space.grid_Position.y;
            closedList[x, y] = true;

            float g, f, h;

            //WEST SUCCESSOR
            if (IsValid(x - 1, y))
            {
                if (IsOptimal(character.target.grid_Position, grid[x - 1, y].grid_Position, character.range))
                {
                    grid[x - 1, y].parent_Position = space.grid_Position;
                    CalculatePath(character, new Vector2(x - 1, y));
                    return;
                }
                if (!closedList[x - 1, y])
                {
                    g = grid[x, y].g + 1;
                    h = CalculateHeuristic(character.target.grid_Position, grid[x - 1, y].grid_Position);
                    f = g + h;

                    if (grid[x - 1, y].f > f)
                    {
                        UpdateSpace(x - 1, y, f, g, h, space.grid_Position);
                        AddElement(openList, grid[x - 1, y]);
                    }
                }
            }
            //EAST SUCCESSOR
            if (IsValid(x + 1, y))
            {
                if (IsOptimal(character.target.grid_Position, grid[x + 1, y].grid_Position, character.range))
                {
                    grid[x + 1, y].parent_Position = space.grid_Position;
                    CalculatePath(character, new Vector2(x + 1, y));
                    return;
                }
                if (!closedList[x + 1, y])
                {
                    g = grid[x, y].g + 1;
                    h = CalculateHeuristic(character.target.grid_Position, grid[x + 1, y].grid_Position);
                    f = g + h;

                    if (grid[x + 1, y].f > f)
                    {
                        UpdateSpace(x + 1, y, f, g, h, space.grid_Position);
                        AddElement(openList, grid[x + 1, y]);
                    }
                }
            }
            //NORTH SUCCESSOR
            if (IsValid(x, y + 1))
            {
                if (IsOptimal(character.target.grid_Position, grid[x, y + 1].grid_Position, character.range))
                {
                    grid[x, y + 1].parent_Position = space.grid_Position;
                    CalculatePath(character, new Vector2(x, y + 1));
                    return;
                }
                if (!closedList[x, y + 1])
                {
                    g = grid[x, y].g + 1;
                    h = CalculateHeuristic(character.target.grid_Position, grid[x, y + 1].grid_Position);
                    f = g + h;

                    if (grid[x, y + 1].f > f)
                    {
                        UpdateSpace(x, y + 1, f, g, h, space.grid_Position);
                        AddElement(openList, grid[x, y + 1]);
                    }
                }
            }
            //SOUTH SUCCESSOR
            if (IsValid(x, y - 1))
            {
                if (IsOptimal(character.target.grid_Position, grid[x, y - 1].grid_Position, character.range))
                {
                    grid[x, y - 1].parent_Position = space.grid_Position;
                    CalculatePath(character, new Vector2(x, y - 1));
                    return;
                }
                if (!closedList[x, y - 1])
                {
                    g = grid[x, y].g + 1;
                    h = CalculateHeuristic(character.target.grid_Position, grid[x - 1, y].grid_Position);
                    f = g + h;

                    if (grid[x, y - 1].f > f)
                    {
                        UpdateSpace(x, y - 1, f, g, h, space.grid_Position);
                        AddElement(openList, grid[x, y - 1]);
                    }
                }
            }
            //NORTHEAST SUCCESSOR
            if (IsValid(x + 1, y + 1))
            {
                if (IsOptimal(character.target.grid_Position, grid[x + 1, y + 1].grid_Position, character.range))
                {
                    grid[x + 1, y + 1].parent_Position = space.grid_Position;
                    CalculatePath(character, new Vector2(x + 1, y + 1));
                    return;
                }
                if (!closedList[x + 1, y + 1])
                {
                    g = grid[x, y].g + 1.414f;
                    h = CalculateHeuristic(character.target.grid_Position, grid[x + 1, y + 1].grid_Position);
                    f = g + h;

                    if (grid[x + 1, y + 1].f > f)
                    {
                        UpdateSpace(x + 1, y + 1, f, g, h, space.grid_Position);
                        AddElement(openList, grid[x + 1, y + 1]);
                    }
                }
            }
            //NORTHWEST SUCCESSOR
            if (IsValid(x - 1, y + 1))
            {
                if (IsOptimal(character.target.grid_Position, grid[x - 1, y + 1].grid_Position, character.range))
                {
                    grid[x - 1, y + 1].parent_Position = space.grid_Position;
                    CalculatePath(character, new Vector2(x - 1, y + 1));
                    return;
                }
                if (!closedList[x - 1, y + 1])
                {
                    g = grid[x, y].g + 1.414f;
                    h = CalculateHeuristic(character.target.grid_Position, grid[x - 1, y + 1].grid_Position);
                    f = g + h;

                    if (grid[x - 1, y + 1].f > f)
                    {
                        UpdateSpace(x - 1, y + 1, f, g, h, space.grid_Position);
                        AddElement(openList, grid[x - 1, y + 1]);
                    }
                }
            }
            //SOUTHEAST SUCCESSOR
            if (IsValid(x + 1, y - 1))
            {
                if (IsOptimal(character.target.grid_Position, grid[x + 1, y - 1].grid_Position, character.range))
                {
                    grid[x + 1, y - 1].parent_Position = space.grid_Position;
                    CalculatePath(character, new Vector2(x + 1, y - 1));
                    return;
                }
                if (!closedList[x + 1, y - 1])
                {
                    g = grid[x, y].g + 1.414f;
                    h = CalculateHeuristic(character.target.grid_Position, grid[x + 1, y - 1].grid_Position);
                    f = g + h;

                    if (grid[x + 1, y - 1].f > f)
                    {
                        UpdateSpace(x + 1, y - 1, f, g, h, space.grid_Position);
                        AddElement(openList, grid[x + 1, y - 1]);
                    }
                }
            }
            //SOUTHWEST SUCCESSOR
            if (IsValid(x - 1, y - 1))
            {
                if (IsOptimal(character.target.grid_Position, grid[x - 1, y - 1].grid_Position, character.range))
                {
                    grid[x - 1, y - 1].parent_Position = space.grid_Position;
                    CalculatePath(character, new Vector2(x - 1, y - 1));
                    return;
                }
                if (!closedList[x - 1, y - 1])
                {
                    g = grid[x, y].g + 1.414f;
                    h = CalculateHeuristic(character.target.grid_Position, grid[x - 1, y - 1].grid_Position);
                    f = g + h;

                    if (grid[x - 1, y - 1].f > f)
                    {
                        UpdateSpace(x - 1, y - 1, f, g, h, space.grid_Position);
                        AddElement(openList, grid[x - 1, y - 1]);
                    }
                }
            }

        }
    }
}
