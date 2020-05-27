using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;

public class Player : MonoBehaviour
{
    public GridSpace gridPrefab;
    public short level; //The player's current level, equivalent to the amount of units they can have
    public short gold; //The amount of gold a player currently has
    public short xp;

    private Dictionary<ATTRIBUTES, short> p_Attributes;
    private Text synergiesText;
    private Canvas playerCanvas;
    public CHARACTER_MODIFIER[] current_Mods;
    PLAYER_MODIFIER player_Mod;
    public List<Character> field_Units;
    public List<Character> bench_Units;
    public List<Item> items;
    private Vector3 previousItemSpot;
    private Shop playerShop;

    public GridSpace[,] grid;
    public GridSpace[] bench;
    public List<GridSpace> occupied_Space;

    //Variables for moving various units around
    private GameObject unit_ToMove;
    private GridSpace previous_Space;
    private bool dragging_Unit;
    private GraphicRaycaster m_Raycaster;
    private PointerEventData m_PointerEventData;
    private List<RaycastResult> ui_Results;
    private EventSystem m_Eventsystem;

    // Start is called before the first frame update
    void Start()
    {
        p_Attributes = new Dictionary<ATTRIBUTES, short>();
        synergiesText = GameObject.Find("synergiesText").GetComponent<Text>();
        field_Units = new List<Character>();
        bench_Units = new List<Character>();
        playerCanvas = GameObject.Find("ShopCanvas").GetComponent<Canvas>();
        playerShop = playerCanvas.GetComponent<Shop>();
        m_Raycaster = playerCanvas.GetComponent<GraphicRaycaster>();
        ui_Results = new List<RaycastResult>();
        m_Eventsystem = GetComponent<EventSystem>();
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
                if (!unit_ToMove.GetComponent<Item>())
                {
                    Character character = unit_ToMove.GetComponent<Character>();
                    mask = 1 << 10;
                    m_PointerEventData = new PointerEventData(m_Eventsystem);
                    m_PointerEventData.position = Input.mousePosition;
                    ui_Results.Clear();
                    m_Raycaster.Raycast(m_PointerEventData, ui_Results);
                    if (ui_Results.Count != 0 && !unit_ToMove.GetComponent<Item>() && ui_Results[0].gameObject.name == "ShopBackground")
                    {
                        //Adding units cost to players gold
                        gold += unit_ToMove.GetComponent<Character>().gold_Cost;
                        if (bench_Units.Contains(character))
                        {
                            bench_Units.Remove(character);
                        }
                        else
                        {
                            FieldToBench(character);
                        }
                        Destroy(unit_ToMove.gameObject);
                        ResetHeldUnit();
                    }
                    else if (Physics.Raycast(direction, out hit, 1000, mask))
                    {
                        GridSpace new_Spot = hit.transform.gameObject.GetComponent<GridSpace>();
                        if (new_Spot.unit == null)
                        {
                            new_Spot.AddCharacter(character);
                            occupied_Space.Add(new_Spot);
                            previous_Space.RemoveCharacter();
                            occupied_Space.Remove(previous_Space);
                            if (previous_Space.transform.position.z <= -6.5f && new_Spot.transform.position.z > -6.5f)
                            {
                                BenchToField(character);
                            }
                            else if (previous_Space.transform.position.z > -6.5f && new_Spot.transform.position.z <= -6.5f)
                            {
                                FieldToBench(character);
                            }

                            ResetHeldUnit();
                        }
                        else if (new_Spot != previous_Space)
                        {
                            Character previous_Unit = character;
                            character = new_Spot.unit;

                            if (previous_Space.transform.position.z <= -6.5f && new_Spot.transform.position.z > -6.5f)
                            {
                                FieldToBench(character);
                                BenchToField(previous_Unit);
                            }
                            else if (previous_Space.transform.position.z > -6.5f && new_Spot.transform.position.z <= -6.5f)
                            {
                                BenchToField(character);
                                FieldToBench(previous_Unit);
                            }

                            new_Spot.AddCharacter(previous_Unit);
                            previous_Space.AddCharacter(character);

                            ResetHeldUnit();
                        }
                        else
                        {
                            previous_Space.ResetUnitPosition();
                            ResetHeldUnit();
                        }
                    }

                    else
                    {
                            previous_Space.ResetUnitPosition();
                            ResetHeldUnit();
                    }
                }
                else
                {
                    mask = 1 << 9;
                    bool unitClicked = Physics.Raycast(direction, out hit, 1000, mask);
                    if(unitClicked)
                    {
                        Character clickedChar = hit.transform.gameObject.GetComponent<Character>();
                        //Add item to characters list if there is room
                        RectTransform itemRectTransform = unit_ToMove.GetComponent<RectTransform>();
                        unit_ToMove.transform.SetParent(clickedChar.transform.GetChild(0));
                        itemRectTransform.localScale = new Vector3(.75f, .75f);
                        itemRectTransform.anchoredPosition3D = new Vector3(Data.itemSpriteSideLength/2 - 5, -Data.itemSpriteSideLength + 5, 0);
                        ResetHeldUnit();
                    }
                    else
                    {
                        unit_ToMove.transform.position = previousItemSpot;
                        ResetHeldUnit();
                    }
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

            m_PointerEventData = new PointerEventData(m_Eventsystem);
            m_PointerEventData.position = Input.mousePosition;
            ui_Results.Clear();
            m_Raycaster.Raycast(m_PointerEventData, ui_Results);
            GameObject objectReturned = ui_Results[0].gameObject;
            
            if (clicked)
            {
                for (short i = 0; i < occupied_Space.Count; i++)
                {
                    if (occupied_Space[i].transform.position == hit.transform.position)
                    {
                        dragging_Unit = true;
                        unit_ToMove = occupied_Space[i].unit.gameObject;
                        previous_Space = occupied_Space[i];
                    }
                }
            }
            else if (ui_Results.Count != 0 && ui_Results[0].gameObject.GetComponent<Item>())
            {
                unit_ToMove = ui_Results[0].gameObject;
                Item itemToMove = unit_ToMove.GetComponent<Item>();
                if (!items.Contains(itemToMove))
                {
                    AddItem(itemToMove);
                    playerShop.RemoveItemFromChoice(itemToMove);
                    playerShop.ClearItems();
                }
                else
                {
                    unit_ToMove = itemToMove.gameObject;
                    previousItemSpot = unit_ToMove.transform.position;
                    dragging_Unit = true;
                }
            }
        }
    }

