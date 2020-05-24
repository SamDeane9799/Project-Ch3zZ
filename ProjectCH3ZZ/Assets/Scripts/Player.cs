using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public GridSpace gridPrefab;
    public short level; //The player's current level, equivalent to the amount of units they can have
    public short gold; //The amount of gold a player currently has

    private Dictionary<ORIGIN, short> p_Origins;
    private Dictionary<CLASS, short> p_Classes;
    public CHARACTER_MODIFIER[] current_Mods;
    PLAYER_MODIFIER player_Mod;
    public List<GameObject> field_Units;
    private List<GameObject> bench_Units;

    private GridSpace[,] grid;
    private GridSpace[] bench;
    private List<GridSpace> occupied_Space;

    //Variables for moving various units around
    private GameObject unit_ToMove;
    private GridSpace previous_Space;
    private bool dragging_Unit;

    // Start is called before the first frame update
    void Start()
    {
        field_Units = new List<GameObject>();
        bench_Units = new List<GameObject>();
        current_Mods = new CHARACTER_MODIFIER[19]; //Number of possible mods

        grid = new GridSpace[8, 4];
        bench = new GridSpace[8];
        occupied_Space = new List<GridSpace>();

        for (short i = 0; i < 4; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                grid[j, i] = Instantiate<GridSpace>(gridPrefab, new Vector3(j - 3.5f, 5, i - 5f), Quaternion.identity);
            }
        }
        for (short i = 0; i < 8; i++)
        {
            bench[i] = Instantiate<GridSpace>(gridPrefab, new Vector3(i - 3.5f, 5, -6.5f), Quaternion.identity);
        }
    }

    void Update()
    {
        if (dragging_Unit)
        {
            RaycastHit hit;
            LayerMask mask = 1 << 8;
            Ray direction = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(direction, out hit, 1000, mask);
            unit_ToMove.transform.position = hit.point;
            if (Input.GetMouseButtonDown(0))
            {
                mask = 1 << 10;
                if (Physics.Raycast(direction, out hit, 1000, mask))
                {
                    GridSpace new_Spot = hit.transform.gameObject.GetComponent<GridSpace>();
                    if (new_Spot.unit == null)
                    {
                        new_Spot.AddCharacter(unit_ToMove);
                        occupied_Space.Add(new_Spot);
                        previous_Space.RemoveCharacter();
                        occupied_Space.Remove(previous_Space);
                        unit_ToMove = null;
                        previous_Space = null;
                        dragging_Unit = false;
                    }
                    else
                    {
                        GameObject previous_Unit = unit_ToMove;
                        unit_ToMove = new_Spot.unit;
                        new_Spot.AddCharacter(previous_Unit);
                        previous_Space.unit = unit_ToMove;
                    }
                }
                else
                {
                    previous_Space.ResetUnitPosition();
                    unit_ToMove = null;
                    previous_Space = null;
                    dragging_Unit = false;
                }
            }
        }

        //actually dont lmao IM ZOE BTW
        else if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            LayerMask mask = 1 << 9;
            Ray direction = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool clicked = Physics.Raycast(direction, out hit, 1000, mask);
            if (clicked)
            {
                Debug.Log("IM ZOE");
                for (short i = 0; i < occupied_Space.Count; i++)
                {
                    if (occupied_Space[i].transform.position == hit.transform.position)
                    {
                        dragging_Unit = true;
                        unit_ToMove = occupied_Space[i].unit;
                        previous_Space = occupied_Space[i];
                    }
                }
            }
        }
    }

    //Adding a unit to the bench from the shop
    public bool AddToBench(GameObject characterPrefab)
    {
        for (short i = 0; i < bench.Length; i++)
        {
            //Debug.Log(bench[i].unit);
            if (bench[i].unit == null)
            {
                GameObject character = Instantiate(characterPrefab, Vector3.zero, Quaternion.identity);
                bench[i].AddCharacter(character);
                occupied_Space.Add(bench[i]);
                return true;
            }
        }
        return false;
    }

    //When a unit is moved, evaluate the buffs on the current board and
    //change what the player has accordingly
    public void UnitMoved(GameObject unit)
    {
        Character u_Char = unit.GetComponent<Character>();
        if (bench_Units.Contains(unit))
        {
            bench_Units.Remove(unit);
            field_Units.Add(unit);
            foreach (ORIGIN o in u_Char.origins)
            {
                if (p_Origins.ContainsKey(o)) p_Origins[o]++;
                else p_Origins.Add(o, 1);
                CheckOrigin(o);
            }
            foreach (CLASS c in u_Char.classes)
            {
                if (p_Classes.ContainsKey(c)) p_Classes[c]++;
                else p_Classes.Add(c, 1);
                CheckClass(c);
            }
        }
        else
        {
            field_Units.Remove(unit);
            bench_Units.Add(unit);
            foreach (ORIGIN o in u_Char.origins)
            {
                if (p_Origins.ContainsKey(o)) p_Origins[o]--;
                CheckOrigin(o);
            }
            foreach (CLASS c in u_Char.classes)
            {
                if (p_Classes.ContainsKey(c)) p_Classes[c]--;
                CheckClass(c);
            }
        }
    }

    //Helper method to add any new modifiers to the list
    private void CheckOrigin(ORIGIN o)
    {
        //Determine what buffs need to be added
        switch (o)
        {
            case ORIGIN.BEAST:
                if (p_Origins[o] >= 6)
                {
                    current_Mods[11] = CHARACTER_MODIFIER.BEAST_3;
                }
                else if (p_Origins[o] >= 4)
                {
                    current_Mods[11] = CHARACTER_MODIFIER.BEAST_2;
                }
                else if (p_Origins[o] >= 2)
                {
                    current_Mods[11] = CHARACTER_MODIFIER.BEAST_1;
                }
                else 
                {
                    current_Mods[11] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ORIGIN.ELDRITCH:
                if (p_Origins[o] >= 4)
                {
                    current_Mods[12] = CHARACTER_MODIFIER.ELDRITCH_2;
                }
                else if (p_Origins[o] >= 2)
                {
                    current_Mods[12] = CHARACTER_MODIFIER.ELDRITCH_1;
                }
                else
                {
                    current_Mods[12] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ORIGIN.FELWALKER:
                if (p_Origins[o] >= 6)
                {
                    current_Mods[13] = CHARACTER_MODIFIER.FELWALKER_2;
                }
                else if (p_Origins[o] >= 3)
                {
                    current_Mods[13] = CHARACTER_MODIFIER.FELWALKER_1;
                }
                else
                {
                    current_Mods[13] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ORIGIN.HUMAN:
                if (p_Origins[o] >= 4)
                {
                    player_Mod = PLAYER_MODIFIER.HUMAN_2;
                }
                else if (p_Origins[o] >= 2)
                {
                    player_Mod = PLAYER_MODIFIER.HUMAN_1;
                }
                else
                {
                    player_Mod = PLAYER_MODIFIER.NULL;
                }
                break;
            case ORIGIN.INSECTOID:
                if (p_Origins[o] >= 4)
                {
                    current_Mods[14] = CHARACTER_MODIFIER.INSECTOID;
                }
                else
                {
                    current_Mods[14] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ORIGIN.NAUTICAL:
                if (p_Origins[o] >= 3)
                {
                    current_Mods[15] = CHARACTER_MODIFIER.NAUTICAL;
                }
                else
                {
                    current_Mods[15] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ORIGIN.VAMPIRE:
                if (p_Origins[o] >= 4)
                {
                    current_Mods[16] = CHARACTER_MODIFIER.VAMPIRE_2;
                }
                else if (p_Origins[o] >= 2)
                {
                    current_Mods[16] = CHARACTER_MODIFIER.VAMPIRE_1;
                }
                else
                {
                    current_Mods[16] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ORIGIN.WIGHT:
                if (p_Origins[o] >= 6)
                {
                    current_Mods[17] = CHARACTER_MODIFIER.WIGHT_3;
                }
                else if (p_Origins[o] >= 4)
                {
                    current_Mods[17] = CHARACTER_MODIFIER.WIGHT_2;
                }
                else if (p_Origins[o] >= 2)
                {
                    current_Mods[17] = CHARACTER_MODIFIER.WIGHT_1;
                }
                else
                {
                    current_Mods[17] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ORIGIN.WOODLAND:
                if (p_Origins[o] >= 4)
                {
                    current_Mods[18] = CHARACTER_MODIFIER.WOODLAND_2;
                }
                else if (p_Origins[o] >= 2)
                {
                    current_Mods[18] = CHARACTER_MODIFIER.WOODLAND_1;
                }
                else
                {
                    current_Mods[18] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ORIGIN.WYRM:
                if (p_Origins[o] >= 2)
                {
                    current_Mods[19] = CHARACTER_MODIFIER.WYRM;
                }
                else
                {
                    current_Mods[19] = CHARACTER_MODIFIER.NULL;
                }
                break;
        }
    }

    //Helper method to add any modifiers based on class
    private void CheckClass(CLASS c)
    {
        switch (c)
        {
            case CLASS.BLIGHTCRAFTER:
                if (p_Classes[c] >= 6)
                {
                    current_Mods[0] = CHARACTER_MODIFIER.BLIGHT_3;
                }
                else if (p_Classes[c] >= 4)
                {
                    current_Mods[0] = CHARACTER_MODIFIER.BLIGHT_2;
                }
                else if (p_Classes[c] >= 2)
                {
                    current_Mods[0] = CHARACTER_MODIFIER.BLIGHT_1;
                }
                else
                {
                    current_Mods[0] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case CLASS.BOUNTYHUNTER:
                if (p_Classes[c] >= 4)
                {
                    current_Mods[1] = CHARACTER_MODIFIER.BOUNTY_2;
                }
                else if (p_Classes[c] >= 2)
                {
                    current_Mods[1] = CHARACTER_MODIFIER.BOUNTY_1;
                }
                else
                {
                    current_Mods[1] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case CLASS.BULWARK:
                if (p_Classes[c] >= 3)
                {
                    current_Mods[2] = CHARACTER_MODIFIER.BULWARK;
                }
                else
                {
                    current_Mods[2] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case CLASS.FANATIC:
                if (p_Classes[c] >= 4)
                {
                    current_Mods[3] = CHARACTER_MODIFIER.FANATIC_2;
                }
                else if (p_Classes[c] >= 2)
                {
                    current_Mods[3] = CHARACTER_MODIFIER.FANATIC_1;
                }
                else
                {
                    current_Mods[3] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case CLASS.PHALANX:
                if (p_Classes[c] >= 6)
                {
                    current_Mods[4] = CHARACTER_MODIFIER.PHALANX_3;
                }
                else if (p_Classes[c] >= 4)
                {
                    current_Mods[4] = CHARACTER_MODIFIER.PHALANX_2;
                }
                else if (p_Classes[c] >= 2)
                {
                    current_Mods[4] = CHARACTER_MODIFIER.PHALANX_1;
                }
                else
                {
                    current_Mods[4] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case CLASS.SHADOWHAND:
                if (p_Classes[c] >= 6)
                {
                    current_Mods[5] = CHARACTER_MODIFIER.SHADOWHAND_3;
                }
                else if (p_Classes[c] >= 4)
                {
                    current_Mods[5] = CHARACTER_MODIFIER.SHADOWHAND_2;
                }
                else if (p_Classes[c] >= 2)
                {
                    current_Mods[5] = CHARACTER_MODIFIER.SHADOWHAND_1;
                }
                else
                {
                    current_Mods[5] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case CLASS.SILENCER:
                if (p_Classes[c] >= 1)
                {
                    current_Mods[6] = CHARACTER_MODIFIER.SILENCER;
                }
                else
                {
                    current_Mods[6] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case CLASS.SWARMLORD:
                if (p_Classes[c] >= 6)
                {
                    current_Mods[7] = CHARACTER_MODIFIER.SWARMLORD_3;
                }
                else if (p_Classes[c] >= 4)
                {
                    current_Mods[7] = CHARACTER_MODIFIER.SWARMLORD_2;
                }
                else if (p_Classes[c] >= 2)
                {
                    current_Mods[7] = CHARACTER_MODIFIER.SWARMLORD_1;
                }
                else
                {
                    current_Mods[7] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case CLASS.WARLOCK:
                if (p_Classes[c] >= 4)
                {
                    current_Mods[8] = CHARACTER_MODIFIER.WARLOCK_2;
                }
                else if (p_Classes[c] >= 2)
                {
                    current_Mods[8] = CHARACTER_MODIFIER.WARLOCK_1;
                }
                else
                {
                    current_Mods[8] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case CLASS.WITCHDOCTOR:
                if (p_Classes[c] >= 4)
                {
                    current_Mods[9] = CHARACTER_MODIFIER.WITCHDOCTOR_2;
                }
                else if (p_Classes[c] >= 2)
                {
                    current_Mods[9] = CHARACTER_MODIFIER.WITCHDOCTOR_1;
                }
                else
                {
                    current_Mods[9] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case CLASS.ZEALOT:
                if (p_Classes[c] >= 4)
                {
                    current_Mods[10] = CHARACTER_MODIFIER.ZEALOT;
                }
                else
                {
                    current_Mods[10] = CHARACTER_MODIFIER.NULL;
                }
                break;
        }
    }
}
