using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    class DamageEvent : CombatEvent
    {
        private string _Source_Character_Name = "";
        private string _Character_Name = "";
        private string _Source_Target_Character_Name = "";
        private string _Target_Character_Name = "";
        private int _Damage = 0;
        private string _Damage_Adjective = "";
        private int _Raw_Damage = 0;
        private string _Damage_Source = "";
        private bool _Sucessful_Save = false;
        private List<Die_Roll> _Damage_Dice = new List<Die_Roll>();

        private bool _init_done = false;
        
        public override string Source_Character_Name => _Source_Character_Name;

        public override string Character_Name
        {
            get { return _Character_Name; }
            set { _Character_Name = value; }
        }

        public override List<Die_Roll> Die_Rolls
        {
            get
            {
                return _Damage_Dice;
            }
        }
        
        public string Source_Target_Character_Name { get => _Source_Target_Character_Name; }

        public string Target_Character_Name
        {
            get { return _Target_Character_Name; }
        }

        public int Damage
        {
            get { return _Damage; }
        }

        public int Raw_Damage
        {
            get { return _Raw_Damage; }
        }

        public string Damage_Source
        {
            get { return _Damage_Source; }
        }

        public string Damage_Adjective
        {
            get { return _Damage_Adjective; }
        }

        public DamageEvent(int inID, string line):base(inID, line) { }

        public override List<Die_Roll> Parse(string line)
        {

            if (line.Contains("deals")&&line.Contains("damage")&&(!_init_done))
            {
                //<div style="margin-left:   0px"><b><b><span style="color:#224863">Wenduag_Companion[c2c0dfd3]</span></b></b> deals <b>15</b> damage (reduced) to <b><b><span style="color:#262626">Zerieks[1507b0b0]</span></b></b>.</div>
                Source += line + "\n";
                _init_done = true;

                GroupCollection damage_header = Regex.Match(line, @"(?:.*?\x22>){2}(.*?)<.*?<b>(\d*).*?\x22>(.*?)<").Groups;
                _Character_Name = damage_header[1].Value;
                _Source_Character_Name = damage_header[1].Value;
                _Damage = int.Parse(damage_header[2].Value);
                _Target_Character_Name = damage_header[3].Value;
                _Source_Target_Character_Name = damage_header[3].Value;
            }
            else if (line.Contains(" receives ") && line.Contains("damage."))
            {
                // Yes, indeed, self-damge isn't recorded the same way as damage to others.  Isn't this fun!

                //<div style="margin-left:   0px"><b><b><span style="color:#262626">WintersunWintersunSiabrae[f514fbd4][f514fbd4]</span></b></b> receives <b>8</b> damage.</div>
                Source += line + "\n";
                _init_done = true;

                GroupCollection damage_header = Regex.Match(line, @"(?:.*?\x22>){2}(.*?]).*>(\d*)<").Groups;
                _Character_Name = damage_header[1].Value;
                _Damage = int.Parse(damage_header[2].Value);
                _Target_Character_Name = damage_header[1].Value; // Target is the same as the source.
            }
            else if (line.Contains("margin-left:   0px"))
            {
                throw new Mode_Change_Exception("Reached end of damage record");
            }
            else if (line.Contains("Damage result:"))
            {
                //<div style="margin-left:  50px">	Damage result: <b>19</b>.</div>
                Source += line + "\n";

                GroupCollection damage_result_header = Regex.Match(line, @".*?lt:.*?>(\d*)").Groups;
                _Raw_Damage = int.Parse(damage_result_header[1].Value);
            }
            else if (line.Contains("Damage source:"))
            {
                //<div style="margin-left:  50px">		Damage source: <b>Shock Flaming Corrosive Cold Iron Kukri +3</b>.</div>
                Source += line + "\n";

                GroupCollection damage_source = Regex.Match(line, @".*?e:.*?>(.*?)<").Groups;
                _Damage_Source = damage_source[1].Value;
            }
            else if (line.Contains("Damage to")) // Not all damage records contains this line
            {
                //<div style="margin-left:  50px">	Damage to <b><b><span style="color:#262626">Zerieks</span></b></b> was reduced to <b>15</b>.</div>
                Source += line + "\n";

                GroupCollection parse_net_damage = Regex.Match(line, @"(?:.*\x22>){2}(.*?)<.*?s (.*?) .*?>(\d*)").Groups;
                string tmp_target_name = parse_net_damage[1].Value;
                _Damage_Adjective = parse_net_damage[2].Value;
                int tmp_damage = int.Parse(parse_net_damage[3].Value);
                if ( (tmp_damage != _Damage) ) // Oddly, the target name used here ommits the UID, so it will never match.  Oh well.
                {
                    throw new System.Exception("Damage doesn't agree when processing damage record with line \"" + line + "\"");
                }
            }
            else
            {
                //<div style="margin-left: 100px">		1d4+9 = <b>10</b> Slashing </div>
                //<div style="margin-left: 100px">		1d8+16 (×2) = <b>42</b> Bludgeoning, Piercing, Slashing </div>
                //<div style="margin-left: 100px">		1d6+6 = <b>4</b> Fire (Multiplied by x0.5) </div>
                //<div style="margin-left: 100px">		4d6+1 = <b>12</b> Fire (Maximized) (Multiplied by x0.5) </div>
                //<div style="margin-left: 100px">  	<b>30</b> precision damage is dealt due to sneak attack.</div>
                Source += line + "\n";

                if (line.Contains("precision damage is dealt due to sneak attack."))
                {
                    // Nothing to do -- the only thing exposed is the total amount of precision damage done, but not the dice that make it up.
                    // I'm still leaving this block in, in case the format changes in the future.
                }
                else if (line.Contains("Successful saving throw halves the damage."))
                {
                    _Sucessful_Save = true;
                }
                else if (line.Contains("Improved Evasion halves the damage"))
                {
                    // Nothing to do, at least for now, but we need to cover this case to prevent fall through.
                }
                else
                {
                    // <div style="margin-left:  50px">	2d8 = <b>9</b> Sonic </div>
                    GroupCollection equals_split = Regex.Match(line, @".*?>\s*(.*?) =.*?>(\d*).*?> (.*?) <").Groups;
                    string mostly_pathfinder_dice = equals_split[1].Value;
                    int dice_result = int.Parse(equals_split[2].Value);
                    string flags_and_attributes = equals_split[3].Value;

                    string pathfinder_dice = "";
                    string before_attributes = "";
                    string after_attributes = "";
                    string flags = "";

                    if (mostly_pathfinder_dice.Contains("("))
                    {
                        GroupCollection before_space_split = Regex.Match(mostly_pathfinder_dice, @"(.*?) (\(.*)").Groups;
                        pathfinder_dice = before_space_split[1].Value;
                        before_attributes = before_space_split[2].Value;
                    }
                    else
                    {
                        pathfinder_dice = mostly_pathfinder_dice;
                    }

                    if (flags_and_attributes.Contains("("))
                    {
                        GroupCollection before_space_split = Regex.Match(flags_and_attributes, @"(.*?) (\(.*)").Groups;
                        flags = before_space_split[1].Value;
                        after_attributes = before_space_split[2].Value;
                    }
                    else
                    {
                        flags = flags_and_attributes;
                    }

                    Die_Roll dice = null;

                    if (!pathfinder_dice.Contains("d"))
                    {
                        // Yes, this actually happens.

                        //<div style="margin-left:   0px"><b><b><span style="color:#224863">Wenduag_Companion[c2c0dfd3]</span></b></b> deals <b>1</b> damage to <b><b><span style="color:#262626">CR17_Mercenary_Dwarf_Ranged_Male[dfdeced4]</span></b></b>.</div>
                        //<div style="margin-left:  50px">	Damage result: <b>1</b>.</div>
                        //<div style="margin-left:  50px">		Damage source: <b>Shock Flaming Corrosive Cold Iron Kukri +3</b>.</div>

                        // Then there are the odd cases:

                        //<div style="margin-left: 100px">		0 = <b>1</b> Bludgeoning </div>
                        //
                        // or
                        //
                        //<div style="margin-left: 100px">		4 = <b>4</b> Piercing (Reduced to 3) </div>

                        // You'd think that this is for constant damage, but it doesn't seem to be that...

                        // In any case, there's no die to add, so skip that processing.
                    }
                    else
                    {
                        dice = new Die_Roll("Damage", _Character_Name, dice_result, pathfinder_dice);

                        if (before_attributes.Contains(")"))
                        {
                            foreach (string curr_attr in before_attributes.Split(')'))
                            {
                                string tmp = curr_attr.Replace("(", "").Trim();
                                dice.Before_Flags.Add(tmp);
                            }
                        }

                        if (flags.Contains(","))
                        {
                            foreach (string curr_flag in flags.Split(','))
                            {
                                string tmp = curr_flag.Trim();
                                dice.Attributes.Add(tmp);
                            }
                        }

                        if (after_attributes.Contains(")"))
                        {
                            foreach (string curr_attr in after_attributes.Split(')'))
                            {
                                string tmp = curr_attr.Replace("(", "").Trim();
                                dice.After_Flags.Add(tmp);
                            }
                        }

                        _Damage_Dice.Add(dice);
                    }
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
                "Damage Source",
                "Target",
                "Total Damage",
                "Semi-Raw Damage",
                "Damage Details"
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
                        case "Damage Source": tb.Text = _Damage_Source; break;
                        case "Target": tb.Text = _Target_Character_Name; break;
                        case "Total Damage": tb.Text = _Damage.ToString(); break;
                        case "Semi-Raw Damage": tb.Text = _Raw_Damage.ToString(); break;
                        case "Damage Details":
                            foreach (Die_Roll roll in _Damage_Dice)
                            {
                                tb.Text += "\n" + roll.Num_Of_Dice.ToString() + "d" + roll.Type_Of_Die.ToString() + ((roll.Bonus < 0) ? roll.Bonus.ToString() : "+" + roll.Bonus.ToString()) + " ";
                                foreach (string str in roll.Before_Flags)
                                {
                                    tb.Text += "(" + str + ") ";
                                }
                                tb.Text += " = " + roll.Roll.ToString()+" ";
                                foreach (string str in roll.Attributes)
                                {
                                    tb.Text += str + ",";
                                }
                                tb.Text = tb.Text.Substring(0, tb.Text.Length);
                                foreach (string str in roll.After_Flags)
                                {
                                    tb.Text += "(" + str + ") ";
                                }
                                tb.Text += "\n";
                            }
                            break;
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