    //Adding a unit to the bench from the shop
    public bool AddToBench(Character characterPrefab)
    {
        for (short i = 0; i < bench.Length; i++)
        {
            if (bench[i].unit == null)
            {
                Character character = Instantiate<Character>(characterPrefab, Vector3.zero, Quaternion.identity);
                bench[i].AddCharacter(character);
                occupied_Space.Add(bench[i]);
                return true;
            }
        }
        return false;
    }

    private void ResetHeldUnit()
    {
        unit_ToMove = null;
        previous_Space = null;
        dragging_Unit = false;
    }

    //When a unit is moved, evaluate the buffs on the current board and
    //change what the player has accordingly
    private void BenchToField(Character unit)
    {
        bench_Units.Remove(unit);
        field_Units.Add(unit);
        foreach (Character c in field_Units)
        {
            if (unit.name == c.name && c != unit)
            {
                return;
            }
        }
        foreach (ATTRIBUTES o in unit.attributes)
        {
            if (p_Attributes.ContainsKey(o)) p_Attributes[o]++;
            else p_Attributes.Add(o, 1);
            CheckAttributes(o);
        }
        SetText();
        //foreach (CHARACTER_MODIFIER mod in current_Mods)
        //{
        //    if (mod != CHARACTER_MODIFIER.NULL)
        //        Debug.Log(mod);
        //}
    }

    private void FieldToBench(Character unit)
    {
        field_Units.Remove(unit);
        bench_Units.Add(unit);
        foreach (Character c in field_Units)
        {
            if (unit.name == c.name && unit != c)
            {
                return;
            }
        }
        foreach (ATTRIBUTES o in unit.attributes)
        {
            if (p_Attributes.ContainsKey(o)) p_Attributes[o]--;
            CheckAttributes(o);
            if (p_Attributes[o] == 0) p_Attributes.Remove(o);
        }
        SetText();
        //foreach (CHARACTER_MODIFIER mod in current_Mods)
        //{
        //    if (mod != CHARACTER_MODIFIER.NULL)
        //        Debug.Log(mod);
        //}
    }

