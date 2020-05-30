using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
public abstract class Character : MonoBehaviour
{
    #region CHARACTER_STATS
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

    public float attack_Timer = 0.0f;
    public int previous_Distance;
    protected Slider healthBar;
    protected Slider manaBar;
    public Character target;
    public GridSpace next_Space;
    public Stack<GridSpace> path;
    public Vector2 grid_Position;
    public List<ATTRIBUTES> attributes;

    public virtual void Awake()
    {
        attributes = new List<ATTRIBUTES>();
        healthBar = GetComponentInChildren<Canvas>().transform.GetChild(0).GetComponent<Slider>();
        manaBar = GetComponentInChildren<Canvas>().transform.GetChild(1).GetComponent<Slider>();
        /*manaBar.value = 0;
        manaBar.maxValue = 100;
        healthBar.maxValue = 100;
        healthBar.value = 100;*/
    }

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
        if (target.health <= 0)
        {
            target = null;
        }
    }

    public void CastUltimate()
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

    public bool Moving(int current_Distance)
    {
        if (path == null)
        {
            return false;
        }
        if (Vector3.Distance(transform.position, next_Space.transform.position) <= 0.1)
        {
            transform.position = next_Space.transform.position;

            if (path.Count == 0 || current_Distance >= previous_Distance)
            {
                path = null;
                return false;
            }

            next_Space.combat_Unit = null;
            next_Space = path.Pop();
            if (next_Space.combat_Unit != null)
            {
                Debug.Log("Here");
                path = null;
                return false;
            }
            next_Space.AddCombatCharacter(this);
            previous_Distance = current_Distance;
        }
        transform.position = Vector3.Lerp(transform.position, next_Space.transform.position, 0.1f);
        return true;
    }
}
