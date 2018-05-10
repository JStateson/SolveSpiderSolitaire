using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
namespace spider
{
    public class cTest
    {
        private cSpinControl cSC;
        private cStrategy Strategy;

        public cTest(ref cSpinControl cSC, ref cStrategy Strategy)
        {
            this.Strategy = Strategy;
            this.cSC = cSC;
        }

        public void RunTest() //ref board tb)
        {
            //Strategy.ExposeTop(ref tb);
            //tb.ShowBoard();
            int ALL_AVAILABLE = Convert.ToInt32("11111111111110", 2);
            
        }

    }
}
