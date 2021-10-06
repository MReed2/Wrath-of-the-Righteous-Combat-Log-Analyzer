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

    public class CombatEventContainer: CombatEvent
    {
        #region Override Properties
        public override string Character_Name { get => _Filename; set => throw new NotImplementedException(); }
        public override string Source_Character_Name => throw new NotImplementedException();
        public override List<Die_Roll> Die_Rolls => throw new System.NotImplementedException();
        #endregion
        #region Override Methods
        public override List<Die_Roll> Parse(string line) { _Filename = line; return null; }

        public CombatEventContainer(int inID, string inLine) : base(inID, inLine) { }
        #endregion

        public event NeedToUpdateCharacterLists OnNeedToUpdateCharacterLists;
        public event NeedToCalculateStatsDelegate OnNeedToCalculateStats;

        private string _Filename = "";
        private CharacterList _Characters_Override = new CharacterList();
        private CharacterList _Characters = new CharacterList();
        private CombatStats _Stats = new CombatStats();

        private int _Children_Count_When_Characters_Last_Refreshed = -1;
        private int _Children_Count_When_Stats_Last_Refreshed = -1;
        private bool _Has_Rendered_Character_UC_After_Refresh = false;
        private bool _Has_Rendered_Stats_UC_After_Refresh = false;

        private bool _Characters_Updating_Async = false;
        private bool _Stats_Updating_Async = false;
        private bool _Children_Changed = false;

        private UserControl _UC_For_Display = null;
        private UserControl _UC_For_Characters = null;

        private StackPanel _Character_List_Panel = null;
        private StackPanel _Stats_Panel = null;

        public string Filename { get => _Filename; }

        public CombatStats Stats { get => _Stats; }

        #region Dealing with Characters List
        public CharacterList Characters
        {
            get
            {
                Update_Characters_List();
                return _Characters;
            }
        }

        public CharacterList Characters_Override { get => _Characters_Override; }

        private void Update_Characters_List()
        {
            // *Don't* clear the _Characters list!
            //
            // Any overrides the users has applied should still be valid -- we just need to add more items to the list, or update it with changes from elsewhere

            if ((_Children_Count_When_Characters_Last_Refreshed != Children.Count)||(_Children_Changed))
            {
                foreach (CombatEvent curr_event in Children)
                {
                    if (curr_event is CombatStartEvent) {  }
                    else if ( (curr_event is SimpleEvent) && (((SimpleEvent)curr_event).Subtype != "Death") ) { }
                    else { _Characters.Add(new CharacterListItem(curr_event)); } // CharacterListItem and CharacterList manage duplicate entries -- no need to do so here
                }

                _Children_Count_When_Characters_Last_Refreshed = Children.Count;
                _Children_Changed = false;
                _Has_Rendered_Character_UC_After_Refresh = false; // Need to rebuild the control.
                _Has_Rendered_Stats_UC_After_Refresh = false; // Stats also need to be rebuilt.
            }
        }

        private UserControl Get_UserControl_For_Characters()
        {
            if (_UC_For_Characters == null) { _Characters.OnCombatEventChanged += new CombatEventChanged(CombatEventChanged); }

            Update_Characters_List();

            if (_UC_For_Characters == null)
            {
                _UC_For_Characters = new UserControl();
                Grid grid = new Grid();
                _UC_For_Characters.Content = grid;
            }

            return _UC_For_Characters;
        }

        private void CombatEventChanged(CombatEvent source)
        {
            _Children_Changed = true;
        }

        private UserControl Update_Characters_UserControl()
        {
            UserControl tmp_UC = Get_UserControl_For_Characters();

            if (_Has_Rendered_Character_UC_After_Refresh) { return tmp_UC; }

            _Has_Rendered_Character_UC_After_Refresh = true;

            Grid grid = (Grid)tmp_UC.Content;
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
                        curr_itm.Character_Type = new_val;
                        _Characters_Override.Add(curr_itm);

                        foreach (CharacterListItem child_itm in curr_itm.Children)
                        {
                            child_itm.Character_Type = new_val;
                            _Characters_Override.Add(child_itm);
                        }
                    }
                }
            }

            if (madeChange)
            {
                _Has_Rendered_Character_UC_After_Refresh = false;
                _Has_Rendered_Stats_UC_After_Refresh = false;

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
            }
        }

        private WebBrowser _Details_webBrowser = null;

        private void OnClickForCharacterDetails(object sender, RoutedEventArgs e)
        {
            Hyperlink inHyperlink = (Hyperlink)sender;
            CharacterListItem itm_to_get_details_for = (CharacterListItem)inHyperlink.Tag;

            Window Details_Window = new Window()
            {
                Height = 500,
                Width = 825,
                Title = "Character details for " + itm_to_get_details_for.Friendly_Name
            };
            Grid grid = new Grid();
            Details_Window.Content = grid;

            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(20, GridUnitType.Star) }); // 300
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50, GridUnitType.Star) }); // 500

            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(50, GridUnitType.Star) });
            TreeView details_treeview = new TreeView();
            Grid.SetRow(details_treeview, 0);
            Grid.SetColumn(details_treeview, 0);
            grid.Children.Add(details_treeview);

            TreeViewItem root_tvi = new TreeViewItem() { Header = itm_to_get_details_for.Friendly_Name, IsExpanded = true, Tag = null };
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

            DockPanel dockPanel = new DockPanel() { LastChildFill = true };
            _Details_webBrowser = new WebBrowser() { VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };
            _Details_webBrowser.LoadCompleted += (s, nav_e) =>
            {
                string script = "document.body.style.whiteSpace = 'nowrap'";
                WebBrowser wb = (WebBrowser)s;
                wb.InvokeScript("execScript", new Object[] { script, "JavaScript" });
            };
            DockPanel.SetDock(_Details_webBrowser, Dock.Right);
            dockPanel.Children.Add(_Details_webBrowser);

            Grid.SetRow(dockPanel, 0);
            Grid.SetColumn(dockPanel, 1);
            grid.Children.Add(dockPanel);

            Details_Window.ShowDialog();
        }

        private void Character_Details_TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            if (_Details_webBrowser == null) { throw new System.Exception("In Character_Details_TreeViewItem_Selected with null _Details_webBrowser."); }

            CharacterListItem itm_clicked = ((CharacterListItem)((TreeViewItem)sender).Tag);

            System.Text.StringBuilder html_to_display = new StringBuilder();
            if (itm_clicked == null)
            {
                foreach (TreeViewItem curr_tv_itm in ((TreeViewItem)sender).Items)
                {
                    CharacterListItem curr_lst = (CharacterListItem)curr_tv_itm.Tag;

                    curr_lst.Parents.Sort(Comparer<CombatEvent>.Create((first, second) => first.ID.CompareTo(second.ID) ));
                    foreach (CombatEvent curr_event in curr_lst.Parents)
                    {
                        html_to_display.Append(curr_event.Source_With_ID.Replace("–", "-").Replace("—", "--").Replace("×", "x"));
                    }
                }
            }
            else
            {
                itm_clicked.Parents.Sort(Comparer<CombatEvent>.Create((first, second) => first.ID.CompareTo(second.ID)));
                foreach (CombatEvent curr_event in itm_clicked.Parents)
                {
                    html_to_display.Append(curr_event.Source_With_ID.Replace("–", "-").Replace("—", "--").Replace("×", "x"));
                }
            }

            _Details_webBrowser.NavigateToString(html_to_display.ToString());
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
                else { throw new System.Exception("Unexpected value in CreateTypeSElectionComboBoxes"); }

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
            if (_UC_For_Display == null)
            {
                _UC_For_Display = new UserControl();

                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new System.Windows.GridLength(0, System.Windows.GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new System.Windows.GridLength(50, System.Windows.GridUnitType.Star) });

                grid.RowDefinitions.Add(new RowDefinition());

                if (_Character_List_Panel == null)
                {
                    _Character_List_Panel = new StackPanel() { Margin = new System.Windows.Thickness(5, 5, 0, 10) };
                }
                Grid.SetRow(_Character_List_Panel, 0);
                Grid.SetColumn(_Character_List_Panel, 0);
                Grid.SetColumnSpan(_Character_List_Panel, 2);
                grid.Children.Add(_Character_List_Panel);

                _Character_List_Panel.Children.Clear();
                _Character_List_Panel.Children.Add(Get_UserControl_For_Characters());

                grid.RowDefinitions.Add(new RowDefinition());

                if (_Stats_Panel == null)
                {
                    _Stats_Panel = new StackPanel() { Margin = new System.Windows.Thickness(5, 5, 0, 10) };
                }
                Grid.SetRow(_Stats_Panel, 1);
                Grid.SetColumn(_Stats_Panel, 0);
                Grid.SetColumnSpan(_Stats_Panel, 2);
                grid.Children.Add(_Stats_Panel);

                _Stats_Panel.Children.Clear();
                _Stats_Panel.Children.Add(_Stats.Get_Analysis_UserControl());

                _UC_For_Display.Content = grid;
            }

            return _UC_For_Display;
        }
#endregion
    }
}
