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

        public int Strict_Full_Reload_Cnt { get => _Strict_Full_Reload_Cnt;  }
        public int Loose_Full_Reload_Cnt { get => _Loose_Full_Reload_Cnt; }
        public int Strict_Starting_Reload_Cnt { get => _Strict_Starting_Reload_Cnt; }
        public int Loose_Starting_Reload_Cnt { get => _Loose_Starting_Reload_Cnt;  }

        public void Update_Smarter_Guesses_Character_Types()
        {
            int changed_cnt = 0;
            int loop_cnt = 0;

            do
            {
                changed_cnt = 0;
                loop_cnt++;
                foreach (CharacterListItem curr_char in Characters)
                {
                    changed_cnt += curr_char.Update_Smarter_Guesses_Character_Types();
                }

                if (changed_cnt > 0)
                {
                    foreach (CharacterListItem curr_char in Characters)
                    {
                        if (curr_char.Character_Type != Char_Enum.Really_Unknown)
                        {
                            foreach (CombatEvent curr_evnt in Children)
                            {
                                if (curr_evnt is AttackEvent)
                                {
                                    AttackEvent curr_atk_evnt = (AttackEvent)curr_evnt;
                                    if ((curr_atk_evnt.Source_Target_Character_Name == curr_char.Source_Character_Name)&&(curr_atk_evnt.Guess_Target_Character_Type != curr_char.Character_Type))
                                    {
                                        curr_atk_evnt.Guess_Target_Character_Type = curr_char.Character_Type;
                                    }
                                }
                            }
                        }
                    }
                }
            } while ((changed_cnt > 0)&&(loop_cnt<2));
        }
        
        public bool Update_Reload(CombatStartEvent in_prev_CombatStartEvent)
        {
            System.Diagnostics.Debug.WriteLine("Strict / Full:\n{0} ==\n{1}", in_prev_CombatStartEvent.Get_Full_Strict_Combat_String(), Get_Full_Strict_Combat_String());
            System.Diagnostics.Debug.WriteLine("Loose / Full:\n{0} ==\n{1}", in_prev_CombatStartEvent.Get_Full_Loose_Combat_String(), Get_Full_Loose_Combat_String());
            System.Diagnostics.Debug.WriteLine("Strict / Starting:\n{0} ==\n{1}", in_prev_CombatStartEvent.Get_Starting_Strict_Combat_String(), Get_Starting_Loose_Combat_String());
            System.Diagnostics.Debug.WriteLine("Loose / Starting:\n{0} ==\n{1}", in_prev_CombatStartEvent.Get_Starting_Loose_Combat_String(), Get_Starting_Loose_Combat_String());

            if (in_prev_CombatStartEvent.Get_Full_Strict_Combat_String() == Get_Full_Strict_Combat_String()) { _Strict_Full_Reload_Cnt += 1 + in_prev_CombatStartEvent.Strict_Full_Reload_Cnt; }
            if (in_prev_CombatStartEvent.Get_Full_Loose_Combat_String() == Get_Full_Loose_Combat_String()) { _Loose_Full_Reload_Cnt += 1 + in_prev_CombatStartEvent.Loose_Full_Reload_Cnt; }
            if (in_prev_CombatStartEvent.Get_Starting_Strict_Combat_String() == Get_Starting_Loose_Combat_String()) { _Strict_Full_Reload_Cnt += 1 + in_prev_CombatStartEvent.Strict_Starting_Reload_Cnt; }
            if (in_prev_CombatStartEvent.Get_Starting_Loose_Combat_String() == Get_Starting_Loose_Combat_String()) { _Loose_Starting_Reload_Cnt += 1 + in_prev_CombatStartEvent.Loose_Starting_Reload_Cnt; }

            return ((_Strict_Full_Reload_Cnt > 0) || (_Loose_Full_Reload_Cnt > 0) || (_Strict_Starting_Reload_Cnt > 0) || (_Loose_Starting_Reload_Cnt > 0));
        }

        private string Get_Full_Strict_Combat_String()
        {
            return Get_Delimited_String_From_List(Get_Initiative_Only_Characters().GetAll(), x => x.Source_Character_Name, false);
        }

        private string Get_Full_Loose_Combat_String()
        {
            return Get_Delimited_String_From_List(Get_Initiative_Only_Characters(), x => x.Friendly_Name, true);
        }

        private string Get_Starting_Strict_Combat_String()
        {
            return Get_Delimited_String_From_List(Get_Only_Early_Initiative_Only_Characters().GetAll(), x => x.Source_Character_Name, false);
        }

        private string Get_Starting_Loose_Combat_String()
        {
            return Get_Delimited_String_From_List(Get_Only_Early_Initiative_Only_Characters(), x => x.Friendly_Name, true);
        }

        private CharacterList Get_Initiative_Only_Characters()
        {
            /* This...  Doesn't work because the combatLog.txt misses too many initatitve events.  I was completely shocked, but it appears that lots of creatures dont have
             * initative events listed in the file.
             * 
             * I'm leaving the code in, but commented out, to justify *why* this method is named the way it is.  Currently, however, it just returns the entire character
             * list -- which includes characters that don't have initiative events
             */

            CharacterList rtn = new CharacterList();
            /*
            foreach (CharacterListItem curr_char in Characters)
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
                }
                if (min_init != null)
                {
                    rtn.Add(curr_char);
                }
            }*/

            rtn.AddRange(Characters);

            rtn.Sort((a, b) => a.Parent.ID.CompareTo(b.Parent.ID) ); // Parent (with no s) is guranteed to be the record with the lowest ID of all the potential parents.

            return rtn;
        }

        private CharacterList Get_Only_Early_Initiative_Only_Characters()
        {
            CharacterList rtn = new CharacterList();

            if (Children.Count == 0) { return rtn; }

            int start_of_inital_inits_ID = int.MaxValue;
            int end_of_inital_inits_ID = int.MinValue;

            Children.Sort((a, b) => a.ID.CompareTo(b.ID) ); // Sort the Children list so that the smallest ID is at the top

            foreach (CombatEvent curr_evnt in Children)
            {
                if ((curr_evnt is InitiativeEvent)&&(curr_evnt.ID < start_of_inital_inits_ID)) { start_of_inital_inits_ID = curr_evnt.ID; }
                if (!(curr_evnt is InitiativeEvent)&&(start_of_inital_inits_ID != int.MaxValue)) { break; }
                end_of_inital_inits_ID = curr_evnt.ID;
            }

            if (start_of_inital_inits_ID == int.MaxValue) { throw new System.Exception("Unable to find *ANY* InitiativeEvents in StartCombatEvent"); }

            foreach (CharacterListItem curr_char in Characters)
            {
                if (curr_char.Character_Type == Char_Enum.Friendly) { continue; }

                int min_init_ID = int.MaxValue;
                InitiativeEvent min_init = null;

                foreach (CombatEvent curr_evnt in curr_char.Parents)
                {
                    if ((curr_evnt is InitiativeEvent) && (curr_evnt.ID < min_init_ID))
                    {
                        min_init_ID = curr_evnt.ID;
                        min_init = (InitiativeEvent)curr_evnt;
                    }
                }
                if ((min_init != null)&&(min_init_ID >= start_of_inital_inits_ID)&&(min_init_ID <= end_of_inital_inits_ID))
                {
                    rtn.Add(curr_char);
                }
            }

            rtn.Sort((a, b) => a.Parent.ID.CompareTo(b.Parent.ID)); // Parent (with no s) is guranteed to be the record with the lowest ID of all the potential parents.

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
