using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;

/*
 * this sos scheme does not take into account that the exposed card might matche the suit as well as have
 * the proper rank to be unstacked.
 * */



// SEQ follows 
namespace spider
{
    public class cAssemblePattern
    {
        public string PatLetter;
        public int index;
        public card c;
        public cAssemblePattern(ref card c, int index)
        {
            this.c = c;
            this.index = index;
        }
    }


    // these moves are always relative to the bottom of the column and do not need to know the exact
    // position of the card to be moved.  is it always "nFrom" up from the bottom of the stack
    public class cLocationlessMoves
    {
        public int From;
        public int nFrom;
        public int To;
        public cLocationlessMoves(int from, int n, int to)
        {
            From = from;
            nFrom = n;
            To = to;
        }
    }

    public class cCardCountMoves
    {
        public card c;
        public int n;
        public cCardCountMoves(ref card nc, int nn)
        {
            c = nc;
            n = nn;
        }
    }

/*
1,0
2,0,1
2,1
3,0,(1,2)
3,1,1
3,1,2
3,2
4,0,(1,2,3)
4,1,(1,3)
4,1,2
4,2
5,0,(1,2,3,4)
5,1,(1,3)
5,2,1
5,2,2
5,2,3
5,2,4
5,3
6,0,(1,2,3,4,5)
6,1,(1,3)
6,2,2
6,2,3
6,2,4
6,2,(1,5)
6,3
7,0,(1,2,3,4,5,6)
7,1,(2,4,6)
7,1,(2,3,5)
7,1,(1,3,5)
7,2,(1,5)
7,2,(2,5)
7,2,3
7,2,4
7,3
*/

    public class cMoves
    {
        public string MoveID;
        // SC is source column, C3 is the column that can hold the 3rd series
        // above plus E0 is the first empty column, E2 the 2nd and above C0..C9
        // DC is the destination column
        //public card CardID;
        //// if source then iStack and iCard is the column and position
        //// if destination then only iStack is needed
        //// the above is filled in when the board is analyzed for moves
        public cMoves(string strID)
        {
            MoveID = strID;
            //CardID = new card();
        }
    }

  
    public class cDisStrategy
    {
        public int NumSeqSeries;
        public int nEmptyColumns;
        public List<int> Ranks;
        public List<cMoves> Moves;
    }

    public class cDisMoves
    {
        public string id_src;
        public string id_des;
        public cDisMoves(string src, string des)
        {
            id_src = src;
            id_des = des;
        }
    }

    public class csosDisStrategy
    {
        public int NumEmptyColumns;
        public bool bMustBeTop;         // when unstacking, the top must be 0 (no cards above)
        public int NumCards;
        public string Pattern;
        public column Collapsible;
        public string strCollapse;
        public int nCollapse;
        public List<cDisMoves> UnstackMoves = new List<cDisMoves>();

        public csosDisStrategy(int iNEC, int iNC, string strPattern, string strHoldCards, string strMoves)
        {
            string[] sSetMoves = strMoves.Split(',');
            string[] sOneMove;
            int i;
            NumEmptyColumns = Math.Abs(iNEC);
            NumCards = iNC;
            bMustBeTop = false;
            if (iNEC <= 0) bMustBeTop = true;
            Pattern = strPattern;
            if (strHoldCards != "")
            {
                nCollapse = Convert.ToInt32(strHoldCards.Substring(0, 1));
                Collapsible = new column();
                strCollapse = strHoldCards.Substring(1);
            }
            for (i = 0; i < sSetMoves.Length; i++)
            {
                sOneMove = sSetMoves[i].Split('-');
                cDisMoves cdm = new cDisMoves(sOneMove[0], sOneMove[1]);
                UnstackMoves.Add(cdm);
            }
        }
    }
   

    public class cWhereSeriesStored
    {
        public int SeriesID;
        public int StackNumber;
    }

 
    public class cDisassemble
    {
        List<card> CollapsibleSEQCardsToMove = new List<card>();
        private int FSptr;
        public List<cCardCountMoves> CardCountMoves;
        public List<cDisStrategy> DisStrategy;
        public List<cWhereSeriesStored> WhereStored;
        public List<int> xEmpties = new List<int>();
        public int[] xDepth = new int[3]; // 3 is all we care about for now when unstacking
        public List<csosDisStrategy> sosDisStrategy;
        Dictionary<string, card> dictLookup = new Dictionary<string, card>();
        public List<cAssemblePattern> AssemblePattern = new List<cAssemblePattern>(32);
        private cSpinControl cSC;
        // this moves empties an "S" only
        // we want to leave S fixed !!!

        public void sosUnstackE(ref board tb, ref csosDisStrategy cds)
        {
            int i, PosLastLetter, n = cds.UnstackMoves.Count;
            card c;
            string srcLetter;   // from the pattern
            string desLetter = cds.Pattern.Substring(0, 1);
            string strPattern = cds.Pattern.Substring(1);
            string strSrc, strDes;
            int FromStack = -1, ToStack = -1, FromCard = -1;
            int e;
            tb.CopyWorkingInts(ref xEmpties, GlobalClass.WorkingType.tEmpties);
            for (i = 0; i < 3; i++)
                xDepth[i] = 0;
            

            for (i = 0; i < n; i++)
            {
                strSrc = cds.UnstackMoves[i].id_src;
                if (strSrc == "S")
                {
                    PosLastLetter = strPattern.Length - 1;
                    srcLetter = strPattern.Substring(PosLastLetter,1);
                    strPattern = strPattern.Substring(0,PosLastLetter);
                    c = dictLookup[srcLetter];
                    FromStack = c.iStack;
                    FromCard = c.iCard;

                }
                else
                {
                    e = Convert.ToInt32(strSrc);
                    FromStack = xEmpties[e];
                    xDepth[e]--;
                    FromCard = xDepth[e];
                }
                strDes = cds.UnstackMoves[i].id_des;
                if (strDes == "S")
                {
                    c = dictLookup[desLetter];
                    ToStack = c.iStack;
                }
                else
                {
                    e = Convert.ToInt32(strDes);
                    ToStack = xEmpties[e];
                    xDepth[e]++;
                }
                tb.moveto(FromStack, FromCard, ToStack);
            }
        }

