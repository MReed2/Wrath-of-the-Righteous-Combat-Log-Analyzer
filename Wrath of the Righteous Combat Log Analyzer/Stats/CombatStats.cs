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

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    public class CombatStats
    {
        // private StringBuilder _Rolls_CSV = new StringBuilder();

        private Grid _Frequency_Analysis_WinGrid = null;
        private Grid _Streak_Analysis_WinGrid = null;
        private Grid _Streak_Length_Analysis_WinGrid = null;
        private Grid _To_Hit_Margin_Analysis_WinGrid = null;
        private Grid _Crit_Confirmation_Margin_Analysis_WinGrid = null;
        private Grid _Misc_WinGrid = null;
        private UserControl _Analysis_UserControl = null;

        public StringBuilder Rolls_CSV
        {
            get
            {
                StringBuilder tmp_sb = new StringBuilder(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}\n", "ID", "Type", "Num Dice", "Dice Type", "Bonus", "Roll", "Roll Required", "Character Name", "Cnt"));
                tmp_sb.Append(_Stats_Categories[0].Rolls_CSV); // 0 = All
                return tmp_sb;
            }
        }
        public UserControl Analysis_UserControl { get => _Analysis_UserControl; set => _Analysis_UserControl = value; }
        public bool Tables_Stale
        {
            get { foreach (Stats_Instance curr_stat in _Stats_Categories) { if (curr_stat.Tables_Stale) { return true; } } return false; }
        }

        private List<Stats_Instance> _Stats_Categories = new List<Stats_Instance>();

        public UserControl Get_Analysis_UserControl()
        {
            if (_Analysis_UserControl == null)
            {
                _Analysis_UserControl = new UserControl();

                Grid outer_grid = new Grid();
                outer_grid.Background = System.Windows.Media.Brushes.AliceBlue;

                outer_grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });
                outer_grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new System.Windows.GridLength(100, System.Windows.GridUnitType.Auto) });

                _Misc_WinGrid = Add_Windows_Table(
                    outer_grid, 
                    "Miscellaneous Statistics", 
                    Merge_All_Arrays_With_Titles((x) => x.Misc_Analysis));

                _Frequency_Analysis_WinGrid = Add_Windows_Table(
                    outer_grid, 
                    "Frequency Analysis", 
                    Merge_All_Arrays_With_Titles((x)=>x.Frequency_Analysis),
                    1,
                    Merge_All_Arrays_With_Titles((x) => x.Frequency_Analysis).GetUpperBound(0) - 2);

                _Streak_Analysis_WinGrid = Add_Windows_Table(
                    outer_grid, 
                    "Streak Analysis", 
                    Merge_All_Arrays_With_Titles((x) => x.Streak_Analysis));

                _Streak_Length_Analysis_WinGrid = Add_Windows_Table(
                    outer_grid, 
                    "Streak Length Anlysis", 
                    Find_Stat_By_Catagory_Name("All").Streak_Length_Analysis);

                _To_Hit_Margin_Analysis_WinGrid = Add_Windows_Table(
                    outer_grid, 
                    "To Hit Margin Analysis", 
                    Merge_All_Arrays_With_Titles((x) => x.Attack_Margin_Analysis),
                    1,
                    Merge_All_Arrays_With_Titles((x) => x.Attack_Margin_Analysis).GetUpperBound(0) - 1);

                _Crit_Confirmation_Margin_Analysis_WinGrid = Add_Windows_Table(
                    outer_grid, 
                    "Critical Confirmation Margin Analysis", 
                    Merge_All_Arrays_With_Titles((x) => x.Crit_Confirmation_Margin_Analysis),
                    1, 
                    Merge_All_Arrays_With_Titles((x) => x.Crit_Confirmation_Margin_Analysis).GetUpperBound(0) - 1);

                _Analysis_UserControl.Content = outer_grid;
            }

            return _Analysis_UserControl;
        }

        private Grid Add_Windows_Table(Grid outer_grid, string title, string[,] array_to_display)
        {
            outer_grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            TextBlock title_tb = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Center };
            title_tb.Inlines.Add(new Run(title) { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
            Grid.SetRow(title_tb, outer_grid.RowDefinitions.Count - 1);
            Grid.SetColumn(title_tb, 0);
            Grid.SetColumnSpan(title_tb, 2);
            outer_grid.Children.Add(title_tb);

            Button Copy_To_Clipboard_Button = new Button()
            {
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Right,
                Content = new TextBlock() { Text = "Steam" }
            };
            Copy_To_Clipboard_Button.Click += Copy_To_Steam_Clipboard_Button_Click;
            Grid.SetRow(Copy_To_Clipboard_Button, outer_grid.RowDefinitions.Count - 1);
            Grid.SetColumn(Copy_To_Clipboard_Button, 1);
            outer_grid.Children.Add(Copy_To_Clipboard_Button);

            outer_grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            Grid new_win_grid = Create_A_Windows_Table(array_to_display);
            Grid.SetRow(new_win_grid, outer_grid.RowDefinitions.Count - 1);
            Grid.SetColumn(new_win_grid, 0);
            Grid.SetColumnSpan(new_win_grid, 2);
            outer_grid.Children.Add(new_win_grid);

            Copy_To_Clipboard_Button.Tag = new_win_grid;

            return new_win_grid;
        }

        private void Copy_To_Steam_Clipboard_Button_Click(object sender, RoutedEventArgs e)
        {
            Button tmp = sender as Button;
            Grid grd = tmp.Tag as Grid;
            string str = Windows_Grid_To_Steam_Table(grd);
            Clipboard.SetText(str);
        }

        private Grid Add_Windows_Table(Grid outer_grid, string title, string[,] array_to_display, int first_graph_row, int last_graph_row)
        {
            Grid new_win_grid = Add_Windows_Table(outer_grid, title, array_to_display);

            Create_A_Windows_Graph(new_win_grid, first_graph_row, last_graph_row);

            return new_win_grid;
        }

        public void Update_Analysis_UserControl()
        {
            if (_Analysis_UserControl == null)
            {
                return;
            }
            if ((_Frequency_Analysis_WinGrid == null) || (_Streak_Analysis_WinGrid == null))
            {
                throw new System.Exception("Analysis grids have not been created");
            }

            if (Tables_Stale)
            {
                Clear_Tables();
                Build_Tables();
            }

            Populate_A_Windows_Table(_Misc_WinGrid, Merge_All_Arrays_With_Titles((x) => x.Misc_Analysis));
            Populate_A_Windows_Table(_Frequency_Analysis_WinGrid, Merge_All_Arrays_With_Titles((x) => x.Frequency_Analysis));
            Populate_A_Windows_Table(_Streak_Analysis_WinGrid, Merge_All_Arrays_With_Titles((x) => x.Streak_Analysis));
            Populate_A_Windows_Table(_Streak_Length_Analysis_WinGrid, Find_Stat_By_Catagory_Name("All").Streak_Length_Analysis);
            Populate_A_Windows_Table(_To_Hit_Margin_Analysis_WinGrid, Merge_All_Arrays_With_Titles((x) => x.Attack_Margin_Analysis));
            Populate_A_Windows_Table(_Crit_Confirmation_Margin_Analysis_WinGrid, Merge_All_Arrays_With_Titles((x) => x.Crit_Confirmation_Margin_Analysis));

            Update_A_Windows_Graph(
                _Frequency_Analysis_WinGrid, // Control
                Find_Stat_By_Catagory_Name("All").D20s, // Data
                +1, // Data offset -- in the D20s array, the first 0 (row 0) contains no data.
                1, // Starting row in WinGrid
                Find_Stat_By_Catagory_Name("All").Frequency_Analysis.GetUpperBound(0) - 2 // Ending row in WinGrid
                );

            Update_A_Windows_Graph(
                _To_Hit_Margin_Analysis_WinGrid, // Control
                Find_Stat_By_Catagory_Name("All").Attack_Margin, // Data
                0, // data offset
                1, // Starting row in WinGrid
                Find_Stat_By_Catagory_Name("All").Attack_Margin_Analysis.GetUpperBound(0) - 1 // ending row in WinGrid
                );

            Update_A_Windows_Graph(
                _Crit_Confirmation_Margin_Analysis_WinGrid, // Control
                Find_Stat_By_Catagory_Name("All").Crit_Confirmation_Margin, // Data
                0, // data offset
                1, // Stargin trow in WinGrid
                Find_Stat_By_Catagory_Name("All").Crit_Confirmation_Margin_Analysis.GetUpperBound(0) - 1 // Ending row in WinGrid.
                );
        }

        internal void Credit_Kill(string inKiller_Name, int inExp, string inKilled_Name, int inID)
        {
            if (inExp == 0)
            {
                System.Diagnostics.Debug.WriteLine("No experience found to match with death of \"{0}\", death ID: {1}", inKilled_Name, inID);
            }
            else if (inKiller_Name == null)
            {
                System.Diagnostics.Debug.WriteLine("No damage record found to match with death of \"{0}\", death ID: {1}", inKilled_Name, inID);
            }
            else
            {
                foreach (Stats_Instance curr_stat in _Stats_Categories)
                {
                    if (
                        (curr_stat.Type_Description == "All") ||
                        (curr_stat.Type_Description == "Friendly") ||
                        (curr_stat.Type_Description == inKiller_Name)
                        )
                    {
                        curr_stat.Credit_Kill(inKiller_Name, inExp, inKiller_Name);

                    }
                }
            }
        }

        private Stats_Instance Find_Stat_By_Catagory_Name(string inCatagory_Name)
        {
            Stats_Instance rtn = null;
            foreach (Stats_Instance curr_stat in _Stats_Categories) { if (curr_stat.Type_Description == inCatagory_Name) { rtn = curr_stat; break; } }
            return rtn;
        }

        public CombatStats()
        {
            Clear_Stats();
        }

        public void Clear_Stats()
        {
            _Stats_Categories.Clear();

            _Stats_Categories.Add(new Stats_Instance("All"));
            _Stats_Categories.Add(new Stats_Instance("Hostile"));
            _Stats_Categories.Add(new Stats_Instance("Friendly"));
            _Stats_Categories.Add(new Stats_Instance("Summons"));
        }

        private void Clear_Tables()
        {
            foreach (Stats_Instance curr_stat in _Stats_Categories)
            {
                curr_stat.Clear_Analysis_Only();
            }
        }

        public void Recalculate_Stats(CombatEventList inEvents)
        {
            Clear_Stats();
            Process_Events(inEvents);
            Build_Tables();
        }

        public void Process_Events(CombatEventList inEvents)
        {
            lock (CombatLog_Parser.CombatLog)
            {
                foreach (CombatEvent curr_Event in inEvents)
                {
                    Process_Event(curr_Event);
                }
            }
        }

        public void Process_Event(CombatEvent inEvent)
        {
            foreach (Stats_Instance curr_stat in _Stats_Categories)
            {
                if (Qualifies_For_Category(inEvent, curr_stat)) { curr_stat.Process_Event(inEvent); }
            }
        }

        public bool Qualifies_For_Category(CombatEvent inEvent, Stats_Instance inStats_Catagory)
        {
            if (inStats_Catagory.Type_Description == "All") { return true; }
            if ((inStats_Catagory.Type_Description == "Hostile") && (inEvent.Character_Type == CombatEvent.Char_Enum.Hostile)) { return true; }
            if ((inStats_Catagory.Type_Description == "Friendly") && (inEvent.Character_Type == CombatEvent.Char_Enum.Friendly)) { return true; }
            if ((inStats_Catagory.Type_Description == "Summons") && (inEvent.Character_Type == CombatEvent.Char_Enum.Summon)) { return true; }

            return false;
        }

        public string Get_Steam_Tables()
        {
            if (Tables_Stale)
            {
                Clear_Tables();
                Build_Tables();
            }

            if (_Misc_WinGrid == null)
            {
                Get_Analysis_UserControl();
                Update_Analysis_UserControl();
            }

            return
                "[u]Misc[/u]\n" + Windows_Grid_To_Steam_Table(_Misc_WinGrid) +
                "[u]Frequency Analysis[/u]\n" + Windows_Grid_To_Steam_Table(_Frequency_Analysis_WinGrid) +
                "[u]Streak Analysis[/u]\n" + Windows_Grid_To_Steam_Table(_Streak_Analysis_WinGrid) +
                "[u]Streak Length Analysis[/u]" + Windows_Grid_To_Steam_Table(_Streak_Analysis_WinGrid) +
                "[u]To Hit Analysis[/u]" + Windows_Grid_To_Steam_Table(_To_Hit_Margin_Analysis_WinGrid) +
                "[u]Crit Confirmation Analsyis[/u]" + Windows_Grid_To_Steam_Table(_Crit_Confirmation_Margin_Analysis_WinGrid);
        }

        public void Build_Tables()
        {
            foreach (Stats_Instance curr_stat in _Stats_Categories) { curr_stat.Build_Analysis(); }
        }

        public void Build_Tables(System.ComponentModel.BackgroundWorker bw)
        {
            int cnt = 0;

            foreach (Stats_Instance curr_stat in _Stats_Categories)
            {
                bw.ReportProgress(cnt / _Stats_Categories.Count);
                curr_stat.Build_Analysis();
            }
        }

        private string[,] Merge_All_Arrays_With_Titles(Func<Stats_Instance, string[,]> inProperty)
        {
            int num_of_rows = 0;
            int num_of_cols = 0;

            if (_Stats_Categories.Count > 0)
            {
                num_of_rows = inProperty(_Stats_Categories[0]).GetUpperBound(0);
                /* This is a mess....
                 * 
                 * Given that we have at least one catagory we need the number of columns in that catagory (inProperty(_Stats_Categories[0]).GetUpperBound(1))
                 * Then we get that same number again, subtract 1 because we won't reproduce the row titles, then multiply by the number of catagories -- also minus 1,
                 * because we've already included the first.
                 * 
                 * It works... :)
                 * 
                 */
                num_of_cols = inProperty(_Stats_Categories[0]).GetUpperBound(1) + 1; // Display the first array fully (all columns)
                int num_of_cols_per_dataset_after_First = inProperty(_Stats_Categories[0]).GetUpperBound(1); // Skip one column after the first
                int num_of_datasets_after_first = _Stats_Categories.Count - 1;
                int num_of_additional_cols_required = num_of_cols_per_dataset_after_First * num_of_datasets_after_first;
                num_of_cols += num_of_additional_cols_required;
            }

            string[,] rtnArray = new string[num_of_rows+1, num_of_cols];

            for (int x=0; x<=rtnArray.GetUpperBound(0); x++)
                for (int y=0; y<=rtnArray.GetUpperBound(1); y++)
                {
                    rtnArray[x, y] = null;
                }

            int loop_cnt = 0;
            
            foreach (Stats_Instance curr_stat in _Stats_Categories)
            {
                for (int x=0; x<=num_of_rows; x++)
                {
                    int start_y = 0;
                    int end_y = 0;
                    int offset_y = 0;

                    if (loop_cnt == 0) { start_y = 0; end_y = inProperty(_Stats_Categories[0]).GetUpperBound(1); offset_y = 0; }
                    else
                    {
                        start_y = inProperty(_Stats_Categories[0]).GetUpperBound(1)+1;
                        start_y += (inProperty(_Stats_Categories[0]).GetUpperBound(1) * (loop_cnt-1));
                        end_y = start_y + inProperty(_Stats_Categories[0]).GetUpperBound(1)-1;
                        offset_y = 1;
                    }

                    for (int y=start_y; y<=end_y; y++)
                    {
                        rtnArray[x, y] = inProperty(curr_stat)[x, y - start_y + offset_y].Replace("*_*", curr_stat.Type_Description);
                    }
                }

                loop_cnt++;
            }

            return rtnArray;
        }
        
        private string String_Array_To_Steam_Table(string[,] inArray)
        {
            StringBuilder str = new StringBuilder("[table]");

            for (int x = 0; x <= inArray.GetUpperBound(0); x++)
            {
                str.Append("[tr]");

                for (int y = 0; y <= inArray.GetUpperBound(1); y++)
                {
                    if (x == 0) { str.Append("[th]"); }
                    else { str.Append("[td]"); }
                    str.Append(inArray[x, y]);
                    if (x == 0) { str.Append("[/th]"); }
                    else { str.Append("[/td]"); }
                }
                str.Append("[/tr]\n");
            }

            str.Append("[/table]");

            return str.ToString();
        }

        public string Windows_Grid_To_Steam_Table(Grid inGrid)
        {
            string[,] tmp_array = new string[inGrid.RowDefinitions.Count, inGrid.ColumnDefinitions.Count-1];

            for (int x=0;x<=tmp_array.GetUpperBound(0); x++)
            {
                for (int y=0;y<=tmp_array.GetUpperBound(1);y++)
                {
                    tmp_array[x, y] = "";
                }
            }

            foreach (UIElement curr_elem in inGrid.Children)
            {
                if (curr_elem is TextBlock)
                {
                    int curr_row = Grid.GetRow(curr_elem);
                    int curr_col = Grid.GetColumn(curr_elem);
                    TextBlock curr_tb = ((TextBlock)curr_elem);

                    if ((curr_tb.Tag is string) && ( ((string)curr_tb.Tag) == "Tick"))
                    { }
                    else
                    {
                        if (curr_tb.Inlines.Count == 0)
                        {
                            tmp_array[curr_row, curr_col] += curr_tb.Text.Trim();
                        }
                        else
                        {
                            foreach (Run curr_run in ((TextBlock)curr_elem).Inlines)
                            {
                                tmp_array[curr_row, curr_col] += curr_run.Text.Trim();
                            }
                        }
                    }
                }
            }

            return String_Array_To_Steam_Table(tmp_array);
        }

        private Grid Create_A_Windows_Table(string[,] inArray)
        {
            Grid outer_grid = new Grid();

            for (int col=0; col <= inArray.GetUpperBound(1)+1; col++) // This is *intentionally* one greater than it should be -- this column is available for use as a graph, if desired.  If not, it is invisible.
            {
                outer_grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) } );
            }

            for (int row=0; row <= inArray.GetUpperBound(0); row++)
            {
                outer_grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) } );
            }

            for (int row=0; row <= inArray.GetUpperBound(0); row++)
            {
                Line top_border = new Line() { StrokeThickness = 1, Stroke = Brushes.Black, VerticalAlignment = VerticalAlignment.Top };
                BindingOperations.SetBinding(top_border, Line.X2Property, new Binding("ActualWidth") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });

                Grid.SetRow(top_border, row);
                Grid.SetColumn(top_border, 0);
                Grid.SetColumnSpan(top_border, inArray.GetUpperBound(1)+1);
                outer_grid.Children.Add(top_border);

                if (row == inArray.GetUpperBound(0))
                {
                    Line bottom_border = new Line { StrokeThickness = 1, Stroke = Brushes.Black, VerticalAlignment = VerticalAlignment.Bottom };
                    BindingOperations.SetBinding(bottom_border, Line.X2Property, new Binding("ActualWidth") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });

                    Grid.SetRow(bottom_border, row);
                    Grid.SetColumn(bottom_border, 0);
                    Grid.SetColumnSpan(bottom_border, inArray.GetUpperBound(1)+1);
                    outer_grid.Children.Add(bottom_border);
                }

                for (int col=0; col <= inArray.GetUpperBound(1); col++)
                {
                    if (col == 99)
                    {
                        Line right_border = new Line() { StrokeThickness = 1, Stroke = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Right };
                        BindingOperations.SetBinding(right_border, Line.Y2Property, new Binding("ActualHeight") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });

                        Grid.SetRow(right_border, 0);
                        Grid.SetColumn(right_border, row);
                        Grid.SetRowSpan(right_border, inArray.GetUpperBound(0)+1);
                        outer_grid.Children.Add(right_border);
                    }
                    TextBlock tb = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    Grid.SetRow(tb, row);
                    Grid.SetColumn(tb, col);
                    outer_grid.Children.Add(tb);
                }
            }

            return outer_grid;
        }

        private void Populate_A_Windows_Table(Grid toPopulate, string [,] inArray)
        {
            for (int row=0; row<inArray.GetUpperBound(0)+1; row++)
            {
                for (int col=0; col<inArray.GetUpperBound(1)+1; col++)
                {
                    List<System.Windows.UIElement> tmpLst = Find_All_Controls_In_Grid_By_Row_And_Col(toPopulate, row, col);

                    foreach (UIElement tmp in tmpLst)
                    {
                        if (tmp is TextBlock)
                        {
                            TextBlock tb = (TextBlock)tmp;
                            string txt = "";
                            if (inArray[row,col] == null) { inArray[row, col] = "***"; }
                            foreach (string tmp_str in System.Text.RegularExpressions.Regex.Replace(inArray[row, col], @"\[(.*?)\]", "").Split('\n'))
                            {
                                txt += "  " + tmp_str + "  \n";
                            }
                            char[] trim_chars = { '\n' };
                            txt = txt.TrimEnd(trim_chars);

                            tb.Inlines.Clear();
                            if ((row == 0) || (col == 0))
                            {
                                tb.Inlines.Add(new Run(txt) { FontWeight = System.Windows.FontWeights.Bold });
                            }
                            else
                            {
                                tb.Inlines.Add(new Run(txt));
                            }
                        }
                    }
                }
            }
        }

        private void Create_A_Windows_Graph(Grid toPopulate, int first_row, int last_row)
        {
            int col_to_draw_in = toPopulate.ColumnDefinitions.Count;

            for (int row = first_row; row <= last_row; row++)
            {
                Rectangle x = new Rectangle()
                {
                    Stroke = Brushes.Black,
                    Fill = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 300,
                    MaxWidth = 300,
                    Height = 10
                };
                //BindingOperations.SetBinding(x, Rectangle.HeightProperty, new Binding("ActualHeight") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });
                Grid.SetRow(x, row);
                Grid.SetColumn(x, col_to_draw_in);
                toPopulate.Children.Add(x);
            }

            for (int pixel_offset = 0; pixel_offset <= 300; pixel_offset += 50)
            {
                Canvas label_canvas = new Canvas();
                Grid.SetRow(label_canvas, last_row + 1);
                Grid.SetColumn(label_canvas, col_to_draw_in);
                toPopulate.Children.Add(label_canvas);

                Line vertical_line = new Line() // X is horizontal, Y is vertical.
                {
                    StrokeThickness = 1,
                    Stroke = Brushes.Green,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    X1 = pixel_offset,
                    X2 = pixel_offset,
                    Y1 = 1
                };
                // BindingOperations.SetBinding(vertical_line, Line.Y2Property, new Binding("ActualHeight") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });

                BindingOperations.SetBinding(vertical_line, Line.Y2Property, new Binding("ActualHeight") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });
                Grid.SetRow(vertical_line, first_row);
                Grid.SetColumn(vertical_line, col_to_draw_in);
                Grid.SetRowSpan(vertical_line, last_row - first_row + 1);
                toPopulate.Children.Add(vertical_line);

                if (pixel_offset > 0)
                {
                    TextBlock label = new TextBlock();

                    Canvas.SetLeft(label, pixel_offset);
                    Canvas.SetTop(label, 0);

                    label.Text = "Test";
                    label.Tag = "Tick";

                    label_canvas.Children.Add(label);
                    vertical_line.Tag = label;
                }

            }
        }

        private void Update_A_Windows_Graph(Grid toPopulate, int[] inArray, int data_offset, int first_row, int last_row)
        {
            int min_value = int.MaxValue;
            int max_value = int.MinValue;

            for (int x=data_offset; x <= inArray.GetUpperBound(0); x++)
            {
                if (inArray[x] < min_value) { min_value = inArray[x]; }
                if (inArray[x] > max_value) { max_value = inArray[x]; }
            }

            max_value += (max_value / 20); // This 20 has nothing to do with d20 -- its just ensures a small gap on the right hand side of the graph.

            int column_to_draw_in = toPopulate.ColumnDefinitions.Count;

            Rectangle[] rects = new Rectangle[last_row-first_row+1+data_offset];

            for (int row = first_row; row <= last_row; row++)
            {
                foreach (UIElement curr_elem in Find_All_Controls_In_Grid_By_Row_And_Col(toPopulate, row, column_to_draw_in))
                {
                    if (curr_elem is Rectangle) { rects[row - first_row] = (Rectangle)Find_Control_In_Grid_By_Row_And_Col(toPopulate, row, column_to_draw_in); }
                }
            }

            for (int x=data_offset; x<=inArray.GetUpperBound(0); x++)
            {
                rects[x-data_offset].Width = ( ((double)inArray[x]) / ((double)max_value) ) * rects[x-data_offset].MaxWidth;
            }

            List<System.Windows.UIElement> ctrls = Find_All_Controls_In_Grid_By_Row_And_Col(toPopulate, first_row, column_to_draw_in);

            foreach (UIElement ctrl in ctrls)
            {
                if (ctrl is Line)
                {
                    Line curr_line = (Line)ctrl;
                    if (curr_line.Tag != null)
                    {
                        TextBlock curr_textblock = (TextBlock)curr_line.Tag;

                        double exact_tickmark_value = (curr_line.X1 / rects[0].MaxWidth) * max_value;
                        int approx_tickmark_value = (int)Math.Round(exact_tickmark_value);
                        curr_textblock.Text = exact_tickmark_value.ToString("N1");

                        curr_textblock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                        double new_left_offset = curr_line.X1 - ((curr_textblock.DesiredSize.Width) / 2);
                        
                        Canvas.SetLeft(curr_textblock, new_left_offset);
                    }
                }
            }
        }

        private System.Windows.UIElement Find_Control_In_Grid_By_Row_And_Col(Grid toSearch, int row, int col)
        {
            foreach (System.Windows.UIElement curr_child in toSearch.Children)
            {
                int curr_row = Grid.GetRow(curr_child);
                int curr_col = Grid.GetColumn(curr_child);
                //System.Diagnostics.Debug.WriteLine("({0}, {1})", curr_row, curr_col);
                if ((curr_row == row) && (curr_col == col))
                {
                    return curr_child;
                }
            }

            return null;
        }

        private List<System.Windows.UIElement> Find_All_Controls_In_Grid_By_Row_And_Col(Grid toSearch, int row, int col)
        {
            List<UIElement> rtn = new List<UIElement>();

            foreach (UIElement curr_child in toSearch.Children)
            {
                int curr_row = Grid.GetRow(curr_child);
                int curr_col = Grid.GetColumn(curr_child);
                //System.Diagnostics.Debug.WriteLine("({0}, {1})", curr_row, curr_col);
                if ((curr_row == row) && (curr_col == col))
                {
                    rtn.Add(curr_child);
                }
            }

            return rtn;
        }
    }
}
