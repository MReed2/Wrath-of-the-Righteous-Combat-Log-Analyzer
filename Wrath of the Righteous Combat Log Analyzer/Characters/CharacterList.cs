using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public class CharacterList: List<CharacterListItem>
    {
        public event CombatEventChanged OnCombatEventChanged;

        public int IndirectFullCount { get { return GetAll().Count;  } }

        public bool Contains(string inStr)
        {
            foreach (CharacterListItem curr_itm in GetAll() ) { if (curr_itm.Source_Character_Name == inStr) { return true; } }

            return false;
        }

        public new void Add(CharacterListItem inItm)
        {
            // Check for duplicates -- if this character has already been added, add the event(s) attached to it as new parents if they are not duplicates
            foreach (CharacterListItem curr_itm in GetAll()) { if (curr_itm.Source_Character_Name == inItm.Source_Character_Name) { curr_itm.AddParents(inItm.Parents);  return; } }

            // We will be adding this itm somewhere, so register to receive change notifications
            inItm.OnCombatEventChanged += new CombatEventChanged(CombatEventChanged);

            // Check to see if the *friendly* name already as a direct child only -- if so, add this as a child to that node
            foreach (CharacterListItem curr_itm in this) { if (curr_itm.Friendly_Name == inItm.Friendly_Name) { curr_itm.Children.Add(inItm, this); return; }  }

            // If neither the friendly name nor the source_character_name already exists, add this as directly.
            base.Add(inItm);
        }

        public void CombatEventChanged(CombatEvent source)
        {
            OnCombatEventChanged?.Invoke(source);
        }

        protected void Add(CharacterListItem inItm, CharacterList inParent)
        {
            foreach (CharacterListItem curr_itm in this) // Check for dups, and don't add them anywhere if found.
            {
                if (curr_itm.Source_Character_Name == inItm.Source_Character_Name) { return; }
            }
            // Don't recurse -- there should only be one layer of children.
            base.Add(inItm);
        }

        public List<CharacterListItem> GetAll() // Return type avoids recursion -- when building *this* list, dups are OK
        {
            List<CharacterListItem> rtn = new CharacterList();

            foreach (CharacterListItem curr_itm in this)
            {
                rtn.Add(curr_itm);
                rtn.AddRange(curr_itm.Children.GetAll());                
            }

            return rtn;
        }
    }
}
