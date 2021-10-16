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

        public CombatStartEvent(int inID, string inLine) : base(inID, inLine) { }
        #endregion

        private int _Strict_Full_Reload_Cnt = 0;
        private int _Loose_Full_Reload_Cnt = 0;
        private int _Strict_Starting_Reload_Cnt = 0;
        private int _Loose_Starting_Reload_Cnt = 0;

        private string _Cached_Full_Strict_Combat_String = "";
        private string _Cached_Full_Loose_Combat_String = "";
        private string _Cached_Starting_Strict_Combat_String = "";
        private string _Cached_Starting_Loose_Combat_String = "";

        private string _Cached_Prev_Full_Strict_Combat_String = "";
        private string _Cached_Prev_Full_Loose_Combat_String = "";
        private string _Cached_Prev_Starting_Strict_Combat_String = "";
        private string _Cached_Prev_Starting_Loose_Combat_String = "";

        public int Strict_Full_Reload_Cnt { get => _Strict_Full_Reload_Cnt;  }
        public int Loose_Full_Reload_Cnt { get => _Loose_Full_Reload_Cnt; }
        public int Strict_Starting_Reload_Cnt { get => _Strict_Starting_Reload_Cnt; }
        public int Loose_Starting_Reload_Cnt { get => _Loose_Starting_Reload_Cnt;  }

        public bool Update_Reload(CombatStartEvent in_prev_CombatStartEvent)
        {
            _Cached_Full_Strict_Combat_String = Get_Full_Strict_Combat_String();
            _Cached_Full_Loose_Combat_String = Get_Full_Loose_Combat_String();
            _Cached_Starting_Strict_Combat_String = Get_Starting_Strict_Combat_String();
            _Cached_Starting_Loose_Combat_String = Get_Starting_Loose_Combat_String();

            _Cached_Prev_Full_Strict_Combat_String = in_prev_CombatStartEvent.Get_Full_Strict_Combat_String();
            _Cached_Prev_Full_Loose_Combat_String = in_prev_CombatStartEvent.Get_Full_Loose_Combat_String();
            _Cached_Prev_Starting_Strict_Combat_String = in_prev_CombatStartEvent.Get_Starting_Strict_Combat_String();
            _Cached_Prev_Starting_Loose_Combat_String = in_prev_CombatStartEvent.Get_Starting_Loose_Combat_String();
            
            //System.Diagnostics.Debug.WriteLine("Strict / Full:\n{0} ==\n{1}", in_prev_CombatStartEvent.Get_Full_Strict_Combat_String(), Get_Full_Strict_Combat_String());
            //System.Diagnostics.Debug.WriteLine("Loose / Full:\n{0} ==\n{1}", in_prev_CombatStartEvent.Get_Full_Loose_Combat_String(), Get_Full_Loose_Combat_String());
            System.Diagnostics.Debug.WriteLine("Strict / Starting:\n{0} ==\n{1}", _Cached_Prev_Starting_Strict_Combat_String, _Cached_Starting_Strict_Combat_String);
            System.Diagnostics.Debug.WriteLine("Loose / Starting:\n{0} ==\n{1}", _Cached_Prev_Starting_Loose_Combat_String, _Cached_Starting_Loose_Combat_String);

            if (_Cached_Prev_Full_Strict_Combat_String == _Cached_Full_Strict_Combat_String) { _Strict_Full_Reload_Cnt = 1 + in_prev_CombatStartEvent.Strict_Full_Reload_Cnt; }
            if (_Cached_Prev_Full_Loose_Combat_String == _Cached_Full_Loose_Combat_String) { _Loose_Full_Reload_Cnt = 1 + in_prev_CombatStartEvent.Loose_Full_Reload_Cnt; }
            if (_Cached_Prev_Starting_Strict_Combat_String == _Cached_Starting_Strict_Combat_String) { _Strict_Starting_Reload_Cnt = 1 + in_prev_CombatStartEvent.Strict_Starting_Reload_Cnt; }
            if (_Cached_Prev_Starting_Loose_Combat_String == _Cached_Starting_Loose_Combat_String) { _Loose_Starting_Reload_Cnt = 1 + in_prev_CombatStartEvent.Loose_Starting_Reload_Cnt; }

            return ((_Strict_Full_Reload_Cnt > 0) || (_Loose_Full_Reload_Cnt > 0) || (_Strict_Starting_Reload_Cnt > 0) || (_Loose_Starting_Reload_Cnt > 0));
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
                int early_threshold = (int)Math.Round((float)max_ID * 0.75);
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

            /*
            List<KeyValuePair<CombatEvent, CharacterListItem>> init_event_lst = new List<KeyValuePair<CombatEvent, CharacterListItem>>();

            int min_non_init_non_simple_event_id = int.MaxValue;
            
            foreach (CharacterListItem curr_char in Characters.GetAll())
            {
                if (curr_char.Character_Type == Char_Enum.Friendly) { continue; }
                int min_init_ID = int.MaxValue;
                InitiativeEvent min_init = null;

                foreach (CombatEvent curr_evnt in curr_char.Parents)
                {
                    if ((curr_evnt is InitiativeEvent)&&(curr_evnt.ID < min_init_ID))
                    {
                        min_init_ID = curr_evnt.ID;
                        min_init = (InitiativeEvent)curr_evnt;
                    }
                    else
                    {
                        if (curr_evnt is SimpleEvent) { }
                        else
                        {
                            if (min_non_init_non_simple_event_id > curr_evnt.ID)
                            {
                                min_non_init_non_simple_event_id = curr_evnt.ID;
                            }
                        }
                    }
                }

                if (min_init != null) { init_event_lst.Add(new KeyValuePair<CombatEvent, CharacterListItem>(min_init, curr_char)); }
            }

            if (min_non_init_non_simple_event_id == int.MaxValue) { throw new System.Exception("No events processed in Get_Initiative_Only_Characters"); }
            if (init_event_lst.Count == 0) { throw new System.Exception("No hostile inititative events found in Get_Initiative_Only_Characters"); }

            foreach (KeyValuePair<CombatEvent, CharacterListItem> curr_kvp in init_event_lst)
            {
                if (only_early) { if (min_non_init_non_simple_event_id > curr_kvp.Key.ID) { rtn.Add(curr_kvp.Value); } }
                else { rtn.Add(curr_kvp.Value); }
            }

            if (rtn.Count == 0) { throw new System.Exception("No initative events found at start of combat in Get_Initiative_Only_Characters"); }

            rtn.Sort((a, b) => a.Parent.ID.CompareTo(b.Parent.ID) ); // Parent (with no s) is guranteed to be the record with the lowest ID of all the potential parents.

            return rtn; */
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
        
        public override UserControl Get_UserControl_For_Display()
        {
            if (_UC_For_Display == null)
            {
                _UC_For_Display = base.Get_UserControl_For_Display();
                Grid outer_grid = (Grid)_UC_For_Display.Content;
                outer_grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });

                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new System.Windows.GridLength(0, System.Windows.GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new System.Windows.GridLength(10, System.Windows.GridUnitType.Star) });

                Grid.SetRow(grid, 3);
                Grid.SetColumn(grid, 0);
                Grid.SetColumnSpan(grid, 2);
                outer_grid.Children.Add(grid);

                string[] label_list = {
                "Event Type",
                "Number of children"
                };

                int row = 0;

                foreach (string curr_label in label_list)
                {
                    RowDefinition r = new RowDefinition();
                    r.Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto);
                    grid.RowDefinitions.Add(r);

                    if (curr_label != "")
                    {
                        Label lbl = new Label()
                        {
                            Content = curr_label,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                        };

                        Grid.SetColumn(lbl, 0);
                        Grid.SetRow(lbl, row);

                        TextBox tb = new TextBox();
                        switch (curr_label)
                        {
                            case "Event Type": tb.Text = "Combat Start"; break;
                            case "Number of children": tb.Text = Children.Count.ToString(); break;
                        }
                        tb.IsReadOnly = true;
                        Grid.SetColumn(tb, 1);
                        Grid.SetRow(tb, row);

                        grid.Children.Add(lbl);
                        grid.Children.Add(tb);
                    }

                    row++;
                }
                //I'm not sure why this is necesary, but it is.
                grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });

                RowDefinition source_r = new RowDefinition();
                source_r.Height = new System.Windows.GridLength(50, System.Windows.GridUnitType.Star);
                grid.RowDefinitions.Add(source_r);

                Label source_lbl = new Label();
                source_lbl.Content = "Source";
                Grid.SetColumn(source_lbl, 0);
                Grid.SetRow(source_lbl, row);

                WebBrowser wb = new WebBrowser()
                {
                    MaxHeight = 750
                };

                System.Text.StringBuilder long_str = new StringBuilder(Source_With_ID.Replace("–", "-").Replace("—", "--").Replace("×", "x"));

                foreach (CombatEvent curr_event in Children)
                {
                    long_str.Append(curr_event.Source_With_ID.Replace("–", "-").Replace("—", "--").Replace("×", "x"));
                }

                wb.NavigateToString(long_str.ToString());
                Grid.SetColumn(wb, 1);
                Grid.SetRow(wb, row);

                grid.Children.Add(source_lbl);
                grid.Children.Add(wb);
            }
            
            return _UC_For_Display;
        }
    }
}
