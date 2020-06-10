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

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            base.OnSerialize(writer, initialState);
            if (!initialState)
            {
                writer.WriteCharacter(unit);
                writer.WriteCharacter(combat_Unit);
                writer.WriteVector2(grid_Position);
            }

            bool wroteSyncVar = false;
            if((base.syncVarDirtyBits & 1u) != 0u)
            {
                if(!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteCharacter(unit);
            }
            if ((base.syncVarDirtyBits & 2u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteCharacter(combat_Unit);
            }
            if ((base.syncVarDirtyBits & 4u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteVector2(grid_Position);
            }
            if (!wroteSyncVar)
            {
                writer.WritePackedInt32(0);
            }
            return wroteSyncVar;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            base.OnDeserialize(reader, initialState);
            if(initialState)
            {
                this.unit = reader.ReadCharacter();
                this.combat_Unit = reader.ReadCharacter();
                this.grid_Position = reader.ReadVector2();
            }

            int num = (int)reader.ReadPackedUInt32();
            if((num & 1) != 0)
            {
                this.unit = reader.ReadCharacter();
            }
            if((num & 2) != 0)
            {
                this.combat_Unit = reader.ReadCharacter();
            }
            if((num & 4) != 0)
            {
                this.grid_Position = reader.ReadVector2();
            }
        }
    }

    [System.Serializable]
    public class SyncListGridSpace : SyncList<GridSpace>
    {
        public GridSpace[] grid;
    }

    [System.Serializable]
    public class SyncStackGridSpace : SyncList<GridSpace>
    {
        //public List<GridSpace> grid;

        public void Push(GridSpace g)
        {
            this.Add(g);
        }

        public GridSpace Pop()
        {
            GridSpace removed = this[this.Count - 1];
            this.RemoveAt(this.Count - 1);
            return removed;
        }
        public GridSpace Peek(GridSpace g)
        {
            return this[this.Count - 1];
        }
    }
}