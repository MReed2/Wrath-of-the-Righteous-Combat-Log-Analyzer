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
        public override string Source
        {
            get
            {
                StringBuilder tmp = new StringBuilder(base.Source);
                foreach (CombatEvent curr_evnt in Children) { tmp.Append("\n"+curr_evnt.Source); }
                return tmp.ToString();
            }
            set => base.Source = value;
        }
        public override string Source_With_ID
        {
            get
            {
                StringBuilder tmp = new StringBuilder(base.Source_With_ID);
                foreach (CombatEvent curr_evnt in Children) { tmp.Append("\n" + curr_evnt.Source_With_ID); }
                return tmp.ToString();
            }
        }
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

        public bool IsReload { get => ((Strict_Full_Reload_Cnt > 0) || (Loose_Full_Reload_Cnt > 0) || (Strict_Starting_Reload_Cnt > 0) || (Loose_Starting_Reload_Cnt > 0)); }
        public int Reload_Cnt { get => Math.Max(Strict_Full_Reload_Cnt, Math.Max(Loose_Full_Reload_Cnt, Math.Max(Strict_Starting_Reload_Cnt, Loose_Starting_Reload_Cnt))); }

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
                    if (curr_itm.Parent.ID < early_threshold)
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
        
        public override UserControl Get_UserControl_For_Display()
        {
            if (_UC_For_Display == null)
            {
                _UC_For_Display = base.Get_UserControl_For_Display();

                Grid outer_grid = ((Grid)((DockPanel)((ScrollViewer)_UC_For_Display.Content).Content).Children[0]);
                               
                string[,] data =
                {
                    { "# of Events", Children.Count.ToString() },
                    { "# of Characters", Characters.GetAll().Count.ToString() },
                    { "# of Unique Characters", Characters.Count.ToString() },
                    { "# of Friendly Characters", Characters.GetAll().FindAll(m => (m.Character_Type == Char_Enum.Friendly)).Count.ToString() },
                    { "# of Hostile Characters", Characters.GetAll().FindAll(m => (m.Character_Type == Char_Enum.Hostile)).Count.ToString() },
                    { "# of Summoned Characters", Characters.GetAll().FindAll(m => (m.Character_Type == Char_Enum.Summon)).Count.ToString() },
                    { "# of Unknown Characters", Characters.GetAll().FindAll(m => (m.Character_Type == Char_Enum.Unknown)).Count.ToString() },
                    { "# of Really Unknown Characters", Characters.GetAll().FindAll(m => (m.Character_Type == Char_Enum.Really_Unknown)).Count.ToString() },

                    { "# of Attack Events", Children.FindAll(m => (m is AttackEvent)).Count.ToString() },
                    { "# of Damage Events", Children.FindAll(m => (m is DamageEvent)).Count.ToString() },
                    { "# of Healing Events", Children.FindAll(m => (m is HealingEvent)).Count.ToString() },
                    { "# of Death Events", Children.FindAll(m => ( (m is SimpleEvent)&&(((SimpleEvent)m).Subtype == "Death") )).Count.ToString() },
                    { "Is Reload?", IsReload.ToString() },
                    { "Reload Count", Reload_Cnt.ToString() },
                    { "", "" },
                    { "", "" }
                };
                
                outer_grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });
                ScrollViewer scrollViewer = New_Windows_Table("Data", data, 2, 500);
                Grid.SetRow(scrollViewer, outer_grid.RowDefinitions.Count-1);
                Grid.SetColumn(scrollViewer, 0);
                Grid.SetColumnSpan(scrollViewer, 2);
                outer_grid.Children.Add(scrollViewer);

                outer_grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });
                TextBlock source_title = new TextBlock() { HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
                source_title.Inlines.Add(new System.Windows.Documents.Run("Source") { FontWeight = System.Windows.FontWeights.Bold, TextDecorations = System.Windows.TextDecorations.Underline });
                Grid.SetRow(source_title, outer_grid.RowDefinitions.Count-1);
                Grid.SetColumn(source_title, 0);
                Grid.SetColumnSpan(source_title, 2);
                outer_grid.Children.Add(source_title);

                WebBrowser webBrowser = New_WebBrowser();
                webBrowser.NavigateToString(Filter_String_For_WebBrowser(Source_With_ID));

                outer_grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(800, System.Windows.GridUnitType.Pixel) });
                Grid.SetRow(webBrowser, outer_grid.RowDefinitions.Count-1);
                Grid.SetColumn(webBrowser, 0);
                Grid.SetColumnSpan(webBrowser, 2);
                outer_grid.Children.Add(webBrowser);
                /*DockPanel dockPanel = new DockPanel() { LastChildFill = true };
                DockPanel.SetDock(outer_grid, Dock.Top);
                dockPanel.Children.Add(outer_grid);

                DockPanel.SetDock(webBrowser, Dock.Bottom);
                dockPanel.Children.Add(webBrowser);

                uc.Content = dockPanel;*/
                return _UC_For_Display;
            }
            
            return _UC_For_Display;
        }
    }
}