        // get the pattern letter from the pattern that matches the index
        // if the pattern to be formed is ABC and the inx is '1' then get the matching 'B'
        // if no pattern return empty string
        private string APLookup(ref List<cAssemblePattern> AssemblyPattern, int inx)
        {
            int i;
            for (i = 0; i < AssemblePattern.Count; i++)
            {
                if (AssemblePattern[i].index == inx)
                {
                    return AssemblePattern[i].PatLetter;
                }
            }
            return "";
        }

        private string AssemblePatternFrom(ref board tb, ref series sCollapse, string strInxvars)
        {
            int i, j, n = sCollapse.size;
            card c;
            string strPattern = "";
            column cCol = tb.ThisColumn[sCollapse.iStack];
            for (i = 0; i < n; i++)
            {
                c = cCol.Cards[i + sCollapse.topCard.iCard];
                cAssemblePattern cap = new cAssemblePattern(ref c, i);
                AssemblePattern.Add(cap);

                // the original pattern needs to be recovered when performing moves
                // ie: if the cards are 5h,3C,4C we move 4C then 3C then bring them back to the faceup 5H
                // We sort only to get the pattern
            }

            AssemblePattern.Sort(delegate(cAssemblePattern c1, cAssemblePattern c2)
            {
                return Comparer<int>.Default.Compare(c1.c.rank,c2.c.rank);
            });
            for (i = 0; i < AssemblePattern.Count; i++)
            {
                AssemblePattern[i].PatLetter = strInxvars.Substring(i, 1);
            }

            for (i = 0; i < AssemblePattern.Count; i++)
            {
                j = AssemblePattern[i].index;
                string a = APLookup(ref AssemblePattern, i);
                dictLookup.Add(a, cCol.Cards[i + sCollapse.topCard.iCard]);
                strPattern += a;
            }
            return  strPattern;
        }


        public bool bCanUnstackCollapseable(ref series sCollapse, ref board tb, ref List<card> Placeholders)
        {
            int n = sCollapse.size;
            int i;
            card SCard = sCollapse.topCard; // this is the "S" Card with which we will be decrementing the iCard value as we move
            
            string strInxvars, strPattern;
            column tCol = tb.ThisColumn[sCollapse.topCard.iStack];

            csosDisStrategy cds;
            strInxvars = GlobalClass.cstrInxVars.Substring(0, n);
            AssemblePattern.Clear();
            dictLookup.Clear();
            //dictLookup.Add("S", SCard);
            // the above is not needed as we will unstack the pattern "BA" (B is actually S) to get A
            strPattern = AssemblePatternFrom(ref tb, ref sCollapse, strInxvars);
            n = strPattern.Length;
            if (n > 4) return false;    // jys !!! have not created patterns bigger then 4 letters
            // check first to see if we can use our empty columns
            for (i = 0; i < sosDisStrategy.Count; i++)
            {
                cds = sosDisStrategy[i];
                if (n == cds.NumCards && cds.NumEmptyColumns <= tb.NumEmptyColumns)
                {
                    if (cds.Pattern != strPattern) continue;
                    if (cds.bMustBeTop) continue;   //we will not be able to put these cards back into this stack
                    //if (cds.Collapsible != null) continue;  // only want to use empties
                    sosUnstackE(ref tb, ref cds);
                    return true;
                }
            }

            return false;
        }

        // remove cards in teh sCollapse series and put them all in one place, somewhere, if possible
        public bool TryExpose(ref series sCollapse, ref board tb)
        {
            
            int n = sCollapse.size;
            int i;
            card SCard=sCollapse.topCard; // this is the "S" Card with which we will be decrementing the iCard value as we move
            const string cstrInxVars = "ABCD"; // jys oct2012 added E
            string strInxvars, strPattern;
            column tCol = tb.ThisColumn[sCollapse.topCard.iStack];
            if (n > 4) return false;
            csosDisStrategy cds;
            strInxvars = cstrInxVars.Substring(0, n);
            AssemblePattern.Clear();
            dictLookup.Clear();
            //dictLookup.Add("S", SCard);
            // the above is not needed as we will unstack the pattern "BA" (B is actually S) to get A
            strPattern = AssemblePatternFrom(ref tb, ref sCollapse, strInxvars);
            n = strPattern.Length;
            if (n > 4) return false;    // jys !!! have not created patterns bigger then 4 letters
            // check first to see if we can use our empty columns
            for (i = 0; i < sosDisStrategy.Count; i++)
            {
                cds = sosDisStrategy[i];
                if (n == cds.NumCards && cds.NumEmptyColumns <= tb.NumEmptyColumns)
                {
                    if (cds.Pattern != strPattern) continue;
                    if (!cds.bMustBeTop) continue;
                    if (cds.Collapsible != null) continue;  // only want to use empties
                    sosUnstackE(ref tb, ref cds);
                    return true;
                }
            }

            return false;
        }


        public bool ReAssembleStackables(ref List<series> Unstackables, ref board tb, string strPattern)
        {
            int i,j, n=strPattern.Length;
            string strPossibles = "ABCDEFG".Substring(0, n);
            dictLookup.Clear();
            j = n;
            for (i = 0; i < n; i++)
            {
                dictLookup.Add(strPossibles.Substring(--j, 1), Unstackables[i].topCard);
            }
            tb.CopyWorkingInts(ref xEmpties, GlobalClass.WorkingType.tEmpties);

            /*
             * the following was to go there, but we are doing a type of expose top and there is no SCard
             * as SCard is the breakup column, not another column. use the FILLDS type tools
             *             SCard = new card(ref sCollapse.bottomCard, sCollapse.bottomCard.iStack, sCollapse.bottomCard.iCard);
            dictLookup.Add("S", SCard);

            // check first to see if we can use our empty columns
            for (i = 0; i < sosDisStrategy.Count; i++)
            {
                cds = sosDisStrategy[i];
                if (n == cds.NumCards && cds.NumEmptyColumns <= tb.NumEmptyColumns)
                {
                    if (cds.Collapsible != null || strPattern != cds.Pattern) continue;  // only want to use empties
                    sosUnstackE(ref tb, ref cds);
                    return true;
                }
            }

             * */


            return false;
        }

