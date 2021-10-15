using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public abstract class CombatEventTargeted: CombatEvent
    {
        public abstract string Target_Character_Name { get; set; }
        public abstract string Source_Target_Character_Name { get; }

        private Char_Enum _Guess_Target_Character_Type = Char_Enum.Really_Unknown;
        private Char_Enum _Target_Character_Type = Char_Enum.Really_Unknown;

        public Char_Enum Guess_Target_Character_Type
        {
            get => _Guess_Target_Character_Type;
            set
            {
                if (_Guess_Target_Character_Type != value)
                {
                    _Guess_Target_Character_Type = value;
                    OnCombatEventChanged_Invoke();
                }
            }
        }
        public Char_Enum Target_Character_Type
        {
            get => _Target_Character_Type;
            set
            {
                if (_Target_Character_Type != value)
                {
                    _Target_Character_Type = value;
                    OnCombatEventChanged_Invoke();
                }
            }
        }

        public CombatEventTargeted(int inID, string inLine) : base(inID, inLine) { }

        public Char_Enum Character_Type_From_Target()
        {
            if (Guess_Target_Character_Type == Char_Enum.Really_Unknown)
            {
                if (Obviously_Hostile(Source_Character_Name))
                {
                    if (this is AttackEvent)
                    {
                        //System.Diagnostics.Debug.WriteLine("\t\t{0} (which is obviously hostile) attacks {1}, hostile vote", Source_Character_Name, Source_Target_Character_Name);
                        return Char_Enum.Hostile;
                    }
                    else if (this is HealingEvent)
                    {
                        //System.Diagnostics.Debug.WriteLine("\t\t{0} (which is obviously hostile) heals {1}, hostile vote", Source_Character_Name, Source_Target_Character_Name);
                        return Char_Enum.Hostile;
                    }
                    else { return Char_Enum.Hostile; }
                } // If the actor is hostile, return that.
                else if (this is AttackEvent)
                {
                    if (Obviously_Hostile(Source_Target_Character_Name))
                    {
                        //System.Diagnostics.Debug.WriteLine("\t\t{0} attacks {1} (which is obviously hostile), friendly vote", Source_Character_Name, Source_Target_Character_Name);
                        return Char_Enum.Friendly;
                    }
                    else if (Guess_Character_Type_From_String(Source_Target_Character_Name) == Char_Enum.Friendly)
                    {
                        //System.Diagnostics.Debug.WriteLine("\t\t{0} attacks {1} (which appears to be friendly), hostile vote", Source_Character_Name, Source_Target_Character_Name);
                        return Char_Enum.Hostile;
                    }
                    else if (Guess_Character_Type_From_String(Source_Target_Character_Name) == Char_Enum.Hostile)
                    {
                        //System.Diagnostics.Debug.WriteLine("\t\t{0} attacks {1} (which appears to be hostile), friendly vote", Source_Character_Name, Source_Target_Character_Name);
                        return Char_Enum.Friendly;
                    }
                    else { return Char_Enum.Really_Unknown; }
                }
                else if (this is HealingEvent)
                {
                    if (Obviously_Hostile(Source_Target_Character_Name))
                    {
                        //System.Diagnostics.Debug.WriteLine("\t\t{0} heals {1} (which is obviously hostile), hostile vote", Source_Character_Name, Source_Target_Character_Name);
                        return Char_Enum.Hostile;
                    }
                    else if (Guess_Character_Type_From_String(Source_Character_Name) == Char_Enum.Friendly)
                    {
                        //System.Diagnostics.Debug.WriteLine("\t\t{0} heals {1} (which appears to be friendly), friendly vote", Source_Character_Name, Source_Target_Character_Name);
                        return Char_Enum.Friendly;
                    }
                    else if (Guess_Character_Type_From_String(Source_Character_Name) == Char_Enum.Hostile)
                    {
                        //System.Diagnostics.Debug.WriteLine("\t\t{0} heals {1} (which appears to be hostile), hostile vote", Source_Character_Name, Source_Target_Character_Name);
                        return Char_Enum.Hostile;
                    }
                    else { return Char_Enum.Really_Unknown; }
                }
                else if (this is DamageEvent) { return Char_Enum.Really_Unknown; }
                else { return Char_Enum.Really_Unknown; }
            }
            else
            {
                if (Guess_Target_Character_Type == Char_Enum.Friendly)
                {
                    if (this is AttackEvent)
                    {
                        //System.Diagnostics.Debug.WriteLine("\t\t{0} attacks {1} (which has been locked as friendly), hostile vote", Source_Character_Name, Source_Target_Character_Name);
                        return Char_Enum.Hostile;
                    }
                    else if (this is HealingEvent)
                    {
                        //System.Diagnostics.Debug.WriteLine("\t\t{0} heals {1} (which has been locked as friendly), friendly vote", Source_Character_Name, Source_Target_Character_Name);
                        return Char_Enum.Friendly;
                    }
                    else
                    {
                        return Char_Enum.Unknown;
                    }
                }
                else if (Guess_Target_Character_Type == Char_Enum.Hostile)
                {
                    if (this is AttackEvent)
                    {
                        //System.Diagnostics.Debug.WriteLine("\t\t{0} attacks {1} (which has been locked as hostile), friendly vote", Source_Character_Name, Source_Target_Character_Name);
                        return Char_Enum.Friendly;
                    }
                    else if (this is HealingEvent)
                    {
                        //System.Diagnostics.Debug.WriteLine("\t\t{0} heals {1} (which has been locked as hostile), hostile vote", Source_Character_Name, Source_Target_Character_Name);
                        return Char_Enum.Hostile;
                    }
                    else { return Char_Enum.Really_Unknown; }
                }
                else { return Char_Enum.Really_Unknown; }
            }
        }
    }
}
