using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    class CombatStartEvent : CombatEventContainer
    {
        #region Standard Fields
        private List<Die_Roll> _Die_Rolls = new List<Die_Roll>();
#endregion
        #region Override Properties
        public override string Character_Name { get => ""; set => throw new NotImplementedException(); }
        public override string Source_Character_Name => throw new NotImplementedException();
        public override List<Die_Roll> Die_Rolls => _Die_Rolls;
        #endregion
        #region Override Methods
        public override List<Die_Roll> Parse(string line)
        {
            return _Die_Rolls;
        }

        public CombatStartEvent(int inID, int inCombatID, string inLine) : base(inID, inCombatID, inLine) { }
        #endregion

        private int _Strict_Full_Reload_Cnt = 0;
        private int _Loose_Full_Reload_Cnt = 0;
        private int _Strict_Starting_Reload_Cnt = 0;
        private int _Loose_Starting_Reload_Cnt = 0;

        private int _Reload_Cnt = 0;

        protected string Full_Strict_Combat_String { get => Get_Full_Strict_Combat_String(); }
        protected string Full_Loose_Combat_String { get => Get_Full_Loose_Combat_String(); }
        protected string Starting_Strict_Combat_String { get => Get_Starting_Strict_Combat_String(); }
        protected string Starting_Loose_Combat_String { get => Get_Starting_Loose_Combat_String(); }

        public int Strict_Full_Reload_Cnt { get => _Strict_Full_Reload_Cnt; protected set => _Strict_Full_Reload_Cnt = value; }
        public int Loose_Full_Reload_Cnt { get => _Loose_Full_Reload_Cnt; protected set => _Loose_Full_Reload_Cnt = value; }
        public int Strict_Starting_Reload_Cnt { get => _Strict_Starting_Reload_Cnt; protected set => _Strict_Starting_Reload_Cnt = value; }
        public int Loose_Starting_Reload_Cnt { get => _Loose_Starting_Reload_Cnt; protected set => _Loose_Starting_Reload_Cnt = value; }

        public bool IsReload { get => (_Reload_Cnt > 0); }
        public override int Reload_Cnt { get => _Reload_Cnt; protected set => _Reload_Cnt = value; }

        public bool Update_Reload()
        {
            CombatStartEvent prev_CSS = ((CombatStartEvent)Prev_CombatEventContainer);

            int loop_cnt = 0;

            while (prev_CSS != null)
            {
                if (prev_CSS.Full_Strict_Combat_String == Full_Strict_Combat_String) { _Strict_Full_Reload_Cnt = 1 + prev_CSS.Strict_Full_Reload_Cnt; }
                if (prev_CSS.Full_Loose_Combat_String == Full_Loose_Combat_String) { _Loose_Full_Reload_Cnt = 1 + prev_CSS.Loose_Full_Reload_Cnt; }
                if (prev_CSS.Starting_Strict_Combat_String == Starting_Strict_Combat_String) { _Strict_Starting_Reload_Cnt = 1 + prev_CSS.Strict_Starting_Reload_Cnt; }
                if (prev_CSS.Starting_Loose_Combat_String == Starting_Loose_Combat_String) { _Loose_Starting_Reload_Cnt = 1 + prev_CSS.Loose_Starting_Reload_Cnt; }

                if ((_Strict_Full_Reload_Cnt > 0) || (_Loose_Full_Reload_Cnt > 0) || (_Strict_Starting_Reload_Cnt > 0) || (_Loose_Starting_Reload_Cnt > 0)) { _Reload_Cnt = 1 + prev_CSS.Reload_Cnt; }

                if (IsReload) { prev_CSS = null; }
                else
                {
                    bool is_subset = true;

                    foreach (CharacterListItem outer_char_itm in prev_CSS.Characters)
                    {
                        if (outer_char_itm.Character_Type != Char_Enum.Hostile) { continue; }
                        if ( (outer_char_itm.Source_Character_Name.Contains("Summon"))&&(!outer_char_itm.Source_Character_Name.Contains("Summoner")) ) { continue; }

                        bool inner_match_found = false;

                        foreach (CharacterListItem inner_char_itm in Characters) // This is intentional -- we *always* check against the current CombatStartEvent
                        {
                            if (inner_char_itm.Character_Type != Char_Enum.Hostile) { continue; }
                            if ((outer_char_itm.Source_Character_Name.Contains("Summon")) && (!outer_char_itm.Source_Character_Name.Contains("Summoner"))) { continue; }
                            if (outer_char_itm.Friendly_Name == inner_char_itm.Friendly_Name) { inner_match_found = true; break; }
                        }

                        if (!inner_match_found) { is_subset = false; break; }
                    }

                    if (!is_subset) { prev_CSS = null; }
                    else { prev_CSS = (CombatStartEvent)prev_CSS.Prev_CombatEventContainer; loop_cnt++; }
                }
            }

            if ((IsReload)&&(loop_cnt > 0))
            {
                prev_CSS = (CombatStartEvent)Prev_CombatEventContainer;
                while (loop_cnt > 0) { prev_CSS = (CombatStartEvent)prev_CSS.Prev_CombatEventContainer; loop_cnt--; }

                CombatStartEvent next_CSS = (CombatStartEvent)prev_CSS.Next_CombatEventContainer;
                do
                {
                    next_CSS.Reload_Cnt = 1 + prev_CSS.Reload_Cnt;

                    prev_CSS = next_CSS;
                    next_CSS = (CombatStartEvent)prev_CSS.Next_CombatEventContainer;

                } while (prev_CSS != this);
            }

            return IsReload;
        }

        private string Get_Full_Strict_Combat_String()
        {
            return Get_Delimited_String_From_List(Get_Initiative_Only_Characters(false).GetAll(), x => x.Source_Character_Name, false);
        }

        private string Get_Full_Loose_Combat_String()
        {
            return Get_Delimited_String_From_List(Get_Initiative_Only_Characters(false), x => x.Friendly_Name, true);
        }

        private string Get_Starting_Strict_Combat_String()
        {
            return Get_Delimited_String_From_List(Get_Initiative_Only_Characters(true).GetAll(), x => x.Source_Character_Name, false);
        }

        private string Get_Starting_Loose_Combat_String()
        {
            return Get_Delimited_String_From_List(Get_Initiative_Only_Characters(true), x => x.Friendly_Name, true);
        }

        private CharacterList Get_Initiative_Only_Characters(bool only_early = false)
        {
            /* It seems like this /should/ be useful -- especially when limited to only early intiative events -- but it isn't.  The combatLog.txt simply ommits too many
             * initative events for this to work at all.  Much, much better is to simply return the entire character list.
             */

            CharacterList rtn = new CharacterList();
            int min_ID = int.MaxValue;
            int max_ID = int.MinValue;

            foreach (CharacterListItem curr_itm in Characters)
            {
                if (curr_itm.Character_Type == Char_Enum.Hostile)
                {
                    if (curr_itm.Friendly_Name.Contains("Summon") && (!curr_itm.Friendly_Name.Contains("Summoner"))) { }
                    else
                    {
                        rtn.Add(curr_itm);
                        min_ID = Math.Min(min_ID, curr_itm.Parent.ID);
                        max_ID = Math.Max(max_ID, curr_itm.Parent.ID);
                    }
                }
            }

            if ((only_early)&&( (max_ID - min_ID) > 100))
            {
                CharacterList chars_to_remove = new CharacterList();
                int early_threshold = (int)Math.Round((float)max_ID * 0.10);
                foreach (CharacterListItem curr_itm in rtn)
                {
                    if (curr_itm.Parent.ID > early_threshold)
                    {
                        chars_to_remove.Add(curr_itm);
                    }
                }
                if (rtn.Count != chars_to_remove.Count) { foreach (CharacterListItem itm_to_remove in chars_to_remove) { rtn.Remove(itm_to_remove); } }
            }

            return rtn;
        }

        private string Get_Delimited_String_From_List(List<CharacterListItem> inLst, Func<CharacterListItem, string> inName, bool Add_Cnt = true)
        {
            string rtn = "";

            inLst.Sort((a, b) => inName(a).CompareTo(inName(b)) ); // This sorts the list according to the name that is output to the string

            foreach (CharacterListItem curr_itm in inLst)
            {
                if (inName(curr_itm)=="") { throw new System.Exception("Empty string in Get_Delimited_String_From_List"); }
                rtn += inName(curr_itm);
                if ((Add_Cnt)&&(curr_itm.Children.Count > 0)) { rtn += string.Format("(x{0})", curr_itm.Children.Count); }
                rtn += ", ";
            }

            if (rtn.Length > 0) { rtn = rtn.Substring(0, rtn.Length - 2); }

            return rtn;
        }

        private UserControl _UC_For_Display = null;

        private void Extra_Stats(Grid outer_grid)
        {
            // This was a good learning experience, so I'm retaining the code as an example for the future -- but its also silly, because this sort of information is exactly what's supposed
            // to be displayed in the stats block.  Thus, the code exists, but it is never called.

            string[,] data_all =
{
                    { "# of Events", Children.Count.ToString() },
                    { "# of Characters", Characters.GetAll().Count.ToString() },
                    { "# of Unique Characters", Characters.Count.ToString() },
                    { "# of Healing Events", Children.FindAll(m => (m is HealingEvent)).Count.ToString() },
                    { "# of Death Events", Children.FindAll(m => ( (m is SimpleEvent)&&(((SimpleEvent)m).Subtype == "Death") )).Count.ToString() },

                    { "# of Attack Events", Children.FindAll(m => (m is AttackEvent)).Count.ToString() },
                    { "  Hit Rate", string.Format("{0:p}", (float)Children.Count(m => ( (m is AttackEvent)&&( ((AttackEvent)m).IsHit) )) / (float)Children.Count(m => (m is AttackEvent) ) ) },
                    { "  Crit Rate",  string.Format("{0:p}", (float)Children.Count(m => ( (m is AttackEvent)&&( ((AttackEvent)m).IsCritical) )) / (float)Children.Count(m => (m is AttackEvent) ) ) },
                    { "# of Damage Events", Children.FindAll(m => (m is DamageEvent)).Count.ToString() },
                    { "  Avg Damage", string.Format("{0:f2}", (float)Children.Sum( m => (m is DamageEvent)?((DamageEvent)m).Damage:0 ) / (float)Children.Count(m => (m is DamageEvent))) }
                };

            InsertGridRow(outer_grid, 0);
            ScrollViewer scrollViewer = New_Windows_Table("All", data_all, 2, 500);
            Grid tmp_Grid = (Grid)scrollViewer.Content;
            scrollViewer.Content = null;

            Grid.SetRow(tmp_Grid, 0);
            Grid.SetColumn(tmp_Grid, 0);
            Grid.SetColumnSpan(tmp_Grid, 2);
            outer_grid.Children.Add(tmp_Grid);

            int row_added_cnt = 1;

            foreach (Char_Enum curr_enum in Enum.GetValues(typeof(Char_Enum)))
            {
                if (Children.Count(m => m.Character_Type == curr_enum) > 0)
                {
                    string[,] data_by_type =
                    {
                            { "# of Events", Children.Count(m => m.Character_Type == curr_enum).ToString() },
                            { "# of Characters", Characters.GetAll().Count(m => m.Character_Type == curr_enum).ToString() },
                            { "# of Unique Characters", Characters.Count(m => m.Character_Type == curr_enum).ToString() },
                            { "# of Healing Events", Children.FindAll(m => (m.Character_Type == curr_enum)&&(m is HealingEvent)).Count.ToString() },
                            { "# of Death Events", Children.FindAll(m => ( (m.Character_Type == curr_enum)&&(m is SimpleEvent)&&(((SimpleEvent)m).Subtype == "Death") )).Count.ToString() },

                            { "# of Attack Events", Children.FindAll(m => (m.Character_Type == curr_enum)&&(m is AttackEvent)).Count.ToString() },
                            { "  Hit Rate", string.Format("{0:p}", (float)Children.Count(m => ( (m.Character_Type == curr_enum)&&(m is AttackEvent)&&( ((AttackEvent)m).IsHit) )) / (float)Children.Count(m => (m.Character_Type == curr_enum)&&(m is AttackEvent) ) ) },
                            { "  Crit Rate",  string.Format("{0:p}", (float)Children.Count(m => ( (m.Character_Type == curr_enum)&&(m is AttackEvent)&&( ((AttackEvent)m).IsCritical) )) / (float)Children.Count(m => (m.Character_Type == curr_enum)&&(m is AttackEvent) ) ) },
                            { "# of Damage Events", Children.FindAll(m => (m.Character_Type == curr_enum)&&(m is DamageEvent)).Count.ToString() },
                            { "  Avg Damage", string.Format("{0:f2}", (float)Children.Sum( m => ( (m.Character_Type == curr_enum)&&(m is DamageEvent) )?((DamageEvent)m).Damage:0 ) / (float)Children.Count(m => (m.Character_Type == curr_enum)&&(m is DamageEvent))) }
                        };

                    InsertGridRow(outer_grid, row_added_cnt);
                    scrollViewer = New_Windows_Table(curr_enum.ToString(), data_by_type, 2, 500);
                    tmp_Grid = (Grid)scrollViewer.Content;
                    scrollViewer.Content = null;

                    Grid.SetRow(tmp_Grid, row_added_cnt);
                    Grid.SetColumn(tmp_Grid, 0);
                    Grid.SetColumnSpan(tmp_Grid, 2);
                    outer_grid.Children.Add(tmp_Grid);

                    row_added_cnt++;
                }
            }
        }

        public override UserControl Get_UserControl_For_Display()
        {
            if (_UC_For_Display == null)
            {
                _UC_For_Display = base.Get_UserControl_For_Display();

                Grid outer_grid = ((Grid)((DockPanel)((ScrollViewer)_UC_For_Display.Content).Content).Children[0]);               

                return _UC_For_Display;
            }
            
            return _UC_For_Display;
        }
    }
}
