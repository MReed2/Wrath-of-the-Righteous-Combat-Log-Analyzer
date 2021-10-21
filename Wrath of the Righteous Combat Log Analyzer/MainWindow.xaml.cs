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
using System.Text.RegularExpressions;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string _Title_Prefix = "Pathfinder: Wrath of the Righteous Log Analyzer";
        public CombatEventList _Unprocessed_CombatEvents_Queue = new CombatEventList();
        Action _emptyDelegate = delegate { };

        private LoadingAwareTreeViewItem _Last_Combat_Start_TreeViewItem = null;
        private LoadingAwareTreeViewItem _Last_File_Start_TreeView_Item = null;
        private LoadingAwareTreeViewItem _Root_TreeViewItem = null;

        private int _Root_Event_Cnt = 0;
        private int _Root_Combat_Cnt = 0;
        private int _Root_File_Cnt = 0;

        private int _File_Combat_Cnt = 0;
        private int _File_Event_Cnt = 0;

        private int _Combat_Event_Cnt = 0;

        private bool _First_Idle_After_Load = false;
                
        public MainWindow()
        {
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
            InitializeComponent();
            CombatLog_Parser.OnNewCombatEvent += CombatLog_Parser_OnNewCombatEvent;
            CombatLog_Parser.OnParserIdle += CombatLog_Parser_OnParserIdle;
            CombatLog_Parser.OnLoadProgress += CombatLog_Parser_OnLoadProgress;
            CombatLog_Parser.OnCurrentFileChanged += CombatLog_Parser_OnCurrentFileChanged;
            Closed += new EventHandler(OnClosed);

            string default_file = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            default_file += @"..\..\locallow\owlcat Games\pathfinder wrath Of The Righteous\combatLog.txt";
            if (System.IO.File.Exists(default_file))
            {
                string[] default_files = { default_file };

                _First_Idle_After_Load = true;
                CombatLog_Parser.Spawn_Parse(default_files);
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            CombatLog_Parser.Kill_Thread();
        }

        private void OnCurrentFileChanged(string inFilename)
        {
            Title = _Title_Prefix + " - " + inFilename;
        }

        private void OnLoadProgress(string inFilename, long Completed, long Total)
        {
            StatusBar_Progress.Maximum = Total;
            StatusBar_Progress.Value = Completed;
            StatusBar_Status_TextBox.Text = String.Format("{0}: {1} / {2} ({3:P})", System.IO.Path.GetFileName(inFilename), Completed, Total, ((double)Completed) / ((double)Total));
        }

        private void OnParserIdle(string status_str)
        {
            StatusBar_Status_TextBox.Text = status_str;

            if (_Root_TreeViewItem != null)
            {
                _Root_TreeViewItem.IsLoading = false;
                _Last_File_Start_TreeView_Item.IsLoading = false;
                _Last_Combat_Start_TreeViewItem.IsLoading = false;
                UpdateNamesOfKeyNodes();

                if (MasterTreeView.SelectedItem == _Root_TreeViewItem)
                {
                    CombatEventContainer tmp = (CombatEventContainer)_Root_TreeViewItem.Tag;
                    tmp.Get_UserControl_For_Display().IsEnabled = true;
                }

                if (MasterTreeView.SelectedItem != null)
                {
                    LoadingAwareTreeViewItem curr_selected_itm = (LoadingAwareTreeViewItem)MasterTreeView.SelectedItem;
                    CombatEvent curr_selected_event = (CombatEvent)curr_selected_itm.Tag;
                    if (curr_selected_event is CombatEventContainer)
                    {
                        curr_selected_event.Update_Display_UserControl(); // This will refresh the data, if required.
                    }
                }

                if (MasterTreeView.SelectedItem == null)
                {
                    _Root_TreeViewItem.IsSelected = true;
                }
            }
            
            if (_First_Idle_After_Load)
            {
                _First_Idle_After_Load = false;
                Update_Reload();
            }
        }

        public void NeedToCalculateStats(CombatEventContainer sender, CombatStats inStatsToRecalc, CombatEventList inCombatEventList)
        {
            string prev_status_str = StatusBar_Status_TextBox.Text;
            StatusBar_Status_TextBox.Text = "Updating Stats";
            StatusBar_Progress.IsIndeterminate = true;
            this.Cursor = Cursors.AppStarting;

            System.ComponentModel.BackgroundWorker bw = new System.ComponentModel.BackgroundWorker() { WorkerReportsProgress = false };

            bw.DoWork += (o, args) =>
            {
                inStatsToRecalc.Process_Events(inCombatEventList);
                inStatsToRecalc.Build_Tables();
            };

            bw.RunWorkerCompleted += (o, args) =>
            {
                this.Cursor = Cursors.Arrow;
                StatusBar_Status_TextBox.Text = prev_status_str;
                StatusBar_Progress.IsIndeterminate = false;
                sender.Update_Stats_Callback();
            };

            bw.RunWorkerAsync();
        }

        public void NeedToUpdateCharacterLists(CombatEventContainer sender, CharacterList newOverride)
        {
            string prev_status_str = StatusBar_Status_TextBox.Text;
            StatusBar_Status_TextBox.Text = "Updating Characters";
            StatusBar_Progress.IsIndeterminate = true;
            this.Cursor = Cursors.AppStarting;

            System.ComponentModel.BackgroundWorker bw = new System.ComponentModel.BackgroundWorker() { WorkerReportsProgress = false };

            bw.DoWork += (o, args) =>
            {
                sender.Children.Update_With_CharacterList(newOverride);
            };

            bw.RunWorkerCompleted += (o, args) =>
            {
                this.Cursor = Cursors.Arrow;
                StatusBar_Status_TextBox.Text = prev_status_str;
                StatusBar_Progress.IsIndeterminate = false;
                sender.Update_CharacterList_Callback();
            };

            bw.RunWorkerAsync();
        }

        public void Update_Reload()
        {
            CombatStartEvent prev_combat = null;

            _Root_TreeViewItem.Header = Regex.Replace((string)_Root_TreeViewItem.Header, @"\((\d*.*?),", string.Format("({0} Reloads,", ((CombatEventContainer)_Root_TreeViewItem.Tag).Reload_Cnt));

            foreach (LoadingAwareTreeViewItem curr_file_tvi in _Root_TreeViewItem.Items)
            {
                curr_file_tvi.Header = Regex.Replace((string)curr_file_tvi.Header, @"\((\d*.*?),", string.Format("({0} Reloads,", ((CombatEventContainer)curr_file_tvi.Tag).Reload_Cnt));

                foreach (LoadingAwareTreeViewItem curr_combat in curr_file_tvi.Items)
                {
                    prev_combat = ((CombatStartEvent)curr_combat.Tag);
                    curr_combat.Header = Regex.Replace((string)curr_combat.Header, @"\((\d*.*?),", string.Format("({0} Reloads,", ((CombatEventContainer)curr_combat.Tag).Reload_Cnt));
                }
            }
        }

        private void InitRootNode()
        {
            MasterTreeView.Items.Clear();

            CombatEventContainer tmp = new CombatEventContainer(-1, -1, "");
            tmp.OnNeedToCalculateStats += NeedToCalculateStats;
            tmp.OnNeedToUpdateCharacterLists += NeedToUpdateCharacterLists;

            _Root_TreeViewItem = new LoadingAwareTreeViewItem() { Header = "All (0 Events)", Tag = tmp, IsExpanded = true };
            MasterTreeView.Items.Add(_Root_TreeViewItem);

            _Root_Event_Cnt = 0;
            _Root_Combat_Cnt = 0;
            _Root_File_Cnt = 0;

            _Last_File_Start_TreeView_Item = null;
            _File_Combat_Cnt = 0;
            _File_Event_Cnt = 0;

            _Last_Combat_Start_TreeViewItem = null;
            _Combat_Event_Cnt = 0;
        }

        private void ResetCurrentFile()
        {
            if ((_Last_File_Start_TreeView_Item == null)||(_Last_File_Start_TreeView_Item==null)) { InitRootNode(); }
            else
            {
                CombatEventList events_to_remove = new CombatEventList();

                foreach (CombatEvent curr_itm in ((CombatEventContainer)_Last_File_Start_TreeView_Item.Tag).Children) { events_to_remove.Add(curr_itm); }
                foreach (CombatEvent curr_itm in events_to_remove) { ((CombatEventContainer)_Root_TreeViewItem.Tag).Children.Remove(curr_itm); }

                ((CombatEventContainer)_Last_File_Start_TreeView_Item.Tag).Clear();
                
                ((CombatEventContainer)_Root_TreeViewItem.Tag).Force_Rebuild_Of_Character_Data();
                ((CombatEventContainer)_Root_TreeViewItem.Tag).Recalc_Stats_Immediately_If_needed();
                
                _Root_Event_Cnt -= _File_Event_Cnt;
                _Root_Combat_Cnt -= _File_Combat_Cnt;

                _File_Event_Cnt = 0;
                _File_Combat_Cnt = 0;

                _Last_File_Start_TreeView_Item.Items.Clear();

                UpdateNamesOfKeyNodes();

                Update_Reload();
            }
        }

        private void UpdateNamesOfKeyNodes()
        {
            if (_Root_TreeViewItem != null)
            { _Root_TreeViewItem.Header = Regex.Replace((string)_Root_TreeViewItem.Header, @"(\(.*\))", "") + String.Format("({0} Reloads, {1} Files, {2} Combats, {3} Events)", ((CombatEventContainer)_Root_TreeViewItem.Tag).Reload_Cnt, _Root_File_Cnt, _Root_Combat_Cnt, _Root_Event_Cnt); }
            if (_Last_File_Start_TreeView_Item != null)
            { _Last_File_Start_TreeView_Item.Header = Regex.Replace((string)_Last_File_Start_TreeView_Item.Header, @"(\(.*\))", "") + String.Format("({0} Reloads, {1} Combats, {2} Events)", ((CombatEventContainer)_Last_File_Start_TreeView_Item.Tag).Reload_Cnt, _File_Combat_Cnt, _File_Event_Cnt); }
            if (_Last_Combat_Start_TreeViewItem != null)
            {
                _Last_Combat_Start_TreeViewItem.Header = Regex.Replace((string)_Last_Combat_Start_TreeViewItem.Header, @"(\(.*\))", "") + 
                    String.Format
                    (
                        "({0} Reloads, {1} Events)", 
                        ((CombatStartEvent)_Last_Combat_Start_TreeViewItem.Tag).Reload_Cnt,
                        _Combat_Event_Cnt
                    );
            }
        }

        private void AddNewItemToMasterTreeView(CombatEvent newCombatEvent, string inFilename)
        {
            if (_Root_TreeViewItem == null) { InitRootNode(); }

            if (newCombatEvent == null) { ResetCurrentFile(); } // This occurs when the filesize of the currently monitored file goes down and the parser resets.
            else
            {
                if ( (_Last_File_Start_TreeView_Item == null) || ( ((CombatEventContainer)_Last_File_Start_TreeView_Item.Tag).Filename != inFilename) )
                {
                    _Root_File_Cnt++;

                    if (_Last_File_Start_TreeView_Item != null)
                    {
                        _Last_File_Start_TreeView_Item.IsLoading = false;
                        if (!_Last_File_Start_TreeView_Item.ContainsSelected()) { _Last_File_Start_TreeView_Item.IsExpanded = false; }

                        UpdateNamesOfKeyNodes();
                    }

                    CombatEventContainer tmp = new CombatEventContainer(_Root_File_Cnt, -1, inFilename);
                    tmp.OnNeedToCalculateStats += NeedToCalculateStats;
                    tmp.OnNeedToUpdateCharacterLists += NeedToUpdateCharacterLists;

                    _Last_File_Start_TreeView_Item = new LoadingAwareTreeViewItem()
                    {
                        Header = String.Format("{0}: {1} ()", _Root_File_Cnt, System.IO.Path.GetFileName(inFilename)),
                        Tag = tmp,
                        IsExpanded = true
                    };

                    _Root_TreeViewItem.Items.Add(_Last_File_Start_TreeView_Item);

                    _File_Event_Cnt = 0;
                    _File_Combat_Cnt = 0;

                    UpdateNamesOfKeyNodes();
                }

                ((CombatEventContainer)_Root_TreeViewItem.Tag).Children.Add(newCombatEvent);
                ((CombatEventContainer)_Last_File_Start_TreeView_Item.Tag).Children.Add(newCombatEvent);

                LoadingAwareTreeViewItem new_itm = new LoadingAwareTreeViewItem() { Header = newCombatEvent.ID + ": ", Tag = newCombatEvent };

                if (newCombatEvent is CombatStartEvent)
                {
                    _Root_Combat_Cnt++;
                    _File_Combat_Cnt++;

                    ((CombatStartEvent)newCombatEvent).OnNeedToCalculateStats += NeedToCalculateStats;
                    ((CombatStartEvent)newCombatEvent).OnNeedToUpdateCharacterLists += NeedToUpdateCharacterLists;

                    if (_Last_Combat_Start_TreeViewItem != null)
                    {
                        _Last_Combat_Start_TreeViewItem.IsLoading = false;
                        if (!_Last_Combat_Start_TreeViewItem.ContainsSelected()) { _Last_Combat_Start_TreeViewItem.IsExpanded = false; }
                        UpdateNamesOfKeyNodes();
                    }

                    new_itm.Header += String.Format("Combat {0} ()", _File_Combat_Cnt);
                    new_itm.IsExpanded = false;

                    _Last_Combat_Start_TreeViewItem = new_itm;
                    _Combat_Event_Cnt = 0;

                    _Last_File_Start_TreeView_Item.Items.Add(new_itm);
                }
                else
                {
                    if (newCombatEvent is AttackEvent)
                    {
                        new_itm.Header += newCombatEvent.Friendly_Name + " attacks...";
                    }
                    else if (newCombatEvent is DamageEvent)
                    {
                        new_itm.Header += newCombatEvent.Friendly_Name + " damages...";
                    }
                    else if (newCombatEvent is HealingEvent)
                    {
                        new_itm.Header += newCombatEvent.Friendly_Name + " heals...";
                    }
                    else if (newCombatEvent is InitiativeEvent)
                    {
                        new_itm.Header += newCombatEvent.Friendly_Name + " rolls initiative";
                    }
                    else if (newCombatEvent is SimpleEvent)
                    {
                        SimpleEvent tmpEvent = (SimpleEvent)newCombatEvent;
                        if (tmpEvent.Subtype == "Death") { new_itm.Header += newCombatEvent.Friendly_Name + " dies"; }
                        else { return; }
                    }
                    else
                    {
                        new_itm.Header += newCombatEvent.GetType().ToString().Split('.')[1];
                    }

                    _Root_Event_Cnt++;
                    _File_Event_Cnt++;
                    _Combat_Event_Cnt++;
                    
                    if (!_Root_TreeViewItem.IsLoading) { _Root_TreeViewItem.IsLoading = true; }                
                    if (!_Last_File_Start_TreeView_Item.IsLoading) { _Last_File_Start_TreeView_Item.IsLoading = true; }
                    if (!_Last_Combat_Start_TreeViewItem.IsLoading) { _Last_Combat_Start_TreeViewItem.IsLoading = true; }

                    _Last_Combat_Start_TreeViewItem.Items.Add(new_itm);
                    UpdateNamesOfKeyNodes();
                }
            }
        }

        private void CombatLog_Parser_OnNewCombatEvent(CombatEvent newCombatEvent, string inFilename)
        {
            Dispatcher.BeginInvoke(new NewCombatEvent(AddNewItemToMasterTreeView), newCombatEvent, inFilename);
        }

        private void CombatLog_Parser_OnParserIdle(string status_str)
        {
            Dispatcher.BeginInvoke(new Parser_idle(OnParserIdle), status_str);
        }

        private void CombatLog_Parser_OnLoadProgress(string inFilename, long Completed, long Total)
        {
            Dispatcher.BeginInvoke(new Load_Progress(OnLoadProgress), inFilename, Completed, Total);
        }

        private void CombatLog_Parser_OnCurrentFileChanged(string inFilename)
        {
            Dispatcher.BeginInvoke(new Current_File_Changed(OnCurrentFileChanged), inFilename);
        }

        private void MasterTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            LoadingAwareTreeViewItem new_itm = (LoadingAwareTreeViewItem)e.NewValue;
            LoadingAwareTreeViewItem old_itm = (LoadingAwareTreeViewItem)e.OldValue;

            if ((new_itm == null)||(new_itm.IsLoading))
            {
                DetailBorder.Child = null;
                if (new_itm != null) { new_itm.IsSelected = false; }
            }
            else if (new_itm.Tag != null)
            { 
                CombatEvent new_data_itm = (CombatEvent)new_itm.Tag;

                UserControl new_uc = new_data_itm.Update_Display_UserControl(); // this creates the control, if required.
                DetailBorder.Child = new_uc;
            }
        }

        private void OnFileOpen(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog filePicker = new Microsoft.Win32.OpenFileDialog() { DefaultExt = "txt" };
            filePicker.Multiselect = true;

            if (filePicker.ShowDialog() == true)
            {
                MasterTreeView.Items.Clear();
                _Root_TreeViewItem = null; // Everything else will be reset once the first event is processed.

                _First_Idle_After_Load = true;
                CombatLog_Parser.Spawn_Parse(filePicker.FileNames);
            }
        }

        private void OnFileApend(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog filePicker = new Microsoft.Win32.OpenFileDialog() { DefaultExt = "txt" };
            filePicker.Multiselect = true;
            if (filePicker.ShowDialog() == true)
            {
                if ((_Root_TreeViewItem != null)&&(MasterTreeView.SelectedItem == _Root_TreeViewItem))
                {
                    CombatEventContainer tmp = (CombatEventContainer)_Root_TreeViewItem.Tag;
                    tmp.Get_UserControl_For_Display().IsEnabled = false;
                }

                _First_Idle_After_Load = true;
                CombatLog_Parser.Change_Input_Files(filePicker.FileNames);
            }
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnSaveOriginalFile(object sender, RoutedEventArgs e)
        {
            if (_Root_TreeViewItem == null) { return; }

            Microsoft.Win32.SaveFileDialog filePicker = new Microsoft.Win32.SaveFileDialog() { DefaultExt = "txt" };
            if (filePicker.ShowDialog()==true)
            {
                System.IO.FileStream out_File = new System.IO.FileStream(filePicker.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                System.IO.StreamWriter out_Stream = new System.IO.StreamWriter(out_File);

                foreach (LoadingAwareTreeViewItem curr_itm in _Root_TreeViewItem.Items)
                {
                    string full_filename = ((CombatEventContainer)curr_itm.Tag).Filename;
                    System.IO.FileStream in_File = new System.IO.FileStream(full_filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    System.IO.StreamReader in_Stream = new System.IO.StreamReader(in_File);
                    while (!in_Stream.EndOfStream)
                    {
                        out_Stream.WriteLine(in_Stream.ReadLine());
                    }
                    in_File.Close();
                }
                out_Stream.Flush();
                out_Stream.Close();
            }
        }

        private void OnSavePrettifiedFile(object sender, RoutedEventArgs e)
        {
            if (_Root_TreeViewItem == null) { return; }

            Microsoft.Win32.SaveFileDialog filePicker = new Microsoft.Win32.SaveFileDialog() { DefaultExt = "html" };
            if (filePicker.ShowDialog() == true)
            {
                System.IO.FileStream out_File = new System.IO.FileStream(filePicker.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                System.IO.StreamWriter out_Stream = new System.IO.StreamWriter(out_File);

                CombatLog_Parser.Flush_Output_Files();

                System.IO.FileStream in_File = CombatLog_Parser.Stream_For_Prettified_File;
                in_File.Seek(0, System.IO.SeekOrigin.Begin);
                System.IO.StreamReader in_Stream = new System.IO.StreamReader(in_File);

                while (!in_Stream.EndOfStream) { out_Stream.WriteLine(in_Stream.ReadLine()); }

                out_Stream.Flush();
                out_Stream.Close();
            }
        }

        private void OnSaveProcessedFile(object sender, RoutedEventArgs e)
        {
            if (_Root_TreeViewItem == null) { return; }

            Microsoft.Win32.SaveFileDialog filePicker = new Microsoft.Win32.SaveFileDialog() { DefaultExt = "html" };
            if (filePicker.ShowDialog() == true)
            {
                System.IO.FileStream out_File = new System.IO.FileStream(filePicker.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                System.IO.StreamWriter out_Stream = new System.IO.StreamWriter(out_File);

                foreach (CombatEvent curr_event in CombatLog_Parser.CombatLog.Log)
                {
                    out_Stream.Write(curr_event.Source_With_ID);
                }
                out_Stream.Flush();
                out_Stream.Close();
            }
        }

        private void OnSaveDieRolls(object sender, RoutedEventArgs e)
        {
            if (_Root_TreeViewItem == null) { return; }

            Microsoft.Win32.SaveFileDialog filePicker = new Microsoft.Win32.SaveFileDialog() { DefaultExt = "CSV" };
            if (filePicker.ShowDialog() == true)
            {
                System.IO.FileStream out_File = new System.IO.FileStream(filePicker.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                System.IO.StreamWriter out_Stream = new System.IO.StreamWriter(out_File);

                CombatEventContainer tmp_Root_Event_Container = (CombatEventContainer)_Root_TreeViewItem.Tag;
                CombatStats tmp_Stats = tmp_Root_Event_Container.Stats;

                bool Stats_Updated_Succeeded = false;

                string prev_status_str = StatusBar_Status_TextBox.Text;
                StatusBar_Status_TextBox.Text = "Generating CSV file -- please be patient";
                StatusBar_Progress.IsIndeterminate = true;
                this.Cursor = Cursors.AppStarting;

                System.ComponentModel.BackgroundWorker bw = new System.ComponentModel.BackgroundWorker() { WorkerReportsProgress = false };

                bw.DoWork += (o, args) =>
                {
                    Stats_Updated_Succeeded = tmp_Root_Event_Container.Recalc_Stats_Immediately_If_needed();
                };

                bw.RunWorkerCompleted += (o, args) =>
                {
                    if (!Stats_Updated_Succeeded) { System.Threading.Thread.Sleep(50); bw.RunWorkerAsync(); }
                    else
                    {
                        this.Cursor = Cursors.Arrow;
                        StatusBar_Status_TextBox.Text = prev_status_str;
                        StatusBar_Progress.IsIndeterminate = false;

                        out_Stream.Write(tmp_Stats.Rolls_CSV);
                        out_Stream.Flush();
                        out_Stream.Close();
                    }
                };

                bw.RunWorkerAsync();
            }
        }
    }
}
