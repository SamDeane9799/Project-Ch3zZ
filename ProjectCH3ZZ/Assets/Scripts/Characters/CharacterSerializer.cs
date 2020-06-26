using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{
    public static class CharacterSerializer
    {
        public static void WriteCharacter(this NetworkWriter writer, Character character)
        {
            writer.WriteVector2(character.grid_Position);
            writer.WriteVector2(character.future_Position);
            writer.WriteInt16(character.gold_Cost);
            writer.WriteInt16(character.level);
            writer.WriteInt16(character.mana);
            writer.WriteInt16(character.max_Mana);
            writer.WriteInt16(character.base_Mana);
            writer.WriteInt16(character.attack_Damage);
            writer.WriteInt16(character.spell_Power);
            writer.WriteDouble(character.attack_Speed);
            writer.WriteInt16(character.maxHealth);
            writer.WriteInt16(character.armor);
            writer.WriteInt16(character.magic_Resistance);
            writer.WriteInt16(character.range);
            writer.WriteInt16(character.ID);

            writer.WriteDouble(character.attack_Timer);
        }

        public static Character ReadCharacter(this NetworkReader reader)
        {
            return Resources.Load<Character>(reader.ReadString());
        }
    }

    /*public static class GridSpaceSerializer
    {
        public static void WriteGridSpace(this NetworkWriter writer, GridSpace gridSpace)
        {
            CharacterSerializer.WriteCharacter(writer, gridSpace.unit);
            CharacterSerializer.WriteCharacter(writer, gridSpace.combat_Unit);
        }

        public static GridSpace ReadGridSpace(this NetworkReader reader)
        {
            return Resources.Load<GridSpace>(reader.ReadString());
        }
    }*/
}