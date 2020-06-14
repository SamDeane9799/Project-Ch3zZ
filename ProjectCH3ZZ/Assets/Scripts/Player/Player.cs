﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mirror
{
    public class Player : NetworkBehaviour
    {
        // --- PLAYER DATA ---
        [Header("Player Data")]
        [SyncVar]
        public short level; //The player's current level, equivalent to the amount of units they can have
        [SyncVar]
        public short gold; //The amount of gold a player currently has
        [SyncVar]
        public short xp;
        [SyncVar]
        public bool in_Combat;
        [SyncVar]
        private PLAYER_MODIFIER player_Mod;

        // --- GRID DATA ---
        [Header("Grid Data")]
        public GameObject gridPrefab;
        public GridSpace[,] grid;
        [SyncVar]
        public GridSpace[] bench;
        private float benchZPosition;

        // --- CHARACTER DATA ---
        [Header("Character Data")]
        //Needs to be synced
        public CHARACTER_MODIFIER[] current_Mods;
        protected Dictionary<ATTRIBUTES, short> p_Attributes;
        public SyncListCharacter field_Units; //All units on the field
        public SyncListCharacter bench_Units; //All units on the bench
        public short[,] characterLevels; //The amount of a particular character at a certain level
        protected Text synergiesText;
        protected const short GRID_WIDTH = 8;
        protected const short GRID_HEIGHT = 4;
        private Camera playerCamera;

        // --- ITEM DATA ---
        [Header("Item Data")]
        //Needs to be synced
        public List<Item> items;
        protected Vector3 previousItemSpot;

        // --- SHOP DATA ---
        protected Canvas playerCanvas;
        protected Shop playerShop;
        public List<GameObject> characterPrefabs;

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
        public virtual void Start()
        {
            //Initialize important stuff i guess
            p_Attributes = new Dictionary<ATTRIBUTES, short>();
            playerCamera = GetComponent<Camera>();
            current_Mods = new CHARACTER_MODIFIER[19]; //Number of possible mods
            characterLevels = new short[53, 3]; //Number of characters and possible levels
            field_Units = new SyncListCharacter();
            bench_Units = new SyncListCharacter();
            benchZPosition = transform.position.z + 6;

            synergiesText = transform.GetChild(0).GetChild(3).GetComponent<Text>();

            playerCanvas = GetComponentInChildren<Canvas>();
            playerShop = GetComponentInChildren<Shop>();
            m_Raycaster = GetComponentInChildren<GraphicRaycaster>();
            ui_Results = new List<RaycastResult>();
            m_Eventsystem = GetComponent<EventSystem>();

           
            if (isLocalPlayer)
            {
                GetComponent<AudioListener>().enabled = true;
                playerCamera.enabled = true;
            }


        }

        //CALLED EVERY FRAME
        public virtual void Update()
        {
            if (isLocalPlayer)
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
                                previous_Space.CmdResetUnitPosition();
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
                    GameObject UIElement = ProjectGraphicalRay();

                    //Checks if we clicked a character
                    if (ProjectRay(unitMask))
                    {
                        //Find the space occupied by the character
                        Character character = hit.transform.gameObject.GetComponent<Character>();

                        Debug.Log(character.GetComponent<NetworkIdentity>().hasAuthority);
                        if (!character.GetComponent<NetworkIdentity>().hasAuthority) return;
                        if (character.grid_Position.y == GRID_HEIGHT) previous_Space = bench[(int)character.grid_Position.x];
                        else previous_Space = grid[(int)character.grid_Position.x, (int)character.grid_Position.y];
                        unit_ToMove = previous_Space.unit.gameObject;
                        dragging_Unit = true;
                    }
                    //Checking if we clicked an item
                    else if (ProjectRay(1 << 12))
                    {
                        unit_ToMove = hit.transform.gameObject;
                        dragging_Unit = true;
                    }
                }
            }
        }

        public void SetUpPlayerGrid()
        {

            grid = new GridSpace[GRID_WIDTH, GRID_HEIGHT];
            bench = new GridSpace[GRID_WIDTH];
            for (short i = 0; i < GRID_HEIGHT; i++)
            {
                for (short j = 0; j < GRID_WIDTH; j++)
                {
                    SpawnGrid(j, i);
                }
            }
            for (short i = 0; i < GRID_WIDTH; i++)
            {
                SpawnBench(i);
            }
        }

        public void SpawnGrid(int j, int i)
        {
            GameObject gridSpot = Instantiate<GameObject>(gridPrefab, new Vector3(transform.position.x + j - 4, 0, transform.position.z + 8 + (i % GRID_HEIGHT)), Quaternion.identity);
            grid[j, i] = gridSpot.GetComponent<GridSpace>();
            grid[j, i].SetGridPosition(new Vector2(j, i));
            NetworkServer.Spawn(gridSpot);
        }
        public void SpawnBench(int i)
        {
            GameObject gridSpot = Instantiate<GameObject>(gridPrefab, new Vector3(transform.position.x + i - 4, 0, transform.position.z + 6), Quaternion.identity);
            bench[i] = gridSpot.GetComponent<GridSpace>();
            NetworkServer.Spawn(gridSpot);
            bench[i].GetComponent<GridSpace>().SetGridPosition(new Vector2(i, GRID_HEIGHT));
        }

        // --- HELPER METHODS ---

        #region GET ITEMS

        //Add the unit to a character
        private void PlaceItem(Character clickedChar)
        {
            /*RectTransform itemRectTransform = unit_ToMove.GetComponent<RectTransform>();
            unit_ToMove.transform.SetParent(clickedChar.transform.GetChild(0));
            itemRectTransform.localScale = new Vector3(.75f, .75f);
            itemRectTransform.anchoredPosition3D = new Vector3(Data.itemSpriteSideLength / 2 - 5, -Data.itemSpriteSideLength + 5, 0);*/
            unit_ToMove.transform.SetParent(clickedChar.transform);
            unit_ToMove.transform.position = new Vector3(clickedChar.transform.position.x, clickedChar.transform.position.y + 1, clickedChar.transform.position.z);
            ResetHeldUnit();
        }

        //Add the item to the player's currently tracked items
        public void AddItem(Item itemToAdd)
        {
            items.Add(itemToAdd);
            Item newItem = Instantiate<Item>(itemToAdd);
            for (int i = 0; i < items.Count; i++)
            {
                newItem.transform.position = new Vector3(-5.5f - ((i % 3) * .5f), 5, -6.5f - (i / 3) * .5f);
            }
        }
        #endregion

        #region BUYING UNITS

        //Method to determine if a unit with a valid ID exists in the 
        //given context
        private int FindUnitID(SyncListCharacter units, short ID, short level)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].ID == ID && units[i].level == level)
                {
                    return i;
                }
            }
            return -1;
        }

        //Upgrade a unit while the player is not currently in combat
        //Prioritize upgrading a unit on the field and searching for units
        //on the bench to get rid of, before getting rid of units
        //on the field
        private bool UpgradeUnit(short ID, short level)
        {
            //Find the id of the first field unit
            int unitIndex = FindUnitID(field_Units, ID, level);

            if (unitIndex != -1)
            {
                field_Units[unitIndex].IncrementLevel();
            }
            //If a field unit of that type does not exist, go ahead and search the bench
            else
            {
                unitIndex = FindUnitID(bench_Units, ID, level);
                bench_Units[unitIndex].IncrementLevel();
            }
            characterLevels[ID, level]++;
            characterLevels[ID, level - 1]--;

            //Search through the bench units and break once enough bench units have
            //been discovered
            for (int i = bench_Units.Count - 1; i >= 0; i--)
            {
                if (bench_Units[i].ID == ID && bench_Units[i].level == level)
                {
                    RemoveCharacter(bench_Units[i]);
                    if (characterLevels[ID, level - 1] == 0) return true;
                }
            }

            //If not enough bench units were found, search the field and find all
            //units after the discovered index
            if (characterLevels[ID, level - 1] > 0)
            {
                for (int i = field_Units.Count - 1; i > unitIndex; i--)
                {
                    if (field_Units[i].ID == ID && field_Units[i].level == level)
                    {
                        RemoveCharacter(field_Units[i]);
                        if (characterLevels[ID, level - 1] == 0) return true;
                    }
                }
            }

            return false;
        }

        //Look specifically at units on the bench while the player is in combat
        //to determine if anything needs an upgrade
        private bool UpgradeUnitInCombat(short ID, short level)
        {
            //Find the index on the bench
            int unitIndex = FindUnitID(bench_Units, ID, level);
            int initialCount = characterLevels[ID, level - 1];
            if (level == 1) characterLevels[ID, level - 1]--;
            List<Character> toRemove = new List<Character>();

            for (int i = bench_Units.Count - 1; i > unitIndex; i--)
            {
                if (bench_Units[i].ID == ID && bench_Units[i].level == level)
                {
                    //Once enough characters of the particular type and level
                    //have been discovered, go ahead and remove them and then
                    //upgrade the units at the particular index
                    toRemove.Add(bench_Units[i]);
                    initialCount++;
                    if (initialCount - characterLevels[ID, level - 1] >= 2)
                    {
                        for (int j = 0; j < toRemove.Count; j++)
                        {
                            RemoveCharacter(toRemove[j]);
                        }
                        characterLevels[ID, level - 1]--;
                        characterLevels[ID, level]++;
                        bench_Units[unitIndex].IncrementLevel();
                        return true;
                    }
                }
            }

            return false;
        }

        //Adding a unit to the bench from the shop
        //After purchasing the units take a look and see if 
        //the units can be upgraded
        public bool BuyUnit(ShopItem charToSpawn)
        {
            characterLevels[charToSpawn.unitID, 0]++;
            //Check to see if an upgrade can be made in outside of combat
            if (!in_Combat && characterLevels[charToSpawn.unitID, 0] >= 3)
            {
                characterLevels[charToSpawn.unitID, 0]--;
                if (UpgradeUnit(charToSpawn.unitID, 1))
                {
                    if (characterLevels[charToSpawn.unitID, 1] >= 3) UpgradeUnit(charToSpawn.unitID, 2);
                    return true;
                }
            }
            //Check to see if an upgrade can be made during combat
            else if (characterLevels[charToSpawn.unitID, 0] >= 3 && bench_Units.Count > 0)
            {
                if (UpgradeUnitInCombat(charToSpawn.unitID, 1))
                {
                    if (characterLevels[charToSpawn.unitID, 1] >= 3) UpgradeUnitInCombat(charToSpawn.unitID, 2);
                    return true;
                }
            }

            //GameObject character = null;
            for (short i = 0; i < bench.Length; i++)
            {
                if (bench[i].unit == null)
                {
                    SpawnUnit(i, charToSpawn.unitID);
                    return true;
                }
            }
            return false;
        }

        public void SpawnUnit(int benchIndex, int unitID)
        {
            GameObject go = Instantiate(characterPrefabs[unitID - 1]);
            Character charComponent = go.GetComponent<Character>();
            bench[benchIndex].GetComponent<GridSpace>().CmdAddCharacter(charComponent);
            bench_Units.Insert(benchIndex, charComponent);
            NetworkServer.Spawn(go);
            go.GetComponent<NetworkIdentity>().AssignClientAuthority(connectionToClient);
        }

