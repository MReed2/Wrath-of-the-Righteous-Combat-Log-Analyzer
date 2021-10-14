using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public class CharacterListItem: IComparable
    {
        private CombatEvent _Parent = null;
        private CombatEventList _Parents = new CombatEventList();

        private CharacterList _Children = new CharacterList();

        public event CombatEventChanged OnCombatEventChanged;

        public string Source_Character_Name { get => _Parent.Source_Character_Name; }
        public string Character_Name { get => _Parent.Character_Name; set => _Parent.Character_Name = value; }
        public string Friendly_Name { get => _Parent.Friendly_Name; set => _Parent.Friendly_Name = value; }
        public CombatEvent.Char_Enum Character_Type { get => _Parent.Character_Type; set => _Parent.Character_Type = value; }
        public CombatEventList Parents { get => _Parents; }
        public CombatEvent Parent { get => _Parent; }

        public CharacterList Children { get => _Children; }

        public int Update_Smarter_Guesses_Character_Types()
        {
            CombatEventList friendly_lst = new CombatEventList();
            CombatEventList hostile_lst = new CombatEventList();
            CombatEventList atk_lst = new CombatEventList();

            int changed_cnt = 0;

            foreach (CombatEvent curr_evnt in Parents)
            {
                if (curr_evnt is AttackEvent)
                {
                    CombatEvent.Char_Enum char_type_from_target = curr_evnt.Character_Type_From_Target();
                    if (char_type_from_target == CombatEvent.Char_Enum.Friendly) { friendly_lst.Add(curr_evnt); }
                    else if (char_type_from_target == CombatEvent.Char_Enum.Hostile) { hostile_lst.Add(curr_evnt); }

                    atk_lst.Add(curr_evnt);
                }
            }

            foreach (CharacterListItem inner_curr_char in Children)
            {
                foreach (CombatEvent curr_evnt in inner_curr_char.Parents)
                {
                    if (curr_evnt is AttackEvent)
                    {
                        CombatEvent.Char_Enum char_type_from_target = curr_evnt.Character_Type_From_Target();
                        if (char_type_from_target == CombatEvent.Char_Enum.Friendly) { friendly_lst.Add(curr_evnt); }
                        else if (char_type_from_target == CombatEvent.Char_Enum.Hostile) { hostile_lst.Add(curr_evnt); }

                        atk_lst.Add(curr_evnt);
                    }
                }
            }

            if (atk_lst.Count > 0)
            {
                if (hostile_lst.Count > friendly_lst.Count)
                {
                    foreach (CombatEvent curr_evnt in Parents)
                    {
                        if (curr_evnt.Smarter_Guess_Character_Type != CombatEvent.Char_Enum.Hostile)
                        {
                            changed_cnt++;
                            curr_evnt.Smarter_Guess_Character_Type = CombatEvent.Char_Enum.Hostile;
                        }
                    }

                    foreach (CharacterListItem inner_curr_char in Children)
                    {
                        foreach (CombatEvent curr_evnt in inner_curr_char.Parents)
                        {
                            if (curr_evnt.Smarter_Guess_Character_Type != CombatEvent.Char_Enum.Hostile)
                            {
                                changed_cnt++;
                                curr_evnt.Smarter_Guess_Character_Type = CombatEvent.Char_Enum.Hostile;
                            }
                        }
                    }
                }
                else if (friendly_lst.Count > hostile_lst.Count)
                {
                    foreach (CombatEvent curr_evnt in Parents)
                    {
                        if (curr_evnt.Smarter_Guess_Character_Type != CombatEvent.Char_Enum.Friendly)
                        {
                            changed_cnt++;
                            curr_evnt.Smarter_Guess_Character_Type = CombatEvent.Char_Enum.Friendly;
                        }
                    }

                    foreach (CharacterListItem inner_curr_char in Children)
                    {
                        foreach (CombatEvent curr_evnt in inner_curr_char.Parents)
                        {
                            if (curr_evnt.Smarter_Guess_Character_Type != CombatEvent.Char_Enum.Friendly)
                            {
                                changed_cnt++;
                                curr_evnt.Smarter_Guess_Character_Type = CombatEvent.Char_Enum.Friendly;
                            }
                        }
                    }
                }
            }

            return changed_cnt;
        }

        public CharacterListItem(CombatEvent inParent)
        {
            AddParent(inParent);
            _Parent.OnCombatEventChanged += new CombatEventChanged(CombatEventChanged);
        }

        public void AddParent(CombatEvent inParent)
        {
            if (_Parent == null) { _Parent = inParent; }
            if (inParent.ID < _Parent.ID) { _Parent = inParent; } // This allows sorting the list in chronological order by first appearance
            foreach (CombatEvent curr_event in _Parents)
            {
                if (curr_event == inParent) { return; }
            }
            _Parents.Add(inParent);
        }

        public void AddParents(CombatEventList inParents)
        {
            if (inParents.Count != 0)
            {
                foreach (CombatEvent curr_event in inParents)
                {
                    AddParent(curr_event);
                }
            }
        }

        private void CombatEventChanged(CombatEvent source)
        {
            OnCombatEventChanged?.Invoke(source);
        }
        
        public int CompareTo(object other) // Used to sort the list
        {
            if (other is CharacterListItem) { return string.Compare(this.Friendly_Name, ((CharacterListItem)other).Friendly_Name); }
            else { throw new System.Exception("Attempted to compare CharacterListItem with '" + other.GetType().ToString() + "', which is not supported."); }
        }
    }
}