        private void VerifyMoves(string strIN)
        {
            int i, j;
            string a;
            string[] sTest, dTest;
            sTest = strIN.Split(',');
            for (i = 0; i < sTest.Length; i++)
            {
                dTest = sTest[i].Split('-');
                for (j = 0; j < dTest.Length; j++)
                {
                    a = dTest[j];
                    if (a == "S") continue;
                    if (a == "HA") continue;
                    if (a == "HB") continue;
                    if (a == "HC") continue;
                    if (a == "HD") continue;
                    if (a == "0") continue;
                    if (a == "1") continue;
                    if (a == "2") continue;
                    Debug.Assert(false);
                }
            }
        }

        private void SOSorder(params object[] args)
        {
            int iPtr = 0;
            int NumVars = 0, NumEmptystacks = 0;
            string strSlots = "";
            string strMoves = "";
            string strPattern = "";


            foreach (object arg in args)
            {
                string strTemp = arg.ToString();
                switch (iPtr)
                {
                    case 0: strPattern = strTemp;
                        NumVars = strPattern.Length;
                        iPtr = 1;
                        break;
                    case 1: NumEmptystacks = Convert.ToInt32(strTemp);
                        iPtr = 2;
                        break;
                    case 2: strSlots = strTemp;
                        iPtr = 3;
                        break;
                    case 3: strMoves = strTemp;
                        iPtr = 0;
                        VerifyMoves(strMoves);
                        csosDisStrategy cds = new csosDisStrategy(NumEmptystacks, strPattern.Length,strPattern, strSlots, strMoves);
                        sosDisStrategy.Add(cds);
                        break;
                }
            }
        }


        /*
         * this takes any combination of 4 SEQ series whos top cards are ab, abc, abcd and arranges them in 
         * the order DCBA.  A postive number indicates they will be moved away from where they are
         * a negative number indicates they will be reassembled back on the stack they came from EXCEPT
         * that stack must have been empty ("A" was the top card" or the top card was of rank "D+1"
         */

        // OCT 7, 2010 DECIDED THAT NEGATIVE MEANS THE FINAL DESTINATION IS "S"  JYS !!!
        /// <summary>
        ///  THIS NEEDS TO BE FIXED:  LOOK AT ALL NEGATIVE AND REMOVE THOSE THAT USE s AS INTERMEDIATE!!!
        ///  PROBABLY NEED A BETTER TABLE: ALL "NEGATIVE" FOR THE ExposeTop
        /// </summary>

