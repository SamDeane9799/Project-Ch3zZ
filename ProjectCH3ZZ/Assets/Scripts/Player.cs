﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;

public class Player : MonoBehaviour
{
    // --- PLAYER DATA ---
    [Header("Player Data")]
    public short level; //The player's current level, equivalent to the amount of units they can have
    public short gold; //The amount of gold a player currently has
    public short xp;
    public bool in_Combat;
    private PLAYER_MODIFIER player_Mod;

    // --- GRID DATA ---
    [Header("Grid Data")]
    public GridSpace gridPrefab;
    public GridSpace[,] grid;
    public GridSpace[] bench;
    protected List<GridSpace> occupied_Space; //All currently occupied spaces

    // --- CHARACTER DATA ---
    [Header("Character Data")]
    public CHARACTER_MODIFIER[] current_Mods;
    protected Dictionary<ATTRIBUTES, short> p_Attributes;
    public List<Character> field_Units; //All units on the field
    public List<Character> bench_Units; //All units on the bench
    public short[,] characterLevels; //The amount of a particular character at a certain level
    protected Text synergiesText;

    // --- ITEM DATA ---
    [Header("Item Data")]
    public List<Item> items;
    protected Vector3 previousItemSpot;

    // --- SHOP DATA ---
    protected Canvas playerCanvas;
    protected Shop playerShop;

    //Variables for moving various units around
    protected GameObject unit_ToMove;
    protected GridSpace previous_Space;
    protected bool dragging_Unit;

    //Important variables for determining
    //the results of a raycast
    protected RaycastHit hit;
    protected GraphicRaycaster m_Raycaster;
    protected PointerEventData m_PointerEventData;
    protected List<RaycastResult> ui_Results;
    protected EventSystem m_Eventsystem;

