using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public abstract class CombatEventTargeted: CombatEvent
    {
        public abstract string Source_Target_Character_Name { get; }
        public abstract string Target_Character_Name { get; set; }

        private string _Target_Friendly_Name = "";
        public string Target_Friendly_Name
        {
            get
            {
                if (_Target_Friendly_Name == "") { return CleanupName(Target_Character_Name); }
                else { return _Target_Friendly_Name; }
            }
            set => _Target_Friendly_Name = value;
        }

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
            get
            {
                if (_Target_Character_Type == Char_Enum.Really_Unknown)
                {
                    if (_Guess_Target_Character_Type != Char_Enum.Really_Unknown) { return _Guess_Target_Character_Type; }
                    else { return Guess_Character_Type_From_String(Target_Character_Name); }
                }
                else { return _Target_Character_Type; }
            }
            set
            {
                if (_Target_Character_Type != value)
                {
                    _Target_Character_Type = value;
                    OnCombatEventChanged_Invoke();
                }
            }
        }

        public CombatEventTargeted(int inID, int inCombatID, string inLine) : base(inID, inCombatID, inLine) { }

        public Char_Enum Character_Type_From_Target()
        {
            if (_Character_Type == Char_Enum.Really_Unknown)
            {
                if (_Target_Character_Type == Char_Enum.Really_Unknown)
                { // Neither actor nor target factions are known -- so we guess.
                    if (this is AttackEvent)
                    {
                        if (Obviously_Friendly(Source_Target_Character_Name)) { return Char_Enum.Hostile; } // X is attacking something that appears friendly, so X is hostile
                        else if (Obviously_Friendly(Source_Character_Name)) { return Char_Enum.Friendly; } // X is friendly, and is attacking something unknown, so that thing is hostile (but we don't really care)
                        else if (Likely_Hostile(Source_Target_Character_Name)) { return Char_Enum.Friendly; } // X is attacking something that appears hostile, so X is friendly
                        else if (Likely_Hostile(Source_Character_Name)) { return Char_Enum.Hostile; } // X is hostile, and is attacking something unknown, so X is hostile (and the target is likely friendly, but we aren't very certain at all)
                        else if (Guess_Character_Type_From_String(Source_Target_Character_Name) == Char_Enum.Friendly) { return Char_Enum.Hostile; }
                        else if (Guess_Character_Type_From_String(Source_Target_Character_Name) == Char_Enum.Hostile) { return Char_Enum.Friendly; }
                        else { return Char_Enum.Really_Unknown; }
                    }
                    else if (this is HealingEvent)
                    {
                        if (Obviously_Friendly(Source_Target_Character_Name)) { return Char_Enum.Friendly; }
                        else if (Obviously_Friendly(Source_Character_Name)) { return Char_Enum.Friendly; }
                        else if (Guess_Character_Type_From_String(Source_Target_Character_Name) == Char_Enum.Friendly) { return Char_Enum.Friendly; }
                        else if (Guess_Character_Type_From_String(Source_Target_Character_Name) == Char_Enum.Hostile) { return Char_Enum.Hostile; }
                        else { return Char_Enum.Really_Unknown; }
                    }
                    else if (this is DamageEvent)
                    {
                        if (Obviously_Friendly(Source_Target_Character_Name)) { return Char_Enum.Hostile; } // X is damaging something that appears friendly, so X is hostile
                        else if (Obviously_Friendly(Source_Character_Name)) { return Char_Enum.Friendly; } // X is friendly, and is damaging something unknown, so that thing is hostile (but we don't really care)
                        else if (Likely_Hostile(Source_Target_Character_Name)) { return Char_Enum.Friendly; } // X is damging something that appears hostile, so X is friendly
                        else if (Likely_Hostile(Source_Character_Name)) { return Char_Enum.Hostile; } // X is hostile, and is damaging something unknown, so X is hostile (and the target is likely friendly, but we aren't very certain at all)
                        else if (Guess_Character_Type_From_String(Source_Target_Character_Name) == Char_Enum.Friendly) { return Char_Enum.Hostile; }
                        else if (Guess_Character_Type_From_String(Source_Target_Character_Name) == Char_Enum.Hostile) { return Char_Enum.Friendly; }
                        else { return Char_Enum.Really_Unknown; }
                    }
                    else { return Char_Enum.Really_Unknown; }
                }
                else
                { // We have a faction for the target, but not for the actor.  Use the known target faction to deduce the actor's faction.
                    if (this is AttackEvent)
                    {
                        if (_Target_Character_Type == Char_Enum.Friendly) { return Char_Enum.Hostile; }
                        else if (_Target_Character_Type == Char_Enum.Hostile) { return Char_Enum.Friendly; }
                        else { return Char_Enum.Really_Unknown; }
                    }
                    else if (this is HealingEvent)
                    {
                        if (_Target_Character_Type == Char_Enum.Friendly) { return Char_Enum.Friendly; }
                        else if (_Target_Character_Type == Char_Enum.Hostile) { return Char_Enum.Hostile; }
                        else { return Char_Enum.Really_Unknown; }
                    }
                    else if (this is DamageEvent)
                    {
                        if (_Target_Character_Type == Char_Enum.Friendly) { return Char_Enum.Hostile; }
                        else if (_Target_Character_Type == Char_Enum.Hostile) { return Char_Enum.Friendly; }
                        else { return Char_Enum.Really_Unknown; }
                    }
                    else { return Char_Enum.Really_Unknown; }
                }
            }
            else
            {
                if (_Target_Character_Type == Char_Enum.Really_Unknown)
                { // We have a facton for the actor, but not for the target.  Set the target's origin to agree.
                    if (_Character_Type == Char_Enum.Friendly)
                    {
                        if (this is AttackEvent) { _Target_Character_Type = Char_Enum.Hostile; return Char_Enum.Friendly; }
                        else if (this is HealingEvent) { _Target_Character_Type = Char_Enum.Friendly; return Char_Enum.Friendly; }
                        else if (this is DamageEvent) { return Char_Enum.Friendly; }
                        else { return Char_Enum.Really_Unknown; }
                    }
                    else if (_Character_Type == Char_Enum.Hostile)
                    {
                        if (this is AttackEvent) { _Target_Character_Type = Char_Enum.Friendly; return Char_Enum.Hostile; }
                        else if (this is HealingEvent) { _Target_Character_Type = Char_Enum.Hostile; return Char_Enum.Hostile; }
                        if (this is DamageEvent) { return Char_Enum.Hostile; }
                        else { return Char_Enum.Really_Unknown; }
                    }
                    else { return Char_Enum.Really_Unknown; }
                }
                else
                { // We have a faction already assigned for both the actor and the target -- see if they agree, and (if not) output a warning.
                    if (this is AttackEvent)
                    {
                        if ((_Character_Type == Char_Enum.Friendly) && (_Target_Character_Type == Char_Enum.Hostile)) { return Char_Enum.Friendly; }
                        else if ((_Character_Type == Char_Enum.Hostile) && (_Target_Character_Type == Char_Enum.Friendly)) { return Char_Enum.Hostile; }
                        else if ((_Character_Type == Char_Enum.Friendly) && (_Target_Character_Type == Char_Enum.Friendly))
                        {
                            System.Diagnostics.Debug.WriteLine("{0} (Friendly) is attacking {1} (Friendly)", Source_Character_Name, Target_Character_Name);
                            return _Character_Type;
                        }
                        else if ((_Character_Type == Char_Enum.Hostile) && (_Target_Character_Type == Char_Enum.Hostile))
                        {
                            System.Diagnostics.Debug.WriteLine("{0} (Hostile) is attacking {1} (Hostile)", Source_Character_Name, Target_Character_Name);
                            return _Character_Type;
                        }
                        else if ((_Character_Type == Char_Enum.Summon) && (_Target_Character_Type == Char_Enum.Hostile)) { return Char_Enum.Friendly; }
                        else if ((_Character_Type == Char_Enum.Summon) && (_Target_Character_Type == Char_Enum.Friendly)) { return Char_Enum.Hostile; }
                        else if ((_Character_Type == Char_Enum.Friendly) && (_Target_Character_Type == Char_Enum.Summon)) { _Target_Character_Type = Char_Enum.Hostile; return Char_Enum.Friendly; }
                        else if ((_Character_Type == Char_Enum.Hostile) && (_Target_Character_Type == Char_Enum.Summon)) { _Target_Character_Type = Char_Enum.Friendly; return Char_Enum.Hostile; }
                        else if ((_Character_Type == Char_Enum.Summon) && (_Target_Character_Type == Char_Enum.Summon)) { return Char_Enum.Really_Unknown; }
                        else { throw new System.Exception(string.Format("Impossible combination of _Character_Type = '{0}' and _Target_Character_Type = '{1}'", _Character_Type, _Target_Character_Type)); }
                    }
                    else if (this is HealingEvent)
                    {
                        if ((_Character_Type == Char_Enum.Friendly) && (_Target_Character_Type == Char_Enum.Friendly)) { return Char_Enum.Friendly; }
                        else if ((_Character_Type == Char_Enum.Hostile) && (_Target_Character_Type == Char_Enum.Hostile)) { return Char_Enum.Hostile; }
                        else if ((_Character_Type == Char_Enum.Friendly) && (_Target_Character_Type == Char_Enum.Hostile))
                        {
                            System.Diagnostics.Debug.WriteLine("{0} (Friendly) is healing {1} (Hostile)", Source_Character_Name, Target_Character_Name);
                            return _Character_Type;
                        }
                        else if ((_Character_Type == Char_Enum.Hostile) && (_Target_Character_Type == Char_Enum.Friendly))
                        {
                            System.Diagnostics.Debug.WriteLine("{0} (Hostile) is healing {1} (Friendly)", Source_Character_Name, Target_Character_Name);
                            return _Character_Type;
                        }
                        else if ((_Character_Type == Char_Enum.Summon) && (_Target_Character_Type == Char_Enum.Hostile)) { return Char_Enum.Hostile; }
                        else if ((_Character_Type == Char_Enum.Summon) && (_Target_Character_Type == Char_Enum.Friendly)) { return Char_Enum.Friendly; }
                        else if ((_Character_Type == Char_Enum.Friendly) && (_Target_Character_Type == Char_Enum.Summon)) { _Target_Character_Type = Char_Enum.Friendly; return Char_Enum.Friendly; }
                        else if ((_Character_Type == Char_Enum.Hostile) && (_Target_Character_Type == Char_Enum.Summon)) { _Target_Character_Type = Char_Enum.Hostile; return Char_Enum.Hostile; }
                        else if ((_Character_Type == Char_Enum.Summon) && (_Target_Character_Type == Char_Enum.Summon)) { return Char_Enum.Really_Unknown; }
                        else { throw new System.Exception(string.Format("Impossible combination of _Character_Type = '{0}' and _Target_Character_Type = '{1}'", _Character_Type, _Target_Character_Type)); }
                    }
                    else if (this is DamageEvent)
                    {
                        if ((_Character_Type == Char_Enum.Friendly) && (_Target_Character_Type == Char_Enum.Hostile)) { return Char_Enum.Friendly; }
                        else if ((_Character_Type == Char_Enum.Hostile) && (_Target_Character_Type == Char_Enum.Friendly)) { return Char_Enum.Hostile; }
                        else if ((_Character_Type == Char_Enum.Friendly) && (_Target_Character_Type == Char_Enum.Friendly))
                        {
                            System.Diagnostics.Debug.WriteLine("{0} (Friendly) is damaging {1} (Friendly)", Source_Character_Name, Target_Character_Name);
                            return Char_Enum.Really_Unknown;
                        }
                        else if ((_Character_Type == Char_Enum.Hostile) && (_Target_Character_Type == Char_Enum.Hostile))
                        {
                            System.Diagnostics.Debug.WriteLine("{0} (Hostile) is damaging {1} (Hostile)", Source_Character_Name, Target_Character_Name);
                            return Char_Enum.Really_Unknown;
                        }
                        else if ((_Character_Type == Char_Enum.Summon) && (_Target_Character_Type == Char_Enum.Hostile)) { return Char_Enum.Friendly; }
                        else if ((_Character_Type == Char_Enum.Summon) && (_Target_Character_Type == Char_Enum.Friendly)) { return Char_Enum.Hostile; }
                        else if ((_Character_Type == Char_Enum.Friendly) && (_Target_Character_Type == Char_Enum.Summon)) { return Char_Enum.Friendly; }
                        else if ((_Character_Type == Char_Enum.Hostile) && (_Target_Character_Type == Char_Enum.Summon)) { return Char_Enum.Hostile; }
                        else if ((_Character_Type == Char_Enum.Summon) && (_Target_Character_Type == Char_Enum.Summon)) { return Char_Enum.Really_Unknown; }
                        else { throw new System.Exception(string.Format("Impossible combination of _Character_Type = '{0}' and _Target_Character_Type = '{1}'", _Character_Type, _Target_Character_Type)); }
                    }
                    else { return _Character_Type; }
                }
            }
        }
    }
}