        public void runSOSdisassemble()
        {
            sosDisStrategy = new List<csosDisStrategy>();
SOSorder(  "AB", 1,    "","S-0,S-0");
SOSorder(  "AB", 0, "2BA","S-HB,S-HA,HB-S,HA-S");
SOSorder(  "BA",-1,    "","S-0,0-S");
SOSorder(  "BA", 0,  "1A","S-HA,HA-S");
SOSorder( "ABC", 1,    "","S-0,S-0,S-0");
SOSorder( "ABC",-2, "", "S-0,S-1,S-1,0-S,1-0,1-S,0-S");
SOSorder( "ABC", 0,"3ABC","S-HC,S-HB,S-HA,HC-S,HB-S,HA-S");
SOSorder( "ACB", 2,    "","S-0,S-1,0-1,S-1");
SOSorder( "ACB", 1,  "1B","S-HB,HC-0,HB-0,S-0");
SOSorder( "ACB",-1, "2AC","S-0,S-HC,S-HA,0-S,HA-S");
SOSorder( "ACB", 0,"3ACB","S-HC,S-HB,S-HA,HC-S,HB-S,HA-S");
SOSorder( "BAC",-3,    "","S-0,S-1,S-2,0-S,2-S,1-S");
SOSorder( "BAC",-2, "1C", "S-HC,S-0,S-1,HC-S,1-S,0-S");
SOSorder( "BAC", 2,    "","S-0,S-1,S-0,1-0");
SOSorder( "BAC", 1,  "1A","S-0,S-HA,S-0,HA-0");
SOSorder( "BAC", 0,"3BAC","S-HC,S-HB,S-HA,HC-S,HB-S,HA-S");

SOSorder( "BCA", 2,    "","S-0,S-1,S-1,0-1");
SOSorder( "BCA",-1,  "1A","S-HA,S-0,S-0,HA-S");
SOSorder( "BCA",-1,"3BCA","S-HC,S-HB,S-HA,HC-S,HB-S,HA-S");
SOSorder( "BCA",-3,    "","S-0,S-1,S-2,1-S,2-1,0-S");

SOSorder( "CAB",-2,    "","S-0,S-1,0-S,1-S");
SOSorder( "CAB", 1,  "1B","S-HB,S-0,1-0,HB-0");
SOSorder( "CAB",-1,  "1A","S-0,S-HA,0-S,HA-S");

SOSorder( "CBA",-2,    "","S-0,S-1,1-S,0-S");
SOSorder( "CBA",-1,  "1A","S-HA,S-0,0-S,HA-S");
SOSorder( "CBA",-1,  "1B","S-0,S-HB,HB-S,0-S");

SOSorder("ABCD", 1,    "","S-0,S-0,S-0,S-0");
SOSorder("ABCD",-3,    "","S-0,S-1,S-0,S-2,S-2,0-S,1-S,2-0,2-S,0-S");
SOSorder("ABDC", 2,    "","S-0,S-1,0-1,S-0,S-0");
SOSorder("ABDC",-3,    "","S-0,S-1,0-1,S-2,S-2,1-S,0-S,2-0,2-S,0-S");
SOSorder("ABDC", 1,  "1C","S-HC,S-0,HC-0,S-0,S-0");
SOSorder("ABDC",-1, "2BD","S-0,S-HD,S-HB,S-HB,HD-S,0-S,HB-0,HB-S,0-S");


SOSorder("ACBD", 2,    "","S-0,S-1,S-0,1-0,S-0");
SOSorder("ACBD", 1,  "1B","S-0,S-HB,S-0,HB-0,S-0");
SOSorder("ACBD",-1,"3ACD","S-HD,S-HC,S-0,S-HA,HD-S,HC-S,0-S,HA-S");
SOSorder("ACBD",-3,    "","S-0,S-1,S-2,1-2,S-1,0-S,2-0,2-S,0-S,1-S");

SOSorder("ACDB", 2,    "","S-0,S-1,S-1,0-1,S-1");
SOSorder("ACDB", 1,  "1B","S-HB,S-0,S-0,HB-0,S-0");
SOSorder("ACDB",-1,"3ACD","S-0,S-HD,S-HC,S-HA,HD-S,HC-S,0-S,HA-S");
SOSorder("ACDB",-3,    "","S-0,S-1,S-2,0-2,S-0,1-S,2-1,2-S,1-2,0-S");

SOSorder("ADBC", 3,    "","S-0,S-1,S-2,0-2,1-2,S-2");
SOSorder("ADBC", 2,  "1C","S-HC,S-0,S-1,HC-1,0-1,S-1");
SOSorder("ADBC", 2,  "1B","S-0,S-HB,S-1,0-1,HB-1,S-1");
SOSorder("ADBC",-2,  "1D","S-0,S-1,S-HD,S-1,HD-S,0-S,1-0,1-S,0-S");
SOSorder("ADBC",-1, "2BC","S-HC,S-HB,S-0,HC-0,HB-0,S-0");
SOSorder("ADBC",-1,"3ADB","S-0,S-HB,S-HD,S-HA,HD-S,0-S,HB-S,HA-S");
SOSorder("ADBC",-3,    "","S-0,S-0,S-1,S-2,1-S,0-1,0-S,1-S,2-S");

SOSorder("ADCB", 3,    "","S-0,S-1,S-2,1-2,0-2,S-2");
SOSorder("ADCB", 2,  "1B","S-HB,S-0,S-1,0-1,HB-1,S-1");
SOSorder("ADCB", 2,  "1C","S-0,S-HC,S-1,HC-1,0-1,S-1");
SOSorder("ADCB",-2, "2AD","S-0,S-1,S-HD,S-HA,HD-S,1-S,0-S,HA-S");
SOSorder("ADCB", 1, "2CB","S-HB,S-HC,S-0,HC-0,HB-0,S-0");
SOSorder("ADCB",-1,"3ADC","S-0,S-HC,S-HD,S-HA,HD-S,HC-S,0-S,HA-S");
SOSorder("ADCB",-3,    "","S-0,S-1,0-1,S-0,S-2,0-S,1-0,1-S,0-S,2-S");


SOSorder("BACD", 2,    "","S-0,S-0,S-1,S-0,1-0");
SOSorder("BACD",-1,  "1A","S-0,S-0,S-HA,S-0,HA-S");
SOSorder("BADC", 2,    "","S-0,S-1,0-1,S-0,S-1,0-1");
SOSorder("BADC", 1, "2AC","S-HC,S-0,HC-0,S-HA,S-0,HA-0");
SOSorder("BACD",-3,    "","S-0,S-0,S-1,S-2,1-2,0-1,0-S,1-S,2-0,2-S,0-S");


SOSorder("BCAD", 2,    "","S-0,S-1,S-0,S-0,1-0");
SOSorder("BCAD", 1,  "1A","S-0,S-HA,S-0,S-0,HA-0");
SOSorder("BCAD",-3,    "","S-0,S-1,S-0,S-2,1-2,0-1,0-S,1-S,2-0,2-S,0-S");

SOSorder("BCDA", 2,    "","S-0,S-1,S-1,S-1,0-1");
SOSorder("BCDA", 1,  "1A","S-HA,S-0,S-0,S-0,HA-0");
SOSorder("BCDA",-3,   "", "S-0,S-1,S-1,S-2,0-2,1-0,1-S,0-S,2-0,2-S,0-S");


SOSorder("CABD", 3,    "","S-0,S-1,S-2,S-0,1-0,2-0");
SOSorder("CABD",-3,    "","S-0,S-1,S-1,S-2,0-S,2-S,1-0,1-S,0-S");
SOSorder("CABD", 1, "2AB","S-0,S-HB,S-HA,S-0,HB-0,HA-0");
SOSorder("CABD",-1, "2BD","S-HD,S-HB,S-HB,S-0,HD-S,0-S,HB-0,HB-S,0-S");

SOSorder("CADB", 3,    "","S-0,S-1,S-2,S-1,0-1,2-1");
SOSorder("CADB",-2,    "","S-0,S-1,S-0,S-1,0-S,0-1,S-1");
SOSorder("CADB",-1,  "1B","S-HB,S-0,S-HB,S-0,HB-S,HB-0,S-0");
SOSorder("CADB",-1,"3CAD","S-0,S-HD,S-HA,S-HC,HD-S,HC-S,0-S,HA-S");
SOSorder("CDAB", 3,    "","S-0,S-1,S-2,S-2,0-2,1-2");
SOSorder("CDAB",-2,    "","S-0,S-0,S-1,S-1,0-S,0-1,S-1");
SOSorder("CDAB",-1,  "1B","S-HB,S-HB,S-0,S-0,HB-S,HB-0,S-0");
SOSorder("CDAB", 1, "2AB","S-HB,S-HA,S-0,S-0,HB-0,HA-0");
SOSorder("CDBA", 3,    "","S-0,S-1,S-2,S-2,1-2,0-1");
SOSorder("CDBA", 2,  "1A","S-HA,S-0,S-1,S-1,0-1,HA-1");
SOSorder("CDBA", 2,  "1B","S-0,S-HB,S-1,S-1,HB-1,0-1");
SOSorder("CDBA",-1,  "1B","S-0,S-HB,0-HB,S-0,S-0,HB-S,HB-0,S-0");
SOSorder("CDBA", 1, "2AB","S-HA,S-HB,S-0,S-0,HB-0,HA-0");
SOSorder("CBAD", 3,    "","S-0,S-1,S-2,S-0,2-0,1-0");
SOSorder("CBAD",-2,  "1B","S-0,S-1,S-HB,1-HB,S-0,HB-S,HB-0,S-0");
SOSorder("CBAD", 1, "2AB","S-0,S-HA,S-HB,S-0,HB-0,HA-0");
SOSorder("CBDA", 3,    "","S-0,S-1,S-2,S-1,2-1,0-1");
SOSorder("CBDA", 2,  "1A","S-HA,S-0,S-1,S-0,1-0,HA-0");
SOSorder("CBDA", 1, "2AB","S-HA,S-0,S-HB,S-0,HB-0,HA-0");
SOSorder("CABD", 3,    "","S-0,S-1,S-2,S-0,1-0,2-0");
SOSorder("CABD",-2,    "","S-0,S-1,S-1,S-0,1-S,1-0,S-1");
SOSorder("CABD", 2,  "1A","S-0,S-1,S-HA,S-0,1-0,HA-0");
SOSorder("CABD", 2,  "1B","S-0,S-HB,S-1,S-0,HB-0,1-0");
SOSorder("CABD",-1,  "1B","S-0,S-HB,S-HB,S-0,HB-S,HB-0,S-0");
SOSorder("CABD", 1, "2AB","S-0,S-HB,S-HA,S-0,HB-0,HA-0");
SOSorder("CADB", 3,    "","S-0,S-1,S-2,S-1,0-1,2-1");
SOSorder("CADB",-2,    "","S-0,S-1,S-0,S-1,0-S,0-1,S-1");
SOSorder("CADB", 2,  "1B","S-HB,S-0,S-1,S-0,HB-0,1-0");
SOSorder("CADB",-1,  "1B","S-HB,S-0,S-HB,S-0,HB-S,HB-0,S-0");
SOSorder("CADB",-1, "2DB","S-HB,S-HD,S-HB,S-0,HD-S,0-S,HB-0,HB-S,0-S");
SOSorder("CADB",-1,"3CAD","S-0,S-HD,S-HA,S-HC,HD-S,HC-S,0-S,HA-S");
SOSorder("DABC",-2,    "","S-0,S-1,S-1,0-S,1-0,1-S,0-S");
SOSorder("DABC",-1, "2BC","S-HC,S-HB,S-0,HC-S,HB-S,0-S");
SOSorder("DABC",-1, "2AB","S-0,S-HB,S-HA,0-S,HB-S,HA-S");
SOSorder("DABC", 0,"3ABC","S-HC,S-HB,S-HA,HC-S,HB-S,HA-S");
SOSorder("DACB",-2,    "","S-0,S-1,S-0,1-S,0-1,0-S,1-S");
SOSorder("DACB",-1, "2BC","S-HB,S-HC,S-0,HC-S,HB-S,0-S");
SOSorder("DACB",-1, "2AC","S-0,S-HC,S-HA,HC-S,0-S,HA-S");
SOSorder("DACB", 0,"3ABC","S-HB,S-HC,S-HA,HC-S,HB-S,HA-S");
SOSorder("DBAC",-3,    "","S-0,S-1,S-2,0-S,2-S,1-S");
SOSorder("DBAC",-2,  "1C","S-HC,S-0,S-1,HC-S,1-S,0-S");
SOSorder("DBAC",-2,  "1A","S-0,S-HA,S-1,0-S,1-S,HA-S");
SOSorder("DBAC",-2,  "1B","S-0,S-1,S-HB,0-S,HB-S,1-S");
SOSorder("DBAC",-1, "2AC","S-HC,S-HA,S-0,HC-S,0-S,HA-S");
SOSorder("DBAC",-1, "2AB","S-0,S-HA,S-HB,0-S,HB-S,HA-S");
SOSorder("DBAC",-1, "2BC","S-HC,S-0,S-HB,HC-S,HB-S,0-S");
SOSorder("DBAC", 0,"3ABC","S-HC,S-HA,S-HB,HC-S,HB-S,HA-S");
SOSorder("DBCA",-3,    "","S-0,S-1,S-2,1-S,2-S,0-S");
SOSorder("DBCA",-2,  "1A","S-HA,S-0,S-1,0-S,1-S,HA-S");
SOSorder("DBCA",-2,  "1B","S-0,S-1,S-HB,1-S,HB-S,0-S");
SOSorder("DBCA",-2,  "1C","S-0,S-HC,S-1,HC-S,1-S,0-S");
SOSorder("DBCA",-1, "2AC","S-HA,S-HC,S-0,HC-S,0-S,HA-S");
SOSorder("DBCA",-1, "2BC","S-0,S-HC,S-HB,HC-S,HB-S,0-S");
SOSorder("DBCA",-1, "2AB","S-HA,S-0,S-HB,0-S,HB-S,HA-S");
SOSorder("DBCA", 0,"1ABC","S-HA,S-HC,S-HB,HC-S,HB-S,HA-S");
SOSorder("DCAB",-2,    "","S-0,S-1,1-0,S-1,1-S,0-1,0-S,1-S");
SOSorder("DCAB",-1,  "1B","S-HB,S-0,S-1,1-S,HB-S,0-S");
SOSorder("DCAB",-1,  "1C","S-0,S-1,S-HC,HC-S,0-S,1-S");
SOSorder("DCAB",-1,  "1A","S-0,S-HA,S-1,1-S,0-S,HA-S");
SOSorder("DCAB", 0,"3ABC","S-HB,S-HA,S-HC,HC-S,HB-S,HA-S");
SOSorder("DCBA",-2,    "","S-0,S-1,0-1,S-0,0-S,1-0,1-S,0-S");
SOSorder("DCBA",-1,  "1A","S-HA,S-0,S-1,1-S,0-S,HA-S");
SOSorder("DCBA",-1,  "1B","S-0,S-HB,0-HB,S-0,0-S,HB-0,HB-S,0-S");

        }

