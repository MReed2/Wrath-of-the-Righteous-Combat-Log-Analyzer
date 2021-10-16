using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public class TargetedCharacterListItem : CharacterListItem
    {
        public override string Source_Character_Name { get => ((CombatEventTargeted)Parent).Source_Target_Character_Name; }
        public override string Character_Name { get => ((CombatEventTargeted)Parent).Target_Character_Name; set => ((CombatEventTargeted)Parent).Target_Character_Name = value; }
        public override string Friendly_Name { get => ((CombatEventTargeted)Parent).Target_Friendly_Name; set => ((CombatEventTargeted)Parent).Target_Friendly_Name = value; }
        public override CombatEvent.Char_Enum Character_Type { get => ((CombatEventTargeted)Parent).Target_Character_Type; set => ((CombatEventTargeted)Parent).Character_Type = value; }

        public TargetedCharacterListItem(CombatEventTargeted inParent) : base(inParent)
        { }

        public string Source_Target_Character_Name
        {
            get
            {
                if (Parent is CombatEventTargeted) { return ((CombatEventTargeted)Parent).Source_Target_Character_Name; }
                else { throw new System.Exception("Attempted to access Source_Target_Character_Name when _Parent was not of the CombatEventTargeted type"); }
            }
        }
        public string Friendly_Target_Name
        {
            get
            {
                if (Parent is CombatEventTargeted) { return ((CombatEventTargeted)Parent).Friendly_Name; }
                else { throw new System.Exception("Attempted to access Friendly_Target_Name when _Parent was not of the CombatEventTargeted type"); }
            }
        }
    }
}
