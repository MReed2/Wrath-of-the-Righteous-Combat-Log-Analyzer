using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public class AttackEvent : CombatEventTargeted
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
        
        public override string Source_Target_Character_Name { get => _Source_Target_Character_Name; }
        public override string Target_Character_Name { get => _Target_Character_Name; set => _Target_Character_Name = value; }

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

        public bool IsHit
        {
            get { return _Attack_Success; }
        }
        
        public bool IsCritical
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

        public AttackEvent(int inID, int inCombatID, string line): base(inID, inCombatID, line) { }
        
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

        private List<Die_Roll> Parse_Comma_List(string inLst, string die_roll_reason, string die_roll_char_name, string die_roll_friendly_name, int die_roll_num_of_sides, int die_roll_num_of_times, int die_roll_bonus, int die_roll_target)
        {
            string[] inArray = inLst.Split(',');
            if (inArray.GetUpperBound(0)==0) { throw new System.Exception("In Parse_Comma_List with input \"" + inLst + "\", no comma found."); }

            List<Die_Roll> rtn = new List<Die_Roll>();
            for (int x=0; x<=inArray.GetUpperBound(0);x++)
            {
                if (inArray[x].Contains("<"))
                {
                    rtn.Add(
                        new Die_Roll(
                            die_roll_reason, 
                            die_roll_char_name,
                            die_roll_friendly_name,
                            int.Parse(Regex.Replace(inArray[x], @"(<.*?>)", "")), 
                            die_roll_num_of_sides, 
                            die_roll_num_of_times, 
                            die_roll_bonus)
                        {
                            Underlined = true,
                            Target = die_roll_target
                        } );
                }
                else
                {
                    rtn.Add(
                        new Die_Roll(
                            die_roll_reason,
                            die_roll_char_name,
                            die_roll_friendly_name,
                            int.Parse(inArray[x]),
                            die_roll_num_of_sides,
                            die_roll_num_of_times,
                            die_roll_bonus)
                        {
                            Target = die_roll_target
                        } );
                }
            }
            return rtn;
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
                //
                //Screwy formatting -- amazing work there, Owlcat.
                //
                //<div style="margin-left:   0px">IvorySanctum_MythicSchir[dcbf72f7]<b><b><span style="color:#262626"></span></b></b> attacks <b><b><span style="color:#AF501F">Ember_Companion[a7e209db]</span></b></b> with <b>Masterwork Bardiche</b>. Hit</div>
                //
                //Yeap, its possible to have a NULL target for an attack as well.  Isn't Owlcat **wonderful**?
                //
                //<div style="margin-left:   0px"><b><b><span style="color:#1356B1">Arueshalae_Companion[41735ffc]</span></b></b> attacks <b><b><span style="color:#262626"></span></b></b> with <b>Eye for an Eye</b>. Critical hit! Sneak attack!</div>
                //
                Source += line + "\n";

                Match attack_hdr = Regex.Match(line, @"(?:\x22>.*?){2}(.*?)<.*?\x22>(.*?)<.*h .*?>(.*?)<.*? (.*?)<");
                if (attack_hdr.Success)
                {
                    if (attack_hdr.Groups[1].Value == "")
                    {
                        attack_hdr = Regex.Match(line, @"(?:\x22>.*?)(.*?)<.*?\x22>.*\x22>(.*?)<.*h .*?>(.*?)<.*? (.*?)<");
                    }
                    _Character_Name = attack_hdr.Groups[1].Value;
                    _Source_Character_Name = attack_hdr.Groups[1].Value;
                    _Target_Character_Name = attack_hdr.Groups[2].Value;
                    _Source_Target_Character_Name = attack_hdr.Groups[2].Value;
                    if (_Target_Character_Name == "")
                    {
                        _Target_Character_Name = "NULL";
                        _Source_Target_Character_Name = "NULL";
                    }
                    _Weapon = attack_hdr.Groups[3].Value;
                    _Attack_Success = (attack_hdr.Groups[4].Value.ToLower().Contains("hit"));
                    _Sneak_Attack = (attack_hdr.Groups[4].Value.ToLower().Contains("sneak attack"));
                    _Attack_Critical = (attack_hdr.Groups[4].Value.ToLower().Contains("critical"));
                    _init_done = true;

                }
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
                        _Concealment_Die_Rolls.AddRange(Parse_Comma_List(concealment_results, "Concealment", _Character_Name, Friendly_Name, 100, 1, 0, _Concealment_Miss_Chance));
                    }
                    else
                    {
                        Die_Roll tmp_Die_Roll = new Die_Roll("Concealment", _Character_Name, Friendly_Name, int.Parse(concealment_results), 100, 1) { Target = _Concealment_Miss_Chance };
                        _Concealment_Die_Rolls.Add(tmp_Die_Roll);
                    }
                }
                //<div style="margin-left: 150px">Attack result: 35 (roll: 15 + modifiers: 20)		Target's Armor Class: 24		Result: hit</div>
                //<div style="margin-left: 150px">Attack result: Natural 20.		Target's Armor Class: 24.		Result: hit	</div>
                //<div style="margin-left: 150px">Attack result: Natural 1.		Target's Armor Class: 37.		Result: critical miss</div>
                //<div style="margin-left:  50px">Attack result: Natural 2, <b><u>1</u></b> [Misfortune].		Target's Armor Class: 28.		Result: critical miss</div>
                if (line.Contains("Attack result:"))
                {
                    //Deal with "<s>1</s> 20" (Mythic Trickster feat) that turns 1s into 20s.  We want to discard the *20* and keep the 1, because that's what was rolled.

                    line = Regex.Replace(line, @"<.>(\d*)<..> (\d*)", "$1");

                    if (line.Contains("Natural"))
                    {
                        line = line.Replace("Natural ", "");

                        //<div style="margin-left:  50px">Attack result: 1.		Target's Armor Class: -2.		Result: critical miss</div>
                        //<div style="margin-left: 150px">Attack result: 20.		Target's Armor Class: 24.		Result: hit	</div>
                        //<div style="margin-left: 150px">Attack result: 1.		Target's Armor Class: 37.		Result: critical miss</div>
                        //<div style="margin-left:  50px">Attack result: 2, <b><u>1</u></b> [Misfortune].		Target's Armor Class: 28.		Result: critical miss</div>
                        //<div style="margin-left:  50px">Attack result: 2, <b><u>1</u></b>.		Target's Armor Class: 28.		Result: critical miss</div>

                        Regex extract_attack_roll = new Regex(@"(.*?>).*?: (.*?).\t.*?: ([+-]?\d*?)\.?\s\s");
                        GroupCollection extract_attack_roll_results = extract_attack_roll.Match(line).Groups;
                        string attack_die_rolls = extract_attack_roll_results[2].Value;
                        _Target_AC = int.Parse(extract_attack_roll_results[3].Value);
                        line = extract_attack_roll.Replace(line, "$1");

                        if (attack_die_rolls.Contains(","))
                        {   
                            // 2, <b><u>1</u></b> [Misfortune]
                            attack_die_rolls = Regex.Replace(attack_die_rolls, @"( \[.*?\])", "");
                            _Attack_Die_Rolls.AddRange(Parse_Comma_List(attack_die_rolls, "Attack", _Character_Name, Friendly_Name, 20, 1, 0, -999));
                        }
                        else
                        {
                            _Attack_Die_Rolls.Add(new Die_Roll("Attack", _Character_Name, Friendly_Name, int.Parse(attack_die_rolls), 20, 1, 0));
                        }
                    }
                    else // Yes, we need to split the cases -- the lines without a Natural 20 / natural 1 contain text within parenthesis -- see below.
                    {
                        //<div style="margin-left:   0px"> Attack result: 32 (roll: 13 + modifiers: 28)		Target's Armor Class: 22		Result: hit</div>
                        //<div style="margin-left:  50px">Attack result: 32 (roll: 13, <b><u>4</u></b> [Touch of Chaos] + modifiers: 28)		Target's Armor Class: 22		Result: hit</div>
                        //<div style="margin-left:  50px">Attack result: 17 (roll: 19 + modifiers: -2)		Target's Armor Class: 41		Result: miss</div>
                        //<div style="margin-left:  50px">Attack result: 24 (roll: 19 + modifiers: 5)		Target's Armor Class: -7		Result: hit</div>
                        //<div style="margin-left:  50px">Attack result: 48 (roll: 18, <b><u>4</u></b> + modifiers: 44)		Target's Armor Class: 31		Result: hit</div>
                        Regex extract_attack_roll = new Regex(@"(.*?>).*?: (\d*?) .*?: (.*?) \+.*?: ([+-]?\d*?)\).*?: ([+-]?\d*?)\t\t");
                        GroupCollection extract_attack_roll_results = extract_attack_roll.Match(line).Groups;
                        _Net_Attack_Value = int.Parse(extract_attack_roll_results[2].Value);
                        string attack_die_rolls = extract_attack_roll_results[3].Value;
                        _Attack_Bonus = int.Parse(extract_attack_roll_results[4].Value);
                        _Target_AC = int.Parse(extract_attack_roll_results[5].Value);
                        if (attack_die_rolls.Contains(","))
                        {
                            //13, <b><u>4</u></b> [Touch of Chaos]
                            //18, <b><u>4</u></b>

                            attack_die_rolls = Regex.Replace(attack_die_rolls, @"(?: \[.*?\])", ""); // Remove anything that is within square brackets.

                            //13, <b><u>4</u></b> 
                            // One or both of the entries will be underlined

                            _Attack_Die_Rolls.AddRange(Parse_Comma_List(attack_die_rolls, "Attack", _Character_Name, Friendly_Name, 20, 1, 0, _Target_AC));
                        }
                        else
                        {
                            _Attack_Die_Rolls.Add(new Die_Roll("Attack", _Character_Name, Friendly_Name, int.Parse(attack_die_rolls), 20, 1, 0) { Target = _Target_AC - _Attack_Bonus });
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
                //<div style="margin-left: 100px">		Critical confirmation result: 34 (roll: <b><u>13</u></b>, <b><u>13</u></b> [Misfortune] + modifiers: 21).		Result: critical hit confirmed</div>
                Source += line + "\n";

                line = Regex.Replace(line, @"<.>(\d*)<..> (\d*)", "$1"); // Turn <s>1</s> 20 into a simple 1.
                Match critical_hit_conf_match = Regex.Match(line, @".*?t: (\d*?) .*?: (\d*?) .*?: ([+-]?\d*?)\).*?: (.*?)<");
                if (critical_hit_conf_match.Success)
                {
                    _Net_Critical_Confirmation_Value = int.Parse(critical_hit_conf_match.Groups[1].Value);
                    _Critical_Confirmation_Rolls.Add(new Die_Roll("Critical Confirmation", _Character_Name, Friendly_Name, int.Parse(critical_hit_conf_match.Groups[2].Value)));
                    _Critical_Confirmation_Bonus = int.Parse(critical_hit_conf_match.Groups[3].Value);
                }
                else if (line.Contains("Natural"))
                {
                    // _Net_Critical_Confirmation_Value = -999;  These are default values, so leave them alone.
                    // _Critical_Confirmation_Bonus = -999;
                    line = line.Replace("Natural ", "");
                    critical_hit_conf_match = Regex.Match(line, @".*?: (\d*)\..*?: (.*?)<");
                    if (critical_hit_conf_match.Success) // Only a single roll
                    {
                        _Critical_Confirmation_Rolls.Add(new Die_Roll("Critical Confirmation", _Character_Name, Friendly_Name, int.Parse(critical_hit_conf_match.Groups[1].Value)));
                    }
                    else
                    {
                        //Assumed, based on how attacks work, but no sample data.  Note that the word "Natural" has been removed by this point.
                        //The following is a guess-estimate of what this sort of line should look like.
                        //<div style="margin-left: 100px">		Critical confirmation result: 2, <b><u>1</u></b> [Misfortune].		Result: critical hit confirmed</div>
                        if (line.Contains(","))
                        {
                            critical_hit_conf_match = Regex.Match(line, @">.*?: (.*?) \[.*?: (.*?)<");
                            if (critical_hit_conf_match.Success)
                            {
                                _Critical_Confirmation_Rolls.AddRange(Parse_Comma_List(critical_hit_conf_match.Groups[1].Value, "Critical Confirmation", _Character_Name, Friendly_Name, 20, 1, 0, -999));
                            }
                            else { throw new System.Exception("Unable to parse critical hit confirmation line (w/ 'Natural' removed) \"" + line + "\""); }
                        }
                        else { throw new System.Exception("Unable to parse critical hit confirmation line (w/ 'Natural' removed) \"" + line + "\""); }
                    }
                }
                else if (line.Contains(","))
                { //<div style="margin-left: 100px">		Critical confirmation result: 34 (roll: <b><u>13</u></b>, <b><u>13</u></b> [Misfortune] + modifiers: 21).		Result: critical hit confirmed</div>
                    critical_hit_conf_match = Regex.Match(line, @">.*?: (.*?) .*?: (.*?)\[.*?: (\d*).*?: (.*?)<");
                    if (critical_hit_conf_match.Success)
                    {
                        _Net_Critical_Confirmation_Value = int.Parse(critical_hit_conf_match.Groups[1].Value);
                        _Critical_Confirmation_Bonus = int.Parse(critical_hit_conf_match.Groups[3].Value);
                        _Critical_Confirmation_Rolls.AddRange(
                            Parse_Comma_List(critical_hit_conf_match.Groups[2].Value,
                            "Critical Confirmation",
                            _Character_Name,
                            Friendly_Name,
                            20,
                            1,
                            0,
                            _Target_AC - _Critical_Confirmation_Bonus)
                            );
                    }
                    else { throw new System.Exception("Unable to parse critical hit confirmation line \"" + line + "\""); }
                }
                else { throw new System.Exception("Unable to parse critical hit confirmation line \"" + line + "\""); }
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
                    foreach (Die_Roll curr_roll in Attack_Die_Rolls) { curr_roll.Bonus = _Attack_Bonus; curr_roll.Target = (_Target_AC - _Attack_Bonus); }
                    foreach (Die_Roll curr_roll in Critical_Confirmation_Rolls)
                    {
                        if (curr_roll.Bonus == -999) { curr_roll.Bonus = _Attack_Bonus; }
                        if (curr_roll.Target == -999)
                        {
                            if (_Critical_Confirmation_Bonus == -999) { curr_roll.Target = (_Target_AC - _Attack_Bonus); }
                            else { curr_roll.Target = (_Target_AC - _Critical_Confirmation_Bonus); }
                        }
                    }
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

        private string Die_Rolls_To_String(List<Die_Roll> inRolls)
        {
            string rtn = "";
            foreach (Die_Roll curr_roll in inRolls) { rtn += curr_roll.Roll.ToString() + ", "; }
            if (inRolls.Count > 0) { rtn = rtn.Substring(0, rtn.Length - 2); }
            else { rtn = "N/A"; }
            return rtn;
        }

        public override UserControl Get_UserControl_For_Display()
        {
            UserControl uc = new UserControl();

            string[,] data =
            {
                { "Character Faction", Character_Type_To_String(Character_Type) },
                { "Character Name", Source_Character_Name },
                { "Friendly Character Name", Friendly_Name },
                { "", "" },
                { "", "" },

                { "Target Faction", Character_Type_To_String(Target_Character_Type) },
                { "Target Character Name", Source_Target_Character_Name },
                { "Target Friendly Name", Target_Friendly_Name },
                { "", "" },
                { "", "" },

                { "Attack Bonus", (_Attack_Bonus==-999)?"N/A (Missed due to concealment)":_Attack_Bonus.ToString() },
                { "Target AC", (_Target_AC==-999)?"N/A (Missed due to concealment)":_Target_AC.ToString() },
                { "Needed to hit", (Need_To_Roll_To_Hit==-999)?"N/A (Missed due to concealment)":Need_To_Roll_To_Hit.ToString() },
                { "To Hit Roll(s)", Die_Rolls_To_String(_Attack_Die_Rolls) },
                { "Result", (_Attack_Success)?"Hit":"Miss" },

                { "Critical Hit Bonus", (_Critical_Confirmation_Bonus==-999)?"N/A (No Threat)":_Critical_Confirmation_Bonus.ToString() },
                { "Target AC", (_Target_AC==-999)?"N/A (No Threat)":_Target_AC.ToString() },
                { "Needed to confirm",  (Need_To_Roll_To_Confirm_Critical==-999)?"N/A (No Threat)":Need_To_Roll_To_Confirm_Critical.ToString() },
                { "Critical Confirmation Roll(s)", Die_Rolls_To_String(_Critical_Confirmation_Rolls) },
                { "Critical Result", (_Attack_Critical)?"Critical Hit":"Not a critical hit" },

                { "Concealment Miss Chance",  _Concealment_Miss_Chance.ToString()+" %" },
                { "Concealment Die Roll(s)", Die_Rolls_To_String(_Concealment_Die_Rolls) },
                { "Concealment Result", (_Attack_Bonus==-999)?"Missed":(_Concealment_Miss_Chance==0)?"N/A":"Bypassed" },
                { "", "" },
                { "", "" },

            };

            Grid outer_grid = new Grid();
            outer_grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new System.Windows.GridLength(10, System.Windows.GridUnitType.Star) });

            outer_grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });
            ScrollViewer scrollViewer = New_Windows_Table("Data", data, 5, 1500);
            Grid.SetRow(scrollViewer, 0);
            Grid.SetColumn(scrollViewer, 0);
            outer_grid.Children.Add(scrollViewer);

            outer_grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });
            TextBlock source_title = new TextBlock() { HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
            source_title.Inlines.Add(new System.Windows.Documents.Run("Source") { FontWeight = System.Windows.FontWeights.Bold, TextDecorations = System.Windows.TextDecorations.Underline });
            Grid.SetRow(source_title, 1);
            Grid.SetColumn(source_title, 0);
            outer_grid.Children.Add(source_title);

            WebBrowser webBrowser = New_WebBrowser();
            webBrowser.NavigateToString(Filter_String_For_WebBrowser(Source_With_ID));

            DockPanel dockPanel = new DockPanel() { LastChildFill = true };
            DockPanel.SetDock(outer_grid, Dock.Top);
            dockPanel.Children.Add(outer_grid);

            DockPanel.SetDock(webBrowser, Dock.Bottom);
            dockPanel.Children.Add(webBrowser);

            uc.Content = dockPanel;
            return uc;
        }
    }
}
