using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rationals;
using System.Numerics;
using System.Threading.Tasks;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public class Stats_Instance
    {
        private string _Type_Description = "";

        private int _Attack_Cnt = 0;
        private int _Hit_Cnt = 0;
        private int _Critical_Threatened_Cnt = 0;
        private int _Critical_Confirmed_Cnt = 0;
        private int _Damage_Done_Total = 0;
        private int _Number_Killed = 0;
        private int _Exp_Earned = 0;
        private List<string> _Kills = new List<string>();

        private int _Critical_Threatned_20_Confirmed_20_Cnt = 0;
        private int _Not_Critical_Threatned_20_Confirmed_20_Cnt = 0;

        private int[] _D20s = new int[21];

        private int[] _D20s_Streaks = new int[21];

        private int[,] _D20s_Streak_Length = new int[21, 11]; // first number is the value rolled, the second value is the length of the streak.

        private int[] _Attack_Margin = new int[41]; // Margin ranges from -20...+20, which is 41 values.

        private int[] _Crit_Confirmation_Margin = new int[41]; // Margin ranges from -20...+20, which is 41 values.

        private List<string> _Characters = new List<string>();

        private string[,] _Character_Names_Analysis = null;
        private string[,] _Frequency_Analysis = new string[23, 3];
        private string[,] _Streak_Analysis = new string[21, 2];
        private string[,] _Streak_Length_Analysis = new string[21, 11];
        private string[,] _Attack_Margin_Analysis = new string[43, 2];
        private string[,] _Crit_Confirmation_Margin_Analysis = new string[43, 2];
        private string[,] _Misc_Analysis = new string[11, 2]; // Simple "x = y" type stuff goes here.  There are extra rows, which won't be displayed in the wingrid

        private StringBuilder _Rolls_CSV = new StringBuilder();

        private int _D20s_Expected_Value = 0;
        private double _D20s_P_Value = 1.0;

        private bool _Tables_Stale = true;

        public string Type_Description { get => _Type_Description; set => _Type_Description = value; }

        public int Attack_Cnt { get => _Attack_Cnt; }
        public int Hit_Cnt { get => _Hit_Cnt; }
        public int Critical_Threatened_Cnt { get => _Critical_Threatened_Cnt; }
        public int Critical_Confirmed_Cnt { get => _Critical_Confirmed_Cnt; }
        public int Damage_Done_Total { get => _Damage_Done_Total;  }
        public int Number_Killed { get => _Number_Killed; }
        public int Critical_Threatned_20_Confirmed_20_Cnt { get => _Critical_Threatned_20_Confirmed_20_Cnt; set => _Critical_Threatned_20_Confirmed_20_Cnt = value; }
        public int Not_Critical_Threatned_20_Confirmed_20_Cnt { get => _Not_Critical_Threatned_20_Confirmed_20_Cnt; set => _Not_Critical_Threatned_20_Confirmed_20_Cnt = value; }

        public int[] D20s { get => _D20s; }
        public int[] D20s_Streaks { get => _D20s_Streaks; }
        public int[,] D20s_Streak_Length { get => _D20s_Streak_Length;  }
        public int[] Attack_Margin { get => _Attack_Margin; }
        public int[] Crit_Confirmation_Margin { get => _Crit_Confirmation_Margin; }

        public List<string> Characters { get => _Characters; }

        public string[,] Character_Names_Analysis { get => _Character_Names_Analysis; }
        public string[,] Frequency_Analysis { get => _Frequency_Analysis; }
        public string[,] Streak_Analysis { get => _Streak_Analysis; }
        public string[,] Streak_Length_Analysis { get => _Streak_Length_Analysis; }
        public string[,] Attack_Margin_Analysis { get => _Attack_Margin_Analysis; }
        public string[,] Crit_Confirmation_Margin_Analysis { get => _Crit_Confirmation_Margin_Analysis; }
        public string[,] Misc_Analysis { get => _Misc_Analysis; }

        public StringBuilder Rolls_CSV { get => _Rolls_CSV; set => _Rolls_CSV = value; }

        public int D20s_Expected_Value { get => _D20s_Expected_Value; }
        public double D20s_P_Value { get => _D20s_P_Value; }
        public bool Tables_Stale { get => _Tables_Stale; }

        private int _Current_Streak = 0;
        private int _Last_Roll = 0;

        public Stats_Instance() { Clear(); }

        public Stats_Instance(string in_type_desc) { _Type_Description = in_type_desc; Clear(); }

        public void Clear()
        {
            _Attack_Cnt = 0;
            _Hit_Cnt = 0;
            _Critical_Threatened_Cnt = 0;
            _Critical_Confirmed_Cnt = 0;
            _Damage_Done_Total = 0;
            _Number_Killed = 0;
            _Exp_Earned = 0;
            _Kills = new List<string>();

            _Critical_Threatned_20_Confirmed_20_Cnt = 0;
            _Not_Critical_Threatned_20_Confirmed_20_Cnt = 0;

            Clear_int_Array(_D20s);
            Clear_int_Array(_D20s_Streaks);
            Clear_int_Array(_D20s_Streak_Length); // first number is the value rolled, the second value is the length of the streak.
            Clear_int_Array(_Attack_Margin); // Margin ranges from -20...+20, which is 41 values.
            Clear_int_Array(_Crit_Confirmation_Margin); // Margin ranges from -20...+20, which is 41 values.

            _Current_Streak = 0;
            _Last_Roll = 0;

            _Characters.Clear();

            _Rolls_CSV.Clear();

            _D20s_Expected_Value = 0;

            _D20s_P_Value = 1.0;

            Clear_Analysis_Only();

            _Tables_Stale = true;
        }

        public void Clear_Analysis_Only()
        {
            _Character_Names_Analysis = null;
            Clear_Analysis_Array(_Frequency_Analysis);
            Clear_Analysis_Array(_Streak_Analysis);
            Clear_Analysis_Array(_Streak_Length_Analysis);
            Clear_Analysis_Array(_Attack_Margin_Analysis);
            Clear_Analysis_Array(_Crit_Confirmation_Margin_Analysis);
            Clear_Analysis_Array(_Misc_Analysis);
        }

        public void Clear_int_Array(int[] inArray)
        {
            for (int x=0; x<=inArray.GetUpperBound(0); x++)
            {
                inArray[x] = 0;
            }
        }

        public void Clear_int_Array(int[,] inArray)
        {
            for (int x = 0; x <= inArray.GetUpperBound(0); x++)
            {
                for (int y = 0; y <= inArray.GetUpperBound(1); y++)
                {
                    inArray[x,y] = 0;
                }
            }
        }

        private void Clear_Analysis_Array(string[,] inArray)
        {
            for (int x=0; x<=inArray.GetUpperBound(0); x++)
            {
                for (int y=0; y<=inArray.GetUpperBound(1); y++)
                {
                    inArray[x, y] = "";
                }
            }
        }

        public void Process_Event(CombatEvent inEvent)
        {
            _Tables_Stale = true;

            if (inEvent is SimpleEvent) { }
            else
            {
                if (_Characters.Contains(inEvent.Character_Name)) { }
                else { _Characters.Add(inEvent.Character_Name); }
            }

            if (inEvent is AttackEvent)
            {
                // Margin analysis

                AttackEvent atk = (AttackEvent)inEvent;
                foreach (int atk_to_hit_margin in atk.To_Hit_Margin) { _Attack_Margin[atk_to_hit_margin + 20]++; }
                foreach (int crit_conf_margin in atk.Critical_Confirmation_Margin) { _Crit_Confirmation_Margin[crit_conf_margin + 20]++; }

                // Track misc stuff

                _Attack_Cnt++;
                if (atk.Attack_Success) { _Hit_Cnt++; }
                if (atk.Critical_Confirmation_Rolls.Count > 0)
                {
                    _Critical_Threatened_Cnt++;
                    if (atk.Attack_Critical)
                    {
                        _Critical_Confirmed_Cnt++;

                        bool atk_roll_is_20 = false;
                        foreach (Die_Roll curr_roll in atk.Attack_Die_Rolls) { if (curr_roll.Roll == 20) { atk_roll_is_20 = true; break; } }

                        bool conf_roll_is_20 = false;
                        foreach (Die_Roll curr_roll in atk.Critical_Confirmation_Rolls) { if (curr_roll.Roll == 20) { conf_roll_is_20 = true; break; } }

                        if (atk_roll_is_20)
                        {
                            if (conf_roll_is_20)
                            {
                                _Critical_Threatned_20_Confirmed_20_Cnt++;
                            }
                            else
                            {
                                _Not_Critical_Threatned_20_Confirmed_20_Cnt++;
                            }
                        }
                    }
                }
            }

            if (inEvent is DamageEvent)
            {
                _Damage_Done_Total += ((DamageEvent)inEvent).Damage;
            }

            foreach (Die_Roll curr_roll in inEvent.Die_Rolls)
            {
                _Rolls_CSV.Append(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}\n", inEvent.ID, curr_roll.Reason, curr_roll.Num_Of_Dice, curr_roll.Type_Of_Die, curr_roll.Bonus, curr_roll.Roll, ((curr_roll.Target != 0) ? curr_roll.Target.ToString() : ""), curr_roll.Character_Name, "1"));

                if (curr_roll.Type_Of_Die == 20)
                {
                    // Simple count
                    _D20s[curr_roll.Roll] += 1;

                    // Streaks
                    if (Fuzzy_Equals(_Last_Roll, curr_roll.Roll)) { _Current_Streak++; }
                    else
                    {
                        if (_D20s_Streaks[_Last_Roll] < _Current_Streak) { _D20s_Streaks[_Last_Roll] = _Current_Streak; }
                        _D20s_Streak_Length[_Last_Roll, Math.Min(_Current_Streak, _D20s_Streak_Length.GetUpperBound(1))]++;
                        _Current_Streak = 1;
                    }
                    _Last_Roll = curr_roll.Roll;
                }
            }
        }

        internal void Credit_Kill(string inKiller_Name, int inExp, string inKilled_Name)
        {
            _Number_Killed++;
            if (inExp > 0) { _Exp_Earned += inExp; } // inExp == -1 when a summoned creature is killed (which never grants experience).
            _Kills.Add(inKilled_Name);            
        }

        public void Build_Analysis()
        {
            Clear_Analysis_Only();

            for (int x = 0; x <= _Misc_Analysis.GetUpperBound(0); x++)
            {
                for (int y = 0; y <= _Misc_Analysis.GetUpperBound(1); y++)
                {
                    _Misc_Analysis[x, y] = "";
                }
            }

            _Misc_Analysis[0, 0] = "Name"; Misc_Analysis[0, 1] = "*_*";
            _Misc_Analysis[1, 0] = "Attack Count"; _Misc_Analysis[1, 1] = _Attack_Cnt.ToString();
            _Misc_Analysis[2, 0] = "Hit Count"; _Misc_Analysis[2, 1] = _Hit_Cnt.ToString();
            _Misc_Analysis[3, 0] = "Critical Threatened"; _Misc_Analysis[3, 1] = _Critical_Threatened_Cnt.ToString();
            _Misc_Analysis[4, 0] = "Critical Confirmed"; _Misc_Analysis[4, 1] = _Critical_Confirmed_Cnt.ToString();
            _Misc_Analysis[5, 0] = "Kill Count"; _Misc_Analysis[5, 1] = _Number_Killed.ToString();
            _Misc_Analysis[6, 0] = "Exp Earned"; _Misc_Analysis[6, 1] = _Exp_Earned.ToString();
            _Misc_Analysis[7, 0] = "Damage Inflicted"; _Misc_Analysis[7, 1] = _Damage_Done_Total.ToString();
            _Misc_Analysis[8, 0] = "-------------------"; _Misc_Analysis[8, 1] = "----";
            _Misc_Analysis[9, 0] = "Critical Threatened On 20, Confirmed On 20"; _Misc_Analysis[9, 1] = _Critical_Threatned_20_Confirmed_20_Cnt.ToString();
            _Misc_Analysis[10, 0] = "Critical Threatened On 20, NOT confirmed on 20"; _Misc_Analysis[10, 1] = _Not_Critical_Threatned_20_Confirmed_20_Cnt.ToString();

            _Characters.Sort();

            int longest_list = _Characters.Count();
            _Character_Names_Analysis = new string[longest_list + 1, 1];

            _Character_Names_Analysis[0, 0] = _Type_Description;

            if (longest_list > 0) { for (int x = 0; x < longest_list; x++) { _Character_Names_Analysis[x + 1, 0] = _Characters[x]; } }

            int d20s_total = 0;

            for (int x = 1; x < 21; x++)
            {
                _Frequency_Analysis[x, 0] = x.ToString();
                _Frequency_Analysis[x, 1] = _D20s[x].ToString();

                d20s_total = d20s_total + _D20s[x];
            }

            // Each die roll sum (_d20s[1], for example) represents an observed value
            // For each die roll, the expected value is the count / 20 (e.g. sum1 / 20 for _d20s).
            // Therefore...

            int degress_of_freedom = 19; // 1 less than than the number of measure values.

            double d20s_critical_value = 0.0;

            for (int x = 1; x < 21; x++) { if (d20s_total > 0) { d20s_critical_value += Math.Pow(_D20s[x] - (d20s_total / 20.0), 2) / (d20s_total / 20.0); } }

            _D20s_P_Value = 1.0;

            if (d20s_total > 0) { _D20s_P_Value = (double)P_Value_ChiSqr(degress_of_freedom, d20s_critical_value); }

            _Frequency_Analysis[0, 0] = "d20s";
            _Frequency_Analysis[0, 1] = "*_* Cnt";
            _Frequency_Analysis[0, 2] = "Prob";

            _Frequency_Analysis[21, 0] = "[b]Expected\nValue[/b]";
            _Frequency_Analysis[21, 1] = String.Format("{0:N0} (0)", (d20s_total / 20));
            _Frequency_Analysis[21, 2] = "";

            _Frequency_Analysis[22, 0] = "[b]Total[/b]";
            _Frequency_Analysis[22, 1] = String.Format("{0}\n(X² = {1:N})", d20s_total, d20s_critical_value);
            _Frequency_Analysis[22, 2] = String.Format("P = {0:N4}", _D20s_P_Value);

            const double chance_of_rolling_a_1 = (1.0 / 20.0);

            Parallel.For(1, 21, 
                (x, state) => 
            {
                _Frequency_Analysis[x, 1] += System.String.Format(" ({0:+###;-###;0})", _D20s[x] - (d20s_total / 20));
                _Frequency_Analysis[x, 2] = Cumulative_Binomial_Probability(d20s_total, _D20s[x], chance_of_rolling_a_1).ToString("P2");
            } );

            _Frequency_Analysis[21, 2] = Cumulative_Binomial_Probability(d20s_total, (d20s_total / 20), chance_of_rolling_a_1).ToString("P2");

            _Streak_Analysis[0, 0] = "d20";
            _Streak_Analysis[0, 1] = "*_* Max Len";

            for (int x = 1; x < 21; x++)
            {
                _Streak_Analysis[x, 0] = String.Format("{0}", x);
                _Streak_Analysis[x, 1] = String.Format("{0}", _D20s_Streaks[x]);
            }

            // Streak Length Analysis

            _Streak_Length_Analysis[0, 0] = "d20";
            for (int x = 1; x <= _D20s_Streak_Length.GetUpperBound(1); x++)
            {
                _Streak_Length_Analysis[0, x] = String.Format("{0}", x);
            }
            _Streak_Length_Analysis[0, _D20s_Streak_Length.GetUpperBound(1)] = String.Format("> {0}", _D20s_Streak_Length.GetUpperBound(1));

            for (int x = 1; x <= _D20s_Streak_Length.GetUpperBound(0); x++)
            {
                for (int y = 0; y <= _D20s_Streak_Length.GetUpperBound(1); y++)
                {
                    if (y == 0) { _Streak_Length_Analysis[x, y] = String.Format("{0}", x); }
                    else { _Streak_Length_Analysis[x, y] = String.Format("{0}", _D20s_Streak_Length[x, y]); }
                }
            }

            // Margin Analysis

            _Attack_Margin_Analysis[0, 0] = "Margin";
            _Attack_Margin_Analysis[0, 1] = "*_* Cnt";

            _Crit_Confirmation_Margin_Analysis[0, 0] = "Margin";
            _Crit_Confirmation_Margin_Analysis[0, 1] = "*_* Cnt";

            int atk_margin_total = 0;

            int crit_conf_margin_total = 0;

            for (int x = -20; x <= +20; x++)
            {
                _Attack_Margin_Analysis[x + 21, 0] = x.ToString("+##;-##;0");
                _Attack_Margin_Analysis[x + 21, 1] = _Attack_Margin[x + 20].ToString();

                atk_margin_total += _Attack_Margin[x + 20];

                _Crit_Confirmation_Margin_Analysis[x + 21, 0] = x.ToString("+##;-##;0");
                _Crit_Confirmation_Margin_Analysis[x + 21, 1] = _Crit_Confirmation_Margin[x + 20].ToString();

                crit_conf_margin_total += _Crit_Confirmation_Margin[x + 20];
            }

            _Attack_Margin_Analysis[42, 0] = "[b]Total[/b]";
            _Attack_Margin_Analysis[42, 1] = atk_margin_total.ToString();

            _Crit_Confirmation_Margin_Analysis[42, 0] = "[b]Total[/b]";

            _Tables_Stale = false;
        }

        private bool Fuzzy_Equals(int inA, int inB)
        {
            return inA == inB;
        }

        private Rational Reciprocal(Rational inVal)
        {
            Rational rslt = Rational.Invert(inVal);

            return rslt;
        }

        private Rational myPow(Rational inBase, Rational inExp)
        {
            // ln (a^b) = b*ln(a)
            // ln (a^(b/c)) = b/c*ln(a)
            //
            //Argh -- Rational.Log(inBase) returns a *double".  It'll still work, but it introduce (potentially large) impercision in the results.

            return inExp * (Rational)Rational.Log(inBase);
        }

        private Rational myExp(Rational inExp)
        {
            Rational e = (Rational)Math.E;
            return myPow(e, inExp);
        }

        private double P_Value_ChiSqr(int Dof, double Cv)
        {
            if (Cv < 0 || Dof < 1)
            {
                return 0;
            }

            double K = Dof * 0.5;
            double X = Cv * 0.5;

            if (Dof == 2)
            {
                return Math.Exp(-1.0 * X);
            }

            double PValue = Incomplete_Gamma(K, X);

            PValue /= Gamma(K);

            return (1 - PValue);
        }

        private double Incomplete_Gamma(double S, double Z)
        {
            if (Z < 0.0)
            {
                return 0.0;
            }
            double Sc = (1.0 / (double)S);
            Sc *= Math.Pow((double)Z, (double)S);
            Sc *= Math.Exp((double)(-Z));

            double Sum = 1;
            double Nom = 1;
            double Denom = 1;
            double last_Sum = 1;

            for (int I = 0; I < 200; I++)
            {
                Nom *= Z;
                S++;
                Denom *= S;
                last_Sum = Sum;
                Sum += (Nom / Denom);

                if (last_Sum == Sum) { break; }

                // if (Sum is decimal.NaN) { Sum = last_Sum; break; }
            }

            return Sum * Sc;
        }

        private double Gamma(double N)
        {
            // Gamma is the floating point version of factorial -- see https://en.wikipedia.org/wiki/Gamma_function.
            //
            // This particular implementation comes from https://www.codeproject.com/Articles/432194/How-to-Calculate-the-Chi-Squared-P-Value
            //

            double A = 15; // Level of accuracy for calculations.

            double SQRT2PI = 2.5066282746310005024157652848110452530069867406099383;

            double Z = N;
            double Sc = Math.Pow(Z + A, Z + 0.5);
            Sc *= Math.Exp(-1 * (Z + A));
            Sc /= Z;

            double F = 1.0;
            double Ck;
            double Sum = SQRT2PI;

            for (int K = 1; K < A; K++)
            {
                Z++;
                Ck = Math.Pow((A - K), (K - 0.5));
                Ck *= Math.Exp(A - K);
                Ck /= F;

                Sum += (Ck / Z);

                F *= (-1 * K);
            }

            return (Sum * Sc);
        }

        private Rational Factorial(long inVal)
        {
            if (inVal == 0) { return 1; }

            Rational rtn = new Rational(1);

            for (int x=1; x<=inVal; x++)
            {
                rtn *= x;
            }

            return rtn;
        }

        private Rational N_Pick_M(long N, long K)
        {
            // http://csharphelper.com/blog/2014/08/calculate-the-binomial-coefficient-n-choose-k-efficiently-in-c/

            Rational rslt = new Rational(1);
            Rational N_rational = new Rational(N);
            Rational K_rational = new Rational(K);

            for (int i = 1; i <= K; i++)
            {
                rslt *= N_rational - (K_rational - ((Rational)i)); // Something funky here -- N - K - i returns incorrect results, but N - (K - i) returns correct results. ????
                rslt /= (Rational)i;
            }

            Rational first_calc_rslt = rslt;

            //Rational second_calc_rslt = new Rational();

            //second_calc_rslt = Factorial(N) / (Factorial(K) * Factorial(N - K));

            return first_calc_rslt;
        }

        private double Binomial_Probability(int trials, int successes, double chance_of_success)
        {
            double chance_of_failure = 1 - chance_of_success;

            Rational c = N_Pick_M(trials, successes);
            Rational px = Rational.Pow((Rational)chance_of_success, successes);
            Rational qnx = Rational.Pow((Rational)chance_of_failure, trials - successes);

            Rational tmp_rtn = (c * px * qnx);
            double rtn = (double)tmp_rtn;

            return rtn;
        }

        private double Cumulative_Binomial_Probability(int trials, int successes, double chance_of_successes)
        {
            double expected_num_of_successes_double = (((double)trials) * ((double)chance_of_successes));
            int expected_num_of_successes = (int)Math.Truncate(expected_num_of_successes_double);

            int inc_by = 0;
            int bound = 0;

            if (successes < expected_num_of_successes)
            {
                inc_by = -1;
                bound = -1;
            }
            else
            {
                inc_by = 1;
                bound = successes + (successes / 2) + 1;
            }

            double rslt = 0;
            double current_rslt = 0;
            double prev_rslt = 0;

            if (trials == 0)
            {
                return 1;
            }
            else
            {
                // System.Diagnostics.Debug.WriteLine("Start Cumulative Binomial Distribution, {0}, {1}, {2} iterations", trials, successes, (inc_by == -1 ? successes : (bound - successes)));
                for (int x = successes; x != bound; x = x + inc_by)
                {
                    current_rslt = Binomial_Probability(trials, x, chance_of_successes);
                    //System.Diagnostics.Debug.WriteLine("\tChance of {0} hits in {1} trials is {2:P4}", x, trials, current_rslt);
                    if (current_rslt < 0.0000001) { break; }
                    if (prev_rslt != 0)
                    {
                        if (current_rslt > prev_rslt)
                        {
                            // System.Diagnostics.Debugger.Break();
                        }
                    }
                    prev_rslt = current_rslt;
                    rslt += current_rslt;
                    if (rslt > 0.5)
                    {
                        // System.Diagnostics.Debug.WriteLine("Warning: rslt > 50 %, currently {0:P4}", rslt);
                        // System.Diagnostics.Debugger.Break();
                    }
                }

                return rslt;
            }
        }
    }
}
