using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public delegate void NewCombatEvent(CombatEvent newCombatEvent, string Filename);
    public delegate void Parser_idle(string inStatus);
    public delegate void Load_Progress(string FileName, long Completed, long Total);
    public delegate void Current_File_Changed(string inFilename);

    static class CombatLog_Parser
    {
        static public event NewCombatEvent OnNewCombatEvent;
        static public event Parser_idle OnParserIdle;
        static public event Load_Progress OnLoadProgress;
        static public event Current_File_Changed OnCurrentFileChanged;

        static private string[] _FileNames = null;
        static private int _FileNames_Indx = 0;
        static private string _Full_Path_And_Filename = "";
        static private string _Full_Filename_For_Prettified_File = "";

        static private System.Threading.Thread _Parse_Thread = null;
        static private CombatLog _CombatLog = null;

        static private System.IO.FileInfo _inFileInfo = null;
        static private System.IO.FileStream _inFileSteam = null;
        static private System.IO.StreamReader _inFileReader = null;
        static private System.IO.FileSystemWatcher _inFileWatcher = null;

        static private System.IO.FileStream _outFileStream = null;
        static private System.IO.StreamWriter _outFileWriter = null;

        static private System.IO.FileStream _outFileSteam_DeDup = null;
        static private System.IO.StreamWriter _OutFileWritter_DeDup = null;

        public enum myThreadState { Not_Started, Processing, Waiting, Done};
        static private myThreadState _Current_myThreadState = myThreadState.Not_Started;
        static private bool _Kill_Thread = false;
        static private bool _Size_Changed = true;

        static private long _Cached_File_Length = 0;
        static private int _Progress_Cnt = 0;

        static int _Number_Of_Consecutive_Events_Loaded = 0;
        static int _Consecutive_Idle_Parser_Loops = 0;
                
        static public string Full_Path_And_Filename
        {
            get { return _Full_Path_And_Filename; }
            set { _Full_Path_And_Filename = value; }
        }

        static public string Path
        {
            get { return System.IO.Path.GetDirectoryName(_Full_Path_And_Filename); }
        }

        static public string Filename
        {
            get { return System.IO.Path.GetFileName(_Full_Path_And_Filename); }
        }

        static public System.Threading.Thread Parse_Thread
        {
            get { return _Parse_Thread;  }
        }

        static public CombatLog CombatLog
        {
            get { return _CombatLog; }
        }

        static public string Full_Filename_For_Prettified_File
        {
            get { return _Full_Filename_For_Prettified_File; }
        }

        static public System.IO.FileStream Stream_For_Prettified_File
        {
            get { return _outFileStream; }
        }
        
        static public void Spawn_Parse(string[] in_filenames) // This clears the current dataset
        {
            Kill_Thread();

            _FileNames = in_filenames;
            _FileNames_Indx = 0;

            Open_Next_InputFile();

            if (_CombatLog != null) { _CombatLog.ClearAll(); } else { _CombatLog = new CombatLog(); }

            Open_Output_Files();
            
            _Parse_Thread = new System.Threading.Thread(new System.Threading.ThreadStart(Parse));
            _Parse_Thread.Start();
        }

        static public void Change_Input_Files(string[] in_filename) // This *adds* the new input file(s) to the current dataset
        {
            Wait_For_Parser_Idle();

            if ((_Parse_Thread == null) || (_Parse_Thread.ThreadState == System.Threading.ThreadState.Aborted))
            {
                Spawn_Parse(in_filename);
            }
            else
            {
                _FileNames = in_filename;
                _FileNames_Indx = 0;
            }
        }

        static private void Open_Output_Files()
        {
            _outFileStream?.Close();
            _Full_Filename_For_Prettified_File = Path + "\\out.html";
            _outFileStream = new System.IO.FileStream(_Full_Filename_For_Prettified_File, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite, System.IO.FileShare.ReadWrite);
            _outFileWriter = new System.IO.StreamWriter(_outFileStream);

            _outFileSteam_DeDup?.Close();
            _outFileSteam_DeDup = new System.IO.FileStream(Path + "\\out-dedup.html", System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite, System.IO.FileShare.ReadWrite);
            _OutFileWritter_DeDup = new System.IO.StreamWriter(_outFileSteam_DeDup);
        }

        static public void Flush_Output_Files()
        {
            _outFileWriter.Flush();
            _OutFileWritter_DeDup.Flush();
        }

        static private void Open_Next_InputFile()
        {
            if (Next_InputFile_Exists())
            {
                Change_Input_File(_FileNames[_FileNames_Indx]);
                _FileNames_Indx++;
            }
        }

        static private bool Next_InputFile_Exists() { return (_FileNames_Indx <= _FileNames.GetUpperBound(0)); }

        static private bool Wait_For_Parser_Idle()
        {
            int wait_for_processing_to_complete_loop = 0;
            int prev_progress_cnt = 0;

            while ((_Parse_Thread != null) && (_Parse_Thread.ThreadState != System.Threading.ThreadState.Aborted) && (_Consecutive_Idle_Parser_Loops == 0) && (wait_for_processing_to_complete_loop < 10))
            {
                if (prev_progress_cnt != _Progress_Cnt) { prev_progress_cnt = _Progress_Cnt; wait_for_processing_to_complete_loop = 0; }
                else { wait_for_processing_to_complete_loop++; }

                System.Threading.Thread.Sleep(70);
            }

            if (wait_for_processing_to_complete_loop == 10) { throw new System.Exception("Timed out waiting for loading to complete before changing file"); }

            return true;
        }

        static private void Change_Input_File(string in_filename)
        {
            if (System.Threading.Thread.CurrentThread != _Parse_Thread) { Wait_For_Parser_Idle(); }

            _Full_Path_And_Filename = in_filename;

            _inFileInfo = new System.IO.FileInfo(_Full_Path_And_Filename);
            _inFileSteam = new System.IO.FileStream(_Full_Path_And_Filename, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            _inFileReader = new System.IO.StreamReader(_inFileSteam);
            _inFileWatcher = new System.IO.FileSystemWatcher(Path, Filename) { NotifyFilter = System.IO.NotifyFilters.Size };
            _inFileWatcher.Changed += Watcher_Changed;
            _inFileWatcher.EnableRaisingEvents = true;

            _Cached_File_Length = _inFileInfo.Length;

            OnCurrentFileChanged?.Invoke(in_filename);
        }

        private static void Watcher_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            // This runs on the main thread, *not* the worker thread!

            if (System.Threading.Thread.CurrentThread == _Parse_Thread) { throw new System.Exception("Called Watcher_changed from the worker thread"); }

            if (!_Size_Changed)
            {
                _Size_Changed = true;
                _inFileInfo.Refresh();

                if (_Cached_File_Length > _inFileInfo.Length ) 
                {
                    Kill_Thread();

                    _inFileSteam.Seek(0, System.IO.SeekOrigin.Begin);

                    Open_Output_Files();
                    _CombatLog.ClearAll();

                    OnNewCombatEvent?.Invoke(null, "");
                    OnParserIdle?.Invoke("File reset detected, parser restarted");

                    _Parse_Thread = new System.Threading.Thread(new System.Threading.ThreadStart(Parse));
                    _Parse_Thread.Start();
                }
            }
        }
        
        static public void Kill_Thread()
        {
            if (_Parse_Thread != null)
            {
                if (_Parse_Thread.ThreadState == System.Threading.ThreadState.Suspended)
                {
                    _Parse_Thread.Abort();
                }
                else
                {
                    _Kill_Thread = true;
                    System.Threading.Thread.Sleep(100);
                    _Parse_Thread.Abort();
                }
            }

            _Kill_Thread = false;
            _Consecutive_Idle_Parser_Loops = 0;
        }

        static private CombatStartEvent _Curr_Start_Of_Combat = null;
        static private CombatStartEvent _Prev_Start_Of_Combat = null;

        static public void Parse()
        {
            int num_total = 0;
            int num_of_attacks = 0;
            int num_of_damage = 0;
            int num_of_healing = 0;
            int num_of_initiative = 0;
            int num_of_simple = 0;
            int num_of_other = 0;

            while (!_Kill_Thread)
            {
                _Size_Changed = false;

                while ((!_inFileReader.EndOfStream) && (!_Kill_Thread))
                {
                    _Cached_File_Length = _inFileInfo.Length;
                    _Consecutive_Idle_Parser_Loops = 0;
                    _Progress_Cnt++;
                    if (_Progress_Cnt == int.MaxValue) { _Progress_Cnt = 0; }

                    string line = _inFileReader.ReadLine();
                    string pretty_line = CleanUp_Line(line);
                    if (pretty_line != "")
                    {
                        foreach (string tmp_line in pretty_line.Split('\n'))
                        {
                            _outFileWriter.WriteLine(tmp_line);

                            CombatEventList new_log_entry = Parse_Line(tmp_line);

                            lock (CombatLog_Parser.CombatLog)
                            {
                                foreach (CombatEvent new_event in new_log_entry)
                                {
                                    if (new_event is CombatStartEvent)
                                    {
                                        System.Diagnostics.Debug.WriteLine("Start of new combat");
                                        if (_Prev_Start_Of_Combat != null)
                                        {
                                            _Curr_Start_Of_Combat.Update_Smarter_Guesses_Character_Types();
                                            _Curr_Start_Of_Combat.Update_Reload(_Prev_Start_Of_Combat);
                                        }

                                        _Prev_Start_Of_Combat = _Curr_Start_Of_Combat;
                                        _Curr_Start_Of_Combat = (CombatStartEvent)new_event;
                                    }
                                    else if (_Curr_Start_Of_Combat != null)
                                    {
                                        _Curr_Start_Of_Combat.Children.Add(new_event);
                                    }
                                    else { throw new System.Exception("Invalid log -- log didn't start with a 'Combat Start' event"); }

                                    WriteDeDupFile(new_event);
                                    if ((_CombatLog.Log.Count > 3) && (new_event is SimpleEvent) && (((SimpleEvent)new_event).Subtype == "Death"))
                                    {
                                        Dispatch_Death((SimpleEvent)new_event);
                                    }
                                    num_total++;
                                    if (new_event is AttackEvent) { num_of_attacks++; }
                                    else if (new_event is DamageEvent) { num_of_damage++; }
                                    else if (new_event is HealingEvent) { num_of_healing++; }
                                    else if (new_event is InitiativeEvent) { num_of_initiative++; }
                                    else if (new_event is SimpleEvent) { num_of_simple++; }
                                    else { num_of_other++; }
                                    OnNewCombatEvent?.Invoke(new_event, _Full_Path_And_Filename);
                                }
                            }
                        }
                    }
                    if (OnLoadProgress != null)
                    {
                        _inFileInfo.Refresh();
                        OnLoadProgress?.Invoke(_Full_Path_And_Filename, _inFileSteam.Position, _inFileInfo.Length);
                    }
                    _Number_Of_Consecutive_Events_Loaded++;
                }

                if (Next_InputFile_Exists())
                {
                    Open_Next_InputFile();
                }
                else
                {
                    if ((_Number_Of_Consecutive_Events_Loaded > 500)&&(_Consecutive_Idle_Parser_Loops==0)) { _Consecutive_Idle_Parser_Loops = 50; }
                    if (_Consecutive_Idle_Parser_Loops == 50) // (50*50) = 2500 ms = 2.5 seconds of idle time
                    {
                        string status_str = String.Format(
                            "Removed {0} duplicates, added {1} events ({2} Attacks, {3} Damages, {4} Healing, {5} Initiative, {6} Simple, {7} Other)",
                            _CombatLog.Duplicate_Count,
                            num_total,
                            num_of_attacks,
                            num_of_damage,
                            num_of_healing,
                            num_of_initiative,
                            num_of_simple,
                            num_of_other
                            );

                        _CombatLog.ResetDuplicateCount();

                        num_total = 0;
                        num_of_attacks = 0;
                        num_of_damage = 0;
                        num_of_healing = 0;
                        num_of_initiative = 0;
                        num_of_simple = 0;
                        num_of_other = 0;

                        OnParserIdle?.Invoke(status_str);
                    }

                    if (_Consecutive_Idle_Parser_Loops < 100000) { _Consecutive_Idle_Parser_Loops++; }
                    _Number_Of_Consecutive_Events_Loaded = 0;

                    if (!_Kill_Thread) { System.Threading.Thread.Sleep(50); }
                }
            }

            System.Diagnostics.Debug.WriteLine("Thread exited normally");
        }

        private static void Dispatch_Death(SimpleEvent new_event)
        {
            SimpleEvent curr_event = (SimpleEvent)new_event;
            SimpleEvent prev_event = _CombatLog.Log[_CombatLog.Log.Count - 2] as SimpleEvent;
            CombatEvent killer_event = null;
            int exp_awarded = 0;

            if ((prev_event != null)&&(prev_event.Subtype == "Experience"))
            {
                exp_awarded = int.Parse(System.Text.RegularExpressions.Regex.Match(_CombatLog.Log[_CombatLog.Log.Count - 2].Source, @"<b>(\d*)<\/b>").Groups[1].Value);
                if (exp_awarded == 0) { exp_awarded = -2; } // Sometimes, 0 xp is awarded for killing various things.  I use 0 xp to mark a warning (no exp found), but don't warn for negative xp (and don't add it to the running total), so...
                killer_event = _CombatLog.Log[_CombatLog.Log.Count - 3];
            }
            else
            {
                if (curr_event.Character_Name.Contains("Summon")) { exp_awarded = -1; } else { exp_awarded = 0; }
                killer_event = _CombatLog.Log[_CombatLog.Log.Count - 2];
            }

            if (killer_event is DamageEvent) { _CombatLog.Stats.Credit_Kill(killer_event.Character_Name, exp_awarded, curr_event.Character_Name, curr_event.ID); }
            else if (killer_event is AttackEvent) { _CombatLog.Stats.Credit_Kill(killer_event.Character_Name, exp_awarded, curr_event.Character_Name, curr_event.ID); }
            else { _CombatLog.Stats.Credit_Kill(null, exp_awarded, curr_event.Character_Name, curr_event.ID); }
        }

        private static void WriteDeDupFile()
        {
            foreach (CombatEvent curr_event in _CombatLog.Log)
            {
                WriteDeDupFile(curr_event);
            }
            _OutFileWritter_DeDup.Flush();
        }

        private static void WriteDeDupFile(CombatEvent inEvent)
        {
            _OutFileWritter_DeDup.Write(inEvent.Source_With_ID);
            if (inEvent.Source.Contains("Combat Ended ["))
            {
                _OutFileWritter_DeDup.WriteLine("<hr>");
            }
        }

        private static int next_line_extra_indent = 0;

        static private string CleanUp_Line(string line)
        {
            if (line.Trim() == "") { return ""; } // Skip blank lines

            // Fix the easy stuff.

            line = System.Text.RegularExpressions.Regex.Replace(line, @"(<color=(.*?)>)", "<span style=\"color:$2\">");
            line = line.Replace("</color>", "</span>");
            line = line.Replace("  ", "\t\t");

            int tab_count = line.Length - line.Replace("\t", "").Length;

            // Override tab count for selected types of lines, to fix identation problems.

            if (line.Contains("DifficultyType"))
            {
                tab_count = 0;
            }
            else if (line.Contains("Attack result:"))
            {
                line = line.Substring(1); // Strip off the first character.
                tab_count = 1;
            }
            //	Concealment miss chance — 50%, roll: <b><u>38</u></b>, <b><u>37</u></b> [Blind Fight] — failed  Result: miss
            else if (line.Contains("Concealment miss chance") && (line.Contains("— failed"))) // "Attack Result" doesn't appear on attack lines where you miss due to concealment.
            {
                tab_count = 1;
            }
            else if (line.Contains("Attack Bonus:"))
            {
                tab_count = 2;
            }
            else if (line.Contains("Armor Class:"))
            {
                tab_count = 1;
            }
            int indent_distance = (tab_count + next_line_extra_indent) * 50;

            string div_tag_txt = String.Format("<div style=\"margin-left: {0,3}px\">", indent_distance);
            string short_div_txt = String.Format("<div style=\"margin-left: {0,3}px\">", indent_distance - 50);
            string long_div_txt = String.Format("<div style=\"margin-left: {0,3}px\">", indent_distance + 50);

            if (next_line_extra_indent > 0)
            {
                div_tag_txt += new string('\t', next_line_extra_indent);
                short_div_txt += new string('\t', next_line_extra_indent);
                long_div_txt += new string('\t', next_line_extra_indent);
                next_line_extra_indent = 0;
            }

            int colon_count = (line.Length - line.Replace(": ", "").Length) / 2; // Divide by 2 because we are looking for two characters (a ":" and a space) rather than one.

            // Split lines where two pieces of information are stored on the same line.

            //	<b>Attack Bonus: +23</b>  Weapon Training (Bows): <color=#004604>+2</color>
            if ((line.Contains("Attack Bonus:")) && (colon_count == 2))
            {
                System.Text.RegularExpressions.Match tmp = System.Text.RegularExpressions.Regex.Match(line, @"\s(.*?)\s\s(.*)");
                line = short_div_txt + tmp.Groups[1].Value + "</div>\n" + div_tag_txt + "\t\t" + tmp.Groups[2].Value + "</div>";
            }
            // 	<b>Armor Class: 20 (Flat-footed)</b>  Base value: 10  Deflection: <color=#004604>+1 [Ring of Protection +1]</color>
            else if ( line.Contains("Armor Class:") && (colon_count == 3) && (!line.Contains("Natural 1.")) && (!line.Contains("Natural 20.")) && (!line.Contains("Natural <s>1</s> 20")) )
            {
                System.Text.RegularExpressions.Match tmp = System.Text.RegularExpressions.Regex.Match(line, @"\s(.*?)\s\s(.*?)\s\s(.*)");
                line = div_tag_txt + tmp.Groups[1].Value + "</div>\n" + long_div_txt + "\t\t" + tmp.Groups[2].Value + "</div>\n" + long_div_txt + "\t\t" + tmp.Groups[3].Value + "</div>";
            }
            //	<b>Armor Class: 10 (Flat-footed, Touch)</b>  Base value: 10
            else if (line.Contains("Armor Class:") && (colon_count == 2) && (!line.Contains("Natural 1.")) && (!line.Contains("Natural 20.")) && (!line.Contains("Natural <s>1</s> 20")) )
            {
                System.Text.RegularExpressions.Match tmp = System.Text.RegularExpressions.Regex.Match(line, @"\s(.*?)\s\s(.*)");
                line = div_tag_txt + tmp.Groups[1].Value + "</div>\n" + long_div_txt + "\t\t" + tmp.Groups[2].Value + "</div>";
            }
            //Attack result: Natural 1.		Target's Armor Class: 8.		Result: critical miss
            //No special handling needed?
            //  Damage source: <b>Blasting Bracers</b>.
            else if (line.Contains("Damage source"))
            {
                line = short_div_txt + line + "</div>";
                next_line_extra_indent = 1;
            }
            //	Attack number: 1 out of 2  Attack result: 35 (roll: 19 + modifiers: 16)  Target's Armor Class: 20  Result: hit  Critical confirmation result: 21 (roll: 5 + modifiers: 16).  Result: critical hit confirmed
            //	Attack number: 6 out of 6  Attack result: Natural 20.  Target's Armor Class: 38.  Result: hit  Critical confirmation result: 34 (roll: 19 + modifiers: 15).  Result: critical hit not confirmed
            //	Attack result: Natural 20.  Target's Armor Class: 12.  Result: hit  Critical confirmation result: 15 (roll: 8 + modifiers: 7).  Result: critical hit confirmed
            //	Attack result: Natural 20.  Target's Armor Class: 36.  Result: hit  Critical confirmation result: 32 (roll: 13 + modifiers: 19).  Result: critical hit not confirmed
            else if (line.Contains("Critical confirmation result"))
            {
                int indx = line.IndexOf("Critical confirmation result");
                string part_one = line.Substring(0, indx - 1);
                string part_two = line.Substring(indx);
                line = div_tag_txt + part_one + "</div>\n" + long_div_txt + "\t\t" + part_two + "</div>";
            }
            //Combat Ended [1]Combat Started [2]
            //Combat Ended [1]
            else if (line.Contains("Combat Ended"))
            {
                int indx = line.IndexOf("]");
                string part_one = line.Substring(0, indx + 1);
                string part_two = line.Substring(indx + 1);
                line = div_tag_txt + part_one + "</div>\n<hr>\n" + div_tag_txt + part_two + "</div>";
            }
            //	<b>30</b> precision damage is dealt due to sneak attack.
            else if (line.Contains("precision damage is dealt due to sneak attack."))
            {
                line = long_div_txt + "\t" + line + "</div>";
            }
            else
            {
                line = div_tag_txt + line + "</div>";
            }

            return line;
        }

        static CombatEventList Parse_Line(string line)
        {
            return _CombatLog.Parse(line);
        }
    }
}
