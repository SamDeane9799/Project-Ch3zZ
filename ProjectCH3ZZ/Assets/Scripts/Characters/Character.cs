using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mirror
{
    public enum ATTRIBUTES
    {
        BEAST,
        ELDRITCH,
        FELWALKER,
        HUMAN,
        INSECTOID,
        NAUTICAL,
        VAMPIRE,
        WIGHT,
        WOODLAND,
        WYRM,
        BLIGHTCRAFTER,
        BULWARK,
        FANATIC,
        PHALANX,
        BOUNTYHUNTER,
        SHADOWHAND,
        SILENCER,
        SWARMLORD,
        WARLOCK,
        WITCHDOCTOR,
        ZEALOT,
    }
    //Basic outline for the character class
    [System.Serializable]
    public abstract class Character : NetworkBehaviour
    {
        #region CHARACTER_STATS
        // --- CHARACTER DATA ---
        [Header("Character Data")]

        [SyncVar]
        public Vector2 grid_Position;
        [SyncVar]
        public Vector2 future_Position;
        [SyncVar]
        public short gold_Cost; //Amount of gold required to purchase the character
        [SyncVar]
        public short tier; //Tier related to its frequency in the shop
        [SyncVar]
        public short level; //The level of the unit
        [SyncVar]
        public short mana;
        [SyncVar]
        public short max_Mana; //Amount of mana required to cast an ultimate
        [SyncVar]
        public short base_Mana; //Amount of mana a character begins the game with
        [SyncVar]
        public short attack_Damage; //Damage dealt per auto attack
        [SyncVar]
        public short spell_Power; //Amplification of the effect of ultimates
        [SyncVar]
        public float attack_Speed; //How fast a character attacks
        [SyncVar]
        public short maxHealth;
        [SyncVar]
        public short health; //Amount of health a character has
        [SyncVar]
        public short armor; //Resistance to physical damage
        [SyncVar]
        public short magic_Resistance; //Resistance to magic damage
        [SyncVar]
        public short range; //Range in tile units
        [SyncVar]
        public short ID; //Character num ID
        public List<ATTRIBUTES> attributes;
        #endregion

        //Information for attacking
        public Character target;
        public float attack_Timer = 0.0f;

        // --- UI DATA ---
        protected Slider healthBar;
        protected Slider manaBar;

        //Information for pathfinding
        protected GridSpace next_Space;
        protected SyncStackGridSpace path;
        protected int future_Distance;
        public bool isMoving;

        public virtual void Awake()
        {
            attributes = new List<ATTRIBUTES>();
            healthBar = GetComponentInChildren<Canvas>().transform.GetChild(0).GetComponent<Slider>();
            manaBar = GetComponentInChildren<Canvas>().transform.GetChild(1).GetComponent<Slider>();
            isMoving = false;
        }

        //Set the important character stats 
        protected void SetStats(short gold, short _tier, short _level, short _mana, short baseMana, short AD, short SP, float AS, short _health, short AR, short MR, short _range)
        {
            gold_Cost = gold;
            tier = _tier;
            level = _level;
            max_Mana = _mana;
            base_Mana = baseMana;
            attack_Damage = AD;
            spell_Power = SP;
            attack_Speed = AS;
            health = _health;
            maxHealth = _health;
            armor = AR;
            magic_Resistance = MR;
            range = _range;
            healthBar.maxValue = maxHealth;
            healthBar.value = health;
            manaBar.value = baseMana;
            manaBar.maxValue = max_Mana;
        }

        //Method to increment the level of this particular unit
        //Make sure to specify damage and health increases in the 
        //overriden version 
        public virtual void IncrementLevel()
        {
            level++;
            RpcScaleUp();
        }

        [ClientRpc]
        public void RpcScaleUp()
        {

            transform.localScale = new Vector3(1f, 1f, 1f);
        }

        //Return whether or not a character can attack
        //If so, reset the attack timer and return true,
        //otherwise increment the timer and return false
        public void Attack()
        {
            if (attack_Timer >= 1.0f / attack_Speed)
            {
                attack_Timer = 0;
                target.TakeDamage();
            }
            else
            {
                attack_Timer += Time.deltaTime;
            }
        }

        //Cast this unit's ultimate move
        public virtual void CastUltimate()
        {
            if (mana >= max_Mana)
            {
                Ultimate();
                mana = 0;
            }
        }

        //Ultimate move of a character that is unique to them
        public abstract void Ultimate();

        public void TakeDamage()
        {
            health -= 100;
            healthBar.value -= 100;
            manaBar.value += 5;
        }

        //Move the character according to their path
        public void Moving(int current_Distance)
        {
            //If the character does not have a path
            if (path == null)
            {
                isMoving = false;
                return;
            }
            if (Vector3.Distance(transform.position, next_Space.transform.position) <= 0.1)
            {
                transform.position = next_Space.transform.position;
                future_Distance = (int)Vector2.Distance(grid_Position, target.grid_Position + target.future_Position);
                //If there are no more tiles to follow, or if the distance
                //between the target and this character grows, 
                //Change path
                if (current_Distance <= range)
                {
                    FaceDirection(new Vector2(target.transform.position.x, target.transform.position.z));
                    CmdResetPath();
                    isMoving = false;
                    return;
                }
                if (path.Count == 0 || current_Distance >= future_Distance)
                {
                    CmdResetPath();
                    isMoving = false;
                    return;
                }

                //Get a new space to find
                next_Space.combat_Unit = null;
                next_Space = path.Pop();

                //Change path if the next space is occupied
                if (next_Space.combat_Unit != null)
                {
                    CmdResetPath();
                    isMoving = false;
                    return;
                }

                //Add the character to the path
                next_Space.AddCombatCharacter(this);
                future_Position = next_Space.grid_Position;
                FaceDirection(new Vector2(next_Space.transform.position.x, next_Space.transform.position.z));
            }
            transform.position = Vector3.Lerp(transform.position, next_Space.transform.position, 0.1f);
            isMoving = true;
        }

        //Pass in a new path to be used when moving
        public void AcquirePath(SyncStackGridSpace _path)
        {
            next_Space = _path.Pop();
            next_Space.AddCombatCharacter(this);
            path = _path;
            future_Distance = (int)Vector2.Distance(grid_Position, target.grid_Position + target.future_Position);
            FaceDirection(new Vector2(next_Space.transform.position.x, next_Space.transform.position.z));
        }

        //Turn to face a particular point in space
        private void FaceDirection(Vector2 point)
        {
            Vector2 distance = new Vector2(point.x - transform.position.x, point.y - transform.position.z);
            Vector2 forward = new Vector2(transform.forward.x, transform.forward.z);
            float angle = Mathf.Rad2Deg * Mathf.Acos(Vector2.Dot(distance, forward) / (distance.magnitude * forward.magnitude));
            if (float.IsNaN(angle)) { angle = 90; }
            transform.Rotate(new Vector3(0, angle, 0));
        }

        //Reset this character's path and make sure other
        //characters know that this character is no longer moving
        [Command]
        private void CmdResetPath()
        {
            path = null;
            future_Position = Vector2.zero;
        }

        //Reset this character's data
        public void Reset()
        {
            gameObject.SetActive(true);
            health = maxHealth;
            healthBar.value = maxHealth;
            mana = base_Mana;
            manaBar.value = base_Mana;
            transform.localRotation = new Quaternion(0, 0, 0, 0);
            CmdResetPath();
        }

        /*public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            //base.OnSerialize(writer, initialState);
            if(initialState)
            {
                //First time data is sent to the client/Server
                writer.WriteVector2(grid_Position);
                writer.WriteVector2(future_Position);
                writer.WriteInt16(gold_Cost);
                writer.WriteInt16(tier);
                writer.WriteInt16(level);
                writer.WriteInt16(mana);
                writer.WriteInt16(max_Mana);
                writer.WriteInt16(base_Mana);
                writer.WriteInt16(attack_Damage);
                writer.WriteInt16(spell_Power);
                writer.WriteSingle(attack_Speed);
                writer.WriteInt16(maxHealth);
                writer.WriteInt16(armor);
                writer.WriteInt16(magic_Resistance);
                writer.WriteInt16(range);
                writer.WriteInt16(ID);
                
                writer.WriteSingle(attack_Timer);
            }

            bool wroteSyncVar = false;
            if((base.syncVarDirtyBits & 1u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteVector2(grid_Position);
            }

            if ((base.syncVarDirtyBits & 2u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteVector2(future_Position);
            }

            if ((base.syncVarDirtyBits & 4u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteInt16(level);
            }

            if ((base.syncVarDirtyBits & 8u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteInt16(mana);
            }

            if ((base.syncVarDirtyBits & 16u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteInt16(max_Mana);
            }

            if ((base.syncVarDirtyBits & 32u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteInt16(base_Mana);
            }
            if ((base.syncVarDirtyBits & 64u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteInt16(attack_Damage);
            }
            if ((base.syncVarDirtyBits & 128u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteInt16(spell_Power);
            }
            if ((base.syncVarDirtyBits & 256u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteDouble(attack_Speed);
            }
            if ((base.syncVarDirtyBits & 512u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteInt16(maxHealth);
            }
            if ((base.syncVarDirtyBits & 1024u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteInt16(health);
            }
            if ((base.syncVarDirtyBits & 2048u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteInt16(armor);
            }
            if ((base.syncVarDirtyBits & 4096u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteInt16(magic_Resistance);
            }
            if ((base.syncVarDirtyBits & 8192u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteInt16(range);
            }
            if ((base.syncVarDirtyBits & 16384u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteSingle(attack_Timer);
            }
            if ((base.syncVarDirtyBits & 32768u) != 0u)
            {
                if (!wroteSyncVar)
                {
                    writer.WritePackedUInt64(base.syncVarDirtyBits);
                    wroteSyncVar = true;
                }
                writer.WriteBoolean(isMoving);
            }

            if (!wroteSyncVar)
            {
                writer.WritePackedInt32(0);
            }

            return wroteSyncVar;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if(initialState)
            {
                this.grid_Position = reader.ReadVector2();
                this.future_Position = reader.ReadVector2();
                this.gold_Cost = reader.ReadInt16();
                this.tier = reader.ReadInt16();
                this.level = reader.ReadInt16();
                this.mana = reader.ReadInt16();
                this.max_Mana = reader.ReadInt16();
                this.base_Mana = reader.ReadInt16();
                this.attack_Damage = reader.ReadInt16();
                this.spell_Power = reader.ReadInt16();
                this.attack_Speed = reader.ReadSingle();
                this.maxHealth = reader.ReadInt16();
                this.health = reader.ReadInt16();
                this.armor = reader.ReadInt16();
                this.magic_Resistance = reader.ReadInt16();
                this.range = reader.ReadInt16();
                this.ID = reader.ReadInt16();

                this.attack_Timer = reader.ReadSingle();
            }
            int num = (int)reader.ReadPackedUInt32();
            if((num & 1) != 0)
            {
                this.grid_Position = reader.ReadVector2();
            }
            if ((num & 2) != 0)
            {
                this.future_Position = reader.ReadVector2();
            }
            if ((num & 4) != 0)
            {
                this.level = reader.ReadInt16();
            }
            if ((num & 8) != 0)
            {
                this.mana = reader.ReadInt16();
            }
            if ((num & 16) != 0)
            {
                this.max_Mana = reader.ReadInt16();
            }
            if ((num & 32) != 0)
            {
                this.base_Mana = reader.ReadInt16();
            }
            if ((num & 64) != 0)
            {
                this.attack_Damage = reader.ReadInt16();
            }
            if ((num & 128) != 0)
            {
                this.spell_Power = reader.ReadInt16();
            }
            if ((num & 256) != 0)
            {
                this.attack_Speed = reader.ReadSingle();
            }
            if ((num & 512) != 0)
            {
                this.maxHealth = reader.ReadInt16();
            }
            if ((num & 1028) != 0)
            {
                this.health = reader.ReadInt16();
            }
            if ((num & 2048) != 0)
            {
                this.armor = reader.ReadInt16();
            }
            if ((num & 4096) != 0)
            {
                this.magic_Resistance = reader.ReadInt16();
            }
            if ((num & 8192) != 0)
            {
                this.range = reader.ReadInt16();
            }
            if ((num & 16384) != 0)
            {
                this.attack_Timer = reader.ReadSingle();
            }
            if((num & 32768) != 0)
            {
                this.isMoving = reader.ReadBoolean();
            }
        }*/
    }

    [System.Serializable]
    public class SyncListCharacter : SyncList<Character>
    {
    }
}