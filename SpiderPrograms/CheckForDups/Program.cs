using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckForDups
{
    class Program
    {
        static void Main(string[] args)
        {
            // The code provided will print ‘Hello World’ to the console.
            // Press Ctrl+F5 (or go to Debug > Start Without Debugging) to run your app.
            int iCnt = 0;
            List<ulong> lHex = new List<ulong>();
            List<string> strNames = new List<string>();
            
            do
            {
                string strIn = Console.ReadLine();
                if (strIn == null) break;
                int k = strIn.IndexOf(' ');
                ulong u = ulong.Parse(strIn.Substring(0,k-1), System.Globalization.NumberStyles.HexNumber);
                lHex.Add(u);
                strNames.Add(strIn.Substring(k+1));
            } while (true);

            int j = lHex.Count();
            for(int i = 1; i < j; i++)
            {
                iCnt++;
                if(lHex[i-1] == lHex[i])
                {
                    Console.WriteLine("Line:" + iCnt.ToString("d4") + " " +lHex[i].ToString("X10") + " " + strNames[i]);
                
                }
            }


            // Go to http://aka.ms/dotnet-get-started-console to continue learning how to build a console app! 
        }
    }
}
