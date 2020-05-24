using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ORIGIN
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
    WYRM
}

public enum CLASS
{
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
    public short mana; //Amount of mana required to cast an ultimate
    public short base_Mana; //Amount of mana a character begins the game with
    public short attack_Damage; //Damage dealt per auto attack
    public short spell_Power; //Amplification of the effect of ultimates
    public float attack_Speed; //How fast a character attacks
    public short health; //Amount of health a character has
    public short armor; //Resistance to physical damage
    public short magic_Resistance; //Resistance to magic damage
    public short range; //Range in tile units
    #endregion

    public float attack_Timer = 0.0f;
    public List<ORIGIN> origins;
    public List<CLASS> classes;

    public void Start()
    {
        origins = new List<ORIGIN>();
        classes = new List<CLASS>();
    }

    //Method to increment the level of this particular unit
    //Make sure to specify damage and health increases in the 
    //overriden version 
    public virtual void IncrementLevel()
    {
        level++;
    }

    //Return whether or not a character can attack
    //If so, reset the attack timer and return true,
    //otherwise increment the timer and return false
    public bool CanAttack()
    {
        if (attack_Timer >= 1.0f / attack_Speed)
        {
            attack_Timer = 0;
            return true;
        }
        else
        {
            attack_Timer += Time.deltaTime;
            return false;
        }
    }

    //Ultimate move of a character that is unique to them
    public abstract void Ultimate(); 
}
