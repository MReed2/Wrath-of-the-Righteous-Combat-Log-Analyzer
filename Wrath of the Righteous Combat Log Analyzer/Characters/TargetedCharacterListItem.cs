using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public class TargetedCharacterListItem : CharacterListItem
    {
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
