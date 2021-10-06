using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    class SimpleEvent : CombatEvent
    {
        private string _Character_Name = "";
        private string _Source_Character_Name = "";
        private string _Subtype = "";
        

        public override string Character_Name
        {
            get { return _Character_Name; }
            set { _Character_Name = value; }
        }

        public override string Source_Character_Name => _Source_Character_Name;
        
        public override List<Die_Roll> Die_Rolls
        {
            get { return new List<Die_Roll>(); }
        }
                
        public string Subtype { get => _Subtype; }

        public SimpleEvent(int inID, string line) : base(inID, line) { }

        public override List<Die_Roll> Parse(string line)
        {
            Source += line + "\n";

            if (line.Contains("Combat Started"))
            {
                _Subtype = "Combat Started";
            }
            else if (line.Contains("Combat Ended"))
            {
                _Subtype = "Combat Ended";
            }
            else if (line.Contains("PlayerCharacter Name:"))
            {
                _Subtype = "PlayerCharacter Name";
                _Character_Name = line.Split(':')[2].Trim();
                _Source_Character_Name = _Character_Name;
            }
            else if (line.Contains("DifficultyType"))
            {
                _Subtype = "Difficulty";
            }
            else if (line.Contains("Class: "))
            {
                _Subtype = "Level Information";
            }
            else if (line.Contains("Gained experience"))
            {
                _Subtype = "Experience";
            }
            else if (line.Contains("dies!"))
            {
                _Subtype = "Death";
                //<div style="margin-left:   0px"><b><b><span style="color:#262626">Zerieks[1507b0b0]</span></b></b> dies!</div>
                _Character_Name = Regex.Match(line, @"(?:.*\x22>){2}(.*?)<").Groups[1].Value;
                _Source_Character_Name = _Character_Name;
            }
            else if (line.Contains("inspect failed"))
            {
                _Subtype = "Failed inspection";
            }
            else if (line.Contains("information updated"))
            {
                _Subtype = "bestiary Updated";
                _Character_Name = Regex.Match(line, @"(?:.*?>){2}(.*?)<").Groups[1].Value;
                _Source_Character_Name = _Character_Name;
            }
            else if (line.Contains("<hr>"))
            {
                _Subtype = "Formatting";
            }
            else if (line.Contains("makes attack of opportunity against"))
            {
                _Subtype = "Attack of Opportunity";
            }
            else
            {
                _Subtype = "Unknown";
                System.Diagnostics.Debug.WriteLine("Unknown line \"" + line + "\", ignoring.");
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
                "Event Type",
                "Event Text"
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
                        case "Event Type": tb.Text = _Subtype; break;
                        case "Event Text": tb.Text = Source; break;
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
