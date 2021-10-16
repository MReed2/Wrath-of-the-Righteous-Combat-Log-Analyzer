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

        private CharacterListItem GetListItemBySourceName(string inStr)
        {
            foreach (CharacterListItem curr_itm in GetAll() )
            {
                if (curr_itm is TargetedCharacterListItem) { if (((TargetedCharacterListItem)curr_itm).Source_Target_Character_Name == inStr) { return curr_itm; } }
                else { if (curr_itm.Source_Character_Name == inStr) { return curr_itm; } }
            }

            return null;
        }

        private CharacterListItem GetListItemByFriendlyName(string inStr)
        {
            foreach (CharacterListItem curr_itm in this)
            {
                if (curr_itm is TargetedCharacterListItem) { if (((TargetedCharacterListItem)curr_itm).Friendly_Target_Name == inStr) { return curr_itm; } }
                else { if (curr_itm.Friendly_Name == inStr) { return curr_itm; } }
            }

            return null;
        }

        public new void Add(CharacterListItem inItm)
        {
            CharacterListItem existing_itm = null;
            CharacterListItem existing_parent = null;

            inItm.OnCombatEventChanged += new CombatEventChanged(CombatEventChanged);

            // Check for duplicates -- if this character has already been added, add the event(s) attached to it as new parents if they are not duplicates
            if (inItm is TargetedCharacterListItem) { existing_itm = GetListItemBySourceName(((TargetedCharacterListItem)inItm).Source_Target_Character_Name); }
            else { existing_itm = GetListItemBySourceName(inItm.Source_Character_Name); }

            if (existing_itm != null) { existing_itm.AddParents(inItm.Parents);  return; }

            // Check to see if the *friendly* name already as a direct child only -- if so, add this as a child to that node
            if (inItm is TargetedCharacterListItem) { existing_itm = GetListItemByFriendlyName(((TargetedCharacterListItem)inItm).Friendly_Target_Name); }
            else { existing_parent = GetListItemByFriendlyName(inItm.Friendly_Name); }

            if (existing_parent != null) { existing_parent.Children.Add(inItm, this); return; }

            // If neither the friendly name nor the source_character_name already exists, add this as directly.
            base.Add(inItm);
        }

        protected void Add(CharacterListItem inItm, CharacterList inParent)
        {
            CharacterListItem existing_itm = null;

            if (inItm is TargetedCharacterListItem) { existing_itm = GetListItemBySourceName(((TargetedCharacterListItem)inItm).Source_Target_Character_Name); }
            else { existing_itm = GetListItemBySourceName(inItm.Source_Character_Name); }

            if (existing_itm != null)
            {
                inItm.AddParents(inItm.Parents);
                return;
            }
            
            base.Add(inItm);
        }

        public void CombatEventChanged(CombatEvent source)
        {
            OnCombatEventChanged?.Invoke(source);
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
