using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Windows;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public delegate void NeedToCalculateStatsDelegate(CombatEventContainer sender, CombatStats newStatsToRecalc, CombatEventList inCombatEventList);
    public delegate void NeedToUpdateCharacterLists(CombatEventContainer sender, CharacterList newOverride);
    public delegate void ReloadUpdated(CombatEventContainer sender);

    public class CombatEventContainer : CombatEvent
    {
        #region Override Properties
        public override string Character_Name { get => _Filename; set => throw new NotImplementedException(); }
        public override string Source_Character_Name => throw new NotImplementedException();
        public override List<Die_Roll> Die_Rolls => throw new System.NotImplementedException();

        public override string Source
        {
            get
            {
                StringBuilder tmp = new StringBuilder(base.Source);
                int curr_combat_ID = -1;
                foreach (CombatEvent curr_evnt in Children)
                {
                    if (curr_combat_ID == -1) { curr_combat_ID = curr_evnt.Combat_ID; }
                    if (curr_combat_ID != curr_evnt.Combat_ID) { curr_combat_ID = curr_evnt.Combat_ID; tmp.Append("<hr>\n"); }
                    tmp.Append(curr_evnt.Source + "\n");
                }
                return tmp.ToString();
            }
            set => base.Source = value;
        }

        public override string Source_With_ID
        {
            get
            {
                StringBuilder tmp = new StringBuilder(base.Source_With_ID);
                int curr_combat_ID = -1;
                foreach (CombatEvent curr_evnt in Children)
                {
                    if (curr_combat_ID == -1) { curr_combat_ID = curr_evnt.Combat_ID; }
                    if (curr_combat_ID != curr_evnt.Combat_ID) { curr_combat_ID = curr_evnt.Combat_ID; tmp.Append("<hr>\n"); }
                    tmp.Append(curr_evnt.Source_With_ID + "\n");
                }
                return tmp.ToString();
            }
        }
        #endregion
        #region Override Methods
        public override List<Die_Roll> Parse(string line) { _Filename = line; return null; }

        public CombatEventContainer(int inID, int inCombatID, string inLine) : base(inID, inCombatID, inLine) { }
        #endregion

        public event NeedToUpdateCharacterLists OnNeedToUpdateCharacterLists;
        public event NeedToCalculateStatsDelegate OnNeedToCalculateStats;
        public event ReloadUpdated OnReloadUpdated;

        private string _Filename = "";
        private CharacterList _Characters_Override = new CharacterList();
        private CharacterList _Characters = new CharacterList();
        private CombatStats _Stats = new CombatStats();
        
        private int _Children_Count_When_Characters_Last_Refreshed = -1;
        private int _Children_Count_When_Stats_Last_Refreshed = -1;
        private bool _Has_Rendered_Character_UC_After_Refresh = false;
        private bool _Has_Rendered_Stats_UC_After_Refresh = false;
        private bool _Updating_Characters_List = false;

        private CombatEventContainer _Cached_Prev_CombatEventContainer = null;
        private CombatEventContainer _Cached_Next_CombatEventContainer = null;

        private bool _Characters_Updating_Async = false;
        private bool _Stats_Updating_Async = false;
        private bool _Children_Changed = false;

        private UserControl _UC_For_Display = null;
        private UserControl _UC_For_Characters = null;

        private StackPanel _Character_List_Panel = null;
        private StackPanel _Stats_Panel = null;

        public string Filename { get => _Filename; }

        public CombatStats Stats { get => _Stats; }

        public CharacterList Characters_Override { get => _Characters_Override; }

        public CombatEventContainer Prev_CombatEventContainer
        {
            get
            {
                if (_Cached_Prev_CombatEventContainer == null)
                {
                    CombatEvent tmp_event = this;

                    while ((tmp_event != null) && (!(tmp_event.Prev_CombatEvent is CombatEventContainer))) { tmp_event = tmp_event.Prev_CombatEvent; }

                    if (tmp_event == null) { _Cached_Prev_CombatEventContainer = null; }
                    else { _Cached_Prev_CombatEventContainer = (CombatEventContainer)tmp_event.Prev_CombatEvent; }
                }           

                return _Cached_Prev_CombatEventContainer;

            }
        }

        public CombatEventContainer Next_CombatEventContainer
        {
            get
            {
                if (_Cached_Next_CombatEventContainer == null)
                {
                    CombatEvent tmp_event = this;

                    while ((tmp_event != null) && (!(tmp_event.Next_CombatEvent is CombatEventContainer))) { tmp_event = tmp_event.Next_CombatEvent; }

                    if (tmp_event == null) { _Cached_Next_CombatEventContainer = null; }
                    else { _Cached_Next_CombatEventContainer = (CombatEventContainer)tmp_event.Next_CombatEvent; }
                }

                return _Cached_Next_CombatEventContainer;
            }
        }
               
        public virtual int Reload_Cnt
        {
            get => Children.Sum((x) => 
            
                ((x is CombatStartEvent) && ((CombatStartEvent)x).IsReload) ? 1 : 
                  !(x is CombatStartEvent)&&(x is CombatEventContainer) ? ((CombatEventContainer)x).Reload_Cnt : 0
            );
            protected set => throw new System.NotImplementedException();
        }

        public virtual int Combats_Cnt_Without_Reload
        {
            get
            {
                int sum = 0;

                foreach (CombatEvent curr_evnt in Children) { if (curr_evnt is CombatEventContainer) { sum += ((CombatEventContainer)curr_evnt).Combats_Cnt_Without_Reload; } }

                return sum;
            }
        }

        public virtual int Combats_Cnt_With_At_Least_One_Reload
        {
            get
            {
                int sum = 0;

                foreach (CombatEvent curr_event in Children) { if (curr_event is CombatEventContainer) { sum += ((CombatEventContainer)curr_event).Combats_Cnt_With_At_Least_One_Reload; } }

                return sum;
            }
        }

        public virtual int Combats_Cnt
        {
            get
            {
                int sum = 0;

                foreach (CombatEvent curr_evnt in Children) { if (curr_evnt is CombatEventContainer) { sum += ((CombatEventContainer)curr_evnt).Combats_Cnt; } }

                return sum;

            }
        }
        
        public virtual bool Update_Reload()
        {
            bool rtn = false;

            foreach (CombatEvent curr_event in Children)
            {
                if (curr_event is CombatEventContainer)
                {
                    // This statement:

                    // rtn = ((CombatEventContainer)curr_event).Update_Reload() || rtn;

                    // *does not* produce the same results as

                    // rtn = rtn || ((CombatEventContainer)curr_event).Update_Reload();

                    // Why is left as an exercise for the reader. :)
                    rtn = ((CombatEventContainer)curr_event).Update_Reload() || rtn;
                }
            }

            return rtn;
        }

        public void Clear()
        {
            _Filename = "";
            // _Characters_Override.Clear();
            _Characters.Clear();
            Children.Clear();
            _Stats.Clear_Stats();

            _Cached_Prev_CombatEventContainer = null;
            _Cached_Next_CombatEventContainer = null;

            _Children_Count_When_Characters_Last_Refreshed = -1;
            _Children_Count_When_Stats_Last_Refreshed = -1;
            _Has_Rendered_Character_UC_After_Refresh = false;
            _Has_Rendered_Stats_UC_After_Refresh = false;
            _Updating_Characters_List = false;

            _Characters_Updating_Async = false;
            _Stats_Updating_Async = false;
            _Children_Changed = false;

            base.Source = "";
        }


        #region Dealing with Characters List
        public CharacterList Characters
        {
            get
            {
                if (!_Updating_Characters_List) { Update_Characters_List(); }
                return _Characters;
            }
        }

        private string Current_Character_Factions_To_String()
        {
            string rtn = "";
            foreach (CharacterListItem curr_itm in _Characters) { rtn += string.Format("{0} = {1}, ", curr_itm.Source_Character_Name, curr_itm.Character_Type); }
            if (rtn != "") { rtn = rtn.Substring(0, rtn.Length - 2); }
            return rtn;
        }

        public void Vote_For_Role()
        {
            int changed_cnt = 0;
            int loop_cnt = 0;

            do
            {
                //System.Diagnostics.Debug.WriteLine("Loop Cnt = {0}", loop_cnt);
                changed_cnt = 0;
                loop_cnt++;
                if (loop_cnt > 5) { System.Diagnostics.Debug.WriteLine(Current_Character_Factions_To_String()); }
                if (loop_cnt > 10) { throw new System.Exception("Stuck in a loop"); }
                foreach (CharacterListItem curr_char in Characters)
                {
                    //System.Diagnostics.Debug.WriteLine("Checking {0}", curr_char.Source_Character_Name);
                    changed_cnt += curr_char.Vote_For_Role();
                }
                // System.Diagnostics.Debug.WriteLine("changed_cnt = {0}", changed_cnt);
            } while ((changed_cnt > 0));

            OnReloadUpdated?.Invoke(this);
        }

        protected void Update_Characters_List()
        {
            if ((_Children_Count_When_Characters_Last_Refreshed != Children.Count)||(_Children_Changed))
            {
                Force_Update_Characters_List();
            }
        }

        protected void Force_Update_Characters_List()
        {
            // *Don't* clear the _Characters list!
            //
            // Any overrides the users has applied should still be valid -- we just need to add more items to the list, or update it with changes from elsewhere
            
            /* Task.Run( () => */
            {
                _Updating_Characters_List = true; // Suppress change notifications while the update is performed.

                // System.Diagnostics.Debug.WriteLine("Entering Update_Characters_List() {0}, {1} Events", System.DateTime.Now, Children.Count);
                
                foreach (CombatEvent curr_event in Children)
                {
                    if (curr_event is CombatStartEvent) { }
                    else if ((curr_event is SimpleEvent) && (((SimpleEvent)curr_event).Subtype != "Death")) { }
                    else
                    {
                        _Characters.Add(new CharacterListItem(curr_event));
                        // This line *DOES* do something new -- it adds the /target/ of the event to the characters list, if it isn't a duplicate.
                        if (curr_event is CombatEventTargeted) { _Characters.Add(new TargetedCharacterListItem((CombatEventTargeted)curr_event)); }
                    } // CharacterListItem and CharacterList manage duplicate entries -- no need to do so here
                }
                Vote_For_Role();

                _Children_Count_When_Characters_Last_Refreshed = Children.Count;
                _Children_Changed = false;
                _Has_Rendered_Character_UC_After_Refresh = false; // Need to rebuild the control.
                _Has_Rendered_Stats_UC_After_Refresh = false; // Stats also need to be rebuilt.
                _Updating_Characters_List = false; // Reenable change notifications.

                // System.Diagnostics.Debug.WriteLine("Exiting Update_Characters_List() {0}", System.DateTime.Now);
            }/* ).Wait(); */
        }

        private UserControl Get_UserControl_For_Characters()
        {
            if (_UC_For_Characters == null) { _Characters.OnCombatEventChanged += new CombatEventChanged(CombatEventChanged); }

            Update_Characters_List();

            if (_UC_For_Characters == null)
            {
                _UC_For_Characters = new UserControl();
                /*
                ScrollViewer scrollViewer = new ScrollViewer() { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                _UC_For_Characters.Content = scrollViewer;
                */
                DockPanel dockPanel = new DockPanel() { LastChildFill = true };
                _UC_For_Characters.Content = dockPanel;

                Grid grid = new Grid();
                DockPanel.SetDock(grid, Dock.Top);
                dockPanel.Children.Add(grid);
            }

            return _UC_For_Characters;
        }

        private void CombatEventChanged(CombatEvent source)
        {
            if (!_Updating_Characters_List) { _Children_Changed = true; }
        }

        private UserControl Update_Characters_UserControl()
        {
            UserControl tmp_UC = Get_UserControl_For_Characters();

            if (_Has_Rendered_Character_UC_After_Refresh) { return tmp_UC; }

            // System.Diagnostics.Debug.WriteLine("Entering Update_Characters_UserControl() {0}", System.DateTime.Now);

            _Has_Rendered_Character_UC_After_Refresh = true;

            Grid grid = ((Grid)((DockPanel)tmp_UC.Content).Children[0]);
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();

            string[] Titles = { "Friendly", "Hostile", "Summon", "Unknown" };

            int max_len = 0;
            CharacterList friendly = new CharacterList();
            CharacterList hostile = new CharacterList();
            CharacterList summon = new CharacterList();
            CharacterList unknown = new CharacterList();

            foreach (CharacterListItem curr_itm in _Characters)
            {
                if (curr_itm.Character_Type == Char_Enum.Friendly) { friendly.Add(curr_itm); }
                else if (curr_itm.Character_Type == Char_Enum.Hostile) { hostile.Add(curr_itm); }
                else if (curr_itm.Character_Type == Char_Enum.Summon) { summon.Add(curr_itm); }
                else { unknown.Add(curr_itm); }
            }

            friendly.Sort();
            hostile.Sort();
            summon.Sort();
            unknown.Sort();

            max_len = Math.Max(friendly.Count, Math.Max(hostile.Count, Math.Max(summon.Count, unknown.Count)));

            if (max_len > 0)
            {
                grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });

                for (int col = 0; col <= (Titles.GetUpperBound(0) * 2); col++)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new System.Windows.GridLength(5, System.Windows.GridUnitType.Star) });

                    if (
                        (((col / 2) == 0) && (friendly.Count > 0)) ||
                        (((col / 2) == 1) && (hostile.Count > 0)) ||
                        (((col / 2) == 2) && (summon.Count > 0)) ||
                        (((col / 2) == 3) && (unknown.Count > 0))
                        )
                    {
                        if ((col % 2) == 0)
                        {
                            TextBlock tb = new TextBlock() { HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
                            tb.Text = Titles[col / 2];
                            Grid.SetRow(tb, 0);
                            Grid.SetColumn(tb, col);
                            Grid.SetColumnSpan(tb, 2);
                            grid.Children.Add(tb);
                        }
                    }
                    else
                    {
                        // Empty columns (columns that will have no text boxes in them) should be hidden.
                        //
                        // We *could* delete them altogether, but this would require special logic elsewhere, and why bother (the extra render time is minimal)

                        grid.ColumnDefinitions[col].Width = new System.Windows.GridLength(0, System.Windows.GridUnitType.Pixel);
                    }
                }

                for (int row = 0; row <= max_len + 1; row++)
                {
                    grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });
                }

                CreateTextEditBoxes(friendly, grid, 0);
                CreateTextEditBoxes(hostile, grid, 2);
                CreateTextEditBoxes(summon, grid, 4);
                CreateTextEditBoxes(unknown, grid, 6);

                CreateTypeSelectionComboBoxes(friendly, grid, 1);
                CreateTypeSelectionComboBoxes(hostile, grid, 3);
                CreateTypeSelectionComboBoxes(summon, grid, 5);
                CreateTypeSelectionComboBoxes(unknown, grid, 7);

                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new System.Windows.GridLength(5, System.Windows.GridUnitType.Star) });
                Button refresh_button = new Button() { Content = "Refresh", Tag = grid };
                refresh_button.Click += Refresh_button_Click;
                Grid.SetColumn(refresh_button, 8);
                Grid.SetRow(refresh_button, 0);
                Grid.SetRowSpan(refresh_button, grid.RowDefinitions.Count);
                grid.Children.Add(refresh_button);
            }

            System.Diagnostics.Debug.WriteLine("Exiting Update_Characters_UserControl() {0}", System.DateTime.Now);

            return tmp_UC;
        }

        #region Characters UserControl Helpers
        private void Refresh_button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button inBtn = (Button)sender;
            Grid inGrid = (Grid)inBtn.Tag;
            bool madeChange = false;

            foreach (System.Windows.UIElement curr_elem in inGrid.Children)
            {
                if (curr_elem is Grid)
                {
                    foreach (System.Windows.UIElement inner_curr_elem in ((Grid)curr_elem).Children)
                    {
                        if (inner_curr_elem is TextBox)
                        {
                            TextBox textBox = (TextBox)inner_curr_elem;
                            CharacterListItem curr_itm = (CharacterListItem)textBox.Tag;

                            if (curr_itm.Friendly_Name != textBox.Text)
                            {
                                madeChange = true;
                                curr_itm.Friendly_Name = textBox.Text;
                                _Characters_Override.Add(curr_itm);
                                foreach (CharacterListItem child_itm in curr_itm.Children)
                                {
                                    child_itm.Friendly_Name = textBox.Text;
                                    _Characters_Override.Add(child_itm);
                                }
                            }
                        }
                    }
                }
                else if (curr_elem is ComboBox)
                {
                    ComboBox comboBox = (ComboBox)curr_elem;
                    CharacterListItem curr_itm = (CharacterListItem)comboBox.Tag;

                    CombatEvent.Char_Enum new_val = Char_Enum.Unknown;
                    if (comboBox.SelectedIndex == 0) { new_val = Char_Enum.Friendly; }
                    else if (comboBox.SelectedIndex == 1) { new_val = Char_Enum.Hostile; }
                    else if (comboBox.SelectedIndex == 2) { new_val = Char_Enum.Summon; }
                    else if (comboBox.SelectedIndex == 3) { new_val = Char_Enum.Unknown; }

                    if (curr_itm.Character_Type != new_val)
                    {
                        madeChange = true;
                        curr_itm.Set_Character_Type(new_val);
                        _Characters_Override.Add(curr_itm);
                    }
                }
            }

            if (madeChange)
            {
                _Has_Rendered_Character_UC_After_Refresh = false;
                _Has_Rendered_Stats_UC_After_Refresh = false;

                Update_Reload();

                Update_Characters_UserControl();  // This is reasonably fast, even when the number of characters is large.

                if (OnNeedToUpdateCharacterLists != null) // This can take a long time, so we want do it async with progress indicators
                {
                    _Characters_Updating_Async = true;
                    _UC_For_Characters.IsEnabled = false; // Can't make changes until these changes have been completed.
                    OnNeedToUpdateCharacterLists.Invoke(this, _Characters_Override);
                }
                else // Or not, not is good too...
                {
                    Children.Update_With_CharacterList(_Characters_Override);
                    Update_CharacterList_Callback();
                }
            }
        }

        public void Update_CharacterList_Callback()
        {
            Recalc_Stats_If_Needed();
            _Characters_Updating_Async = false;
            _UC_For_Characters.IsEnabled = true;
        }

        private void CreateTextEditBoxes(CharacterList inList, Grid inGrid, int inCol)
        {
            int row = 1;
            CharacterListItem prev_itm = null;
            foreach (CharacterListItem curr_itm in inList)
            {
                Grid inner_grid = new Grid();
                Grid.SetRow(inner_grid, row);
                Grid.SetColumn(inner_grid, inCol);
                inGrid.Children.Add(inner_grid);
                row++;

                inner_grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new System.Windows.GridLength(10, System.Windows.GridUnitType.Star) });
                inner_grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new System.Windows.GridLength(0, System.Windows.GridUnitType.Star) });
                inner_grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });

                TextBox tb = new TextBox()
                {
                    Text = curr_itm.Friendly_Name,
                    Tag = curr_itm,
                    VerticalAlignment = System.Windows.VerticalAlignment.Bottom,
                    MaxWidth = 200
                };
                Grid.SetRow(tb, 0);
                Grid.SetColumn(tb, 0);
                inner_grid.Children.Add(tb);                

                TextBlock textBlock = new TextBlock()
                {
                    Margin = new System.Windows.Thickness(5, 0, 0, 0),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    VerticalAlignment = System.Windows.VerticalAlignment.Bottom
                };
                Hyperlink textBlock_link = new Hyperlink(new Run(String.Format("x{0,3}", curr_itm.Children.Count + 1)));
                textBlock_link.Tag = curr_itm;
                textBlock_link.Click += new System.Windows.RoutedEventHandler(OnClickForCharacterDetails);
                textBlock.Inlines.Add(textBlock_link);
                
                Grid.SetRow(textBlock, 0);
                Grid.SetColumn(textBlock, 1);
                inner_grid.Children.Add(textBlock);

                if (prev_itm != null) { if (curr_itm.Friendly_Name == prev_itm.Friendly_Name) { throw new System.Exception("Added two entries with the same text back-to-back"); } }
                prev_itm = curr_itm;
            }
        }

        private DockPanel _Details_Dock_Panel = null;
        
        private void OnClickForCharacterDetails(object sender, RoutedEventArgs e)
        {
            Hyperlink inHyperlink = (Hyperlink)sender;
            CharacterListItem itm_to_get_details_for = (CharacterListItem)inHyperlink.Tag;

            Window Details_Window = new Window()
            {
                Height = 750,
                Width = 1250,
                Title = "Character details for " + itm_to_get_details_for.Friendly_Name
            };
            Grid grid = new Grid();
            Details_Window.Content = grid;

            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10, GridUnitType.Star) }); // 300
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50, GridUnitType.Star) }); // 500

            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(50, GridUnitType.Star) });
            TreeView details_treeview = new TreeView();
            Grid.SetRow(details_treeview, 0);
            Grid.SetColumn(details_treeview, 0);
            grid.Children.Add(details_treeview);

            TreeViewItem root_tvi = new TreeViewItem() { Header = itm_to_get_details_for.Friendly_Name, IsExpanded = true, Tag = itm_to_get_details_for };
            root_tvi.Selected += Character_Details_TreeViewItem_Selected;
            details_treeview.Items.Add(root_tvi);

            TreeViewItem tmp = new TreeViewItem() { Header = itm_to_get_details_for.Source_Character_Name, Tag = itm_to_get_details_for };
            tmp.Selected += Character_Details_TreeViewItem_Selected;
            root_tvi.Items.Add(tmp);

            itm_to_get_details_for.Children.Sort(Comparer<CharacterListItem>.Create((first, second) => first.Parent.ID.CompareTo(second.Parent.ID)));
            foreach (CharacterListItem curr_itm in itm_to_get_details_for.Children)
            {
                tmp = new TreeViewItem() { Header = curr_itm.Source_Character_Name, Tag = curr_itm };
                tmp.Selected += Character_Details_TreeViewItem_Selected;
                root_tvi.Items.Add(tmp);
            }

            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            Button OK_btn = new Button() { Content = "OK" };
            OK_btn.IsCancel = true;
            Grid.SetRow(OK_btn, 1);
            Grid.SetColumn(OK_btn, 0);
            Grid.SetColumnSpan(OK_btn, 2);
            grid.Children.Add(OK_btn);

            _Details_Dock_Panel = new DockPanel() { LastChildFill = false };
            Grid.SetRow(_Details_Dock_Panel, 0);
            Grid.SetColumn(_Details_Dock_Panel, 1);
            grid.Children.Add(_Details_Dock_Panel);

            root_tvi.IsSelected = true;
            details_treeview.Focus();

            Details_Window.ShowDialog();
        }

        private void Character_Details_TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            CharacterListItem itm_clicked = ((CharacterListItem)((TreeViewItem)sender).Tag);

            if (e.Handled) { }

            _Details_Dock_Panel.Children.Clear();
            if (itm_clicked.Source_Character_Name == ((TreeViewItem)sender).Header.ToString())
            {
                UserControl details_uc = itm_clicked.Get_UserControl_For_Display(false);
                if (details_uc.Parent != null) { ((DockPanel)details_uc.Parent).Children.Remove(details_uc); }
                DockPanel.SetDock(details_uc, Dock.Top);
                _Details_Dock_Panel.Children.Add(details_uc);
            }
            else
            {
                UserControl details_uc = itm_clicked.Get_UserControl_For_Display(true);
                if (details_uc.Parent != null) { ((DockPanel)details_uc.Parent).Children.Remove(details_uc); }
                DockPanel.SetDock(details_uc, Dock.Top);
                _Details_Dock_Panel.Children.Add(details_uc);
            }

            e.Handled = true;
        }

        private void CreateTypeSelectionComboBoxes(CharacterList inList, Grid inGrid, int inCol)
        {
            int row = 1;
            foreach (CharacterListItem curr_itm in inList)
            {
                ComboBox comboBox = CreateComboBoxForCharType();
                comboBox.Tag = curr_itm;

                if (curr_itm.Character_Type == Char_Enum.Friendly) { comboBox.SelectedIndex = 0; }
                else if (curr_itm.Character_Type == Char_Enum.Hostile) { comboBox.SelectedIndex = 1; }
                else if (curr_itm.Character_Type == Char_Enum.Summon) { comboBox.SelectedIndex = 2; }
                else if (curr_itm.Character_Type == Char_Enum.Unknown) { comboBox.SelectedIndex = 3; }
                else { throw new System.Exception("Unexpected value in CreateTypeSelectionComboBoxes"); }

                Grid.SetRow(comboBox, row);
                Grid.SetColumn(comboBox, inCol);
                inGrid.Children.Add(comboBox);

                row++;
            }
        }

        private ComboBox CreateComboBoxForCharType()
        {
            ComboBox rtn = new ComboBox() { Margin = new System.Windows.Thickness(10, 0, 10, 0) };
            rtn.Items.Add(new ComboBoxItem() { Content = "Friendly" });
            rtn.Items.Add(new ComboBoxItem() { Content = "Hostile" });
            rtn.Items.Add(new ComboBoxItem() { Content = "Summon" });
            rtn.Items.Add(new ComboBoxItem() { Content = "Unknown" });

            return rtn;
        }
        #endregion
        #endregion

        #region Dealing with Stats
        public override UserControl Update_Display_UserControl()
        {
            Get_UserControl_For_Display();  // This will create the control if required

            Update_Characters_UserControl(); // This will re-render the control, if required, otherwise return the previously created instance

            Recalc_Stats_If_Needed();

            return _UC_For_Display;
        }

        public bool Recalc_Stats_Immediately_If_needed() // This exists to allow an accurate CSV file to be generated, and should be wrapped in an async wrapper to ensure proper responsiveness
        {
            if (_Stats_Updating_Async) { return false; }  // Already generating stats, so can't do it again

            if ((!_Has_Rendered_Stats_UC_After_Refresh) || (_Children_Count_When_Stats_Last_Refreshed != Children.Count))
            {
                _Stats_Updating_Async = true;
                _Stats.Clear_Stats();
                _Stats.Process_Events(Children);
                _Stats.Build_Tables();
                Update_Stats_Callback();
            }

            return true;
        }

        private void Recalc_Stats_If_Needed()
        {
            if (_Stats_Updating_Async) { return; } // We are already updating the stats, don't try to start again.
            if ((!_Has_Rendered_Stats_UC_After_Refresh) || (_Children_Count_When_Stats_Last_Refreshed != Children.Count))
            {
                _Stats.Clear_Stats();
                if (OnNeedToCalculateStats != null) // Stat calculation takes a long time, potentially, so this should be in its own thread, with UI updates, so we bubble back up to do that
                {
                    _Stats_Updating_Async = true;
                    OnNeedToCalculateStats.Invoke(this, _Stats, this.Children);
                    FadeStats();
                }
                else // Well, maybe not. :)
                {
                    _Stats.Process_Events(Children);
                    _Stats.Build_Tables();
                    Update_Stats_Callback();
                }
            }
        }

        public void Force_Rebuild_Of_Character_Data()
        {
            _Children_Count_When_Characters_Last_Refreshed = -1;
            _Characters_Override.Clear();
            _Characters.Clear();
            Update_Characters_List();
        }

        public void Update_Stats_Callback() // This is the callback function for when the stats recalc has completed.
        {
            _Stats_Updating_Async = false;
            UnfadeStats();
            _Stats.Update_Analysis_UserControl();
            
            _Has_Rendered_Stats_UC_After_Refresh = true;
            _Children_Count_When_Stats_Last_Refreshed = Children.Count;
        }

        private void FadeStats()
        {
            if (_Stats_Panel == null) { throw new System.Exception("In fade stats, but _Stats_Panel is null."); }
            UserControl stats_userControl = (UserControl)_Stats_Panel.Children[0]; // There will be only one child, and it will always be a UserControl control.

            System.Windows.Style faded_style = new System.Windows.Style();
            faded_style.Setters.Add(new System.Windows.Setter(Control.ForegroundProperty, System.Windows.Media.Brushes.LightGray));

            foreach (System.Windows.UIElement curr_UI_elem in ((Grid)stats_userControl.Content).Children)
            {
                Control curr_control = curr_UI_elem as Control;
                if (curr_control != null) { curr_control.Style = faded_style; }
                else
                {
                    System.Windows.FrameworkElement curr_framework_elem = curr_UI_elem as System.Windows.FrameworkElement;
                    if (curr_framework_elem != null)
                    {
                        curr_framework_elem.Style = faded_style;
                    }
                }
            }
        }

        private void UnfadeStats()
        {
            if (_Stats_Panel == null) { return; }
            UserControl stats_userControl = (UserControl)_Stats_Panel.Children[0]; // There will be only one child, and it will always be a UserControl control.

            foreach (System.Windows.UIElement curr_UI_elem in ((Grid)stats_userControl.Content).Children)
            {
                Control curr_control = curr_UI_elem as Control;
                if (curr_control != null) { curr_control.Style = null; }
                else
                {
                    System.Windows.FrameworkElement curr_framework_elem = curr_UI_elem as System.Windows.FrameworkElement;
                    if (curr_framework_elem != null)
                    {
                        curr_framework_elem.Style = null;
                    }
                }
            }
        }

        public override UserControl Get_UserControl_For_Display()
        {
            string[,] data_reload =
                    {
                        { "# of distinct combats", Combats_Cnt_Without_Reload.ToString() },
                        { "# of distinct combats reloaded at least once", Combats_Cnt_With_At_Least_One_Reload.ToString() },
                        { "Reload Rate", string.Format("{0:P}", (float)Combats_Cnt_With_At_Least_One_Reload / (float)Combats_Cnt_Without_Reload) }
                    };

            if (_UC_For_Display == null)
            {
                _UC_For_Display = new UserControl();

                ScrollViewer scrollViewer = new ScrollViewer() { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                _UC_For_Display.Content = scrollViewer;

                DockPanel dockPanel = new DockPanel() { LastChildFill = true };
                scrollViewer.Content = dockPanel;

                Grid grid = new Grid();
                DockPanel.SetDock(grid, Dock.Top);
                dockPanel.Children.Add(grid);

                Update_Characters_List();
                Update_Reload();

                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new System.Windows.GridLength(0, System.Windows.GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new System.Windows.GridLength(50, System.Windows.GridUnitType.Star) });

                if (this is CombatStartEvent) { }
                else
                {
                    grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
                    ScrollViewer data_scrollViewer = New_Windows_Table("Reload Statistics", data_reload, 1, 500);
                    Grid data_Grid = (Grid)data_scrollViewer.Content;
                    data_scrollViewer.Content = null;

                    Grid.SetRow(data_Grid, grid.RowDefinitions.Count - 1);
                    Grid.SetColumn(data_Grid, 0);
                    Grid.SetColumnSpan(data_Grid, 2);
                    grid.Children.Add(data_Grid);

                    // This doesn't actually do anything, because while nested CombatEventContainers is a thing for display (in the tree view), the data structures aren't nested.
                    // The root node directly contains all of the events, including combatstartevents, for all data that is loaded.

                    // I'm leaving it in because...  Well, it isn't *impossible* that I'll change how things work, and this would be a useful think to have if I do. :)

                    List<CombatEvent> Non_CombatStartEvent_Children = Children.FindAll((curr_evnt) => ((curr_evnt is CombatEventContainer) && (!(curr_evnt is CombatStartEvent))));

                    if (Non_CombatStartEvent_Children.Count > 0)
                    {
                        string[,] file_reload_stats = new string[Non_CombatStartEvent_Children.Count+1, 1];
                        file_reload_stats[0, 0] = "Filename";
                        file_reload_stats[0, 1] = "Reload Rate";
                        int row_cnt = 1;
                        foreach (CombatEvent curr_event in Non_CombatStartEvent_Children)
                        {
                            CombatEventContainer curr_CEC = (CombatEventContainer)curr_event;

                            curr_CEC.Update_Characters_List();
                            curr_CEC.Update_Reload();

                            file_reload_stats[row_cnt, 0] = System.IO.Path.GetFileName(curr_CEC.Filename);
                            file_reload_stats[row_cnt, 1] = string.Format("{0:P}", (float)curr_CEC.Combats_Cnt_With_At_Least_One_Reload / (float)curr_CEC.Combats_Cnt_Without_Reload);
                            row_cnt++;
                        }

                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
                        ScrollViewer file_reload_scrollViewer = New_Windows_Table("Reload Detail Statistics", data_reload, 1, 500);
                        Grid file_reload_Grid = (Grid)data_scrollViewer.Content;
                        file_reload_scrollViewer.Content = null;

                        Grid.SetRow(file_reload_Grid, grid.RowDefinitions.Count - 1);
                        Grid.SetColumn(file_reload_Grid, 0);
                        Grid.SetColumnSpan(file_reload_Grid, 2);
                        grid.Children.Add(file_reload_Grid);
                    }
                }

                grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });
                TextBlock char_title = new TextBlock() { HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
                char_title.Inlines.Add(new System.Windows.Documents.Run("Characters") { FontWeight = System.Windows.FontWeights.Bold, TextDecorations = System.Windows.TextDecorations.Underline });
                Grid.SetRow(char_title, grid.RowDefinitions.Count - 1);
                Grid.SetColumn(char_title, 0);
                Grid.SetColumnSpan(char_title, 2);
                grid.Children.Add(char_title);

                grid.RowDefinitions.Add(new RowDefinition());

                if (_Character_List_Panel == null)
                {
                    _Character_List_Panel = new StackPanel() { Margin = new System.Windows.Thickness(5, 5, 0, 10) };
                }
                Grid.SetRow(_Character_List_Panel, grid.RowDefinitions.Count - 1);
                Grid.SetColumn(_Character_List_Panel, 0);
                Grid.SetColumnSpan(_Character_List_Panel, 2);
                grid.Children.Add(_Character_List_Panel);

                _Character_List_Panel.Children.Clear();
                _Character_List_Panel.Children.Add(Get_UserControl_For_Characters());

                grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });
                TextBlock stats_title = new TextBlock() { HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
                stats_title.Inlines.Add(new System.Windows.Documents.Run("Statstics") { FontWeight = System.Windows.FontWeights.Bold, TextDecorations = System.Windows.TextDecorations.Underline });
                Grid.SetRow(stats_title, grid.RowDefinitions.Count - 1);
                Grid.SetColumn(stats_title, 0);
                Grid.SetColumnSpan(stats_title, 2);
                grid.Children.Add(stats_title);

                grid.RowDefinitions.Add(new RowDefinition());

                if (_Stats_Panel == null)
                {
                    _Stats_Panel = new StackPanel();
                }
                Grid.SetRow(_Stats_Panel, grid.RowDefinitions.Count - 1);
                Grid.SetColumn(_Stats_Panel, 0);
                Grid.SetColumnSpan(_Stats_Panel, 2);
                grid.Children.Add(_Stats_Panel);

                _Stats_Panel.Children.Clear();
                _Stats_Panel.Children.Add(_Stats.Get_Analysis_UserControl());

                grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });
                TextBlock source_title = new TextBlock() { HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
                source_title.Inlines.Add(new System.Windows.Documents.Run("Source") { FontWeight = System.Windows.FontWeights.Bold, TextDecorations = System.Windows.TextDecorations.Underline });
                Grid.SetRow(source_title, grid.RowDefinitions.Count - 1);
                Grid.SetColumn(source_title, 0);
                Grid.SetColumnSpan(source_title, 2);
                grid.Children.Add(source_title);

                WebBrowser webBrowser = New_WebBrowser();

                grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(800, System.Windows.GridUnitType.Pixel) });
                Grid.SetRow(webBrowser, grid.RowDefinitions.Count - 1);
                Grid.SetColumn(webBrowser, 0);
                Grid.SetColumnSpan(webBrowser, 2);
                grid.Children.Add(webBrowser);

                Button refresh_button = new Button() { Content = new TextBlock(new Run("Refresh")), Width = 100, HorizontalAlignment = HorizontalAlignment.Right };
                refresh_button.Click += 
                    (sender, obj) =>
                    {
                        refresh_button.IsEnabled = false;
                        webBrowser.NavigateToString(string.Format("Loading {0} events -- please be patient", Children.Count));
                        string tmp_string = "";

                        System.ComponentModel.BackgroundWorker bg = new System.ComponentModel.BackgroundWorker();
                        bg.DoWork += (bg_sender, bg_obj) => tmp_string = Filter_String_For_WebBrowser(Source_With_ID);
                        bg.RunWorkerCompleted += (bg_sender, bg_obj) => { webBrowser.NavigateToString(tmp_string); refresh_button.IsEnabled = true; };
                        bg.RunWorkerAsync();
                    };
                Grid.SetRow(refresh_button, grid.RowDefinitions.Count - 2); // Backup to the "Source" title row.
                Grid.SetColumn(refresh_button, 0);
                Grid.SetColumnSpan(refresh_button, 2);
                grid.Children.Add(refresh_button);

                if (Children.Count < 1000) { webBrowser.NavigateToString(Filter_String_For_WebBrowser(Source_With_ID)); }
                else { webBrowser.NavigateToString(string.Format("Refresh to view {0} events -- this may take a while to generate!", Children.Count)); }
            }
            else
            {
                ScrollViewer scrollViewer = (ScrollViewer)_UC_For_Display.Content;
                DockPanel dockPanel = (DockPanel)scrollViewer.Content;
                Grid grid = (Grid)dockPanel.Children[0];

                Update_Characters_List();
                Update_Reload();

                if (this is CombatStartEvent) { }
                {
                    Update_A_Windows_Table(grid, "Reload Statistics", data_reload, 1);
                }

                Update_Characters_UserControl();
                _Stats.Update_Analysis_UserControl();

                WebBrowser webBrowser = null;

                foreach (UIElement curr_elem in grid.Children) { if (curr_elem is WebBrowser) { webBrowser = (WebBrowser)curr_elem; break; } }

                webBrowser.NavigateToString(string.Format("Click on refersh to {0} events", Children.Count));
            }

            return _UC_For_Display;
        }
#endregion
    }
}
