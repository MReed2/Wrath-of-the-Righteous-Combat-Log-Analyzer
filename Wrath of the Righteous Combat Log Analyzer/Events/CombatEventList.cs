using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public class CombatEventList:List<CombatEvent>
    {
        public event CombatEventChanged OnCombatEventChanged;

        public new void Add(CombatEvent inEvent)
        {
            inEvent.OnCombatEventChanged += new CombatEventChanged(CombatEventChanged);
            base.Add(inEvent);
        }

        private void CombatEventChanged(CombatEvent source)
        {
            OnCombatEventChanged?.Invoke(source);
        }

        public void Update_With_CharacterList(CharacterList inLstToUpdateWith)
        {
            foreach (CombatEvent curr_event in this)
            {
                if ((curr_event is SimpleEvent)||(curr_event is CombatStartEvent)) { continue; } // Because the names used in SimpleEvents don't match the names used elsewhere.
                foreach (CharacterListItem curr_char in inLstToUpdateWith.GetAll())
                {
                    if (curr_event.Source_Character_Name == curr_char.Source_Character_Name)
                    {
                        bool made_change = false;

                        if (curr_event.Friendly_Name != curr_char.Friendly_Name)
                        {
                            curr_event.Friendly_Name = curr_char.Friendly_Name;
                            made_change = true;
                        }
                        if (curr_event.Character_Type != curr_char.Character_Type)
                        {
                            curr_event.Character_Type = curr_char.Character_Type;
                            made_change = true;
                        }
                        if (made_change)
                        {
                        }
                    }  // Target Character Names don't get updated, at least not yet.  I don't see any use of trying, and its hard to implement.
                    curr_event.Children.Update_With_CharacterList(inLstToUpdateWith);
                }
            }
        }
    }
}