        public cDisassemble(ref cSpinControl cSC)
        {
            this.cSC = cSC;
            runSEQdisassemble();
            runSOSdisassemble();
            CardCountMoves = new List<cCardCountMoves>();
        }

        public void runSEQdisassemble()
        {
            FSptr = 0;
            DisStrategy = new List<cDisStrategy>();
            WhereStored = new List<cWhereSeriesStored>();
            FillDS(1,0,"SC","DC");
            FillDS(2,0,1,"SC","C1","SC","DC","C1","DC");
            FillDS(2,1, "SC", "E0", "SC", "DC", "E0", "DC");
            FillDS(3,0,2,1,"SC","C2","SC","C1","SC","DC","C1","DC","C2","DC" );
            //FillDS(3,1,1, "SC", "C1", "SC", "E0", "SC", "DC", "E0", "DC", "C1", "DC");
            FillDS(3, 1, 1, "SC", "E0", "SC", "C1", "SC", "DC", "C1", "DC", "E0", "DC");
            FillDS(3,1,2, "SC", "C2", "SC", "E0", "SC", "DC", "E0", "DC", "C2", "DC");
            FillDS(3,2,"SC","E0","SC","E1","SC","DC","E1","DC","E0","DC");
            FillDS(4,0,3,2,1,"SC","C1","SC","C2","SC","C3","SC","DC","C3","DC","C2","DC","C1","DC");
            FillDS(4,1,3,1,"SC","C3","SC","C1","SC","E0","SC","DC","E0","DC","C1","DC","C3","DC");
            FillDS(4,1,2,"SC","E0","SC","C2","E0","C2","SC","E0","SC","DC","E0","DC","C2","E0","C2","DC","E0","DC");
            FillDS(4,2,"SC","E0","SC","E1","E0","E1","SC","E0","SC","DC","E0","DC","E1","E0","E1","DC","E0","DC");
            FillDS(5,0,4,3,2,1,"SC","C4","SC","C3","SC","C2","SC","C1","SC","DC","C1","DC","C2","DC","C3","DC","C4","DC");
            FillDS(5,1,3,1,"SC","E0","SC","C3","E0","C3","SC","E0","SC","C1","SC","DC","C1","DC","E0","DC","C3","E0","C3","DC","E0","DC");
            FillDS(5,2,1,"SC","E0","SC","E1","E0","E1","SC","E0","SC","C1","SC","DC","C1","DC","E0","DC","E1","E0","E1","DC","E0","DC");
            FillDS(5,2,2,"SC","E0","SC","E1","SC","C1","E1","C2","SC","E1","SC","DC","E1","DC","C2","E1","C2","DC","E1","DC","E0","DC");
            FillDS(5,2,3,"SC","E0","SC","C3","E0","C3","SC","E0","SC","E1","SC","DC","E1","DC","E0","DC","C3","E1","C3","DC","E1","DC");
            FillDS(5,2,4,"SC","C4","SC","E0","SC","E1","E0","E1","SC","E0","SC","DC","E0","DC","E1","E0","E1","DC","E0","DC","C4","DC");
            FillDS(5,3,"SC","E0","SC","E1","SC","E2","E1","E2","SC","E1","SC","DC","E1","DC","E2","E1","E2","DC","E1","DC","E0","DC");
            FillDS(6, 0, 5, 4, 3, 2, 1, "SC","C5","SC", "C4", "SC", "C3", "SC", "C2", "SC", "C1", "SC", "DC", "C1", "DC", "C2", "DC", "C3", "DC", "C4", "DC", "C5","DC");
            FillDS(6,1,4,1,"SC","E0","SC","C4","E0","C4","SC","E0","SC","C1","E0","C1","SC","E0","SC","DC","E0","DC","C1","E0","C1","DC","E0","DC","C4","E)","C4","DC","E0","DC");
            FillDS(6,2,2,"SC","E0","SC","E1","E0","E1","SC","E0","SC","C2","E0","C2","SC","E0","SC","DC","E0","DC","C2","E0","C2","DC","E0","DC","E1","E0","E1","DC","E0","DC");
            FillDS(6,2,3,"SC","E0","SC","E1","SC","C3","E1","C3","E0","C3","SC","E0","SC","E1","SC","DC","E1","DC","E0","DC","C3","E0","C3","E1","C3","DC","E1","DC","E0","DC");
            FillDS(6,2,4,"SC","E0","SC","C4","E0","C4","SC","E0","SC","E1","E0","E1","SC","E0","SC","DC","E0","DC","E1","E0","E1","DC","E0","DC","C4","E0","C4","DC","E0","DC");
            FillDS(6,2,5,1,"SC","C5","SC","E0","SC","E1","E0","E1","SC","E0","SC","C1","SC","DC","C1","DC","E0","DC","E1","E0","E1","DC","E0","DC","C5","DC");
            FillDS(6,3,"SC","E0","SC","E1","SC","E2","E1","E2","E0","E2","SC","E0","SC","E1","SC","DC","E1","DC","E0","DC","E2","E1","E2","E0","E2","DC","E0","DC","E1","DC");
            FillDS(7,0,6,5,4,3,2,1,"SC","C6","SC","C5","SC", "C4", "SC", "C3", "SC", "C2", "SC", "C1", "SC", "DC", "C1", "DC", "C2", "DC", "C3", "DC", "C4", "DC", "C5","DC", "C6","DC");
            FillDS(7,1,6,4,2,"SC","C6","SC","E0","SC","C4","E0","C4","SC","E0","SC","C2","E0","C2","SC","E0","SC","DC","E0","DC","C2","E0","C2","DC","E0","DC","C4","E0","C4","DC","E0","DC","C6","DC");
            FillDS(7,1,5,3,2,"SC","E0","SC","C5","E0","C5","SC","e0","SC","C3","E0","C3","SC","C2","SC","E0","SC","DC","E0","DC","C2","DC","C3","E0","C3","DC","E0","DC","C5","E0","C5","DC","E0","DC");
            FillDS(7,1,5,3,1,"SC","E0","SC","C5","E0","C5","SC","E0","SC","C3","SC","E0","SC","C1","SC","DC","C1","DC","E0","DC","C3","E0","C3","DC","E0","DC","C5","E0","C5","DC","E0","DC");
            FillDS(7,2,5,1,"SC","E0","SC","C5","E0","C5","SC","E0","SC","E1","E0","E1","SC","E0","SC","C1","SC","DC","C1","DC","E0","DC","E1","E0","E1","DC","E0","DC","C5","E0","C5","DC","E0","DC");
            // 7,2,1,5 means the following 7:number of series with the top("0") the one we want to move
            // to some destination column.  2 means there are only 2 empty columns, 1 and 5 means that
            // in the source column, series 1 (2nd from top) and series 5 (next to last) must have
            // a place to fit on some other column  if S1.top has rank 4 then a 5 must be available
            // for 7 in a series, the top is 0 the bottom is 6 and CardsToMove are ordered 6..0 
            // we remove cards from the end and move those first.  the last remaining card is the first one[0] 
            // which was the '6' 
            FillDS(7,2,5,2,"SC","E0","SC","C5","E0","C5","SC","E0","SC","E1","E0","E1","SC","C2","SC","E0","SC","DC","E0","DC","C2","DC","E1","E0","E1","DC","E0","DC","C5","E0","C5","DC","E0","DC");
            FillDS(7,2,3,"SC","E0","SC","E1","E0","E1","SC","E0","SC","C3","E0","C3","E1","E0","E1","C3","E0","C3","SC","E0","SC","E1","SC","DC","E1","DC","E0","DC","C3","E0","C3","E1","E0","E1","C3","E0","C3","DC","E0","DC","E1","E0","E1","DC","E0","DC");
            FillDS(7,2,4,"SC","E0","SC","E1","SC","C4","E1","C4","E0","C4","SC","E0","SC","E1","E0","E1","SC","E0","SC","DC","E0","DC","E1","E0","E1","DC","E0","DC","C4","E0","C4","E1","C5","DC","E1","DC","E0","DC");
            FillDS(7,3,"SC","E0","SC","E1","SC","E2","E1","E2","E0","E2","SC","E0","SC","E1","E0","E1","SC","E0","SC","DC","E0","DC","E1","E0","E1","DC","E0","DC","E2","E1","E2","E0","E2","DC","E1","DC","E0","DC");
        }

