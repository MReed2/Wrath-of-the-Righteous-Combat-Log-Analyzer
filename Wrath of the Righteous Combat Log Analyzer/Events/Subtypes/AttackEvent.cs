using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    class AttackEvent : CombatEvent
    {
        private string _Source_Character_Name = "";
        private string _Character_Name = "";
        private string _Source_Target_Character_Name = "";
        private string _Target_Character_Name = "";
        private string _Weapon = "";

        private int _Attack_Num = 1;
        private int _Max_Attack_Num = 1;
        private int _Attack_Bonus = -999;
        private int _Net_Attack_Value = -999;
        private int _Target_AC = -999;

        private int _Concealment_Miss_Chance = 0;

        private int _Net_Critical_Confirmation_Value = -999;
        private int _Critical_Confirmation_Bonus = -999;

        private List<Die_Roll> _Concealment_Die_Rolls = new List<Die_Roll>();
        private List<Die_Roll> _Attack_Die_Rolls = new List<Die_Roll>();
        private List<Die_Roll> _Critical_Confirmation_Rolls = new List<Die_Roll>();

        private bool _Attack_Success = false;

        // These two variables cover things like mirror image -- where a hit can be converted into a miss as an exception.
        private bool _Hit_To_Miss_Exception = false;
        private string _Hit_To_Miss_Exception_Reason = "";

        private bool _Attack_Critical = false;
        private bool _Sneak_Attack = false;

        private List<string> _AC_Flags = new List<string>();

        private List<Bonus> _Attack_Bonus_Detail = new List<Bonus>();
        private List<Bonus> _AC_Detail = new List<Bonus>();

        private int  _Parsing_State = 0;
        private bool _init_done = false;

        private CombatEventList _Children = new CombatEventList();
                
        public override string Source_Character_Name => _Source_Character_Name;

        public override string Character_Name
        {
            get { return _Character_Name; }
            set { _Character_Name = value; }
        }
        
        public string Source_Target_Character_Name { get => _Source_Character_Name; }

        public string Target_Character_Name
        {
            get { return _Target_Character_Name; }
        }

        public string Weapon
        {
            get { return _Weapon; }
        }

        public int Attack_Num
        {
            get { return _Attack_Num; }
        }

        public int Max_Attack_Num
        {
            get { return _Max_Attack_Num; }
        }

        public int Attack_Bonus
        {
            get { return _Attack_Bonus; }
        }

        public int Net_Attack_Value
        {
            get { return _Net_Attack_Value; }
        }

        public int Target_AC
        {
            get { return _Target_AC; }
        }

        public int Concealment_Miss_Chance
        {
            get { return _Concealment_Miss_Chance; }
        }

        public int Net_Critical_Confirmation_Value
        {
            get { return _Net_Critical_Confirmation_Value;  }
        }

        public int Critical_Confirmation_Bonus
        {
            get { return _Critical_Confirmation_Bonus;  }
        }

        public bool Attack_Success
        {
            get { return _Attack_Success; }
        }
        
        public bool Attack_Critical
        {
            get { return _Attack_Critical; }
        }

        public List<string> AC_Flags
        {
            get { return _AC_Flags; }
        }

        public List<Bonus> Attack_Bonus_Detail
        {
            get { return _Attack_Bonus_Detail; }
        }

        public List<Bonus> AC_Detail
        {
            get { return _AC_Detail; }
        }
        
        public int Need_To_Roll_To_Hit
        {
            get
            {
                if ((_Attack_Bonus == -999) || (_Target_AC == -999))
                {
                    return -999;
                }
                else
                {
                    int rtn = _Target_AC - _Attack_Bonus;

                    if (rtn < 0) { rtn = 0; }
                    if (rtn > 21) { rtn = 21; }

                    return rtn;
                }
            }
        }

        public List<int> To_Hit_Margin
        {
            get
            {
                List<int> rtn = new List<int>();

                if (Need_To_Roll_To_Hit != -999)
                {
                    foreach (Die_Roll curr_roll in Attack_Die_Rolls)
                    {
                        rtn.Add(curr_roll.Roll - Need_To_Roll_To_Hit);
                    }
                }

                return rtn;
            }
        }

        public int Need_To_Roll_To_Confirm_Critical
        {
            get
            {
                if (_Critical_Confirmation_Bonus == -999)
                {
                    if (_Critical_Confirmation_Rolls.Count == 0)
                    {
                        return -999;  // Didn't threathen a critical hit.
                    }
                    else // Have a problem here -- a confirmed critical hit on a Natural 20 *doesn't providea a critical hit confirmation bonus*.
                    {
                        return Need_To_Roll_To_Hit; // This is very close to the correct value (and the exact value only matters a little bit).
                    }
                }
                else
                {
                    int rtn = _Target_AC - Critical_Confirmation_Bonus;

                    if (rtn < 0) { rtn = 0; }
                    if (rtn > 21) { rtn = 21; }
                    return rtn;
                }
            }
        }

        public List<int> Critical_Confirmation_Margin
        {
            get
            {
                List<int> rtn = new List<int>();

                if (Need_To_Roll_To_Confirm_Critical != -999)
                {
                    foreach (Die_Roll curr_roll in Critical_Confirmation_Rolls)
                    {
                        rtn.Add(curr_roll.Roll - Need_To_Roll_To_Confirm_Critical);
                    }
                }

                return rtn;
            }
        }

        public AttackEvent(int inID, string line): base(inID, line) { }
        
        public override List<Die_Roll> Die_Rolls
        {
            get
            {
                List<Die_Roll> tmp = new List<Die_Roll>();
                tmp.AddRange(_Attack_Die_Rolls);
                tmp.AddRange(_Critical_Confirmation_Rolls);
                tmp.AddRange(_Concealment_Die_Rolls);
                return tmp;
            }
        }

        public List<Die_Roll> Concealment_Die_Rolls
        {
            get { return _Concealment_Die_Rolls; }
        }

        public List<Die_Roll> Attack_Die_Rolls
        {
            get { return _Attack_Die_Rolls; }
        }

        public List<Die_Roll> Critical_Confirmation_Rolls
        {
            get { return _Critical_Confirmation_Rolls; }
        }
        
        public override List<Die_Roll> Parse(string line)
        {
            if ((line.Contains("attacks")) && (!line.Contains("attack of opportunity")) && (_Parsing_State == 0) )
            {
                if (_init_done)
                {
                    throw new System.Exception("Started reading a second attack after having already processed one");
                }
                //<div style="margin-left: 0px"><b><b><span style="color:#224863">Wenduag_Companion[c2c0dfd3]</span></b></b> attacks <b><b><span style="color:#262626">Zerieks[1507b0b0]</span></b></b> with <b>Shock Flaming Corrosive Cold Iron Kukri +3</b>. Hit</div>
                //<div style="margin-left:   0px"><b><b><span style="color:#262626">CR17_Mercenary_Human_Melee_Male[99e83994]</span></b></b> attacks <b><b><span style="color:#224863">Wenduag_Companion[c2c0dfd3]</span></b></b> with <b>Punching Dagger</b>. Hit Sneak attack!</div>
                Source += line + "\n";

                GroupCollection attack_hdr = Regex.Match(line, @"(?:\x22>.*?){2}(.*?)<.*?\x22>(.*?)<.*h .*?>(.*?)<.*? (.*?)<").Groups;
                _Character_Name = attack_hdr[1].Value;
                _Source_Character_Name = attack_hdr[1].Value;
                _Target_Character_Name = attack_hdr[2].Value;
                _Source_Target_Character_Name = attack_hdr[3].Value;
                _Weapon = attack_hdr[3].Value;
                _Attack_Success = (attack_hdr[4].Value.ToLower().Contains("hit"));
                _Sneak_Attack = (attack_hdr[4].Value.ToLower().Contains("sneak attack"));
                _Attack_Critical = (attack_hdr[4].Value.ToLower().Contains("critical"));
                _init_done = true;
            }
            else if ( (line.Contains("Result:")) && (!line.Contains("Critical confirmation result")) )
            {
                //<div style="margin-left: 50px">Concealment miss chance — 50%, roll: 100 — successful		Attack result: 35 (roll: 15 + modifiers: 20)		Target's Armor Class: 24		Result: hit</div>
                Source += line + "\n";

                if (line.Contains("Attack number:"))
                {
                    Regex extract_attack_num = new Regex(@"(.*?>)(?:.*: )(\d*?) out of (\d*?)\s\s");
                    GroupCollection extract_attack_num_groups = extract_attack_num.Match(line).Groups;
                    _Attack_Num = int.Parse(extract_attack_num_groups[2].Value);
                    _Max_Attack_Num = int.Parse(extract_attack_num_groups[3].Value);
                    line = extract_attack_num.Replace(line, "$1"); // Strips off the "Attack number: A out of B/t/t" prefix.
                }
                //<div style="margin-left: 50px">Concealment miss chance — 50%, roll: 100 — successful		Attack result: 35 (roll: 15 + modifiers: 20)		Target's Armor Class: 24		Result: hit</div>
                //<div style="margin-left: 150px">	Concealment miss chance — 50%, roll: 38 — failed		Result: miss</div>
                //<div style="margin-left: 150px">	Concealment miss chance — 50%, roll: <b><u>34</u></b>, <b><u>34</u></b> [Blind Fight] — failed		Result: miss</div>
                //<div style="margin-left: 150px">	Concealment miss chance — 50%, roll: <b><u>34</u></b>, <b><u>34</u></b> [Blind Fight] — successful		Attack result: 35 (roll: 15 + modifiers: 20)		Target's Armor Class: 24		Result: hit</div>
                if (line.Contains("Concealment miss chance"))
                {
                    Regex extract_concealment = new Regex(@"(.*?>).*?— (\d*)%.*?: (.*) — (.*?)\s\s");
                    GroupCollection extract_concealment_results = extract_concealment.Match(line).Groups;
                    _Concealment_Miss_Chance = int.Parse(extract_concealment_results[2].Value);
                    string concealment_results = extract_concealment_results[3].Value;
                    line = extract_concealment.Replace(line, "$1");

                    if (concealment_results.Contains(","))
                    {
                        //<b><u>34</u></b>, <b><u>34</u></b> [Blind Fight]
                        concealment_results = Regex.Replace(concealment_results, @"(<.*?>)", "");
                        //34, 34 [Blind Fight]
                        concealment_results = Regex.Replace(concealment_results, @"( \[.*?\])", "");
                        //34, 34
                        foreach (string num_str in concealment_results.Split(','))
                        {
                            Die_Roll tmp_Die_Roll = new Die_Roll("Concealment", _Character_Name, int.Parse(num_str), 100, 1);
                            tmp_Die_Roll.Target = _Concealment_Miss_Chance;
                            _Concealment_Die_Rolls.Add(tmp_Die_Roll);
                        }
                    }
                    else
                    {
                        Die_Roll tmp_Die_Roll = new Die_Roll("Concealment", _Character_Name, int.Parse(concealment_results), 100, 1);
                        tmp_Die_Roll.Target = _Concealment_Miss_Chance;
                        _Concealment_Die_Rolls.Add(tmp_Die_Roll);
                    }
                }
                //<div style="margin-left: 150px">Attack result: 35 (roll: 15 + modifiers: 20)		Target's Armor Class: 24		Result: hit</div>
                //<div style="margin-left: 150px">Attack result: Natural 20.		Target's Armor Class: 24.		Result: hit	</div>
                //<div style="margin-left: 150px">Attack result: Natural 1.		Target's Armor Class: 37.		Result: critical miss</div>
                if (line.Contains("Attack result:"))
                {
                    if (line.Contains("Natural"))
                    // I believe that this logic will fail in the following case:
                    //   1) The attack roll is rolled twice (or more).
                    //   2) The worst roll is kept.
                    //   3) The best roll is a natural 20.
                    // In this scenario, I have no idea how the string will be formatted.  Fingers crossed that it simply shows "20" in the list of
                    // die rolls (rather than "Natural 20"), but...  Given the craziness of the formatting elsewhere, I'm very, very doubtful.
                    {
                        //<div style="margin-left:  50px">Attack result: Natural 1.		Target's Armor Class: -2.		Result: critical miss</div>
                        Regex extract_attack_roll = new Regex(@"(.*?>).*?: (.*?).\t.*?: ([+-]?\d*?)\.?\s\s");
                        GroupCollection extract_attack_roll_results = extract_attack_roll.Match(line).Groups;
                        string attack_die_rolls = extract_attack_roll_results[2].Value;
                        _Target_AC = int.Parse(extract_attack_roll_results[3].Value);
                        line = extract_attack_roll.Replace(line, "$1");

                        if (attack_die_rolls.Contains(",")) // this is *really* dubious
                        {
                            // No sample data, and I'm not sure this is even required -- given the other odd formatting, 
                            // I suspect that even if you had multiple attack rolls, if the roll that was kept was a 20
                            // (or 1) then the other rolls aren't recorded.  If they *are* recorded, I'm very dubious the
                            // format is the same as for the single roll case (in particular, the "." that appears after
                            // "Natural 20" or "Natural 1").  However it doesn't hurt anything to include it, so...
                            attack_die_rolls = Regex.Replace(attack_die_rolls, @"(<.*?>)", "");
                            attack_die_rolls = Regex.Replace(attack_die_rolls, @"( \[.*?\])", "");
                            foreach (string num_str in attack_die_rolls.Split(','))
                            {
                                if (num_str.Contains("Natural 1"))
                                {
                                    _Attack_Die_Rolls.Add(new Die_Roll("Attack", _Character_Name, 1));
                                }
                                else if (num_str.Contains("Natural 20"))
                                {
                                    _Attack_Die_Rolls.Add(new Die_Roll("Attack", _Character_Name, 20));
                                }
                                else if (line.Contains("Natural <s>1</s> 20")) // This is how the "1s turn into 20s" feat appears in the log.
                                {
                                    _Attack_Die_Rolls.Add(new Die_Roll("Critical Confirmation", _Character_Name, 1));
                                }
                                else
                                {
                                    _Attack_Die_Rolls.Add(new Die_Roll("Attack", _Character_Name, int.Parse(num_str.Trim())));
                                }
                            }
                        }
                        else
                        {
                            if (attack_die_rolls.Contains("Natural 1"))
                            {
                                _Attack_Die_Rolls.Add(new Die_Roll("Attack", _Character_Name, 1));
                            }
                            else if (attack_die_rolls.Contains("Natural 20"))
                            {
                                _Attack_Die_Rolls.Add(new Die_Roll("Attack", _Character_Name, 20));
                            }
                            else if (attack_die_rolls.Contains("Natural <s>1</s> 20")) // This is how the "1s turn into 20s" feat appears in the log.
                            {
                                _Attack_Die_Rolls.Add(new Die_Roll("Attack", _Character_Name, 1));
                            }
                        }
                    }
                    else
                    {
                        //<div style="margin-left:   0px"> Attack result: 32 (roll: 13 + modifiers: 28)		Target's Armor Class: 22		Result: hit</div>
                        //<div style="margin-left:  50px">Attack result: 32 (roll: 13, <b><u>4</u></b> [Touch of Chaos] + modifiers: 28)		Target's Armor Class: 22		Result: hit</div>
                        //<div style="margin-left:  50px">Attack result: 17 (roll: 19 + modifiers: -2)		Target's Armor Class: 41		Result: miss</div>
                        //<div style="margin-left:  50px">Attack result: 24 (roll: 19 + modifiers: 5)		Target's Armor Class: -7		Result: hit</div>
                        Regex extract_attack_roll = new Regex(@"(.*?>).*?: (\d*?) .*?: (.*?) .*?: ([+-]?\d*?)\).*?: ([+-]?\d*?)\t\t");
                        GroupCollection extract_attack_roll_results = extract_attack_roll.Match(line).Groups;
                        _Net_Attack_Value = int.Parse(extract_attack_roll_results[2].Value);
                        string attack_die_rolls = extract_attack_roll_results[3].Value;
                        _Attack_Bonus = int.Parse(extract_attack_roll_results[4].Value);
                        _Target_AC = int.Parse(extract_attack_roll_results[5].Value);
                        if (attack_die_rolls.Contains(","))
                        {
                            // This regular expression is subtly different -- instead of ending the 3rd capture group with a space, it looks for a "[" instead.  This ensures that all the
                            // numbers rolled gets included in the result set.
                            attack_die_rolls = Regex.Match(line, @"(.*?>).*?: (\d*?) .*?: (.*?)\[.*?: (\d*?)\).*?: (\d*?)\t\t").Groups[3].Value;

                            //13, <b><u>4</u></b> 
                            // One or both of the entries will be underlyined.

                            foreach (string num_str in attack_die_rolls.Split(','))
                            {
                                Die_Roll tmp_roll = new Die_Roll("Attack", Character_Name, -1); // Placeholder die roll value

                                if (num_str.Contains("<")) // it contains html tags, so it is underlined, as well as bold
                                {
                                    tmp_roll.Roll = int.Parse(Regex.Replace(num_str, @"<.?.?>", ""));
                                    tmp_roll.Underlined = true;
                                }
                                else
                                {
                                    tmp_roll.Roll = int.Parse(num_str);
                                }

                                _Attack_Die_Rolls.Add(tmp_roll);
                            }
                        }
                        else
                        {
                            _Attack_Die_Rolls.Add(new Die_Roll("Attack", _Character_Name, int.Parse(attack_die_rolls.Trim())));
                        }
                        foreach (Die_Roll tmp_Die_Roll in _Attack_Die_Rolls)
                        {
                            tmp_Die_Roll.Target = _Target_AC - Attack_Bonus;
                        }
                        line = extract_attack_roll.Replace(line, "$1");
                    }
                }
                //<div style="margin-left: 150px">Result: hit</div>
                //<div style="margin-left:  50px">Result: hit		Fortification armor chance: 15%, roll: 57 — fail</div>
                if (line.Contains("Result:"))
                {
                    Regex extract_attack_success = new Regex(@"(.*?>).*?: (.*?)<");
                    GroupCollection extract_attack_success_results = extract_attack_success.Match(line).Groups;
                    bool tmp = (extract_attack_success_results[2].Value.Contains("hit"));
                    if (tmp != _Attack_Success)
                    {
                        //<div style="margin-left:  50px">Result: miss (Mirror Image)	</div>
                        Match check_for_exception = Regex.Match(line, @"(.*?>).*: (.*?) \((.*?)\)");
                        if (check_for_exception.Success)
                        {
                            _Hit_To_Miss_Exception = true;
                            _Hit_To_Miss_Exception_Reason = check_for_exception.Groups[3].Value;
                            _Attack_Success = tmp;
                        }
                        else
                        {
                            throw new System.Exception("Parsing is getting inconsistant results for Attack_Success");
                        }
                    }
                    line = extract_attack_success.Replace(line, "$1<");
                }
                if (line != "<div style=\"margin-left:  50px\"></div>")
                {
                    throw new System.Exception("Unexpected residual text when parsing attack -- \"" + line + "\"");
                }
            }
            else if (line.Contains("Critical confirmation result:"))
            {
                //<div style="margin-left: 100px">		Critical confirmation result: 21 (roll: 5 + modifiers: 16).		Result: critical hit confirmed</div>
                //<div style="margin-left: 100px">		Critical confirmation result: Natural 20.		Result: critical hit confirmed</div>
                //<div style="margin-left: 100px">		Critical confirmation result: Natural <s>1</s> 20.		Result: critical hit confirmed</div>
                //<div style="margin-left: 100px">		Critical confirmation result: 1 (roll: 3 + modifiers: -2).		Result: critical hit not confirmed</div>
                Source += line + "\n";

                if (line.Contains("Natural 20"))  // Sigh.  The rules are ambigious, but (IMO) a Natural 20 shouldn't auto-hit on a critical confirmation roll.
                {
                    _Critical_Confirmation_Rolls.Add(new Die_Roll("Critical Confirmation", _Character_Name, 20));
                }
                else if (line.Contains("Natural 1")) // Just in case -- if a natural 20 autohits, then a natural 1 should automiss.
                {
                    _Critical_Confirmation_Rolls.Add(new Die_Roll("Critical Confirmation", _Character_Name, 1));
                }
                else if (line.Contains("Natural <s>1</s> 20")) // This is how the "1s turn into 20s" feat appears in the log.
                {
                    _Critical_Confirmation_Rolls.Add(new Die_Roll("Critical Confirmation", _Character_Name, 1));
                }
                else 
                {
                    GroupCollection critical_hit_groups = Regex.Match(line, @".*?t: (\d*?) .*?: (\d*?) .*?: ([+-]?\d*?)\).*?: (.*?)<").Groups;
                    _Net_Critical_Confirmation_Value = int.Parse(critical_hit_groups[1].Value);
                    _Critical_Confirmation_Rolls.Add(new Die_Roll("Critical Confirmation", _Character_Name, int.Parse(critical_hit_groups[2].Value)));
                    _Critical_Confirmation_Bonus = int.Parse(critical_hit_groups[3].Value);
                }
                foreach (Die_Roll tmp_Die_Roll in _Critical_Confirmation_Rolls)
                {
                    tmp_Die_Roll.Target = _Target_AC - _Critical_Confirmation_Bonus;
                }
            }
            else if (line.Contains("Attack Bonus:")&&(!line.Contains("Base Attack Bonus")))
            {
                // <div style="margin-left: 50px"><b>Attack Bonus: +25</b></div>
                //<div style="margin-left:  50px"><b>Attack Bonus: –2</b></div>
                Source += line + "\n";

                string tmp_str = Regex.Match(line, @"s: (.*?)<").Groups[1].Value.Replace('–', '-');
                int tmp_attack_bonus = int.Parse(tmp_str);
                if (_Attack_Bonus == -999) // This is possible when a 1 or 20 is rolled.
                {
                    _Attack_Bonus = tmp_attack_bonus;
                }
                if (tmp_attack_bonus != _Attack_Bonus)
                {
                    throw new System.Exception("Summary attack bonus doesn't match header detail attack bonus");
                }
                _Parsing_State = 1;
            }
            else if ( line.Contains("Armor Class:")&&(!line.Contains("Target's Armor Class")) ) 
            {
                //<div style="margin-left: 50px"><b>Armor Class: 8 (Flat-footed, Touch)</b></div>
                //<div style="margin-left:  50px"><b>Armor Class: -7 (Flat-footed, Touch)</b></div>

                Source += line + "\n";

                int tmp_AC = int.Parse(Regex.Match(line, @".*?s: ([+-]?\d*)[ .<]").Groups[1].Value);
                if (tmp_AC != _Target_AC)
                {
                    throw new System.Exception("Summary AC doesn't match header detail AC");
                }
                if (line.Contains("("))
                {
                    string paren_contents = Regex.Match(line, @"\((.*?)\)").Groups[1].Value;
                    if (paren_contents.Contains(","))
                    {
                        foreach (string AC_Flag in paren_contents.Split(','))
                        {
                            _AC_Flags.Add(AC_Flag.Trim());
                        }
                    }
                    else
                    {
                        _AC_Flags.Add(paren_contents);
                    }
                }
                _Parsing_State = 2;
            }
            else if (_Parsing_State == 1)
            {
                Source += line + "\n";
                _Attack_Bonus_Detail.Add(new Bonus(line));
            }
            else if (_Parsing_State == 2)
            {
                try
                {
                    _AC_Detail.Add(new Bonus(line));
                }
                catch (End_Of_Bonus)
                {
                    throw new Mode_Change_Exception("\"" + line + "\" is the start of a new event");
                }
                Source += line + "\n";
            }
            else
            {
                throw new System.Exception("Failed to parse line \"" + line + "\" in AttackEvent");
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
                "Character Name",
                "Target",
                "Weapon",
                "",
                "Concealment Miss Chance",
                "Concealment Die Roll (d100)",
                "Concealment Result",
                "",
                "Attack Bonus",
                "Target AC",
                "Needed To Hit",
                "To Hit Roll (d20)",
                "Result",
                "",
                "Critical Hit Bonus",
                "Target AC ",
                "Needed to confirm",
                "Confirmation Roll (d20)",
                "Critical Result"
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
                        case "Character Name": tb.Text = _Character_Name; break;
                        case "Target": tb.Text = _Target_Character_Name; break;
                        case "Weapon": tb.Text = _Weapon; break;
                        case "Concealment Miss Chance": tb.Text = _Concealment_Miss_Chance.ToString()+" %"; break;
                        case "Concealment Die Roll (d100)":
                            foreach (Die_Roll curr_roll in _Concealment_Die_Rolls)
                            {
                                tb.Text += ", " + curr_roll.Roll.ToString();
                            }
                            if (tb.Text == "")
                            {
                                tb.Text = "N/A";
                            }
                            else
                            {
                                tb.Text = tb.Text.Substring(1);
                            }
                            break;
                        case "Concealment Result":
                            if (_Attack_Bonus == -999)
                            {
                                tb.Text = "Missed";
                            }
                            else
                            {
                                tb.Text = "Bypassed";
                            }
                            break;
                        case "Attack Bonus":
                            if (_Attack_Bonus == -999)
                            {
                                tb.Text = "N/A (Missed due to concealment";
                            }
                            else
                            {
                                tb.Text = ((_Attack_Bonus <= 0) ? _Attack_Bonus.ToString() : "+" + _Attack_Bonus.ToString());
                            }
                            break;
                        case "Target AC": 
                            if (_Target_AC == -999)
                            {
                                tb.Text = "N/A (Missed due to concealment";
                            }
                            else
                            {
                                tb.Text = _Target_AC.ToString();
                            }
                            break;
                        case "Needed To Hit":
                            if (_Attack_Bonus == -999)
                            {
                                tb.Text = "N/A (Missed due to concealment";
                            }
                            else
                            {
                                int target = _Target_AC - _Attack_Bonus;
                                if (target < 0) { target = 0; }
                                if (target > 20) { target = 20; }
                                tb.Text = target.ToString();
                            }
                            break;
                        case "To Hit Roll (d20)":
                            foreach (Die_Roll curr_roll in _Attack_Die_Rolls)
                            {
                                tb.Text += ", " + curr_roll.Roll.ToString();
                            }
                            if (tb.Text == "")
                            {
                                tb.Text = "N/A";
                            }
                            else
                            {
                                tb.Text = tb.Text.Substring(2);
                            }
                            break;
                        case "Result": tb.Text = (_Attack_Success ? "Hit" : "Miss"); break;
                        case "Critical Hit Bonus":
                            if (_Critical_Confirmation_Bonus == -999)
                            {
                                if (_Critical_Confirmation_Rolls.Count > 0)
                                {
                                    tb.Text = "Unknown (value not specified in log)";
                                }
                                else
                                {
                                    tb.Text = "N/A (didn't threaten a critical hit)";
                                }
                            }
                            else
                            {
                                tb.Text = ((_Critical_Confirmation_Bonus <= 0) ? _Critical_Confirmation_Bonus.ToString() : "+" + _Critical_Confirmation_Bonus.ToString());
                            }
                            break;
                        case "Target AC ":
                            if (_Critical_Confirmation_Bonus == -999)
                            {
                                if (_Critical_Confirmation_Rolls.Count > 0)
                                {
                                    tb.Text = _Target_AC.ToString();
                                }
                                else
                                {
                                    tb.Text = "N/A (didn't threaten a critical hit)";
                                }
                            }
                            else
                            {
                                tb.Text = _Target_AC.ToString();
                            }
                            break;
                        case "Needed to confirm":
                            if (_Critical_Confirmation_Bonus == -999)
                            {
                                if (_Critical_Confirmation_Rolls.Count > 0)
                                {
                                    tb.Text = Need_To_Roll_To_Confirm_Critical.ToString();
                                }
                                else
                                {
                                    tb.Text = "N/A (didn't threaten a critical hit)";
                                }
                            }
                            else
                            {
                                tb.Text = Need_To_Roll_To_Confirm_Critical.ToString();
                            }
                            break;
                        case "Confirmation Roll (d20)":
                            foreach (Die_Roll curr_roll in _Critical_Confirmation_Rolls)
                            {
                                tb.Text += ", " + curr_roll.Roll.ToString();
                            }
                            if (tb.Text == "")
                            {
                                tb.Text = "N/A (didn't threaten a critical hit)";
                            }
                            else
                            {
                                tb.Text = tb.Text.Substring(2);
                            }
                            break;
                        case "Critical Result":
                            tb.Text = (_Attack_Critical ? "Critical hit" : "Not a critical hit");
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