/*        [ClientRpc]
        public void RpcAddUnitToBench(Character charToAdd, int benchIndex)
        {
            
        }*/

        //Sell a unit by removing all references to it 
        private void SellUnit(Character unitToSell)
        {
            //Adding units cost to players gold
            gold += unitToSell.gold_Cost;
            RemoveCharacter(unitToSell);
            ResetHeldUnit();
        }

        #endregion

        #region CLICKING UNITS

        //Projects a basic raycast at the given mask
        private bool ProjectRay(LayerMask mask)
        {
            Ray direction = playerCamera.ScreenPointToRay(Input.mousePosition);
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
                new_Spot.CmdAddCharacter(character);
                previous_Space.CmdRemoveCharacter();

                if (previous_Space.transform.position.z <= benchZPosition && new_Spot.transform.position.z > benchZPosition)
                {
                    BenchToField(character);
                }
                else if (previous_Space.transform.position.z > benchZPosition && new_Spot.transform.position.z <= benchZPosition)
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

                new_Spot.CmdAddCharacter(previous_Unit);
                previous_Space.CmdAddCharacter(character);

                ResetHeldUnit();
            }
            else
            {
                previous_Space.CmdResetUnitPosition();
                ResetHeldUnit();
            }
        }

        //Reset the unit being held so that
        //a new one can be acquired
        private void ResetHeldUnit()
        {
            unit_ToMove = null;
            previous_Space = null;
            dragging_Unit = false;
        }

        //Remove a character currently being tracked by the player
        private void RemoveCharacter(Character character)
        {
            if (character.grid_Position.y < 4)
            {
                grid[(int)character.grid_Position.x, (int)character.grid_Position.y].CmdRemoveCharacter();
                FieldToBench(character);
            }
            else
            {
                bench[(int)character.grid_Position.x].CmdRemoveCharacter();
            }
            bench_Units.Remove(character);
            characterLevels[character.ID, character.level - 1]--;
            Destroy(character.gameObject);
        }

        //When a unit is moved, evaluate the buffs on the current board and
        //change what the player has accordingly
        //ENSURE THAT YOU UPDATE THE GRID POSITION THIS CHARACTER IS CURRENTLY ON
        protected virtual void BenchToField(Character unit)
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
        }

        //Move a unit from the field to the bench,
        //adding it to the appropriate data structure
        //and updating the synergies
        //ENSURE THAT YOU UPDATE THE GRID POSITION THIS CHARACTER IS CURRENTLY ON
        protected virtual void FieldToBench(Character unit)
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

        #region RESET

        //Reset the player's units after combat
        public void Reset()
        {
            //Reset all of the grid data
            //as well as each grid space's character data
            in_Combat = false;
            for (int i = 0; i < GRID_HEIGHT; i++)
            {
                for (int j = 0; j < GRID_WIDTH; j++)
                {
                    grid[j, i].SetGridPosition(new Vector2(j, i));
                    if (grid[j, i].unit != null)
                    {
                        grid[j, i].CmdResetUnitPosition();
                        grid[j, i].unit.Reset();
                    }
                    else
                    {
                        grid[j, i].combat_Unit = null;
                    }
                }
            }

            //Check to see if any units can be upgraded
            Dictionary<int, int> checkedIDs = new Dictionary<int, int>();
            List<short> toUpgrade = new List<short>();
            for (int i = 0; i < bench_Units.Count; i++)
            {
                if (!checkedIDs.ContainsKey(bench_Units[i].ID))
                {
                    checkedIDs.Add(bench_Units[i].ID, 1);
                    if (characterLevels[bench_Units[i].ID, 0] >= 3 || characterLevels[bench_Units[i].ID, 1] >= 3)
                    {
                        toUpgrade.Add(bench_Units[i].ID);
                    }
                }
            }
            for (int i = 0; i < toUpgrade.Count; i++)
            {
                if (characterLevels[toUpgrade[i], 0] >= 3)
                {
                    if (UpgradeUnit(toUpgrade[i], 1) && characterLevels[toUpgrade[i], 1] >= 3)
                        UpgradeUnit(toUpgrade[i], 2);
                }
                else
                {
                    UpgradeUnit(toUpgrade[i], 2);
                }
            }
        }

        #endregion

    }
}