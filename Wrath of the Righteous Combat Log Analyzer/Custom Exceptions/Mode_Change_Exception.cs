using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    class Mode_Change_Exception: System.Exception
    {
        public Mode_Change_Exception(string message):base(message)
        {

        }
    }
}
