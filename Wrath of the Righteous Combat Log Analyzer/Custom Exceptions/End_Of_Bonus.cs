using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrath_of_the_Righteous_Combat_Log_Analyzer
{
    class End_Of_Bonus: System.Exception
    {
        public End_Of_Bonus(string message):base(message)
        {
            
        }
    }
}
