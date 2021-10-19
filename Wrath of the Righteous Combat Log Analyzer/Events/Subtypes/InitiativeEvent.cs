using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    class InitiativeEvent : CombatEvent
    {
        private string _Source_Character_Name = "";
        private string _Character_Name = "";
        private int _Initiative = 0;
        private int _Net_Initiative_Modifiers = 0;
        private List<Die_Roll> _Die_Rolls = new List<Die_Roll>();

        private bool _init_done = false;
        
        public override string Source_Character_Name => _Source_Character_Name;

        public override string Character_Name
        {
            get { return _Character_Name; }
            set { _Character_Name = value; }
        }
        
        public override List<Die_Roll> Die_Rolls
        {
            get { return _Die_Rolls; }
        }

        public int Initiative
        {
            get { return _Initiative; }
        }
        
        public InitiativeEvent(int inID, int inCombatID, string line):base(inID, inCombatID, line) { }

        public override List<Die_Roll> Parse(string line)
        {
            if (line.Contains("Initiative check")&&(!_init_done))
            {
                //<div style="margin-left: 0px"><b><b><span style="color:#262626">CR14_Cyborg_CrusaderTankLevel12[1cc7942c]</span></b></b>: Initiative check <b>31</b>.</div>
                //<div style="margin-left:   0px"><b><b><span style="color:#262626">Lair_SuccubusBossHelperMeleeLevel7WithRapier[2aad38c6]</span></b></b>: Initiative check <b>-2</b>.</div>
                //<div style="margin-left:   0px">IvorySanctum_MythicSchir[dcbf72f7]<b><b><span style="color:#262626"></span></b></b>: Initiative check <b>19</b>.</div>
                Source += line + "\n";
                _init_done = true;

                Match parsed_line = Regex.Match(line, @"(?:.*?\x22>){2}(.*?)<(?:.*?k <b>)([+-]?\d*)<");
                if (parsed_line.Success)
                {
                    if (parsed_line.Groups[1].Value != "")
                    {
                        _Character_Name = parsed_line.Groups[1].Value;
                        _Source_Character_Name = parsed_line.Groups[1].Value;
                        _Initiative = int.Parse(parsed_line.Groups[2].Value);
                    }
                    else
                    {
                        parsed_line = Regex.Match(line, @"(?:.*?\x22>)(.*?)<(?:.*?k <b>)([+-]?\d*)<");
                        if (parsed_line.Success)
                        {
                            _Character_Name = parsed_line.Groups[1].Value;
                            _Source_Character_Name = parsed_line.Groups[1].Value;
                            _Initiative = int.Parse(parsed_line.Groups[2].Value);
                        }
                        else
                        {
                            throw new System.Exception("Unable to parse initiative 1st line \"" + line + "\".");
                        }
                    }
                }
                else
                {
                    throw new System.Exception("Unable to parse initiative 1st line \""+line+"\".");
                }

                return _Die_Rolls;
            }
            else if (line.Contains("Check:"))
            {
                //<div style="margin-left: 50px">	Check: 31 (roll: 20 + modifiers: 11).</div>
                //<div style="margin-left:  50px">	Check: 34 (roll: <s>1</s> 20 + modifiers: 14).</div>
                //<div style="margin-left:  50px">	Check: -2 (roll: 3 + modifiers: -5).</div>
                //<div style="margin-left:  50px">	Check: 17 (roll: <b><u>17</u></b>, <b><u>4</u></b> [Bit of Luck] + modifiers: 0).</div>
                Source += line + "\n";

                Match parsed_line = Regex.Match(line, @".*?k: ([+-]?\d*?) .*?: (.*?)\+.*?: ([+-]?\d*)"); // .*?k: (\d*?) .*?: (\d*?) .*?: ([+-]?\d*)
                if (parsed_line.Success)
                {
                    int tmp_init = int.Parse(parsed_line.Groups[1].Value);
                    string fix_strikethrough = Regex.Replace(parsed_line.Groups[2].Value, @"(<s>)(\d*?)(<\/s>) \d*", "$2"); // converts "<s>1</s> 20" to "1", and does it multiple times if necessary (if there are commas, for example).

                    foreach (string curr_str in fix_strikethrough.Split(',')) // if no commas, this loops one with the entire string
                    {
                        //<b><u>17</u></b>
                        //<b><u>4</u></b> [Bit of Luck] 
                        string fixed_curr_str = Regex.Replace(curr_str, @"\[.*\]", "").Trim(); // strips off whatever is in square brackets, if anything
                        Match is_underlined = Regex.Match(fixed_curr_str, @"u>(\d*?)<\/");
                        if (is_underlined.Success) // this value is underlined
                        {
                            Die_Roll underlined_roll = new Die_Roll("Initiative", _Character_Name, Friendly_Name, int.Parse(is_underlined.Groups[1].Value));
                            underlined_roll.Underlined = true;
                            _Die_Rolls.Add(underlined_roll);
                        }
                        else
                        {
                            _Die_Rolls.Add(new Die_Roll("Initiative", _Character_Name, Friendly_Name, int.Parse(fixed_curr_str)));
                        }
                    }
                                        
                    if (tmp_init != _Initiative)
                    {
                        throw new System.Exception("Invalid initative 'Check' line -- initiative was " + _Initiative.ToString() + " but is now " + tmp_init.ToString());
                    }
                    _Net_Initiative_Modifiers = int.Parse(parsed_line.Groups[3].Value);

                    if (_Die_Rolls.Count == 0)
                    {
                        throw new System.Exception("Empty die roll list");
                    }

                    return _Die_Rolls;
                }
                else
                {
                    throw new System.Exception("Unable to parse initative 2nd line \"" + line + "\".");
                }
            }
            else
            {
                throw new Mode_Change_Exception("End of initative records");
            }
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

            string[] label_list = { "Character Name", "Initiative", "Initiative Bonus", "Initiative Roll (d20)" };

            int cnt = 0;
            foreach (string curr_label in label_list)
            {
                RowDefinition r = new RowDefinition();
                r.Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto);
                grid.RowDefinitions.Add(r);

                Label lbl = new Label();
                lbl.Content = curr_label;
                Grid.SetColumn(lbl, 0);
                Grid.SetRow(lbl, cnt);

                TextBox tb = new TextBox();
                switch (curr_label)
                {
                    case "Character Name":
                        tb.Text = _Character_Name;
                        break;
                    case "Initiative":
                        tb.Text = _Initiative.ToString();
                        break;
                    case "Initiative Bonus":
                        tb.Text = _Net_Initiative_Modifiers.ToString();
                        break;
                    case "Initiative Roll (d20)":
                        if (_Die_Rolls.Count == 0) { tb.Text = "Unknown (this is a bug)"; }
                        else { tb.Text = _Die_Rolls[0].Roll.ToString(); }
                        break;
                }
                tb.IsReadOnly = true;
                Grid.SetColumn(tb, 1);
                Grid.SetRow(tb, cnt);

                grid.Children.Add(lbl);
                grid.Children.Add(tb);

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

            wb.NavigateToString(Source.Replace("–", "-").Replace("—", "--"));
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
