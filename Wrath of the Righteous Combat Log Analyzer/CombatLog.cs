using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    class CombatLog
    {
        private CombatEventList _Log = new CombatEventList();
        private CombatEventList _Current_Combat = new CombatEventList();
        private CombatStats _Stats = new CombatStats();
        private CombatEvent _Current_Event = null;
        private int _Current_Mode = 0;
        private string _prev_line = "";

        private int _Dup_Count = 0;

        public CombatEventList Log => _Log;
        public CombatStats Stats => _Stats;
        public int Duplicate_Count => _Dup_Count;

        public CombatLog()
        { }

        public void ResetDuplicateCount()
        {
            _Dup_Count = 0;
        }

        public void RecalculateStats()
        {
            _Stats.Recalculate_Stats(_Log);
        }

        public void ClearAll()
        {
            _Log.Clear();
            _Current_Combat.Clear();
            _Stats.Recalculate_Stats(_Log);
        }

        public CombatEventList Parse(int inCombatID, string line)
        {
            CombatEventList rtn_event = new CombatEventList();
            
            if (_Current_Mode > 0)
            {
                if (!line.Contains("style=\"margin-left:   0px\"")) // This is all that's necessary to bail out properly.  The code to bail out via exceptions is still present, but never fires.
                {
                    try
                    { _Current_Event.Parse(line); }
                    catch (Mode_Change_Exception)
                    { _Current_Mode = 0; }
                }
                else
                {
                    _Current_Mode = 0;
                }
            }

            if (_Current_Mode == 0)
            {
                if (!line.Contains("margin-left:   0px")&&(!line.Contains("<hr>"))) 
                {
                    throw new System.Exception("In Mode 0, but current line (\"" + line + "\") isn't the start of a new record");
                }

                if (_Current_Event != null) // The previous event is completed, so add it to the database.
                {
                    if (DupCheck(_Current_Event, _Current_Combat) != null)
                    {
                        // This is a duplicate, so discard it.
                        _Dup_Count++;
                    }
                    else
                    {
                        if (_Current_Event is CombatStartEvent)
                        {
                            _Current_Combat = new CombatEventList();
                        }
                        _Current_Combat.Add(_Current_Event);
                        _Log.Add(_Current_Event);
                        rtn_event.Add(_Current_Event);
                        
                        _Stats.Process_Event(_Current_Event);
                    }
                    _Current_Event = null;
                }

                if (line.Contains("Initiative check"))
                {
                    _Current_Mode = 1;
                    _Current_Event = new InitiativeEvent(_Log.Count, inCombatID, line);
                }
                else if (line.Contains(" attacks "))
                {
                    _Current_Mode = 2;
                    _Current_Event = new AttackEvent(_Log.Count, inCombatID, line);
                }
                else if  ( (line.Contains(" deals ")&&line.Contains(" damage ")) || (line.Contains(" receives ") && line.Contains("damage.") ) )
                {
                    _Current_Mode = 3;
                    _Current_Event = new DamageEvent(_Log.Count, inCombatID, line);
                }
                else if (line.Contains(" heals "))
                {
                    _Current_Mode = 4;
                    _Current_Event = new HealingEvent(_Log.Count, inCombatID, line);
                }
                else // SimpleEvents are, by defination, only one line in length, so they can just be added immediately.
                {
                    if (line.Contains("margin-left:   0px"))
                    {
                        CombatEvent tmp = null;
                        if (line.Contains("Combat Started"))  // This is still a SimpleEvent, but it needs to be split out into its own class
                        {
                            tmp = new CombatStartEvent(_Log.Count, inCombatID, line);
                        }
                        else if (line.Contains("dies!"))  // Also still a SimpleEvent, but needs to be split out into its own class
                        {
                            tmp = new DeathEvent(_Log.Count, inCombatID, line);
                        }
                        else
                        {
                            tmp = new SimpleEvent(_Log.Count, inCombatID, line);
                        }
                        _Log.Add(tmp);
                        rtn_event.Add(tmp);
                    }
                    else if (line.Contains("<hr>"))
                    {
                        // do nothing -- just ignore the horizontal rules.
                    }
                    else
                    {
                        throw new System.Exception("Unrecognized event type, starts with \"" + _prev_line + "\"");
                    }
                }
            }

            _prev_line = line;

            return rtn_event;
        }

        public CombatEvent FindCombatEventBySourceString(string inSource)
        {
            foreach (CombatEvent curr_event in _Log)
            {
                if (curr_event.Source.GetHashCode() == inSource.GetHashCode())
                {
                    if (curr_event.Source == inSource)
                    {
                        return curr_event;
                    }
                }
            }

            return null;
        }

        public int Remove_Duplicates()
        {
            CombatEventList Events_To_Be_Removed = new CombatEventList();
            int Num_Entries_Removed = 0;
            double loop_cnt = 0;
            double expected_num_of_loops = _Log.Count * _Log.Count;
            int remove_cnt = 0;
            double percent_complete = 0;

            foreach (CombatEvent first_event in _Log)
            {
                foreach (CombatEvent second_event in _Log)
                {
                    percent_complete = loop_cnt / expected_num_of_loops;
                    int i_percent_complete = (int)Math.Round(percent_complete * 100000, 0);
                    if ((i_percent_complete % 1000) == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("{0:P}: {1} / {2}, {3} removed", percent_complete, loop_cnt, expected_num_of_loops, remove_cnt);
                    }

                    loop_cnt++;
                
                    if (DupCheck(first_event, second_event) != null)
                    {
                        Events_To_Be_Removed.Add(first_event);
                        remove_cnt++;
                    }
                
                }
            }

            foreach (CombatEvent deleted_event in Events_To_Be_Removed)
            {
                Num_Entries_Removed++;
                _Log.Remove(deleted_event);
            }

            return Num_Entries_Removed;
        }

        private CombatEvent DupCheck(CombatEvent event1, CombatEvent event2)
        {
            if (event1 == event2) { return null; }
            else if (event1.GetType() != event2.GetType()) { return null; }
            else if ((event1 is SimpleEvent)||(event1 is CombatStartEvent)) { return null; }
            else if ((event1 is DamageEvent) && ((DamageEvent)event1).Is_Maximized) { return null; }
            else if (event1.Cached_Source_Hashcode != event2.Cached_Source_Hashcode) { return null; }
            else if (event1.Source != event2.Source) { return null; }
            else { return event2; }
        }

        private CombatEvent DupCheck(CombatEvent event1, CombatEventList event2List)
        {
            CombatEvent tmp = null;

            foreach (CombatEvent event2 in event2List)
            {
                tmp = DupCheck(event1, event2);
                if (tmp != null) { return tmp; }
            }

            return null;
        }
    }
}
