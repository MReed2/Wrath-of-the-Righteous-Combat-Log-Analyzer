using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public class Bonus
    {
        private string _Bonus_Major_Type = "";
        private string _Bonus_Minor_Type = "";
        private int _Bonus_Amount = 0;

        public string Bonus_Major_Type
        {
            get { return _Bonus_Major_Type; }
            set { _Bonus_Major_Type = value; }
        }

        public string Bonus_Minor_Type
        {
            get { return _Bonus_Minor_Type; }
            set { _Bonus_Minor_Type = value; }
        }

        public int Bonus_Amount
        {
            get { return _Bonus_Amount; }
            set { _Bonus_Amount = value; }
        }

        public Bonus()
        {
        }

        public Bonus(string line)
        {
            Parse(line);
        }

        public List<Die_Roll> Parse(string line)
        {
            if (!line.Contains("margin-left: 100px"))
            {
                throw new End_Of_Bonus("\""+line+"\" is not a valid bonus");
            }

            line = line.Replace("–", "-"); // Yes, these are different characters.  "–" is \x2013, while "-" is \x2D

            //<div style="margin-left: 100px">		Armor: <span style="color:#004604">+8 [Breastplate]</span></div>

            Match minor_type = Regex.Match(line, @">\t\t(.*?):.*?([+-]\d) \[(.*?)\]");

            //<div style="margin-left: 100px">		Size: <span style="color:#971818">-2</span></div>

            Match no_minor_type = Regex.Match(line, @">\t\t(.*?):.*?([+-]\d)");

            //<div style="margin-left: 100px">		Base value: 10</div>
            //<div style="margin-left: 100px">	Base value: 10</div>

            Match simple_value = Regex.Match(line, @">\t?\t?(.*?): (\d*)");

            if (minor_type.Success)
            {
                _Bonus_Major_Type = minor_type.Groups[1].Value;
                _Bonus_Amount = int.Parse(minor_type.Groups[2].Value);
                _Bonus_Minor_Type = minor_type.Groups[3].Value;
            }
            else if (no_minor_type.Success)
            {
                _Bonus_Major_Type = no_minor_type.Groups[1].Value;
                _Bonus_Amount = int.Parse(no_minor_type.Groups[2].Value);
            }
            else if (simple_value.Success)
            {
                _Bonus_Major_Type = simple_value.Groups[1].Value;
                _Bonus_Amount = int.Parse(simple_value.Groups[2].Value);
            }
            else
            {
                throw new System.Exception("Unable to parse \"" + line + "\" as a bonus");
            }

            if ((_Bonus_Major_Type == "")||(_Bonus_Amount == 0))
            {
                throw new System.Exception("Failed to properly parse \"" + line + "\" as a bonus");
            }

            return null;
        }

    }
}
