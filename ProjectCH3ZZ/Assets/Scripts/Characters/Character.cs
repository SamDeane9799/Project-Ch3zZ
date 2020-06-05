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
    public abstract class Character : NetworkBehaviour
    {
        #region CHARACTER_STATS
        // --- CHARACTER DATA ---
        [Header("Character Data")]
        public List<ATTRIBUTES> attributes;
        [SyncVar]
        public Vector2 grid_Position;
        public Vector2 future_Position;
        public short gold_Cost; //Amount of gold required to purchase the character
        public short tier; //Tier related to its frequency in the shop
        public short level; //The level of the unit
        public short mana;
        public short max_Mana; //Amount of mana required to cast an ultimate
        public short base_Mana; //Amount of mana a character begins the game with
        public short attack_Damage; //Damage dealt per auto attack
        public short spell_Power; //Amplification of the effect of ultimates
        public float attack_Speed; //How fast a character attacks
        public short maxHealth;
        public short health; //Amount of health a character has
        public short armor; //Resistance to physical damage
        public short magic_Resistance; //Resistance to magic damage
        public short range; //Range in tile units
        public short ID; //Character num ID
        #endregion

        //Information for attacking
        public Character target;
        protected float attack_Timer = 0.0f;

        // --- UI DATA ---
        protected Slider healthBar;
        protected Slider manaBar;

        //Information for pathfinding
        protected GridSpace next_Space;
        protected Stack<GridSpace> path;
        protected int future_Distance;

        public virtual void Awake()
        {
            attributes = new List<ATTRIBUTES>();
            healthBar = GetComponentInChildren<Canvas>().transform.GetChild(0).GetComponent<Slider>();
            manaBar = GetComponentInChildren<Canvas>().transform.GetChild(1).GetComponent<Slider>();
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
        public bool Moving(int current_Distance)
        {
            //If the character does not have a path
            if (path == null)
            {
                return false;
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
                    ResetPath();
                    return false;
                }
                if (path.Count == 0 || current_Distance >= future_Distance)
                {
                    Debug.Log("here");
                    ResetPath();
                    return false;
                }

                //Get a new space to find
                next_Space.combat_Unit = null;
                next_Space = path.Pop();

                //Change path if the next space is occupied
                if (next_Space.combat_Unit != null)
                {
                    ResetPath();
                    return false;
                }

                //Add the character to the path
                next_Space.AddCombatCharacter(this);
                future_Position = next_Space.grid_Position;
                FaceDirection(new Vector2(next_Space.transform.position.x, next_Space.transform.position.z));
            }
            transform.position = Vector3.Lerp(transform.position, next_Space.transform.position, 0.1f);
            return true;
        }

        //Pass in a new path to be used when moving
        public void AcquirePath(Stack<GridSpace> _path)
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
        private void ResetPath()
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
            ResetPath();
        }
    }
}