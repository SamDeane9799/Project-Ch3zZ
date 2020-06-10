using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{
    [System.Serializable]
    public class GridSpace : NetworkBehaviour
    {
        // --- GRID DATA ---
        [Header("Grid Data")]
        public Character unit;
        public Character combat_Unit;
        public Vector2 grid_Position;

        // --- A* DATA ---
        [Header("A* Data")]
        public Vector2 parent_Position;
        public float g;
        public float f;
        public float h;

        //Set the position of the space, to be used when updating grid space for combat
        //and whatnot
        [Command]
        public void CmdSetGridPosition(Vector2 pos)
        {
            grid_Position = pos;
            if (unit != null) unit.grid_Position = pos;
        }

        //Add a new character to the space and update their 
        //grid data to correspond appropriately
        [Command]
        public void CmdAddCharacter(Character character)
        {
            unit = character;
            combat_Unit = character;
            character.grid_Position = grid_Position;
            character.transform.position = transform.position;
        }

        //Add a character to the space for the sole purpose of combat
        [Command]
        public void CmdAddCombatCharacter(Character character)
        {
            combat_Unit = character;
            character.grid_Position = grid_Position;
        }

        //Remove a character from the space
        [Command]
        public void CmdRemoveCharacter()
        {
            unit = null;
            combat_Unit = null;
        }

        //Reset the position of this grid's unit
        [Command]
        public void CmdResetUnitPosition()
        {
            unit.transform.position = transform.position;
            unit.grid_Position = grid_Position;
        }

        //Reset the A* data costs of this space
        [Command]
        public void CmdResetCosts()
        {
            g = int.MaxValue;
            h = int.MaxValue;
            f = int.MaxValue;
        }
    }

    [System.Serializable]
    public class SyncListGridSpace : SyncList<GridSpace>
    {
        public GridSpace[] grid;
    }
}