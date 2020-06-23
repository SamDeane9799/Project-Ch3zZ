using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;

namespace Mirror
{
    public class TestPlayer : Player
    {
        public Character characterPrefab;

        //Start is called before the first frame update
        public override void Start()
        {
            p_Attributes = new Dictionary<ATTRIBUTES, short>();
            current_Mods = new CHARACTER_MODIFIER[19]; //Number of possible mods
            characterLevels = new short[53, 3]; //Number of characters and possible levels
            field_Units = new SyncListCharacter();
            bench_Units = new SyncListCharacter();
            benchZPosition = transform.position.z + 6;
        }

        //protected override void CmdBenchToField(Character unit)
        //{
        //    bench_Units.Remove(unit);
        //    field_Units.Add(unit);
        //    foreach (Character c in field_Units)
        //    {
        //        if (unit.name == c.name && c != unit)
        //        {
        //            return;
        //        }
        //    }
        //    foreach (ATTRIBUTES o in unit.attributes)
        //    {
        //        if (p_Attributes.ContainsKey(o)) p_Attributes[o]++;
        //        else p_Attributes.Add(o, 1);
        //        CheckAttributes(o);
        //    }
        //}
        //
        //protected override void CmdFieldToBench(Character unit)
        //{
        //    field_Units.Remove(unit);
        //    bench_Units.Add(unit);
        //    foreach (Character c in field_Units)
        //    {
        //        if (unit.name == c.name && unit != c)
        //        {
        //            return;
        //        }
        //    }
        //    foreach (ATTRIBUTES o in unit.attributes)
        //    {
        //        if (p_Attributes.ContainsKey(o)) p_Attributes[o]--;
        //        CheckAttributes(o);
        //        if (p_Attributes[o] == 0) p_Attributes.Remove(o);
        //    }
        //}
        
        public void SetupGrid()
        {
            grid = new GridSpace[GRID_WIDTH, GRID_HEIGHT];
            bench = new GridSpace[GRID_WIDTH];
            if (isServer) RpcCreateBoard();
            for (short i = 0; i < GRID_WIDTH; i++)
            {
                GameObject benchSpot = Instantiate<GameObject>(gridPrefab);
                benchSpot.transform.SetParent(transform);
                benchSpot.transform.SetPositionAndRotation(new Vector3(transform.position.x + i - 4, 0, transform.position.z + 6), Quaternion.Euler(Vector3.zero));
                NetworkServer.Spawn(benchSpot, connectionToClient);
                if (isServer) RpcSetBenchSpot(i, benchSpot);
                bench[i] = benchSpot.GetComponent<GridSpace>();
                bench[i].SetGridPosition(new Vector2(i, GRID_HEIGHT));
                for (short j = 0; j < GRID_HEIGHT; j++)
                {
                    GameObject gridSpot = Instantiate<GameObject>(gridPrefab);
                    gridSpot.transform.SetParent(transform);
                    gridSpot.transform.SetPositionAndRotation(new Vector3(transform.position.x + i - 4, 0, transform.position.z + 8 + (j % GRID_HEIGHT)), Quaternion.Euler(Vector3.zero));
                    NetworkServer.Spawn(gridSpot, connectionToClient);
                    if (isServer) RpcSetGridSpot(i, j, gridSpot);
                    grid[i, j] = gridSpot.GetComponent<GridSpace>();
                    grid[i, j].SetGridPosition(new Vector2(i, j));
                }
            }
            

            Character character = Instantiate<Character>(characterPrefab);
            character.transform.SetParent(transform);
            character.transform.rotation = Quaternion.Euler(Vector3.zero);
            NetworkServer.Spawn(character.gameObject);
            //CmdBenchToField(character);
            grid[7, 3].AddCharacter(character);
        }
    }
}
 