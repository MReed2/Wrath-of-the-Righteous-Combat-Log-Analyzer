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
        public CombatEvent Parent { get => _Parent; set => _Parent = value; }
        public CharacterList Children { get => _Children; }

        public CombatEventList Get_Combined_Parents()
        {
            CombatEventList rtn = new CombatEventList();

            rtn.AddRange(_Parents);
            return rtn;
        }

        public int Update_Smarter_Guesses_Character_Types(CharacterListItem inChar)
        {
            CombatEventList friendly_lst = new CombatEventList(); // These are lists instead of simple counters for troubleshooting
            CombatEventList hostile_lst = new CombatEventList();
            CombatEventList other_lst = new CombatEventList();

            int changed_cnt = 0;

            friendly_lst.Clear();
            hostile_lst.Clear();
            other_lst.Clear();
            changed_cnt=0;

            foreach (CombatEvent curr_evnt_tmp in _Parents)
            {
                if (!(curr_evnt_tmp is CombatEventTargeted)) { continue; }
                if (curr_evnt_tmp is DamageEvent) { continue; } // Ignore damageevents -- no conclusions as to friendly / hostile can be drawn from them

                CombatEventTargeted curr_evnt = (CombatEventTargeted)curr_evnt_tmp;
                if (curr_evnt.Source_Character_Name != inChar.Source_Character_Name) { continue; } // Not useful for voting

                CombatEvent.Char_Enum proposed_char_type = curr_evnt.Character_Type_From_Target();

                if (proposed_char_type == CombatEvent.Char_Enum.Friendly) { friendly_lst.Add(curr_evnt); }
                else if (proposed_char_type == CombatEvent.Char_Enum.Hostile) { hostile_lst.Add(curr_evnt); }
                else { other_lst.Add(curr_evnt); }
            }

            if (other_lst.Count > (friendly_lst.Count + hostile_lst.Count)) { return 0; } // This shouldn't happen often, if ever, but if it does then the vote is inconclusive.
            else if (friendly_lst.Count > hostile_lst.Count)
            {
                //System.Diagnostics.Debug.WriteLine("\t{0} appears to be Friendly ({1} to {2}, Other {3})", Source_Character_Name, friendly_lst.Count, hostile_lst.Count, other_lst.Count);
                changed_cnt = SetCharacterType(inChar, CombatEvent.Char_Enum.Friendly);
            }
            else if (hostile_lst.Count > friendly_lst.Count)
            {
                //System.Diagnostics.Debug.WriteLine("\t{0} appears to be Hostile ({1} to {2}, Other {3})", Source_Character_Name, friendly_lst.Count, hostile_lst.Count, other_lst.Count);
                changed_cnt = SetCharacterType(inChar, CombatEvent.Char_Enum.Hostile);
            }

            //System.Diagnostics.Debug.WriteLine("Updated {0} combat events", changed_cnt);

            return changed_cnt;
        }

        private int SetCharacterType(CharacterListItem inChar, CombatEvent.Char_Enum inType)
        {
            int changed_cnt = 0;
            CombatEvent.Char_Enum opp_Type = CombatEvent.Char_Enum.Really_Unknown;

            if (inType == CombatEvent.Char_Enum.Friendly) { opp_Type = CombatEvent.Char_Enum.Hostile; }
            else if (inType == CombatEvent.Char_Enum.Hostile) { opp_Type = CombatEvent.Char_Enum.Friendly; }
            else { throw new System.Exception("SetCharacterType can only handle 'Friendly' and 'Hostile' char_enum types."); }

            foreach (CombatEvent curr_evnt in Parents)
            {
                if (curr_evnt is CombatEventTargeted)
                {
                    CombatEventTargeted tmp = (CombatEventTargeted)curr_evnt;
                    if (tmp.Source_Character_Name == inChar.Source_Character_Name)
                    {
                        if (tmp.Character_Type != inType) { changed_cnt++; tmp.Character_Type = inType; }

                        if (tmp is AttackEvent) { if (tmp.Target_Character_Type != opp_Type) { changed_cnt++; tmp.Target_Character_Type = opp_Type; } }
                        else if (tmp is HealingEvent) { if (tmp.Target_Character_Type != inType) { changed_cnt++; tmp.Target_Character_Type = inType; } }
                        else if (tmp is DamageEvent) { /* Do nothing */ }
                        else { /* Do nothing */ }
                    }
                    else
                    {
                       //if (tmp.Character_Type != opp_Type) { changed_cnt++; tmp.Character_Type = opp_Type; }

                       // if (tmp is AttackEvent) { if (tmp.Target_Character_Type != inType) { changed_cnt++; tmp.Target_Character_Type = inType; } }
                       // else if (tmp is HealingEvent) { if (tmp.Target_Character_Type != opp_Type) { changed_cnt++; tmp.Target_Character_Type = opp_Type; } }
                       // else if (tmp is DamageEvent) { /* Do nothing */ }
                       // else { /* Do nothing */ }
                    }
                }
                else
                {
                    if (curr_evnt.Character_Type != inType) { changed_cnt++; curr_evnt.Character_Type = inType; }
                }
            }

            foreach (CharacterListItem curr_itm in Children)
            {
                foreach (CombatEvent inner_curr_evnt in curr_itm.Parents)
                {
                    if (inner_curr_evnt is CombatEventTargeted)
                    {
                        CombatEventTargeted tmp = (CombatEventTargeted)inner_curr_evnt;
                        if (tmp.Source_Character_Name == inChar.Source_Character_Name)
                        {
                            if (tmp.Character_Type != inType) { changed_cnt++; tmp.Character_Type = inType; }

                            if (tmp is AttackEvent) { if (tmp.Target_Character_Type != opp_Type) { changed_cnt++; tmp.Target_Character_Type = opp_Type; } }
                            else if (tmp is HealingEvent) { if (tmp.Target_Character_Type != inType) { changed_cnt++; tmp.Target_Character_Type = inType; } }
                            else if (tmp is DamageEvent) { /* Do nothing */ }
                            else { /* Do nothing */ }
                        }
                        else
                        {
                            //if (tmp.Character_Type != opp_Type) { changed_cnt++; tmp.Character_Type = opp_Type; }

                            //if (tmp is AttackEvent) { if (tmp.Target_Character_Type != inType) { changed_cnt++; tmp.Target_Character_Type = inType; } }
                            //else if (tmp is HealingEvent) { if (tmp.Target_Character_Type != opp_Type) { changed_cnt++; tmp.Target_Character_Type = opp_Type; } }
                            //else if (tmp is DamageEvent) { /* Do nothing */ }
                            //else { /* Do nothing */ }
                        }
                    }
                    else
                    {
                        if (inner_curr_evnt.Character_Type != inType) { changed_cnt++; inner_curr_evnt.Character_Type = inType; }
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
            if (inParent.ID < _Parent.ID)
            {
                _Parent = inParent;
            } // This allows sorting the list in chronological order by first appearance
            foreach (CombatEvent curr_event in _Parents) { if (curr_event == inParent) { return; } }
            _Parents.Add(inParent);
            inParent.OnCombatEventChanged += new CombatEventChanged(CombatEventChanged);
        }

        public void AddParents(CombatEventList inParents)
        {
            if (this is TargetedCharacterListItem) { ((TargetedCharacterListItem)this).AddParents(inParents); }
            if (inParents.Count != 0) { foreach (CombatEvent curr_event in inParents) { AddParent(curr_event); } }
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
