using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{
    public class CombatManager : NetworkBehaviour
    {
        // --- COMBAT DATA --- 
        private Player main_Player;
        private Player other_Player;
        private GridSpace[,] grid;
        protected const short GRID_WIDTH = 8;
        protected const short GRID_HEIGHT = 4;

        //Setup the grid for combat between the two players
        public void SetCombat(Player main, Player other)
        {
            main_Player = main;
            other_Player = other;
            main_Player.in_Combat = true;
            other_Player.in_Combat = true;
            for (short i = 0; i < GRID_HEIGHT; i++)
            {
                for (short j = 0; j < GRID_WIDTH; j++)
                {
                    grid[j, i] = main_Player.grid[j, i];
                    grid[j, i].SetGridPosition(new Vector2(j, i));
                }
            }
            for (short i = 0; i < GRID_HEIGHT; i++)
            {
                for (short j = 0; j < GRID_WIDTH; j++)
                {
                    grid[j, i + 4] = other_Player.grid[j, i];
                    grid[j, i + 4].SetGridPosition(new Vector2(j, i + 4));
                }
            }
        }

        // Start is called before the first frame update
        public CombatManager()
        {
            grid = new GridSpace[GRID_WIDTH, GRID_HEIGHT];
        }

        // Update is called once per frame
        public void Update()
        {
            SimulateCombat(main_Player.field_Units, other_Player.field_Units);
            SimulateCombat(other_Player.field_Units, main_Player.field_Units);
        }

        //Simulate combat between two players by determining the next action of their units
        private void SimulateCombat(SyncListCharacter fielded_Units, SyncListCharacter enemy_Units)
        {
            //Loop through the first player's units and determine what they should do 
            foreach (Character c in fielded_Units)
            {
                if (c.isActiveAndEnabled)
                {
                    if (c.target == null)
                    {
                        //Find the next target based on the relative distance between 
                        //the enemy and the character
                        float shortestDistance = float.MaxValue, distance = 0;
                        foreach (Character e in enemy_Units)
                        {
                            distance = Vector3.Distance(c.transform.position, e.transform.position);
                            if (distance < shortestDistance && e.health >= 0)
                            {
                                shortestDistance = distance;
                                c.target = e;
                            }
                        }
                    }
                    else
                    {
                        int current_Distance = (int)Vector2.Distance(c.grid_Position, c.target.grid_Position);
                        //Debug.Log(c.grid_Position + " " + c.target.grid_Position);

                        //Determine if a new path needs to be generated
                        c.Moving(current_Distance);
                        if (!c.isMoving && current_Distance > c.range)
                        {
                            FindTarget(c);
                        }
                        //If the character is in range, begin attacking
                        else if (current_Distance <= c.range)
                        {
                            c.CastUltimate();
                            c.Attack();
                            if (c.target.health <= 0)
                            {
                                grid[(int)c.target.grid_Position.x, (int)c.target.grid_Position.y].combat_Unit = null;
                                c.target.gameObject.SetActive(false);
                                c.target = null;
                            }
                        }
                    }
                }
            }
        }

        #region PATHFINDING
        //Target is the position of the enemy, desired is the 
        //desired position of the character
        private bool IsOptimal(Vector2 target, Vector2 desired, short range)
        {
            return Mathf.Abs(target.x - desired.x) <= range && Mathf.Abs(target.y - desired.y) <= range;
        }

        //Determine if the position is valid within the bounds of the grid
        private bool IsValid(int x, int y)
        {
            if (x >= 0 && x < GRID_WIDTH && y >= 0 && y < GRID_HEIGHT * 2)
                return grid[x, y].combat_Unit == null;
            return false;
        }

        //Calculate the heuristic value of the tile 
        private int CalculateHeuristic(Vector2 target, Vector2 desired)
        {
            return (int)(Mathf.Max(Mathf.Abs(target.x - desired.x), Mathf.Abs(target.y - desired.y)));
        }

        //Update the a* values of the gridspace
        private void UpdateSpace(int x, int y, float f, float g, float h, Vector2 parent)
        {
            grid[x, y].f = f;
            grid[x, y].g = g;
            grid[x, y].h = h;
            grid[x, y].parent_Position = parent;
        }

        //Starting from the determined target, find the path the character should
        //take by checking the parents of each tile in the grid
        private void CalculatePath(Character character, Vector2 target)
        {
            SyncStackGridSpace path = new SyncStackGridSpace();
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

            //Set the character's path and first tile to begin the pathfinding
            grid[(int)character.grid_Position.x, (int)character.grid_Position.y].combat_Unit = null;
            character.AcquirePath(path);
        }

        //Add an element to the open list, sorting it by position relative to its
        //f cost
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

        //Perform an A* search to determine an optimal path to an un-specific target
        //based on the character's range
        private void FindTarget(Character character)
        {
            //Reset the grid
            for (short i = 0; i < 8; i++)
            {
                for (short j = 0; j < 8; j++)
                {
                    grid[j, i].ResetCosts();
                }
            }

            //Initialize the open and closed lists
            List<GridSpace> openList = new List<GridSpace>();
            bool[,] closedList = new bool[8, 8];

            //Get the first space which is the space currently occupied by
            //the character
            GridSpace space = grid[(int)character.grid_Position.x, (int)character.grid_Position.y];
            space.f = (int)Vector2.Distance(character.grid_Position, character.target.grid_Position);
            space.g = 0;
            space.h = 0;
            space.parent_Position = space.grid_Position;
            openList.Add(space);

            while (openList.Count > 0)
            {
                //Remove the space from the openlist and 
                //add it to the closed list
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
                        h = CalculateHeuristic(character.target.grid_Position, grid[x, y - 1].grid_Position);
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
        #endregion
    }
}