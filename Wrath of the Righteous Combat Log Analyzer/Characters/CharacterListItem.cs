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

        public virtual string Source_Character_Name { get => _Parent.Source_Character_Name; }
        public virtual string Character_Name { get => _Parent.Character_Name; set => _Parent.Character_Name = value; }
        public virtual string Friendly_Name { get => _Parent.Friendly_Name; set => _Parent.Friendly_Name = value; }
        public virtual CombatEvent.Char_Enum Character_Type { get => _Parent.Character_Type; set => _Parent.Character_Type = value; }

        public CombatEventList Parents { get => _Parents; }
        public CombatEvent Parent { get => _Parent; set => _Parent = value; }
        public CharacterList Children { get => _Children; }

        public CombatEventList Get_Combined_Parents()
        {
            CombatEventList rtn = new CombatEventList();

            int no_target_events_cnt = 0;
            int targeted_events_where_this_char_is_source = 0;
            int targeted_events_where_this_char_is_target = 0;

            foreach (CombatEvent curr_itm in _Parents)
            {
                rtn.Add(curr_itm);
                if (curr_itm is CombatEventTargeted)
                {
                    if (((CombatEventTargeted)curr_itm).Source_Target_Character_Name == Source_Character_Name) { targeted_events_where_this_char_is_target++; }
                    else { targeted_events_where_this_char_is_source++; }
                }
                else { no_target_events_cnt++; }
            }

            System.Diagnostics.Debug.WriteLine("{0}: no_targets_events_cnt = {1}, targeted_events_where_this_char_is_source = {2}, targeted_events_where_this_char_is_target = {3}", Source_Character_Name, no_target_events_cnt, targeted_events_where_this_char_is_source, targeted_events_where_this_char_is_target);

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
                if (curr_evnt_tmp is DamageEvent) { continue; } // Ignore DamageEvents -- no conclusions as to friendly / hostile can be drawn from them

                CombatEventTargeted curr_evnt = (CombatEventTargeted)curr_evnt_tmp;
                if (curr_evnt.Source_Character_Name == inChar.Source_Character_Name)
                {
                    // If X = the character we are interested in, these are events of the type "X <verb> Y".  X is the source of the event.

                    CombatEvent.Char_Enum proposed_char_type = curr_evnt.Character_Type_From_Target();

                    if (proposed_char_type == CombatEvent.Char_Enum.Friendly) { friendly_lst.Add(curr_evnt); }
                    else if (proposed_char_type == CombatEvent.Char_Enum.Hostile) { hostile_lst.Add(curr_evnt); }
                    else { other_lst.Add(curr_evnt); }
                }
                else
                {
                    // These are "Y <verb> X", where again, X is the character we are interested in.

                    CombatEvent.Char_Enum proposed_target_char_type = curr_evnt.Character_Type_From_Target();

                    // Some processing is required here -- what we have is the *source* type, but what we want is the *target* type.
                    if (curr_evnt_tmp is AttackEvent)
                    {
                        // Attacks are aimed at people of the opposing faction, generally.
                        if (proposed_target_char_type == CombatEvent.Char_Enum.Friendly) { hostile_lst.Add(curr_evnt); }
                        else if (proposed_target_char_type == CombatEvent.Char_Enum.Hostile) { friendly_lst.Add(curr_evnt); }
                        else { other_lst.Add(curr_evnt); }
                    }
                    else if (curr_evnt_tmp is HealingEvent)
                    {
                        // Heals are aimed at people of the same faction, generally.
                        if (proposed_target_char_type == CombatEvent.Char_Enum.Friendly) { friendly_lst.Add(curr_evnt); }
                        else if (proposed_target_char_type == CombatEvent.Char_Enum.Hostile) { hostile_lst.Add(curr_evnt); }
                        else { other_lst.Add(curr_evnt); }
                    }
                    else // This should never happen, but just for the sake of completeness...
                    {
                        other_lst.Add(curr_evnt);
                    }
                }
            }
            
            if (other_lst.Count > (friendly_lst.Count + hostile_lst.Count))
            {
                System.Diagnostics.Debug.WriteLine("\t{0} isn't classified due to too many 'Other' events ({1} Friendly, {2} Hostile, {3} Other)", Source_Character_Name, friendly_lst.Count, hostile_lst.Count, other_lst.Count);
                return 0;
            } // This shouldn't happen often, if ever, but if it does then the vote is inconclusive.
            else if (friendly_lst.Count > hostile_lst.Count)
            {
                System.Diagnostics.Debug.WriteLine("\t{0} appears to be Friendly ({1} Friendly, {2} Hostile, {3} Other)", Source_Character_Name, friendly_lst.Count, hostile_lst.Count, other_lst.Count);
                changed_cnt = SetCharacterType(inChar, CombatEvent.Char_Enum.Friendly);
            }
            else if (hostile_lst.Count > friendly_lst.Count)
            {
                System.Diagnostics.Debug.WriteLine("\t{0} appears to be Hostile ({1} Friendly, {2} Hostile, {3} Other)", Source_Character_Name, friendly_lst.Count, hostile_lst.Count, other_lst.Count);
                changed_cnt = SetCharacterType(inChar, CombatEvent.Char_Enum.Hostile);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("\t{0} isn't classified due to a tie ({1} Friendly, {2} Hostile, {3} Other)", Source_Character_Name, friendly_lst.Count, hostile_lst.Count, other_lst.Count);
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
        }

        public void AddParent(CombatEvent inParent)
        {
            if (_Parent == null) { _Parent = inParent; }
            if (inParent.ID < _Parent.ID) { _Parent = inParent; } // This allows sorting the list in chronological order by first appearance

            foreach (CombatEvent curr_event in _Parents) { if (curr_event == inParent) { return; } } // Ignore dups.

            _Parents.Add(inParent);
            inParent.OnCombatEventChanged += new CombatEventChanged(CombatEventChanged);
        }

        public void AddParents(CombatEventList inParents)
        {
            if (inParents.Count != 0) { foreach (CombatEvent curr_event in inParents) { AddParent(curr_event); } }
        }

        private void CombatEventChanged(CombatEvent source)
        {
            OnCombatEventChanged?.Invoke(source);
        }

        public int CompareTo(object other) // Used to sort the list
        {
            if (other is CharacterListItem)
            {
                CharacterListItem tmp_other = (CharacterListItem)other;
                return string.Compare(this.Friendly_Name, tmp_other.Friendly_Name);
            }
            else { throw new System.Exception("Attempted to compare CharacterListItem with '" + other.GetType().ToString() + "', which is not supported."); }
        }
    }
}