        private void FillDS(params object[] args)
        {
            int i = 0;
            int v = 0;
            bool bIsInt;
            cDisStrategy cds;
            foreach (object arg in args)
            {
                string str = arg.ToString();
                bIsInt = false;
                if (str.Length == 1)
                {
                    v = Convert.ToInt32(arg);
                    bIsInt = true;
                }

                if (i == 0)
                {
                    cds = new cDisStrategy();
                    Debug.Assert(bIsInt);
                    cds.NumSeqSeries = v;
                    cds.Moves = new List<cMoves>();
                    cds.Ranks = new List<int>();
                    DisStrategy.Add(cds);
                    i++;
                    continue;
                }

                FSptr = DisStrategy.Count - 1;

                if (i == 1)
                {
                    Debug.Assert(bIsInt);
                    DisStrategy[FSptr].nEmptyColumns = v;
                    i++;
                    continue;
                }
                if (i > 1)
                {
                    if (bIsInt)
                    {
                        DisStrategy[FSptr].Ranks.Add(v);
                        continue;
                    }
                    else
                    {
                        cMoves cM = new cMoves(str);
                        DisStrategy[FSptr].Moves.Add(cM);
                    }
                }
            }
        }


        int GetOperands(string str, out string Operator)
        {
            int n = str.Length;
            int r;
            Debug.Assert(n == 2);
            if (str == "DC")
            {
                Operator = "DC";
                return -1;
            }
            if (str == "SC")
            {
                Operator = "SC";
                return -1;
            }
            Operator = str.Substring(0, 1);
            r = Convert.ToInt32(str.Substring(1, 1));
            return r;
        }