    private void AddItem(Item itemToAdd)
    {
        items.Add(itemToAdd);
        RectTransform newItem = itemToAdd.GetComponent<RectTransform>();
        newItem.SetParent(playerCanvas.transform.GetChild(4).transform);
        for (int i = 0; i < items.Count; i++)
        {
            newItem.anchorMin = new Vector2(0, 1);
            newItem.anchorMax = new Vector2(0, 1);
            newItem.anchoredPosition3D = new Vector3((i % 3) * Data.itemSpriteSideLength + (i % 3 * 5), -(Data.itemSpriteSideLength + 5) * (items.Count / 4) - 25, 0);
        }
    }

    //Helper method to add any new modifiers to the list
    private void CheckAttributes(ATTRIBUTES o)
    {
        //Determine what buffs need to be added
        switch (o)
        {
            case ATTRIBUTES.BEAST:
                if (p_Attributes[o] >= 6)
                {
                    current_Mods[11] = CHARACTER_MODIFIER.BEAST_3;
                }
                else if (p_Attributes[o] >= 4)
                {
                    current_Mods[11] = CHARACTER_MODIFIER.BEAST_2;
                }
                else if (p_Attributes[o] >= 2)
                {
                    current_Mods[11] = CHARACTER_MODIFIER.BEAST_1;
                }
                else 
                {
                    current_Mods[11] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.ELDRITCH:
                if (p_Attributes[o] >= 4)
                {
                    current_Mods[12] = CHARACTER_MODIFIER.ELDRITCH_2;
                }
                else if (p_Attributes[o] >= 2)
                {
                    current_Mods[12] = CHARACTER_MODIFIER.ELDRITCH_1;
                }
                else
                {
                    current_Mods[12] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.FELWALKER:
                if (p_Attributes[o] >= 6)
                {
                    current_Mods[13] = CHARACTER_MODIFIER.FELWALKER_2;
                }
                else if (p_Attributes[o] >= 3)
                {
                    current_Mods[13] = CHARACTER_MODIFIER.FELWALKER_1;
                }
                else
                {
                    current_Mods[13] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.HUMAN:
                if (p_Attributes[o] >= 4)
                {
                    player_Mod = PLAYER_MODIFIER.HUMAN_2;
                }
                else if (p_Attributes[o] >= 2)
                {
                    player_Mod = PLAYER_MODIFIER.HUMAN_1;
                }
                else
                {
                    player_Mod = PLAYER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.INSECTOID:
                if (p_Attributes[o] >= 4)
                {
                    current_Mods[14] = CHARACTER_MODIFIER.INSECTOID;
                }
                else
                {
                    current_Mods[14] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.NAUTICAL:
                if (p_Attributes[o] >= 3)
                {
                    current_Mods[15] = CHARACTER_MODIFIER.NAUTICAL;
                }
                else
                {
                    current_Mods[15] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.VAMPIRE:
                if (p_Attributes[o] >= 4)
                {
                    current_Mods[16] = CHARACTER_MODIFIER.VAMPIRE_2;
                }
                else if (p_Attributes[o] >= 2)
                {
                    current_Mods[16] = CHARACTER_MODIFIER.VAMPIRE_1;
                }
                else
                {
                    current_Mods[16] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.WIGHT:
                if (p_Attributes[o] >= 6)
                {
                    current_Mods[17] = CHARACTER_MODIFIER.WIGHT_3;
                }
                else if (p_Attributes[o] >= 4)
                {
                    current_Mods[17] = CHARACTER_MODIFIER.WIGHT_2;
                }
                else if (p_Attributes[o] >= 2)
                {
                    current_Mods[17] = CHARACTER_MODIFIER.WIGHT_1;
                }
                else
                {
                    current_Mods[17] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.WOODLAND:
                if (p_Attributes[o] >= 4)
                {
                    current_Mods[18] = CHARACTER_MODIFIER.WOODLAND_2;
                }
                else if (p_Attributes[o] >= 2)
                {
                    current_Mods[18] = CHARACTER_MODIFIER.WOODLAND_1;
                }
                else
                {
                    current_Mods[18] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.WYRM:
                if (p_Attributes[o] >= 2)
                {
                    current_Mods[19] = CHARACTER_MODIFIER.WYRM;
                }
                else
                {
                    current_Mods[19] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.BLIGHTCRAFTER:
                if (p_Attributes[o] >= 6)
                {
                    current_Mods[0] = CHARACTER_MODIFIER.BLIGHT_3;
                }
                else if (p_Attributes[o] >= 4)
                {
                    current_Mods[0] = CHARACTER_MODIFIER.BLIGHT_2;
                }
                else if (p_Attributes[o] >= 2)
                {
                    current_Mods[0] = CHARACTER_MODIFIER.BLIGHT_1;
                }
                else
                {
                    current_Mods[0] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.BOUNTYHUNTER:
                if (p_Attributes[o] >= 4)
                {
                    current_Mods[1] = CHARACTER_MODIFIER.BOUNTY_2;
                }
                else if (p_Attributes[o] >= 2)
                {
                    current_Mods[1] = CHARACTER_MODIFIER.BOUNTY_1;
                }
                else
                {
                    current_Mods[1] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.BULWARK:
                if (p_Attributes[o] >= 3)
                {
                    current_Mods[2] = CHARACTER_MODIFIER.BULWARK;
                }
                else
                {
                    current_Mods[2] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.FANATIC:
                if (p_Attributes[o] >= 4)
                {
                    current_Mods[3] = CHARACTER_MODIFIER.FANATIC_2;
                }
                else if (p_Attributes[o] >= 2)
                {
                    current_Mods[3] = CHARACTER_MODIFIER.FANATIC_1;
                }
                else
                {
                    current_Mods[3] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.PHALANX:
                if (p_Attributes[o] >= 6)
                {
                    current_Mods[4] = CHARACTER_MODIFIER.PHALANX_3;
                }
                else if (p_Attributes[o] >= 4)
                {
                    current_Mods[4] = CHARACTER_MODIFIER.PHALANX_2;
                }
                else if (p_Attributes[o] >= 2)
                {
                    current_Mods[4] = CHARACTER_MODIFIER.PHALANX_1;
                }
                else
                {
                    current_Mods[4] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.SHADOWHAND:
                if (p_Attributes[o] >= 6)
                {
                    current_Mods[5] = CHARACTER_MODIFIER.SHADOWHAND_3;
                }
                else if (p_Attributes[o] >= 4)
                {
                    current_Mods[5] = CHARACTER_MODIFIER.SHADOWHAND_2;
                }
                else if (p_Attributes[o] >= 2)
                {
                    current_Mods[5] = CHARACTER_MODIFIER.SHADOWHAND_1;
                }
                else
                {
                    current_Mods[5] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.SILENCER:
                if (p_Attributes[o] >= 1)
                {
                    current_Mods[6] = CHARACTER_MODIFIER.SILENCER;
                }
                else
                {
                    current_Mods[6] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.SWARMLORD:
                if (p_Attributes[o] >= 6)
                {
                    current_Mods[7] = CHARACTER_MODIFIER.SWARMLORD_3;
                }
                else if (p_Attributes[o] >= 4)
                {
                    current_Mods[7] = CHARACTER_MODIFIER.SWARMLORD_2;
                }
                else if (p_Attributes[o] >= 2)
                {
                    current_Mods[7] = CHARACTER_MODIFIER.SWARMLORD_1;
                }
                else
                {
                    current_Mods[7] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.WARLOCK:
                if (p_Attributes[o] >= 4)
                {
                    current_Mods[8] = CHARACTER_MODIFIER.WARLOCK_2;
                }
                else if (p_Attributes[o] >= 2)
                {
                    current_Mods[8] = CHARACTER_MODIFIER.WARLOCK_1;
                }
                else
                {
                    current_Mods[8] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.WITCHDOCTOR:
                if (p_Attributes[o] >= 4)
                {
                    current_Mods[9] = CHARACTER_MODIFIER.WITCHDOCTOR_2;
                }
                else if (p_Attributes[o] >= 2)
                {
                    current_Mods[9] = CHARACTER_MODIFIER.WITCHDOCTOR_1;
                }
                else
                {
                    current_Mods[9] = CHARACTER_MODIFIER.NULL;
                }
                break;
            case ATTRIBUTES.ZEALOT:
                if (p_Attributes[o] >= 4)
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

    private void SetText()
    {
        List<KeyValuePair<ATTRIBUTES, short>> sortedAttributes = p_Attributes.ToList();

        sortedAttributes.Sort(
            delegate (KeyValuePair<ATTRIBUTES, short> pair1,
            KeyValuePair<ATTRIBUTES, short> pair2)
            {
                if (pair1.Value == pair2.Value)
                {
                    return pair2.Key.CompareTo(pair1.Key);
                }
                return pair2.Value.CompareTo(pair1.Value);
            });
        synergiesText.text = "";
        foreach (KeyValuePair<ATTRIBUTES, short> o in sortedAttributes)
        {
            synergiesText.text += o.Key + " : " + o.Value + "\n";
        }      
    }
}
