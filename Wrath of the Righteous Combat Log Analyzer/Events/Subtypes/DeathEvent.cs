using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    class DeathEvent: SimpleEvent
    {
        private DamageEvent _Death_Source_DamageEvent = null;
        private SimpleEvent _Experience_Event = null;
        private int _Exp_Earned = 0;

        public DamageEvent Death_Source { get => _Death_Source_DamageEvent; }
        public SimpleEvent Exp_Source { get => _Experience_Event; }
        public int Exp_Earned { get => _Exp_Earned; }

        public override CombatEvent Next_CombatEvent
        {
            get => base.Next_CombatEvent;
            set
            {
                base.Next_CombatEvent = value;
            }
        }

        public override CombatEvent Prev_CombatEvent
        {
            get => base.Prev_CombatEvent;
            set
            {
                base.Prev_CombatEvent = value;
                Update_Death();
            }
        }

        public DeathEvent(int inID, int inCombatID, string inLine) : base(inID, inCombatID, inLine) { }

        private void Update_Death()
        {
            if (Source_Character_Name == "CR16_IncubusToughMelee[d397d1d3]")
            {
                int kdkd = 1;
            }
            if (Prev_CombatEvent != null)
            {
                if ((Prev_CombatEvent is SimpleEvent) && (((SimpleEvent)Prev_CombatEvent).Subtype == "Experience"))
                {
                    _Experience_Event = (SimpleEvent)Prev_CombatEvent;
                    if ((Prev_CombatEvent.Prev_CombatEvent != null) && (Prev_CombatEvent.Prev_CombatEvent is DamageEvent)) { _Death_Source_DamageEvent = (DamageEvent)Prev_CombatEvent.Prev_CombatEvent; }
                    else { System.Diagnostics.Debug.WriteLine("Can't find damage event for death of \"" + Source_Character_Name + "\""); }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No experience found for death of \"" + Source_Character_Name + "\"");
                    if ((Prev_CombatEvent.Prev_CombatEvent != null) && (Prev_CombatEvent is DamageEvent)) { _Death_Source_DamageEvent = (DamageEvent)Prev_CombatEvent; }
                    else { System.Diagnostics.Debug.WriteLine("Can't find damage event for death of \"" + Source_Character_Name + "\""); }
                }
                if ((_Death_Source_DamageEvent != null) && (_Death_Source_DamageEvent.Source_Target_Character_Name != Source_Character_Name))
                {
                    System.Diagnostics.Debug.WriteLine("Can't find damage event for death of \"" + Source_Character_Name + "\", previous damage event has a target of \"" + _Death_Source_DamageEvent.Source_Target_Character_Name + "\"");
                    _Death_Source_DamageEvent = null;
                }
                if (_Experience_Event != null)
                {
                    //<div style="margin-left:   0px">Gained experience: <b>864</b>.</div>
                    _Exp_Earned = int.Parse(System.Text.RegularExpressions.Regex.Match(_Experience_Event.Source, @"<b>(\d*)<").Groups[1].Value);
                }
            }
            else
            {
                if ((_Experience_Event != null)||(_Death_Source_DamageEvent != null))
                {
                    System.Diagnostics.Debug.WriteLine("Prev_Event was set to null after being non-null value");
                    _Experience_Event = null;
                    _Death_Source_DamageEvent = null;
                }
            }
        }
    }
}
