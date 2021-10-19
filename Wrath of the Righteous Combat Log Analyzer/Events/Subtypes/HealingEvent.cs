using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    class HealingEvent : CombatEventTargeted
    {
        private string _Source_Character_Name = "";
        private string _Character_Name = "";
        private string _Source_Target_Character_Name = "";
        private string _Target_Character_Name = "";
        private List<Die_Roll> _Healing_Dice = new List<Die_Roll>();
        private int _Net_Healing = 0;
        
        private bool _init_done = false;
        
        public override string Source_Character_Name => _Source_Character_Name;

        public override string Character_Name
        {
            get { return _Character_Name; }
            set { _Character_Name = value; }
        }

        public override string Source_Target_Character_Name { get => _Source_Target_Character_Name; }
        public override string Target_Character_Name { get => _Target_Character_Name; set => _Target_Character_Name = value; }

        public override List<Die_Roll> Die_Rolls
        {
            get { return _Healing_Dice; }
        }
                        
        public HealingEvent(int inID, int inCombatID, string line):base(inID, inCombatID, line) {  }

        public override List<Die_Roll> Parse(string line)
        {
            if (line.Contains(" heals "))
            {
                if (_init_done)
                {
                    throw new Mode_Change_Exception("Bailing out of back-to-back healing records");
                }
                _init_done = true;
                Source += line + "\n";
                
                Match tmp_Match = Regex.Match(line, @"(?:.*?\x22>){2}(.*?)<.*?\x22>(.*?)<.*? .*?>(\d*)");
                if (tmp_Match.Success)
                {
                    //<div style="margin-left:   0px"><b><b><span style="color:#262626">CR15_Mercenary_Oread_Caster_Male[16be503f]</span></b></b> heals <b><b><span style="color:#262626">CR17_Mercenary_Dwarf_Ranged_Male[d0dea6f7]</span></b></b> for <b>47</b> hit points.</div>
                    GroupCollection tmp = tmp_Match.Groups;
                    _Character_Name = tmp[1].Value;
                    _Source_Character_Name = tmp[1].Value;
                    _Target_Character_Name = tmp[2].Value;
                    _Source_Target_Character_Name = tmp[2].Value;
                    _Net_Healing = int.Parse(tmp[3].Value);
                }
                else
                {
                    //<div style="margin-left:   0px"><b><b><span style="color:#262626">Zerieks[1507b0b0][1507b0b0]</span></b></b> heals for <b>8</b> hit points.</div>
                    GroupCollection tmp = Regex.Match(line, @"(?:.*\x22>){2}(.*?]).*?<*.>.*? .*?>(\d*)").Groups;
                    _Character_Name = tmp[1].Value;
                    _Source_Character_Name = tmp[1].Value;
                    _Target_Character_Name = _Character_Name;
                    _Source_Target_Character_Name = _Character_Name;
                    _Net_Healing = int.Parse(tmp[2].Value);
                }
            }
            else if (line.Contains("Roll result:"))
            {
                Source += line + "\n";

                //<div style="margin-left:  50px">	Roll result: <b>8</b>(1d8).</div>
                Match tmp_Match = Regex.Match(line, @".*?>.*?: .*?>(\d*).*?\((.*?)\)");
                if (tmp_Match.Success)
                {
                    GroupCollection tmp = tmp_Match.Groups;
                    _Healing_Dice.Add(new Die_Roll("Healing", _Character_Name, Friendly_Name, int.Parse(tmp[1].Value), tmp[2].Value));
                }
                else
                //<div style="margin-left:  50px">	Roll result: <b>47</b>.</div>
                {
                    // Nothing to do -- no dice are available.  If I was extract raw healing, instead of only net healing, logic would need to be here.
                }
            }
            else if (line.Contains("Effective heal:"))
            {
                Source += line + "\n";

                //<div style="margin-left: 100px">		Effective heal: <b>8</b>.</div>
                GroupCollection tmp = Regex.Match(line, @".*?:.*?>(\d*)<").Groups;
                int tmp_Net_Healing = int.Parse(tmp[1].Value);
                if (tmp_Net_Healing != _Net_Healing)
                {
                    throw new System.Exception("Detail and summary net healing information does not agree in HealingEvent");
                }
            }
            else
            {
                if (line.Contains("margin-left:   0px"))
                {
                    throw new Mode_Change_Exception("Exiting HealingEvent");
                }
                else
                {
                    throw new System.Exception("Unexpected line \"" + line + "\" found in HealingEvent");
                }
            }

            return null;
        }

        public override UserControl Update_Display_UserControl()
        {
            return Get_UserControl_For_Display();
        }

        public override UserControl Get_UserControl_For_Display()
        {
            UserControl uc = new UserControl();

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            grid.ColumnDefinitions[0].Width = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto);

            string[] label_list = {
                "Character",
                "Target",
                "Net Healing"
                };

            int cnt = 0;
            foreach (string curr_label in label_list)
            {
                RowDefinition r = new RowDefinition();
                r.Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto);
                grid.RowDefinitions.Add(r);

                if (curr_label != "")
                {
                    Label lbl = new Label();
                    lbl.Content = curr_label;
                    Grid.SetColumn(lbl, 0);
                    Grid.SetRow(lbl, cnt);

                    TextBox tb = new TextBox();
                    switch (curr_label)
                    {
                        case "Character": tb.Text = _Character_Name; break;
                        case "Target": tb.Text = _Target_Character_Name; break;
                        case "Net Healing": tb.Text = _Net_Healing.ToString(); break;
                    }
                    tb.IsReadOnly = true;
                    Grid.SetColumn(tb, 1);
                    Grid.SetRow(tb, cnt);

                    grid.Children.Add(lbl);
                    grid.Children.Add(tb);
                }

                cnt++;
            }

            RowDefinition source_r = new RowDefinition();
            source_r.Height = new System.Windows.GridLength(10, System.Windows.GridUnitType.Star);
            grid.RowDefinitions.Add(source_r);

            Label source_lbl = new Label();
            source_lbl.Content = "Source";
            Grid.SetColumn(source_lbl, 0);
            Grid.SetRow(source_lbl, cnt);

            WebBrowser wb = new WebBrowser();

            wb.NavigateToString(Source_With_ID.Replace("–", "-").Replace("—", "--").Replace("×", "x"));
            Grid.SetColumn(wb, 1);
            Grid.SetRow(wb, cnt);

            grid.Children.Add(source_lbl);
            grid.Children.Add(wb);

            cnt++;

            uc.Content = grid;
            return uc;
        }
    }
}
