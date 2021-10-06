using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
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
    class LoadingAwareTreeViewItem: TreeViewItem
    {
        private bool _IsLoading = false;
        private Brush _SavedBrush = null;

        public bool IsLoading
        {
            get
            {
                return _IsLoading;
            }
            set
            {
                if ((_IsLoading) && (!value))
                {
                    if (_SavedBrush != null) { Foreground = _SavedBrush; _SavedBrush = null; }
                    else { Foreground = Brushes.Black; }
                }
                else if ((!_IsLoading)&&(value))
                {
                    if (_SavedBrush == null) { _SavedBrush = Foreground; }
                    Foreground = SystemColors.GrayTextBrush;
                }

                _IsLoading = value;
            }
        }

        public bool ContainsSelected()
        {
            if (IsSelected) { return true; }

            foreach (LoadingAwareTreeViewItem curr_elem in Items)
            {
                if (curr_elem.ContainsSelected()) { return true; }
            }

            return false;
        }
    }
}