        public bool FormMovesFromRules(int fptr, ref board tb, ref List<int> Empties, ref List<card> CardsToMove)
        {
            int i,j, n;
            int r1, r2;
            int FromStack=0, FromLoc=0, ToStack=0;
            string strOper1, strOper2;
            card TopCardToMove = CardsToMove[0];
            int TopCardsDestination = tb.tag;
            int SrcColumn = TopCardToMove.iStack;
            string strSrc, strDes;
            List<cMoves> Moves = DisStrategy[fptr].Moves;
            n = Moves.Count;
            Debug.Assert(n%2 == 0);   // must be an even number
            tb.ClearStacks();
            for(i=0; i<n; i+=2)
            {
                strSrc = Moves[i].MoveID;
                r1 = GetOperands(strSrc, out strOper1);
                if (strOper1 == "SC")
                {
                    FromStack = SrcColumn;
                    FromLoc = CardsToMove.Last().iCard;
                    CardsToMove.RemoveAt(CardsToMove.Count - 1);
                }
                else if (strOper1 == "E")
                {
                    FromStack = Empties[r1];
                    FromLoc = tb.ThisColumn[FromStack].HoldMoves.Pop();
                }
                else if (strOper1 == "C")
                {
                    for (j = 0; j < WhereStored.Count; j++)
                    {
                        if (WhereStored[j].SeriesID == r1)
                        {
                            FromStack = WhereStored[j].StackNumber;
                            FromLoc = tb.ThisColumn[FromStack].HoldMoves.Pop();
                        }
                    }
                }
                else
                {
                    Debug.Assert(false);
                    return false;
                }


                strDes = Moves[i+1].MoveID;
                r2 = GetOperands(strDes, out strOper2);

                if (strOper2 == "DC")
                {
                    ToStack = TopCardsDestination;
                }
                else if (strOper2 == "E")
                {
                    ToStack = Empties[r2];
                    tb.ThisColumn[ToStack].HoldMoves.Push(tb.ThisColumn[ToStack].Cards.Count);
                }
                else if (strOper2 == "C")
                {
                    for (j = 0; j < WhereStored.Count; j++)
                    {
                        if (WhereStored[j].SeriesID == r2)
                        {
                            ToStack = WhereStored[j].StackNumber;
                            tb.ThisColumn[ToStack].HoldMoves.Push(tb.ThisColumn[ToStack].Cards.Count);
                        }
                    }
                }
                else
                {
                    Debug.Assert(false);
                    return false;
                }
                tb.moveto(FromStack, FromLoc, ToStack);
            }
            return true;
        }