    // Start is called before the first frame update
    public virtual void Awake()
    {
        //Initialize important stuff i guess
        p_Attributes = new Dictionary<ATTRIBUTES, short>();
        current_Mods = new CHARACTER_MODIFIER[19]; //Number of possible mods
        characterLevels = new short[53, 3]; //Number of characters and possible levels
        field_Units = new List<Character>();
        bench_Units = new List<Character>();

        synergiesText = GameObject.Find("synergiesText").GetComponent<Text>();

        playerCanvas = GetComponentInChildren<Canvas>();
        playerShop = GetComponentInChildren<Shop>();
        m_Raycaster = GetComponentInChildren<GraphicRaycaster>();
        ui_Results = new List<RaycastResult>();
        m_Eventsystem = GetComponent<EventSystem>();
        
        grid = new GridSpace[8, 4];
        bench = new GridSpace[8];
        occupied_Space = new List<GridSpace>();

        //Initialize the player's grid
        for (short i = 0; i < 4; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                grid[j, i] = Instantiate<GridSpace>(gridPrefab, new Vector3(j - 3.5f, 5, i - 5f), Quaternion.identity);
                grid[j, i].SetGridPosition(new Vector2(j, i));
            }
        }
        for (short i = 0; i < 8; i++)
        {
            bench[i] = Instantiate<GridSpace>(gridPrefab, new Vector3(i - 3.5f, 5, -6.5f), Quaternion.identity);
            bench[i].SetGridPosition(new Vector2(i, 0));
        }
    }

    //CALLED EVERY FRAME
    public virtual void Update()
    {
        //Check if the player is holding a unit
        if (dragging_Unit)
        {
            //Put out raycast on Gridpositions
            LayerMask mask = 1 << 8; //Plane layer
            ProjectRay(mask);
            unit_ToMove.transform.position = hit.point;
            //Check if we left clicked
            if (Input.GetMouseButtonDown(0))
            {
                //Check if the unit we're holding is an item or character
                if (!unit_ToMove.GetComponent<Item>())
                {
                    //Checks if we're trying to sell a unit by shooting out a graphical raycast at specific ui elements
                    Character character = unit_ToMove.GetComponent<Character>();
                    mask = 1 << 10; //Grid layer
                    ProjectGraphicalRay();
                    if (ui_Results.Count != 0 && !unit_ToMove.GetComponent<Item>() && ui_Results[0].gameObject.name == "ShopBackground")
                    {
                        SellUnit(character);
                    }
                    //If we hit anything with our initial raycast we want to place the unit there
                    else if (ProjectRay(mask))
                    {
                        GridSpace new_Spot = hit.transform.gameObject.GetComponent<GridSpace>();
                        PlaceUnit(new_Spot, character);
                    }
                    //If the raycast hits nothing we want to place the unit in its original position and reset the held unit
                    else
                    {                          
                        previous_Space.ResetUnitPosition();
                        ResetHeldUnit();
                    }
                }
                //If the unit we're holding is an item we go in here
                else
                {
                    //Checking if the player clicked on a character
                    mask = 1 << 9; //Character layer
                    if (ProjectRay(mask))
                    {
                        Character clickedChar = hit.transform.gameObject.GetComponent<Character>();
                        //Add item to characters list if there is room
                        PlaceItem(clickedChar);
                    }
                    //If we don't click on a unit, place the item back to its original spot
                    else
                    {
                        unit_ToMove.transform.position = previousItemSpot;
                        ResetHeldUnit();
                    }
                }
            }
        }

        //Check when the player clicks on a unit
        else if (Input.GetMouseButtonDown(0))
        {
            //If player left clicks while nothing is done here we do this
            LayerMask unitMask = 1 << 9; //Character layer
            ProjectGraphicalRay();

            //Checks if we clicked a character
            if (ProjectRay(unitMask))
            {
                //Updates the occupied space array
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

            //Checks if we clicked an item
            else if (ui_Results.Count != 0 && ui_Results[0].gameObject.GetComponent<Item>())
            {
                //Checks if we're clicking an item that is either in the shop or on our item bench
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

    // --- HELPER METHODS ---

    #region GET ITEMS

    //Add the unit to a character
    private void PlaceItem(Character clickedChar)
    {
        RectTransform itemRectTransform = unit_ToMove.GetComponent<RectTransform>();
        unit_ToMove.transform.SetParent(clickedChar.transform.GetChild(0));
        itemRectTransform.localScale = new Vector3(.75f, .75f);
        itemRectTransform.anchoredPosition3D = new Vector3(Data.itemSpriteSideLength / 2 - 5, -Data.itemSpriteSideLength + 5, 0);
        ResetHeldUnit();
    }

    //Add the item to the player's currently tracked items
    protected void AddItem(Item itemToAdd)
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
    #endregion

    #region BUYING UNITS

    //Upgrade a unit while the player is not currently in combat
    //Prioritize upgrading a unit on the field and searching for units
    //on the bench to get rid of, before getting rid of units
    //on the field
    private bool UpgradeUnit(Character unit, short level)
    {
        if (characterLevels[unit.ID, level - 1] == 3)
        {
            int unitIndex = -1;
            for (int i = 0; i < field_Units.Count; i++)
            {
                if (field_Units[i].ID == unit.ID && field_Units[i].level == level)
                {
                    unitIndex = i;
                    break;
                }
            }
            if (unitIndex != -1)
            {
                field_Units[unitIndex].IncrementLevel();
                characterLevels[unit.ID, level]++;
            }
            else
            {
                for (int i = 0; i < bench_Units.Count; i++)
                {
                    if (bench_Units[i].ID == unit.ID && bench_Units[i].level == level)
                    {
                        unitIndex = i;
                        break;
                    }
                }
                bench_Units[unitIndex].IncrementLevel();
                characterLevels[unit.ID, level]++;
            }
            characterLevels[unit.ID, level - 1]--;
            for (int i = bench_Units.Count - 1; i >= 0; i--)
            {
                if (bench_Units[i].ID == unit.ID && bench_Units[i].level == level)
                {
                    characterLevels[bench_Units[i].ID, level - 1]--;
                    Destroy(bench_Units[i].gameObject);
                    bench_Units.RemoveAt(i);
                    if (characterLevels[unit.ID, level - 1] == 0) break;
                }
            }
            if (characterLevels[unit.ID, level - 1] > 0)
            {
                for (int i = unitIndex + 1; i < field_Units.Count; i++)
                {
                    if (field_Units[i].ID == unit.ID && field_Units[i].level == level)
                    {
                        Destroy(field_Units[i].gameObject);
                        field_Units.RemoveAt(i);
                        characterLevels[unit.ID, level - 1]--;
                        break;
                    }
                }
            }
            return true;
        }
        return false;
    }

    //Look specifically at units on the bench while the player is in combat
    //to determine if anything needs an upgrade
    private bool UpgradeUnitInCombat(Character unit, short level)
    {
        if (characterLevels[unit.ID, level - 1] >= 3 && bench_Units.Count > 0)
        {
            int unitIndex = -1;
            for (int i = 0; i < bench_Units.Count; i++)
            {
                if (bench_Units[i].ID == unit.ID && bench_Units[i].level == level)
                {
                    unitIndex = i;
                    break;
                }
            }
            List<Character> unitsToRemove = new List<Character>();
            for (int i = bench_Units.Count - 1; i > unitIndex; i--)
            {
                if (bench_Units[i].ID == unit.ID && bench_Units[i].level == level)
                {
                    unitsToRemove.Add(bench_Units[i]);
                    if (unitsToRemove.Count == 2)
                    {
                        for (int j = 0; j < unitsToRemove.Count; j++)
                        {
                            Destroy(unitsToRemove[j].gameObject);
                            bench_Units.Remove(unitsToRemove[j]);
                            characterLevels[unit.ID, level - 1]--;
                        }
                    }
                    bench_Units[unitIndex].IncrementLevel();
                    characterLevels[unit.ID, level - 1]++;
                    return true;
                }
            }
        }
        return false;
    }
    #endregion

    #region CLICKING UNITS
    
    //Projects a basic raycast at the given mask
    private bool ProjectRay(LayerMask mask)
    {
        Ray direction = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(direction, out hit, 1000, mask);
    }

    //projects a raycast on UI units
    private GameObject ProjectGraphicalRay()
    {
        m_PointerEventData = new PointerEventData(m_Eventsystem);
        m_PointerEventData.position = Input.mousePosition;
        ui_Results.Clear();
        m_Raycaster.Raycast(m_PointerEventData, ui_Results);
        return ui_Results[0].gameObject;
    }

    //Place a unit on the grid based on the 
    //gridspace being occupied or not, otherwise
    //just reset the unit's position
    private void PlaceUnit(GridSpace new_Spot, Character character)
    {
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

    //Adding a unit to the bench from the shop
    //After purchasing the units take a look and see if 
    //the units can be upgraded
    public bool BuyUnit(Character characterPrefab)
    {
        Character character = null;
        for (short i = 0; i < bench.Length; i++)
        {
            if (bench[i].unit == null)
            {
                character = Instantiate<Character>(characterPrefab, Vector3.zero, Quaternion.identity);
                bench[i].AddCharacter(character);
                bench_Units.Insert(i, character);
                occupied_Space.Add(bench[i]);
                character.gameObject.SetActive(false);
                break;
            }
        }
        if (character == null) return false;
        characterLevels[characterPrefab.ID, 0]++;
        if (!in_Combat)
        {
            if (UpgradeUnit(characterPrefab, 1)) UpgradeUnit(characterPrefab, 2);
            else character.gameObject.SetActive(true);
        }
        else
        {
            if (UpgradeUnitInCombat(characterPrefab, 1)) UpgradeUnit(characterPrefab, 2);
            else character.gameObject.SetActive(true);
        }
        return true;
    }

    //Sell a unit by removing all references to it 
    private void SellUnit(Character unitToSell)
    {
        //Adding units cost to players gold
        gold += unitToSell.gold_Cost;
        if (bench_Units.Contains(unitToSell))
        {
            bench_Units.Remove(unitToSell);
        }
        else
        {
            FieldToBench(unitToSell);
            bench_Units.Remove(unitToSell);
        }
        Destroy(unit_ToMove.gameObject);
        ResetHeldUnit();
    }

    //Reset the unit being held so that
    //a new one can be acquired
    private void ResetHeldUnit()
    {
        unit_ToMove = null;
        previous_Space = null;
        dragging_Unit = false;
    }

    //When a unit is moved, evaluate the buffs on the current board and
    //change what the player has accordingly
    protected virtual void BenchToField(Character unit)
    {
        bench_Units.Remove(unit);
        field_Units.Add(unit);
        bench[(int)unit.grid_Position.x].RemoveCharacter();
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
    }

    //Move a unit from the field to the bench,
    //adding it to the appropriate data structure
    //and updating the synergies
    protected virtual void FieldToBench(Character unit)
    {
        field_Units.Remove(unit);
        bench_Units.Add(unit);
        grid[(int)unit.grid_Position.x, (int)unit.grid_Position.y].RemoveCharacter();

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
    }

    //Set the text of the player's UI
    //to properly display their current 
    //synergies
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

    //Helper method to add any new modifiers to the list
    protected void CheckAttributes(ATTRIBUTES o)
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
    #endregion
}
