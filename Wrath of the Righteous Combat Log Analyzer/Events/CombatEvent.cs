using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public delegate void CombatEventChanged(CombatEvent source);

    public abstract class CombatEvent
    {
        public enum Char_Enum { Friendly, Hostile, Summon, Unknown, Really_Unknown }

        public event CombatEventChanged OnCombatEventChanged;

        private string _Source = "";
        private int _ID = -1;
        private Char_Enum _Character_Type = Char_Enum.Really_Unknown;
        private Char_Enum _Smarter_Guess_Character_Type = Char_Enum.Really_Unknown;
        private string _Friendly_Name = "";
        private CombatEventList _Children = new CombatEventList();
        private int _Cached_Source_Hashcode = 0;
        private int _Cached_Source_String_Length = 0;

        public int Cached_Source_Hashcode
        {
            get
            {
                if ((_Cached_Source_String_Length == _Source.Length) && (_Cached_Source_Hashcode != 0)) { return _Cached_Source_Hashcode; }
                else
                {
                    _Cached_Source_Hashcode = _Source.GetHashCode();
                    _Cached_Source_String_Length = _Source.Length;
                    return _Cached_Source_Hashcode;
                }
            }
        }
      
        public Char_Enum Character_Type
        {
            get
            {
                if (_Character_Type == Char_Enum.Really_Unknown)
                {
                    if (_Smarter_Guess_Character_Type == Char_Enum.Really_Unknown) { return Guess_Character_Type_From_String(Character_Name); }
                    else { return _Smarter_Guess_Character_Type; }
                }
                else { return _Character_Type; }
            }
            set
            {
                if (value != Character_Type)
                {
                    _Character_Type = value;
                    OnCombatEventChanged?.Invoke(this);
                }
            }
        }

        public Char_Enum Smarter_Guess_Character_Type
        {
            get => _Smarter_Guess_Character_Type;
            set
            {
                if (value != _Smarter_Guess_Character_Type)
                {
                    _Smarter_Guess_Character_Type = value;
                    OnCombatEventChanged?.Invoke(this);
                }
            }
        }

        private Char_Enum Guess_Character_Type_From_String(string inStr)
        {
            if (inStr.Contains("Companion") || (inStr.Contains("Player_Unit")) ) { return Char_Enum.Friendly; }
            else if (inStr.Contains("Summoner")) { return Char_Enum.Hostile; }
            else if (inStr.Contains("Summon")) { return Char_Enum.Summon; }
            else { return Char_Enum.Hostile; }
        }

        private bool Obviously_Hostile(string inStr)
        {
            return (Regex.Match(inStr, @".*?CR(\d*?)_.*").Success); // Looking for "...CR###_..."  -- not all hostiles use this format, but most do.
        }

        public Char_Enum Character_Type_From_Target()
        {
            if (this is AttackEvent)
            {
                if (((AttackEvent)this).Guess_Target_Character_Type == Char_Enum.Really_Unknown)
                {
                    if (Obviously_Hostile(Source_Character_Name)) { return Char_Enum.Hostile; } // If the actor is hostile, return that.
                    else if (Obviously_Hostile(((AttackEvent)this).Source_Target_Character_Name)) { return Char_Enum.Friendly; } // If the target is hostile, then the source is almost certainly friendly
                    else if (Guess_Character_Type_From_String(((AttackEvent)this).Source_Target_Character_Name) == Char_Enum.Friendly) { return Char_Enum.Hostile; }
                    else if (Guess_Character_Type_From_String(((AttackEvent)this).Source_Target_Character_Name) == Char_Enum.Hostile) { return Char_Enum.Friendly; }
                    else { return Char_Enum.Really_Unknown; }
                }
                else
                {
                    if (((AttackEvent)this).Guess_Target_Character_Type == Char_Enum.Friendly) { return Char_Enum.Hostile; }
                    else if (((AttackEvent)this).Guess_Target_Character_Type == Char_Enum.Hostile) { return Char_Enum.Friendly; }
                    else { return Char_Enum.Really_Unknown; }
                }
            }
            else
            {
                return Char_Enum.Really_Unknown;
            }
        }

        public string Friendly_Name
        {
            get
            {
                if (_Friendly_Name != "") { return _Friendly_Name; }
                else { return CleanupName(Character_Name); }
            }
            set
            {
                if (_Friendly_Name != value)
                {
                    _Friendly_Name = value;
                    OnCombatEventChanged?.Invoke(this);
                }
            }
        }

        public string Source_With_ID { get => Regex.Replace(_Source, @"(^.*?\x22>)", "$1ID: " + _ID.ToString() + " ", RegexOptions.None); }

        public string Source { get => _Source; set => _Source = value; }

        public int ID { get => _ID; }

        public abstract string Source_Character_Name { get; }

        public abstract string Character_Name { get; set; }
        
        public abstract List<Die_Roll> Die_Rolls { get; }

        public CombatEventList Children { get => _Children; }

        public CombatEvent(int inID, string line) { _ID = inID;  Parse(line); }
        
        public abstract List<Die_Roll> Parse(string line);

        public abstract System.Windows.Controls.UserControl Get_UserControl_For_Display();

        public abstract System.Windows.Controls.UserControl Update_Display_UserControl();

        public override int GetHashCode() { return Source.GetHashCode(); }

        protected string CleanupName(string name)
        {
            string rtn = Regex.Replace(name, @"(\[.*?\])", ""); // remove GUID
            if (rtn.Contains("StartGame_Player_Unit")) { rtn = "CHARNAME"; }
            if (rtn.Contains("AnimalCompanionUnit")) { rtn = rtn.Replace("AnimalCompanionUnit", "");}
            if (rtn.Contains("_PreorderBonus")) { rtn = rtn.Replace("_PreorderBonus", ""); }
            if (rtn.Contains("Companion")) { rtn = rtn.Replace("Companion", ""); }
            rtn = rtn.Replace("_", " ");
            rtn = Regex.Replace(rtn, @"(?:Level(\d*))", " (Level $1)");
            rtn = rtn.Trim();

            return rtn;
        }
    }
}
