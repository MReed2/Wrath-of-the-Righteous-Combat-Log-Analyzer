using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
        protected Char_Enum _Character_Type = Char_Enum.Really_Unknown;
        protected Char_Enum _Smarter_Guess_Character_Type = Char_Enum.Really_Unknown;
        private string _Friendly_Name = "";
        private CombatEventList _Children = new CombatEventList();
        private int _Cached_Source_Hashcode = 0;
        private int _Cached_Source_String_Length = 0;
        private int _Combat_ID = 0;

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
                if (value != _Character_Type)
                {
                    _Character_Type = value;
                    OnCombatEventChanged?.Invoke(this);
                }
            }
        }

        public Char_Enum Guess_Character_Type
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

        protected void OnCombatEventChanged_Invoke()
        {
            OnCombatEventChanged?.Invoke(this);
        }

        protected Char_Enum Guess_Character_Type_From_String(string inStr)
        {
            if (inStr.Contains("Companion") || (inStr.Contains("Player_Unit")) ) { return Char_Enum.Friendly; }
            else if (inStr.Contains("Summoner")) { return Char_Enum.Hostile; }
            else if (inStr.Contains("Summon")) { return Char_Enum.Summon; }
            else { return Char_Enum.Hostile; }
        }

        protected bool Likely_Hostile(string inStr)
        {
            return (Regex.Match(inStr, @".*?CR(\d*?)_.*").Success); // Looking for "...CR###_..."  -- not all hostiles use this format, but most do.
        }

        protected bool Obviously_Friendly(string inStr)
        {
            return (inStr.Contains("Companion") || (inStr.Contains("Player_Unit")));
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

        public virtual int Combat_ID { get => _Combat_ID; }

        public virtual string Source_With_ID { get => Regex.Replace(_Source, @"(^.*?\x22>)", "$1ID: " + _ID.ToString() + " ", RegexOptions.None); }

        public virtual string Source { get => _Source; set => _Source = value; }

        public int ID { get => _ID; }

        public abstract string Source_Character_Name { get; }

        public abstract string Character_Name { get; set; }
        
        public abstract List<Die_Roll> Die_Rolls { get; }

        public CombatEventList Children { get => _Children; }

        public CombatEvent(int inID, int inCombatID, string line) { _ID = inID;  _Combat_ID = inCombatID;  Parse(line); }
        
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

        public ScrollViewer New_Windows_Table(string title, string[,] array_to_display, int num_of_columns = 1, int MinWidth = 0)
        {
            Grid outer_grid = new Grid();
            if (MinWidth != 0) outer_grid.MinWidth = MinWidth;
            
            for (int x=0; x<num_of_columns; x++) { outer_grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10, GridUnitType.Star) }); }

            if (title != "")
            {
                outer_grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
                TextBlock title_tb = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Center };
                title_tb.Inlines.Add(new Run(title) { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                Grid.SetRow(title_tb, outer_grid.RowDefinitions.Count - 1);
                Grid.SetColumn(title_tb, 0);
                Grid.SetColumnSpan(title_tb, num_of_columns);
                outer_grid.Children.Add(title_tb);
            }

            int num_items_per_col = (array_to_display.GetUpperBound(0)+1) / num_of_columns;
            if ( ((array_to_display.GetUpperBound(0) + 1) % num_of_columns) != 0) { num_items_per_col++; }

            outer_grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });

            for (int col = 0; col < num_of_columns; col++)
            {
                Grid new_win_grid = Create_A_Windows_Table(array_to_display, num_items_per_col * col, num_items_per_col * (col+1) - 1);
                Grid.SetRow(new_win_grid, outer_grid.RowDefinitions.Count - 1);
                Grid.SetColumn(new_win_grid, col);
                outer_grid.Children.Add(new_win_grid);
            }

            ScrollViewer scrollViewer = new ScrollViewer() { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
            scrollViewer.Content = outer_grid;

            return scrollViewer;
        }

        public Grid Update_A_Windows_Table(Grid inVery_Outer_Grid, string title, string[,] array_to_display, int num_of_columns = 1)
        {
            Grid outer_grid = null;

            int num_items_per_col = (array_to_display.GetUpperBound(0) + 1) / num_of_columns;
            if (((array_to_display.GetUpperBound(0) + 1) % num_of_columns) != 0) { num_items_per_col++; }

            foreach (UIElement title_search_elem in inVery_Outer_Grid.Children)
            {
                UIElement tmp_elem = title_search_elem;

                if (tmp_elem is ScrollViewer)
                {
                    if (((ScrollViewer)tmp_elem).Content is UIElement)
                    {
                        tmp_elem = (UIElement)((ScrollViewer)tmp_elem).Content;
                    }
                }

                if (tmp_elem is Grid)
                {
                    Grid title_search_inner_grid = (Grid)tmp_elem;

                    foreach (UIElement title_search_inner_grid_elem in title_search_inner_grid.Children)
                    {
                        if (title_search_inner_grid_elem is TextBlock)
                        {
                            foreach (Inline curr_inline in ((TextBlock)title_search_inner_grid_elem).Inlines)
                            {
                                if (curr_inline is Run)
                                {
                                    if ( ((Run)curr_inline).Text == title )
                                    {
                                        outer_grid = title_search_inner_grid;
                                        break;
                                    }
                                }
                            }

                            if (outer_grid != null) { break; }
                        }
                    }
                }

                if (outer_grid != null) { break; }
            }

            if (outer_grid == null) { throw new System.Exception("Cannot find grid"); }

            foreach (UIElement outer_curr_elem in outer_grid.Children)
            {
                if (outer_curr_elem is Grid)
                {
                    Grid middle_grid = (Grid)outer_curr_elem;
                    int middle_row = Grid.GetRow(middle_grid);
                    int middle_col = Grid.GetColumn(middle_grid);

                    foreach (UIElement inner_curr_elem in middle_grid.Children)
                    {
                        if (inner_curr_elem is TextBlock)
                        {
                            TextBlock tb = (TextBlock)inner_curr_elem;
                            int inner_row = Grid.GetRow(tb);
                            int inner_col = Grid.GetColumn(tb);

                            int array_row = (middle_col * num_items_per_col) + inner_row;
                            int array_col = inner_col;

                            tb.Text = array_to_display[array_row, array_col];
                        }
                    }
                }
            }

            return outer_grid;
        }

        public Grid Create_A_Windows_Table(string[,] inArray, int start_row = 0, int end_row = int.MaxValue)
        {
            if (end_row > inArray.GetUpperBound(0)) { end_row = inArray.GetUpperBound(0); }

            Grid outer_grid = new Grid();

            for (int col = 0; col <= inArray.GetUpperBound(1); col++) 
            {
                outer_grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10, GridUnitType.Star) });
            }

            for (int row = start_row; row <= end_row; row++)
            {
                outer_grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            }

            for (int row = start_row; row <= end_row; row++)
            {
                Line top_border = new Line() { StrokeThickness = 1, Stroke = Brushes.Black, VerticalAlignment = VerticalAlignment.Top };
                BindingOperations.SetBinding(top_border, Line.X2Property, new Binding("ActualWidth") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });

                Grid.SetRow(top_border, row-start_row);
                Grid.SetColumn(top_border, 0);
                Grid.SetColumnSpan(top_border, inArray.GetUpperBound(1) + 1);
                outer_grid.Children.Add(top_border);

                if (row == end_row)
                {
                    Line bottom_border = new Line { StrokeThickness = 1, Stroke = Brushes.Black, VerticalAlignment = VerticalAlignment.Bottom };
                    BindingOperations.SetBinding(bottom_border, Line.X2Property, new Binding("ActualWidth") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });

                    Grid.SetRow(bottom_border, row - start_row);
                    Grid.SetColumn(bottom_border, 0);
                    Grid.SetColumnSpan(bottom_border, inArray.GetUpperBound(1) + 1);
                    outer_grid.Children.Add(bottom_border);
                }

                for (int col = 0; col <= inArray.GetUpperBound(1); col++)
                {
                    if (col == 99)
                    {
                        Line right_border = new Line() { StrokeThickness = 1, Stroke = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Right };
                        BindingOperations.SetBinding(right_border, Line.Y2Property, new Binding("ActualHeight") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });

                        Grid.SetRow(right_border, 0);
                        Grid.SetColumn(right_border, row - start_row);
                        Grid.SetRowSpan(right_border, inArray.GetUpperBound(0) + 1);
                        outer_grid.Children.Add(right_border);
                    }
                    TextBlock tb = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Text = inArray[row, col] };
                    Grid.SetRow(tb, row - start_row);
                    Grid.SetColumn(tb, col);
                    outer_grid.Children.Add(tb);
                }
            }

            return outer_grid;
        }

        public WebBrowser New_WebBrowser()
        {
            WebBrowser webBrowser = new WebBrowser() { VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };
            webBrowser.LoadCompleted += (s, nav_e) =>
            {
                string script = "document.body.style.whiteSpace = 'nowrap'";
                WebBrowser wb = (WebBrowser)s;
                wb.InvokeScript("execScript", new Object[] { script, "JavaScript" });
            };

            return webBrowser;
        }

        public int InsertGridRow(Grid inGrid, int new_row_indx)
        {
            inGrid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });
            foreach (UIElement curr_child in inGrid.Children)
            {
                if (Grid.GetRow(curr_child) >= new_row_indx) { Grid.SetRow(curr_child, Grid.GetRow(curr_child) + 1); }
            }

            return new_row_indx;
        }

        public string Filter_String_For_WebBrowser(string inStr)
        {
            return inStr.Replace("–", "-").Replace("—", "--").Replace("×", "x");
        }

        protected string Character_Type_To_String(Char_Enum inCharType)
        {
            string rtn = "";
            if (inCharType == Char_Enum.Friendly) { rtn = "Friendly"; }
            else if (inCharType == Char_Enum.Hostile) { rtn = "Hostile"; }
            else if (inCharType == Char_Enum.Summon) { rtn = "Summon"; }
            else if (inCharType == Char_Enum.Unknown) { rtn = "Unknown"; }
            else if (inCharType == Char_Enum.Really_Unknown) { rtn = "Really Unknown"; }
            else rtn = "???";

            return rtn;

        }
    }
}
