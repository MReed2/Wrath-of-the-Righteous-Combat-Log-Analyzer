using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    class CombatStartEvent : CombatEventContainer
    {
        #region Standard Fields
        private List<Die_Roll> _Die_Rolls = new List<Die_Roll>();
#endregion
        #region Override Properties
        public override string Character_Name { get => ""; set => throw new NotImplementedException(); }
        public override string Source_Character_Name => throw new NotImplementedException();
        public override List<Die_Roll> Die_Rolls => _Die_Rolls;
        #endregion
        #region Override Methods
        public override List<Die_Roll> Parse(string line)
        {
            return _Die_Rolls;
        }

        public CombatStartEvent(int inID, string inLine) : base(inID, inLine) { }
        #endregion

        private UserControl _UC_For_Display = null;
        
        public override UserControl Get_UserControl_For_Display()
        {
            if (_UC_For_Display == null)
            {
                _UC_For_Display = base.Get_UserControl_For_Display();
                Grid outer_grid = (Grid)_UC_For_Display.Content;
                outer_grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });

                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new System.Windows.GridLength(0, System.Windows.GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new System.Windows.GridLength(10, System.Windows.GridUnitType.Star) });

                Grid.SetRow(grid, 3);
                Grid.SetColumn(grid, 0);
                Grid.SetColumnSpan(grid, 2);
                outer_grid.Children.Add(grid);

                string[] label_list = {
                "Event Type",
                "Number of children"
                };

                int row = 0;

                foreach (string curr_label in label_list)
                {
                    RowDefinition r = new RowDefinition();
                    r.Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto);
                    grid.RowDefinitions.Add(r);

                    if (curr_label != "")
                    {
                        Label lbl = new Label()
                        {
                            Content = curr_label,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                        };

                        Grid.SetColumn(lbl, 0);
                        Grid.SetRow(lbl, row);

                        TextBox tb = new TextBox();
                        switch (curr_label)
                        {
                            case "Event Type": tb.Text = "Combat Start"; break;
                            case "Number of children": tb.Text = Children.Count.ToString(); break;
                        }
                        tb.IsReadOnly = true;
                        Grid.SetColumn(tb, 1);
                        Grid.SetRow(tb, row);

                        grid.Children.Add(lbl);
                        grid.Children.Add(tb);
                    }

                    row++;
                }
                //I'm not sure why this is necesary, but it is.
                grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(0, System.Windows.GridUnitType.Auto) });

                RowDefinition source_r = new RowDefinition();
                source_r.Height = new System.Windows.GridLength(50, System.Windows.GridUnitType.Star);
                grid.RowDefinitions.Add(source_r);

                Label source_lbl = new Label();
                source_lbl.Content = "Source";
                Grid.SetColumn(source_lbl, 0);
                Grid.SetRow(source_lbl, row);

                WebBrowser wb = new WebBrowser()
                {
                    MaxHeight = 750
                };

                System.Text.StringBuilder long_str = new StringBuilder(Source_With_ID.Replace("–", "-").Replace("—", "--").Replace("×", "x"));

                foreach (CombatEvent curr_event in Children)
                {
                    long_str.Append(curr_event.Source_With_ID.Replace("–", "-").Replace("—", "--").Replace("×", "x"));
                }

                wb.NavigateToString(long_str.ToString());
                Grid.SetColumn(wb, 1);
                Grid.SetRow(wb, row);

                grid.Children.Add(source_lbl);
                grid.Children.Add(wb);
            }
            
            return _UC_For_Display;
        }
    }
}
