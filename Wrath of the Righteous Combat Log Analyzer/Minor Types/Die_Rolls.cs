using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public class Die_Roll
    {
        private int _Roll = 0;
        private string _Character_Name = "";
        private string _Reason = "";
        private int _Num_Of_Dice = 1;
        private int _Type_Of_Die = 20;
        private int _Bonus = 0;
        private int _Target = 0;
        public bool _Underlined = false;
        private List<string> _Before_Flags = new List<string>();
        private List<string> _Attributes = new List<string>();
        private List<string> _After_Flags = new List<string>();

        public int Roll
        {
            get { return _Roll; }
            set { _Roll = value; }
        }

        public string Character_Name
        {
            get { return _Character_Name;  }
            set { _Character_Name = value; }
        }

        public string Reason
        {
            get { return _Reason; }
            set { _Reason = value; }
        }

        public int Num_Of_Dice
        {
            get { return _Num_Of_Dice; }
            set { _Num_Of_Dice = value; }
        }

        public int Type_Of_Die
        {
            get { return _Type_Of_Die; }
            set { _Type_Of_Die = value; }
        }

        public int Bonus
        {
            get { return _Bonus; }
            set { _Bonus = value; }
        }

        public List<string> Attributes
        {
            get { return _Attributes; }
        }

        public List<string> Before_Flags
        {
            get { return _Before_Flags; }
        }

        public List<string> After_Flags
        {
            get { return _After_Flags; }
        }

        public string Pathfinder_Dice_String
        {
            get { return _Num_Of_Dice.ToString() + "d" + _Type_Of_Die.ToString() + ((Bonus != 0) ? ((_Bonus < 0) ? "-" : "+") + Math.Abs(_Bonus).ToString() : ""); }
        }

        public int Target
        {
            get { return _Target; }
            set { _Target = value; }
        }

        public bool Underlined
        {
            get { return _Underlined; }
            set { _Underlined = value; }
        }

        
        public Die_Roll(string reason, string char_name, int roll)
        {
            _Reason = reason;
            _Character_Name = char_name;
            /*if ((roll < 1) || (roll > ( (_Type_Of_Die*_Num_Of_Dice) + _Bonus) )) { throw new System.Exception(String.Format("Invalid result {0} on d{1}", roll, _Type_Of_Die)); }
            else {*/
            _Roll = roll; /*}*/
        }

        public Die_Roll(string reason, string char_name, int roll, int die_type, int num_of_dice)
        {
            _Num_Of_Dice = num_of_dice;
            _Type_Of_Die = die_type;
            _Reason = reason;
            _Character_Name = char_name;
            /*if ((roll < 1) || (roll > ( (_Type_Of_Die*_Num_Of_Dice) + _Bonus) )) { throw new System.Exception(String.Format("Invalid result {0} on d{1}", roll, _Type_Of_Die)); }
            else {*/
            _Roll = roll; /*}*/
        }

        public Die_Roll(string reason, string char_name, int roll, int die_type, int num_of_dice, int bonus)
        {
            _Bonus = bonus;
            _Num_Of_Dice = num_of_dice;
            _Type_Of_Die = die_type;
            _Reason = reason;
            _Character_Name = char_name;
            /*if ((roll < 1) || (roll > ( (_Type_Of_Die*_Num_Of_Dice) + _Bonus) )) { throw new System.Exception(String.Format("Invalid result {0} on d{1}", roll, _Type_Of_Die)); }
            else {*/
            _Roll = roll; /*}*/
        }

        public Die_Roll(string reason, string char_name, int roll, string pathfinder_dice)
        {
            // 5d8+18
            // 1d20
            // 2d6-4
            if (!pathfinder_dice.Contains("d"))
            {
                throw new System.Exception("Invalid (\"" + pathfinder_dice + "\") pathfinder dice input to Die_Roll");
            }

            if (pathfinder_dice.Contains("+")||pathfinder_dice.Contains("-"))
            {
                GroupCollection parse_die = Regex.Match(pathfinder_dice, @"(\d*)d(\d*)([+-]\d*)").Groups;
                _Num_Of_Dice = int.Parse(parse_die[1].Value);
                _Type_Of_Die = int.Parse(parse_die[2].Value);
                _Bonus = int.Parse(parse_die[3].Value);
            }
            else
            {
                GroupCollection parse_die = Regex.Match(pathfinder_dice, @"(\d*)d(\d*)").Groups;
                _Num_Of_Dice = int.Parse(parse_die[1].Value);
                _Type_Of_Die = int.Parse(parse_die[2].Value);
                _Bonus = 0;
            }

            if ( (_Num_Of_Dice==0) || (_Type_Of_Die == 0))
            {
                throw new System.Exception("Invalid (\"" + pathfinder_dice + "\") pathfinder dice input to Die_Roll");
            }

            _Reason = reason;
            _Character_Name = char_name;
            /*if ((roll < 1) || (roll > ( (_Type_Of_Die*_Num_Of_Dice) + _Bonus) )) { throw new System.Exception(String.Format("Invalid result {0} on d{1}", roll, _Type_Of_Die)); }
            else {*/ _Roll = roll; /*}*/
        }
    }
}