        public bool bCanDisassembleSOS(int nSubSeries, ref board tb, ref List<int> Empties, ref List<card> PlaceHolders, ref List<card> CardsToMove)
        {
            int i,j, k;
            int NumExpected = 0;
            int nEmpties = Empties.Count;

            for (i = DisStrategy.Count-1; i >= 0; i--)
            {
                // going backwards we see the max empty columns before the ones requiring help
                if (DisStrategy[i].NumSeqSeries == nSubSeries)
                {
                    if (nEmpties >= DisStrategy[i].nEmptyColumns)
                    {
                        // we may have sufficient resources to move the column
                        List<int> Ranks = DisStrategy[i].Ranks;
                        WhereStored.Clear();
                        NumExpected = Ranks.Count;
                        if (NumExpected > 0)
                        {
                            // jys !!!! 14nov2012 I actually used j below and had two j's
                            for (k = 0; k < Ranks.Count; k++)
                            {
                                int iReqFit = Ranks[k];
                                // the top card at the subseries iReqFit must have a place to stay
                                card TryMove = CardsToMove[iReqFit];

                                for (j = 0; j < PlaceHolders.Count; j++)
                                {
                                    if ((PlaceHolders[j].rank - 1) == TryMove.rank)
                                    {
                                        cWhereSeriesStored wss = new cWhereSeriesStored();
                                        wss.StackNumber = PlaceHolders[j].iStack;
                                        wss.SeriesID = iReqFit;
                                        WhereStored.Add(wss);
                                        NumExpected--;
                                        if (NumExpected == 0)
                                        {
                                            return  FormMovesFromRules(i, ref tb, ref Empties, ref CardsToMove);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            return FormMovesFromRules(i, ref tb, ref Empties, ref CardsToMove);
                        }
                    }
                }
            }
            return false;
        }

        //PsudoMoveList must have moves inserted in the "front" to keep the move order correct
        // board tag contains destination of series to be moved to
        public bool MoveCollapsible(ref  series sCollapse, ref board tb)
        {
            List<card> BackupOfCardsToMove = new List<card>();
            if (cSC.bTrigger) Console.WriteLine(MethodBase.GetCurrentMethod().Name);
            column cCol = tb.ThisColumn[sCollapse.iStack];
            int n = sCollapse.pattern.Length;
            if (n > 7) return false;
            tb.CopyWorkingInts(ref xEmpties, GlobalClass.WorkingType.tEmpties);
            if (tb.tag < 0)
            {
                tb.tag = xEmpties[0];
                xEmpties.RemoveAt(0);
            }
            CollapsibleSEQCardsToMove.Clear();
            series s = sCollapse;
            for (int i = 0; i < sCollapse.pattern.Length; i++)
            {
                string a = sCollapse.pattern.Substring(i, 1);
                CollapsibleSEQCardsToMove.Add(s.topCard);
                BackupOfCardsToMove.Add(s.topCard);
                s = s.NextSeries;
            }

            for (int i = DisStrategy.Count - 1; i >= 0; i--)
            {
                // going backwards we see the max empty columns before the ones requiring help
                if (DisStrategy[i].NumSeqSeries == n)
                {
                    if (xEmpties.Count >= DisStrategy[i].nEmptyColumns)
                    {
                        if (DisStrategy[i].Ranks.Count > 0) continue;   // must use empty stack for now JYS !!!
                                // if a placeholder was available, it would have been assigned to tab.tag
                                // since all series are rankable, they can all go under the top card
                                // JYS !!!  we could possibly have looked for another placeholder or two to make
                                //          complicated moves

                        bool bAny = FormCollapsibleMoves(i, ref tb);
                        return bAny;
                    }
                }
            }

            return false;
        }

        private bool FormCollapsibleMoves(int fptr, ref board tb)
        {
            int i, n;
            int r1, r2;
            int FromStack = 0, FromLoc = 0, ToStack = 0;

            string strOper1, strOper2;
            card TopCardToMove = CollapsibleSEQCardsToMove[0];
            int TopCardsDestination = tb.tag;
            int SrcColumn = TopCardToMove.iStack;
            string strSrc, strDes;
            List<cMoves> Moves = DisStrategy[fptr].Moves;
            n = Moves.Count;
            Debug.Assert(n % 2 == 0);   // must be an even number
            tb.ClearStacks();
            for (i = 0; i < n; i += 2)
            {
                strSrc = Moves[i].MoveID;
                r1 = GetOperands(strSrc, out strOper1);

                if (strOper1 == "SC")
                {
                    FromStack = SrcColumn;
                    FromLoc =CollapsibleSEQCardsToMove.Last().iCard;
                    CollapsibleSEQCardsToMove.RemoveAt(CollapsibleSEQCardsToMove.Count - 1);
                }
                else if (strOper1 == "E")
                {
                    FromStack = xEmpties[r1];
                    FromLoc = tb.ThisColumn[FromStack].HoldMoves.Pop();
                }
                //else if (strOper1 == "C")
                //{
                //    for (j = 0; j < WhereStored.Count; j++)
                //    {
                //        if (WhereStored[j].SeriesID == r1)
                //        {
                //            FromStack = WhereStored[j].StackNumber;
                //            FromLoc = tb.ThisColumn[FromStack].HoldMoves.Pop();
                //        }
                //    }
                //}
                else
                {
                    Debug.Assert(false);
                    return false;
                }


                strDes = Moves[i + 1].MoveID;
                r2 = GetOperands(strDes, out strOper2);

                if (strOper2 == "DC")
                {
                    ToStack = TopCardsDestination;
                }
                else if (strOper2 == "E")
                {
                    ToStack = xEmpties[r2];
                    tb.ThisColumn[ToStack].HoldMoves.Push(tb.ThisColumn[ToStack].Cards.Count);
                }
                //else if (strOper2 == "C")
                //{
                //    for (j = 0; j < WhereStored.Count; j++)
                //    {
                //        if (WhereStored[j].SeriesID == r2)
                //        {
                //            ToStack = WhereStored[j].StackNumber;
                //            tb.ThisColumn[ToStack].HoldMoves.Push(tb.ThisColumn[ToStack].Cards.Count);
                //        }
                //    }
                //}
                else
                {
                    Debug.Assert(false);
                    return false;
                }
                tb.moveto(FromStack, FromLoc, ToStack);
                //pseudoCard pC = new pseudoCard();
            }
            return true;
        }

    }
}
