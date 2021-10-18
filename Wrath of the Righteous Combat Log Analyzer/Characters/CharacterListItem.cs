using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public class CharacterListItem: IComparable
    {
        private CombatEvent _Parent = null;
        private CombatEventList _Parents = new CombatEventList();
        private CharacterList _Children = new CharacterList();

        // I'm making these lists and persistant for troubleshooting purposes.  This way I can output the results of VFR into the application, rather than depending on debug statements.

        private CombatEventList _VFR_Friendly_CombatEvents = new CombatEventList();
        private CombatEventList _VFR_Hostile_CombatEvents = new CombatEventList();
        private CombatEventList _VFR_Other_CombatEvents = new CombatEventList();

        public event CombatEventChanged OnCombatEventChanged;

        public virtual string Source_Character_Name { get => _Parent.Source_Character_Name; }
        public virtual string Character_Name { get => _Parent.Character_Name; set => _Parent.Character_Name = value; }
        public virtual string Friendly_Name { get => _Parent.Friendly_Name; set => _Parent.Friendly_Name = value; }
        public virtual CombatEvent.Char_Enum Character_Type
        {
            get => _Parent.Character_Type;
            set => Set_Character_Type(value);
        }

        public CombatEventList Parents { get => _Parents; }
        public CombatEvent Parent { get => _Parent; set => _Parent = value; }
        public CharacterList Children { get => _Children; }

        public string Source_With_ID
        {
            get
            {
                StringBuilder tmp_sb = new StringBuilder();
                CombatEventList tmp_lst = Get_All_CombatEvents();
                tmp_lst.Sort((first, second) => first.ID.CompareTo(second.ID));
                foreach (CombatEvent curr_evnt in tmp_lst) { tmp_sb.Append(curr_evnt.Source_With_ID + "\n"); }
                return tmp_sb.ToString();
            }
        }

        public int Set_Character_Type(CombatEvent.Char_Enum in_Char_Type)
        {
            int changed_cnt = 0;

            foreach (CombatEvent curr_event in Parents)
            {
                if (curr_event.Source_Character_Name == Source_Character_Name)
                {
                    if (curr_event.Character_Type != in_Char_Type) { changed_cnt++; curr_event.Character_Type = in_Char_Type; curr_event.Guess_Character_Type = in_Char_Type; }
                }

                if (curr_event is CombatEventTargeted)
                {
                    CombatEventTargeted curr_tgt_event = (CombatEventTargeted)curr_event;
                    if (curr_tgt_event.Source_Target_Character_Name == Source_Character_Name)
                    {
                        if (curr_tgt_event.Target_Character_Type != in_Char_Type) { changed_cnt++; curr_tgt_event.Target_Character_Type = in_Char_Type; curr_tgt_event.Guess_Target_Character_Type = in_Char_Type;  }
                    }
                }
            }

            foreach (CharacterListItem curr_child in Children) { changed_cnt += curr_child.Set_Character_Type(in_Char_Type); }

            return changed_cnt;
        }

        public CombatEventList Get_Combined_Parents()
        {/*  Useful for troubleshooting, but not necessary otherwise.
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
            }*/

            return _Parents;
        }

        private CombatEventList Get_All_CombatEvents()
        {
            CombatEventList all_events = new CombatEventList();
            all_events.AddRange(Parents);
            foreach (CharacterListItem curr_child_char in Children) { all_events.AddRange(curr_child_char.Parents); }

            return all_events;
        }

        public int Vote_For_Role()
        {
            int changed_cnt = 0;

            _VFR_Friendly_CombatEvents.Clear();
            _VFR_Hostile_CombatEvents.Clear();
            _VFR_Other_CombatEvents.Clear();
            changed_cnt=0;

            CombatEventList all_events = Get_All_CombatEvents();

            foreach (CombatEvent curr_evnt_tmp in all_events)
            {
                if (!(curr_evnt_tmp is CombatEventTargeted)) { continue; }
                if (curr_evnt_tmp is DamageEvent) { continue; } // Ignore DamageEvents -- no conclusions as to friendly / hostile can be drawn from them

                CombatEventTargeted curr_evnt = (CombatEventTargeted)curr_evnt_tmp;
                if (curr_evnt.Friendly_Name == Friendly_Name)
                {
                    // If X = the character we are interested in, these are events of the type "X <verb> Y".  X is the source of the event.

                    CombatEvent.Char_Enum proposed_char_type = curr_evnt.Character_Type_From_Target();

                    if (proposed_char_type == CombatEvent.Char_Enum.Friendly) { _VFR_Friendly_CombatEvents.Add(curr_evnt); }
                    else if (proposed_char_type == CombatEvent.Char_Enum.Hostile) { _VFR_Hostile_CombatEvents.Add(curr_evnt); }
                    else { _VFR_Other_CombatEvents.Add(curr_evnt); }
                }
                else
                {
                    // These are "Y <verb> X", where again, X is the character we are interested in.

                    CombatEvent.Char_Enum proposed_target_char_type = curr_evnt.Character_Type_From_Target();

                    // Some processing is required here -- what we have is the *source* type, but what we want is the *target* type.
                    if (curr_evnt_tmp is AttackEvent)
                    {
                        // Attacks are aimed at people of the opposing faction, generally.
                        if (proposed_target_char_type == CombatEvent.Char_Enum.Friendly) { _VFR_Hostile_CombatEvents.Add(curr_evnt); }
                        else if (proposed_target_char_type == CombatEvent.Char_Enum.Hostile) { _VFR_Friendly_CombatEvents.Add(curr_evnt); }
                        else { _VFR_Other_CombatEvents.Add(curr_evnt); }
                    }
                    else if (curr_evnt_tmp is HealingEvent)
                    {
                        // Heals are aimed at people of the same faction, generally.
                        if (proposed_target_char_type == CombatEvent.Char_Enum.Friendly) { _VFR_Friendly_CombatEvents.Add(curr_evnt); }
                        else if (proposed_target_char_type == CombatEvent.Char_Enum.Hostile) { _VFR_Hostile_CombatEvents.Add(curr_evnt); }
                        else { _VFR_Other_CombatEvents.Add(curr_evnt); }
                    }
                    else // This should never happen, but just for the sake of completeness...
                    {
                        _VFR_Other_CombatEvents.Add(curr_evnt);
                    }
                }
            }

            bool show_debug_lines = false /*(Friendly_Name == "PlaguedSmilodonSummon")*/;
            if (show_debug_lines) { System.Diagnostics.Debug.WriteLine("---"); }
            
            if (_VFR_Other_CombatEvents.Count > (_VFR_Friendly_CombatEvents.Count + _VFR_Hostile_CombatEvents.Count))
            {
                if (show_debug_lines) System.Diagnostics.Debug.WriteLine("\tVFR: {0} isn't classified due to too many 'Other' events ({1} Friendly, {2} Hostile, {3} Other)", Source_Character_Name, _VFR_Friendly_CombatEvents.Count, _VFR_Hostile_CombatEvents.Count, _VFR_Other_CombatEvents.Count);
                return 0;
            } // This shouldn't happen often, if ever, but if it does then the vote is inconclusive.
            else if (_VFR_Friendly_CombatEvents.Count > _VFR_Hostile_CombatEvents.Count)
            {
                if (show_debug_lines) System.Diagnostics.Debug.WriteLine("\tVFR: {0} appears to be Friendly ({1} Friendly, {2} Hostile, {3} Other)", Source_Character_Name, _VFR_Friendly_CombatEvents.Count, _VFR_Hostile_CombatEvents.Count, _VFR_Other_CombatEvents.Count);
                changed_cnt = Set_Character_Type(CombatEvent.Char_Enum.Friendly);
            }
            else if (_VFR_Hostile_CombatEvents.Count > _VFR_Friendly_CombatEvents.Count)
            {
                if (show_debug_lines) System.Diagnostics.Debug.WriteLine("\tVFR: {0} appears to be Hostile ({1} Friendly, {2} Hostile, {3} Other)", Source_Character_Name, _VFR_Friendly_CombatEvents.Count, _VFR_Hostile_CombatEvents.Count, _VFR_Other_CombatEvents.Count);
                changed_cnt = Set_Character_Type(CombatEvent.Char_Enum.Hostile);
            }
            else
            {
                if (show_debug_lines) System.Diagnostics.Debug.WriteLine("\tVFR: {0} isn't classified due to a tie ({1} Friendly, {2} Hostile, {3} Other)", Source_Character_Name, _VFR_Friendly_CombatEvents.Count, _VFR_Hostile_CombatEvents.Count, _VFR_Other_CombatEvents.Count);
            }

            //System.Diagnostics.Debug.WriteLine("Updated {0} combat events", changed_cnt);

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

        private UserControl _Details_UserControl = null;
        private DockPanel _Details_DockPanel = null;
        private Grid _Details_OuterGrid = null;

        private CombatStats _Details_Stats = null;

        public UserControl Get_UserControl_For_Display(bool show_all = false)
        {
            if (_Details_UserControl == null)
            {
                _Details_UserControl = new UserControl();
                ScrollViewer scrollViewer = new ScrollViewer() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
                _Details_UserControl.Content = scrollViewer;

                _Details_DockPanel = new DockPanel() { LastChildFill = false };
                scrollViewer.Content = _Details_DockPanel;

                _Details_OuterGrid = new Grid();
                DockPanel.SetDock(_Details_OuterGrid, Dock.Top);
                _Details_DockPanel.Children.Add(_Details_OuterGrid);

                _Details_OuterGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10, GridUnitType.Star) });
            }

            return Update_UserControl_For_Display(show_all);
        }

        public UserControl Update_UserControl_For_Display(bool show_all = false)
        {
            if (_Details_UserControl == null) { throw new System.Exception("In CharacterListItem.Update_UserControl_For_Display when _Details_UserControl == null."); }

            if (_Parent == null)
            {
                _Details_DockPanel.Children.Clear();

                _Details_OuterGrid = new Grid();
                DockPanel.SetDock(_Details_OuterGrid, Dock.Top);
                _Details_DockPanel.Children.Add(_Details_OuterGrid);

                _Details_OuterGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10, GridUnitType.Star) });

                _Details_OuterGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
                TextBlock this_space_deliberately_blank = new TextBlock(new Run("No items available to view."));
                Grid.SetRow(this_space_deliberately_blank, 0);
                Grid.SetColumn(this_space_deliberately_blank, 0);
                _Details_OuterGrid.Children.Add(this_space_deliberately_blank);
            }
            else
            {
                _Details_OuterGrid.RowDefinitions.Clear();
                _Details_OuterGrid.Children.Clear();

                string[,] char_info =
                {
                    { "Friendly Character Name", Friendly_Name },
                    { "Source Character Name", Source_Character_Name },
                    { "Faction", String.Format("{0} (VFR: Friendly {1}, Hostile {2}, Don't Know {3})", Character_Type, _VFR_Friendly_CombatEvents.Count, _VFR_Hostile_CombatEvents.Count, _VFR_Other_CombatEvents.Count) },
                    { "Number of characters", (Children.Count+1).ToString() }
                };

                ScrollViewer tmp_char_info_scrollViewer = _Parent.New_Windows_Table("Summary", char_info, 2, 100);
                Grid char_info_Grid = ((Grid)tmp_char_info_scrollViewer.Content);
                tmp_char_info_scrollViewer.Content = null;

                _Details_OuterGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
                Grid.SetRow(char_info_Grid, _Details_OuterGrid.RowDefinitions.Count - 1);
                Grid.SetColumn(char_info_Grid, 0);
                _Details_OuterGrid.Children.Add(char_info_Grid);

                CombatEventList all_events = null;
                if (show_all) { all_events = Get_All_CombatEvents(); System.Diagnostics.Debug.WriteLine("All stats"); }
                else { all_events = _Parents; }

                TextBlock stats_Title = new TextBlock(new Run("Statistics") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline }) { HorizontalAlignment = HorizontalAlignment.Center };
                _Details_OuterGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
                Grid.SetRow(stats_Title, _Details_OuterGrid.RowDefinitions.Count - 1);
                Grid.SetColumn(stats_Title, 0);
                _Details_OuterGrid.Children.Add(stats_Title);

                Run stats_Note_Run = new Run("Statistics include attacks and damage targeted at a character, not just attacks and damage inflicted by the character.");
                TextBlock stats_Note = new TextBlock(stats_Note_Run) { HorizontalAlignment = HorizontalAlignment.Left };
                _Details_OuterGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
                Grid.SetRow(stats_Note, _Details_OuterGrid.RowDefinitions.Count - 1);
                Grid.SetColumn(stats_Note, 0);
                _Details_OuterGrid.Children.Add(stats_Note);

                if (_Details_Stats == null) { _Details_Stats = new CombatStats(); }
                if (_Details_Stats.CombatEvent_Count != all_events.Count) { _Details_Stats.Recalculate_Stats(all_events); }
                UserControl stats_uc = _Details_Stats.Get_Analysis_UserControl();
                if (stats_uc.Parent != null) // This happens when you click on the root tree node (which shows all events that fall under this friendlyname), then click on the detail that corrsponds with the parent.
                {
                    ((Grid)stats_uc.Parent).Children.Remove(stats_uc);
                }
                _Details_Stats.Update_Analysis_UserControl();

                _Details_OuterGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
                Grid.SetRow(stats_uc, _Details_OuterGrid.RowDefinitions.Count - 1);
                Grid.SetColumn(stats_uc, 0);
                _Details_OuterGrid.Children.Add(stats_uc);

                TextBlock source_Title = new TextBlock(new Run("Source") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline }) { HorizontalAlignment = HorizontalAlignment.Center };
                _Details_OuterGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
                Grid.SetRow(source_Title, _Details_OuterGrid.RowDefinitions.Count - 1);
                Grid.SetColumn(source_Title, 0);
                _Details_OuterGrid.Children.Add(source_Title);

                WebBrowser webBrowser = _Parent.New_WebBrowser();

                _Details_OuterGrid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(800, System.Windows.GridUnitType.Pixel) });
                Grid.SetRow(webBrowser, _Details_OuterGrid.RowDefinitions.Count - 1);
                Grid.SetColumn(webBrowser, 0);
                Grid.SetColumnSpan(webBrowser, 2);
                _Details_OuterGrid.Children.Add(webBrowser);

                Button refresh_button = new Button() { Content = new TextBlock(new Run("Refresh")), Width = 100, HorizontalAlignment = HorizontalAlignment.Right };
                refresh_button.Click +=
                    (sender, obj) =>
                    {
                        refresh_button.IsEnabled = false;
                        webBrowser.NavigateToString(string.Format("Loading {0} events -- please be patient", Children.Count));
                        string tmp_string = "";

                        System.ComponentModel.BackgroundWorker bg = new System.ComponentModel.BackgroundWorker();
                        bg.DoWork += (bg_sender, bg_obj) => tmp_string = _Parent.Filter_String_For_WebBrowser(Source_With_ID);
                        bg.RunWorkerCompleted += (bg_sender, bg_obj) => { webBrowser.NavigateToString(tmp_string); refresh_button.IsEnabled = true; };
                        bg.RunWorkerAsync();
                    };
                Grid.SetRow(refresh_button, _Details_OuterGrid.RowDefinitions.Count - 2); // Backup to the "Source" title row.
                Grid.SetColumn(refresh_button, 0);
                _Details_OuterGrid.Children.Add(refresh_button);

                if (Children.Count < 1000) { webBrowser.NavigateToString(_Parent.Filter_String_For_WebBrowser(Source_With_ID)); }
                else { webBrowser.NavigateToString(string.Format("Refresh to view {0} events -- this may take a while to generate!", Children.Count)); }
            }

            return _Details_UserControl;
        }

    }
}
