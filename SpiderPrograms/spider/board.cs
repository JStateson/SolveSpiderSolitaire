using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace spider
{

    public class cSuitedStatus
    {
        public bool bSuitsCompletable;  // true if 13 bit are set in SuitedRankBits
        public int SuitedRankBits;      // a bit is set if the rank is present and faceup
        public int UnexposedRankBits;   // if a bit is set then that rank is present in the facedown cards
        public bool bExposesRequired;   // if true, then it is possible to complete a suit using unexposed cards
        public List<int> MissingTypes;  // list of card types that are missing and are required to build the suit
                                        //   if it is empty but the suit is not completable then one or more
                                        //   unexposed cards need to be turned faceup
        public List<int> MustExpose;    // list of card types that need to be exposed or the suit cannot be completed
                                        // this list is empty if 1 or more required cards have not yet been dealt
        public List<int> BonusExposes;  // list of card types that have been exposed. points are added for any expose
                                        // these values are good thru the remainder of the deal (are not cleared)
        public int index;
        public bool bSuitNotOnBoard;     // all of the suites were completed and remove from the board
        public int NumCompletedAndRemoved;
    }

    public class board
    {
        public DateTime TimeOfLastDeal;
        public int BuildingThisSuit;    // if > 0 then 1 + number of the suit that is being reduced
            // eg: if == 2 then we are reducing clubs and ignoraing any other suits until clubs are reduce
            // this applies only to the reduce program
        public List<series> SuitedSEQ;
        public int NumMoves;    // original number of moves
        private static int BoardCounter = 0;
        public string CompletedBy;
        public Stack<int> SaveCompletedBy = new Stack<int>();
        public int LastBest;
        public int RunCounter;
        public bool SuitsLocked;    // if true then required cards are behind a king and no more cards
        // can be delt that would allow any suit to be collapsed.
        private const int VALUE_SUIT_EXPOSED = 100;
        private const int VALUE_ONE_EXPOSED = 1;
        public List<series> SortedSEQ = new List<series>();
        public const int MAXMOVES = 256;
        public bool bIsCompletable;         // if true then one or more suits are exposed and could be completed
        public cSuitedStatus[] SuitStatus = new cSuitedStatus[4];
        public bool bHaveResourcesToComplete;
        public int score, nchild, ExtraPoints;
        public bool dead;   // this board was dealt to.  since it was the seed, it is not used anymore after the deal
        public int from;
        public int ID;
        public int UniqueID;
        public int NumEmptyColumns;
        public int TotalCardsOnBoard;
        public List<cCompleted> Completed;
        public column[] ThisColumn;
        public cMoveClass MyMoves = new cMoveClass();
        public int NumCompletedSuits;
        public bool NewlyCompleted;
        public int DealCounter;
        //public const int FIRST_CARD = -16384;
        //public const int DEALT_A_CARD = -8192;    // 0xe000  rank 14 does not exist
        //public const int BUILT_SUIT = -4096;      // 0xf000  neither does rank 15
        public Int64 chkvalue;  // top 14 bits is value, 50bits = 10 * 5 where 5 is first faceup
        public int tag;

        // series is "ThisSeries" and are SEQ type for TopMost and BottomMost
        public List<card> TopMost;     // first card of the series of series
        public List<card> BottomMost;  // last card of the series of series = last card of any stack
        public List<int> UnexposedTypes;
        public List<int> Empties;
        public List<int> NonEmpties;
        public bool NotifySuitJustCompleted;
        public bool bSignalBoardComplete;
        static int[] vLast = new int[16];
        public int DealCountIndex;  // deals are made to a number of boards.
            // this index traces which deal was done here.  the first deal  may not have been the best
        public bool bOnLastDeal;
        public int bitJustCompleted;
        public bool bWasDealtTo;    // had no empty comumns and was dealt into
        public string strSuitsRemoved;
        public string DealString;

        public void init()
        {
            int i;
            score = 0;
            dead = false;
            nchild = 0;
            from = 0;
            bIsCompletable = false;
            NumEmptyColumns = 0;
            ThisColumn = new column[12];    // column 11 is the remaining deck and the 12th one is "complete"
            Empties = new List<int>();
            NonEmpties = new List<int>();
            Completed = new List<cCompleted>();
            NewlyCompleted = false;
            bitJustCompleted = 0;
            strSuitsRemoved = "";
            DealString = "";
            bWasDealtTo = false;    // not copied because it is used as a signaling variable
            for (i = 0; i < 12; i++)
            {
                ThisColumn[i] = new column();
                ThisColumn[i].iStack = i;
                ThisColumn[i].init();
            }
            bOnLastDeal = false;
            DealCounter = 0;
            NumCompletedSuits = 0;
            tag = 0;
            TopMost = new List<card>();
            BottomMost = new List<card>();
            SuitedSEQ = new List<series>();
            NotifySuitJustCompleted = false;
            SaveCompletedBy.Push((int)GlobalClass.TypeCompletedBy.ID_UNK);
            bSignalBoardComplete = false;
            UniqueID = BoardCounter++;
            SuitsLocked = false;
            BuildingThisSuit = 0;   // we are building "all" suits
            for (i = 0; i < 4; i++)
            {
                SuitStatus[i] = new cSuitedStatus();
                SuitStatus[i].MissingTypes = new List<int>();
                SuitStatus[i].MustExpose = new List<int>();
                SuitStatus[i].BonusExposes = new List<int>();
                SuitStatus[i].index = i;
                SuitStatus[i].NumCompletedAndRemoved = 0;
            }

            UnexposedTypes = new List<int>();
        }

        public board()
        {
            init();
        }

        public board(ref board nb)
        {
            int i, j;
            column cCol;
            init();
            // jys !!! need to copy the suitability stuff
            NumCompletedSuits = nb.NumCompletedSuits;
            bitJustCompleted = nb.bitJustCompleted; // fixs problem with suits missing from deck?
            for (i = 0; i < nb.UnexposedTypes.Count; i++)
                UnexposedTypes.Add(nb.UnexposedTypes[i]);
            strSuitsRemoved = nb.strSuitsRemoved;
            DealString = nb.DealString;
            for (i = 0; i < 11; i++)
            {
                cCol = nb.ThisColumn[i];
                for (j = 0; j < cCol.Cards.Count; j++)
                {
                    card nc = new card();
                    nc.bFaceUp = cCol.Cards[j].bFaceUp;
                    nc.ID = cCol.Cards[j].ID;
                    ThisColumn[i].Cards.Add(nc);
                }
            }

            foreach (cSuitedStatus cSS in SuitStatus)
            {
                int ii = cSS.index;
                cSS.bSuitsCompletable = nb.SuitStatus[ii].bSuitsCompletable;
                cSS.bExposesRequired = nb.SuitStatus[ii].bExposesRequired;
                cSS.UnexposedRankBits = nb.SuitStatus[ii].UnexposedRankBits;
                cSS.SuitedRankBits = nb.SuitStatus[ii].SuitedRankBits;
                cSS.NumCompletedAndRemoved = nb.SuitStatus[ii].NumCompletedAndRemoved;
            }
            ExtraPoints = nb.ExtraPoints;
            bOnLastDeal = nb.bOnLastDeal;
            score = nb.score;
            dead = nb.dead;
            nchild = nb.nchild;
            from = nb.ID;
            NumEmptyColumns = nb.NumEmptyColumns;
            DealCounter = nb.DealCounter;
            bIsCompletable = nb.bIsCompletable;
            MyMoves.CopyMoves(ref nb.MyMoves);
            BuildingThisSuit = nb.BuildingThisSuit;
            for (i = 0; i < nb.SuitedSEQ.Count; i++)
            {
                SuitedSEQ.Add(nb.SuitedSEQ[i]);
            }
            TimeOfLastDeal = nb.TimeOfLastDeal;
            foreach (cSuitedStatus css in SuitStatus)
            {
                for (i = 0; i < nb.SuitStatus[css.index].BonusExposes.Count; i++ )
                    css.BonusExposes.Add(nb.SuitStatus[css.index].BonusExposes[i]);
            }
                
            ReScoreBoard();
        }




        // need volatol copies - get them here
        public int CopyWorkingCards(ref List<card> CardList, GlobalClass.WorkingType wt)
        {
            CardList.Clear();
            switch (wt)
            {
                case GlobalClass.WorkingType.tBottomMost:
                    foreach (card c in BottomMost)
                        CardList.Add(c);
                    break;
                case GlobalClass.WorkingType.tTopMost:
                    foreach (card c in TopMost)
                        CardList.Add(c);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
            return CardList.Count;
        }
        public int CopyWorkingInts(ref List<int> IntList, GlobalClass.WorkingType wt)
        {
            IntList.Clear();
            switch (wt)
            {
                case GlobalClass.WorkingType.tEmpties:
                    foreach (int e in Empties)
                        IntList.Add(e);

                    break;
                case GlobalClass.WorkingType.tNonEmpties:
                    foreach (int e in NonEmpties)
                        IntList.Add(e);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
            return IntList.Count;
        }


        public int NumFacedownColumns()
        {
            int n = 0;
            foreach (int e in NonEmpties)
            {
                column cCol = ThisColumn[e];
                if (cCol.top == 0) continue;
                n++;
            }
            return n;
        }

        public bool RunEndgame()
        {
            if (NumEmptyColumns == 0) return false;
            if (NumEmptyColumns > 1) return true;
            if (ThisColumn[10].Cards.Count == 0) return true;
            if (bIsCompletable && NumEmptyColumns > 0) return true;
            return false;
        }

        public void BuildPlaceholders(ref List<card> PlaceHolder)
        {
            PlaceHolder.Clear();
            for (int i = 0; i < BottomMost.Count; i++)
            {
                BottomMost[i].ExcludePlaceholder = false;
                if (BottomMost[i].rank != 1)
                    PlaceHolder.Add(BottomMost[i]);
            }
        }

        // if a bit set then build that suit
        // set all bits (value = 0xf) for all suits
        public List<series> BuildSortedSEQ(int SuitMask, GlobalClass.eSortedSEQtype eSortType)
        {
            bool bAdded = false;
            SortedSEQ.Clear();
            int ThisSuitBit;
            bool bDoNotUse = true;
            foreach (int i in NonEmpties)
            {
                foreach (series s in ThisColumn[i].ThisSeries)
                {
                    ThisSuitBit = 1 << s.sSuit;
                    bDoNotUse = ((ThisSuitBit & SuitMask) == 0);
                    if (bDoNotUse) continue;
                    if (SortedSEQ.Count == 0)
                    {
                        SortedSEQ.Add(s);
                    }
                    else
                    {
                        bAdded = false;
                        for (int k = 0; k < SortedSEQ.Count; k++)
                        {
                            switch (eSortType)
                            {
                                case GlobalClass.eSortedSEQtype.SortBySize:
                                    if (SortedSEQ[k].size > s.size) // smallest size first
                                    {
                                        SortedSEQ.Insert(k, s);
                                        bAdded = true;
                                    }
                                    break;
                                case GlobalClass.eSortedSEQtype.SortByCost:
                                    if (SortedSEQ[k].nEmptiesToUnstack > s.nEmptiesToUnstack)   // smallest first
                                    {
                                        SortedSEQ.Insert(k, s);
                                        bAdded = true;
                                    }
                                    break;

                                case GlobalClass.eSortedSEQtype.SortByRank:
                                    if (SortedSEQ[k].topCard.rank < s.topCard.rank) // largest first
                                    {
                                        SortedSEQ.Insert(k, s);
                                        bAdded = true;
                                    }
                                    break;
                            }
                            if (bAdded) break;
                        }
                        if (!bAdded)
                        {
                            SortedSEQ.Add(s);
                        }
                    }
                }
            }
            return SortedSEQ;
        }

        public int ScoreBoard()
        {
            int i;
            score = 0;
            for (i = 0; i < 10; i++)
            {
                score += ThisColumn[i].value;
            }
            return score;
        }


        public int RemoveCompletedSuit(int iCol)
        {
            column tc = ThisColumn[iCol];
            int i, j, n = tc.ThisSeries.Count - 1;
            int suit = tc.ThisSeries[n].sSuit;
            Debug.Assert(n >= 0);
            j = tc.ThisSeries[n].bottom;   // position of last card in last series
            Debug.Assert(j == tc.Cards.Count - 1); // should always be last card
            // instead of deleting 13 cards from location top
            // we will move 13 cards from the bottom of the stack to stack 11 (12th stack)
            for (i = 0; i < 13; i++)
            {
                card c = tc.Cards[j - i];
                ThisColumn[11].Cards.Add(c);
            }
            tc.Cards.RemoveRange(tc.ThisSeries[n].top, 13);
            n = tc.Cards.Count - 1;
            if (n >= 0)
                tc.Cards[n].bFaceUp = true;
            return suit;
        }

        
        public int ReScoreBoard()
        {
            bool bChanged = ReScoreBoardEmptiesChanged();
            if (bChanged)
            {
                bChanged = ReScoreBoardEmptiesChanged();
                Debug.Assert(!bChanged);
            }
            if (GlobalClass.bLookForFirstColumn && (score > 500))
            {
                GlobalClass.FirstEmptyColumn = this;
                GlobalClass.bLookForFirstColumn = false;
                GlobalClass.bFoundFirstColunn = true;
                
            }

            return score;
        }

        public int BoardState()
        {
            // bits 0..3 for suits completable
            // bit 4 for bOnLastDeal
            int iState = (bOnLastDeal) ? 4 : 0;
            int j = 1;
            for (int i = 0; i < 4; i++)
            {
                if (SuitStatus[i].bSuitsCompletable) iState |= j;
                j = j << 1;
            }
            return iState;
        }

        public bool ReScoreBoardEmptiesChanged()
        {
            int i, j;
            int nEmptiesBefore = NumEmptyColumns;
            score = 0;
            int suit;
            cCompleted cC;
            bool JustFoundCompletedSuit = false;
            NewlyCompleted = false;
            bitJustCompleted = 0;

            for (i = 0; i < 10; i++)
            {
                j = ThisColumn[i].CalculateColumnValue(i, NumEmptyColumns, BoardState());
                JustFoundCompletedSuit = (j == 1000);
                NewlyCompleted |= JustFoundCompletedSuit;
                if (JustFoundCompletedSuit)
                {
                    suit = RemoveCompletedSuit(i);
                    strSuitsRemoved += GlobalClass.cSuits[suit].ToString();
                    SuitStatus[suit].NumCompletedAndRemoved++;
                    NumCompletedSuits++;
                    j = ThisColumn[i].CalculateColumnValue(i,NumEmptyColumns, BoardState());
                    cC = new cCompleted();
                    cC.suit = suit;
                    cC.score = j;
                    Completed.Add(cC);  // board ptr needs to be added at boardseries level
                    // there can be more than 1 series completed on a move
                    // IFF the move is a "DEAL"
                    SuitStatus[suit].bSuitsCompletable = false;    // there might be another suit of course
                    bitJustCompleted |= (1 << suit);
                    bIsCompletable = false;
                    for (j = 0; j < 4; j++)
                    {
                        bIsCompletable |= SuitStatus[j].bSuitsCompletable;
                    }
                    NotifySuitJustCompleted = true; // boardseries needs to set min/max values
                    //ExplainNoRescoreBoard("raw rescore");
                }
            }
            if (nEmptiesBefore != NumEmptyColumns) return true;
            GetSortedTops();    // this requires that series be valid
            score = CalcBoardScore();
            // the following needs to be changed because we should be able to re-compute values on the fly
            CheckBoardForSuitability(); // jys !!! OPTOMIZE: use this at "InitialBoard" or after a deal
            score += AddExtraForSuitability();

            for (i = 0; i < Completed.Count; i++)
            {
                AddSuitCompleted(Completed[i].suit);
            }
            Completed.Clear();
            if (score >= GlobalClass.MaxScore)
            {
                bSignalBoardComplete = true;
                ExplainNoRescoreBoard("board is complete!");
                GlobalClass.Board_Completed.Data.Add("MSG", "board is complete!");
                throw GlobalClass.Board_Completed;
            }

            return false;
        }

        public void AddSuitCompleted(int isuit)
        {
            int j = SaveCompletedBy.Peek();
            MyMoves.AddMoveInfo(GlobalClass.BUILT_SUIT, isuit, j);
            utils.AnnounceCompletion(isuit, "Completed suit " + GlobalClass.SuitNames[isuit]);
            //if(j != (int)board.TypeCompletedBy.ID_BS)  ExplainNoRescoreBoard("completed " + cGetSuits.SuitNames[isuit]);
            
        }

        // only needed for ranking (1..13 )
        private int CountRankBits(int iValue)
        {
            int i, j, k = 0;
            for (i = 1; i < 14; i++)
            {
                j = 1 << i;
                if ((iValue & j) > 0) k++;
            }
            return k;
        }

       //  simply marks the board as capable of being suited or not
        public bool CheckBoardForSuitability()
        {
            int i, j, r, s, rBit;
            column tCol;
            card tCrd;

            foreach(cSuitedStatus cSS in SuitStatus)
            {

                cSS.bSuitsCompletable = false;
                cSS.SuitedRankBits = 0;
                cSS.UnexposedRankBits = 0;
                cSS.bExposesRequired = false;
            }

            for (i = 0; i < 10; i++)
            {
                tCol = ThisColumn[i];
                if (tCol.Cards.Count == 0) continue;
                for (j = 0; j < tCol.Cards.Count; j++)
                {
                    tCrd = tCol.Cards[j];
                    r = tCrd.rank;
                    s = tCrd.suit;
                    rBit = 1 << r;
                    if (j < tCol.top)
                    {
                        SuitStatus[s].UnexposedRankBits |= rBit;
                        continue;
                    }
                    SuitStatus[s].SuitedRankBits |= rBit;
                }
            }

            bIsCompletable = AreAnySuitsCompletable();

            foreach(cSuitedStatus cSS in SuitStatus)
            {       // MissingTypes is the list of card types that are missing and are required to build the suit
                cSS.MissingTypes.Clear();
                cSS.MustExpose.Clear();
                if (cSS.bSuitsCompletable) continue;
                j = 1;
                for (r = 1; r < 14; r++)
                {
                    if (((j<<r) & cSS.SuitedRankBits) == 0)
                    {
                        int TypeOfMissing = utils.FindCardTypeFromInfo(r, cSS.index);
                        if (TypeOfMissing < 0)
                        {
                            cSS.bSuitNotOnBoard = true;
                            break;
                        }
                        if (!UnexposedTypes.Contains(TypeOfMissing))
                            cSS.MissingTypes.Add(TypeOfMissing);
                        else cSS.MustExpose.Add(TypeOfMissing);
                    }
                }
                if (UnexposedTypes.Count > 0)
                    cSS.MustExpose.Clear(); // dont track cards that we cannot use till after next deal

            }
            return bIsCompletable;
        }

        private bool AreAnySuitsCompletable()
        {
            bool BoardIsCompletable = false;
            foreach(cSuitedStatus cSS in SuitStatus)
            {
                //         static readonly int ALL_AVAILABLE = Convert.ToInt32("11111111111110", 2);
                cSS.bSuitsCompletable = (0x3ffe == cSS.SuitedRankBits);
                cSS.bExposesRequired = cSS.bSuitsCompletable ? false : ((cSS.SuitedRankBits | cSS.UnexposedRankBits) == 0x3ffe);
                BoardIsCompletable |= cSS.bSuitsCompletable;
            }
            return BoardIsCompletable;
        }


        // do not attempt to use any series in this function as it may have changed and was not re-calculated yet
        public int CalcBoardScore()
        {
            int i, k = 0;
            column tc;
            Empties.Clear();
            NonEmpties.Clear();
            NumEmptyColumns = 0;
            TotalCardsOnBoard = 0;

            for (i = 0; i < 10; i++)
            {
                tc = ThisColumn[i];
                if (tc.Cards.Count == 0)
                {
                    NumEmptyColumns++;
                    Empties.Add(i);
                }
                else
                {
                    TotalCardsOnBoard += tc.Cards.Count;
                    NonEmpties.Add(i);
                }

                k += tc.value;
            }
            i = 3000 * NumCompletedSuits;
            i += k;

            if (NewlyCompleted)
            {
                int isuit = Completed[0].suit;
                if(utils.bGetSetMessageGivenStatus(isuit))
                    ExplainNoRescoreBoard("raw rescore");
                NewlyCompleted = false;
            }
            score = i;
            return score;
        }

        private int AddExtraForSuitability()
        {
            int points=0;
            ExtraPoints = 0;
            foreach (cSuitedStatus css in SuitStatus)
            {
                int n = css.BonusExposes.Count;
                if (n > 0)
                {
                    points = GlobalClass.EXTRA_POINTS_IF_UNEXPOSED_CAN_COMPLETE_SUIT * n;
                }
            }
            ExtraPoints += points;
            return ExtraPoints;
        }

        private void GetSortedTops()
        {
            int j;
            bool bAdded;
            card tC, bC;
            column cCol;
            TopMost.Clear();
            BottomMost.Clear();
            foreach (int i in NonEmpties)
            {
                cCol = ThisColumn[i];
                // 8nov2012
                if (cCol.Cards.Count == 0) break;
                tC = cCol.ThisSeries.Last().topCard;
                bC = cCol.ThisSeries.Last().bottomCard;
                if (TopMost.Count == 0)
                {
                    TopMost.Add(tC);
                    BottomMost.Add(bC);
                }

                else
                {
                    bAdded = false;
                    for (j = 0; j < TopMost.Count; j++)
                    {
                        if (TopMost[j].rank < tC.rank)
                        {
                            TopMost.Insert(j, tC);
                            BottomMost.Insert(j, bC);
                            bAdded = true;
                            break;
                        }
                    }
                    if (!bAdded)
                    {
                        TopMost.Add(tC);
                        BottomMost.Add(bC);
                    }
                }
            }
        }

   
        public int FormDupList(int SrcCol, int SrcCard, int DesCol, int DesCard, ref int[] des, ref Int64 ChkValue)
        {
            int i, j, top, nCount, desptr = 0;
            int NumToMove;
            Int64 t;
            column tc = ThisColumn[SrcCol];
            card dc;
            int WouldBeTop, WouldBeCount;
            int odesptr;
            int n = tc.Cards.Count;
            bool bCollapsible = (tc.Cards[n - 1].rank == 1);    // must have an ace at the bottom of the move seq
            int suit = (int)tc.Cards[SrcCard].suit;    // suit of each of the cards we are moving
            int NumAltSuits = 0;

            if (ThisColumn[DesCol].ThisSeries.Count > 0)
            {
                series lseq = ThisColumn[DesCol].ThisSeries.Last();
                dc = ThisColumn[DesCol].Cards[DesCard - 1];
                bCollapsible &= ((tc.Cards[SrcCard].rank + 1) == dc.rank);
                bCollapsible &= (suit == dc.suit);
                bCollapsible &= (suit == lseq.sSuit);
                bCollapsible &= (lseq.topCard.rank == 13);
            }
            else bCollapsible = false;  // would never have got this far as it would have already collapsed
            // the above shows we can concat similar suits but we need to check the ones above the
            // destination card to see if they run up to king and also have the same suit



            NumToMove = ThisColumn[SrcCol].Cards.Count - SrcCard;
            Debug.Assert(NumToMove > 0);
            ChkValue = 0;
            for (i = 0; i < 10; i++)
            {
                tc = ThisColumn[i];
                nCount = tc.Cards.Count;
                top = tc.top;

                if (i == SrcCol)
                {
                    WouldBeCount = nCount - NumToMove;
                    WouldBeTop = SrcCard - 1;
                    if (WouldBeCount == 0)
                    {
                        des[desptr++] = 0;
                        continue;        //all were moved so column would be empty and nothing to shift for ChkWord
                    }
                    if (WouldBeTop < top)
                    {
                        Debug.Assert((top - WouldBeTop) == 1);
                        // we would have exposed a new card: there are no leftovers
                        des[desptr++] = 1;  // only 1 card, the newly exposed one
                        des[desptr++] = tc.Cards[top - 1].GetValue();
                    }
                    else
                    {
                        if (WouldBeTop > top)
                        {
                            WouldBeTop = top;
                            des[desptr++] = WouldBeCount - WouldBeTop;
                            for (j = WouldBeTop; j < WouldBeCount; j++)
                            {
                                des[desptr++] = tc.Cards[j].GetValue();
                            }
                        }
                        else
                        {
                            des[desptr++] = 1;
                            des[desptr++] = tc.Cards[top].GetValue();
                        }
                    }
                }
                else if (i == DesCol)
                {
                    WouldBeCount = nCount + NumToMove;
                    WouldBeTop = nCount > 0 ? top : 0;
                    // there are more cards in this column than the board shows so
                    // we have to sequence it manually to avoid going beyond the actual count
                    // we also need to see if the suit would have collapsed and the 13 cards removed
                    odesptr = desptr;
                    des[desptr++] = WouldBeCount - WouldBeTop;
                    for (j = top; j < nCount; j++)
                    {
                        des[desptr++] = tc.Cards[j].GetValue();
                    }
                    for (j = 0; j < NumToMove; j++)
                    {
                        des[desptr++] = ThisColumn[SrcCol].Cards[SrcCard + j].GetValue();
                    }
                    // check the last 13 cards and see if they form a completed suit
                    if (bCollapsible)
                    {
                        des[odesptr] -= 13;
                        desptr -= 13;
                    }
                }
                else
                {
                    WouldBeTop = top;
                    des[desptr++] = nCount - top;    // number exposed boards
                    for (j = top; j < nCount; j++)
                    {
                        des[desptr++] = tc.Cards[j].GetValue();
                    }
                }
                t = WouldBeTop;
                t = (t & 31) << (i * 5);
                ChkValue |= t;

            }
            NumAltSuits = CalcNumAltSuits(desptr, ref des);
            t = NumAltSuits;
            t = t << 50;
            ChkValue |= t;
            
            //return desptr;
            return utils.ReOrderDups(desptr, ref des);
        }

        private int CalcNumAltSuits(int ndesptr, ref int[] des)
        {
            int desptr = 0;
            int NumAltSuit = 0;
            int i, j, v, n;
            int lastsuit = 0, thissuit; // note that these are shifted up 4 bits
            bool bNeedFirst = true;
            for (i = 0; i < 10; i++)
            {
                n = des[desptr++];
                for (j = 0; j < n; j++)
                {
                    v = des[desptr++];
                    thissuit = v & 0x300;    // suit is above rank(8 bits) and suit take 2 bits
                    if (bNeedFirst)
                    {
                        bNeedFirst = false;
                        lastsuit = thissuit;
                    }
                    if (lastsuit != thissuit)
                    {
                        NumAltSuit++;
                        lastsuit = thissuit;
                    }
                }
            }
            return NumAltSuit;
        }

        public int ShowBoard()
        {
            int RtnVal = ReScoreBoard();
            ShowRawBoard();
            return RtnVal;
        }

        public string GetSuitables()
        {
            int any = 0;
            string strMove = "";
            string suitName = "";
            if (bIsCompletable)
            {
                strMove = " completables:";
                foreach(cSuitedStatus cSS in SuitStatus)
                {
                    suitName = GlobalClass.SuitNames[cSS.index];
                    if (cSS.bSuitsCompletable)
                    {
                        strMove += suitName + " ";
                        any++;
                    }
                    else
                    {
                        if (cSS.bExposesRequired)
                        {
                            string strEE = CountRankBits(cSS.UnexposedRankBits).ToString();
                            suitName = "[" + strEE + suitName.ToUpper() + "]";
                        }
                    }
                }
                Debug.Assert(any > 0);
            }
            return strMove;
        }



        public void ClearStacks()
        {
            int i;
            for (i = 0; i < 11; i++)
                ThisColumn[i].HoldMoves.Clear();
        }

        public card moveto(int FromStack, int FromLoc, int ToStack)
        {
            int n;
            card CardAbove = null;
            bool bWasUp = ThisColumn[FromStack].Cards[FromLoc].bFaceUp;
            Debug.Assert(bWasUp);

            n = ThisColumn[FromStack].Cards.Count - FromLoc;
            CardAbove = PerformMove(FromStack, FromLoc, ToStack, n);
            ThisColumn[FromStack].value =
                ThisColumn[FromStack].CalculateColumnValue(FromStack, NumEmptyColumns, BoardState());
            ThisColumn[ToStack].value = ThisColumn[ToStack].CalculateColumnValue(ToStack, NumEmptyColumns, BoardState());
            CalcBoardScore();
            return CardAbove;
        }



        // returns the card exposed by the move, if any
        public card PerformMove(int FromStack, int FromLoc, int ToStack, int NumMoved)
        {
            bool bWasFacedown = false;
            int i, suit;
            card NewCard, TopCardMoved;
            card CardExposed = null;
            column sCol = ThisColumn[FromStack];
            TopCardMoved = sCol.Cards[FromLoc];
            bool bWasUp = TopCardMoved.bFaceUp;
            int RankAbove, RankBelow, ToLoc;
            suit = TopCardMoved.suit;

#if DEBUG
            Debug.Assert((FromLoc + NumMoved) == sCol.Cards.Count);

            for (int j = 0; j < NumMoved; j++)
            {
                Debug.Assert(suit == sCol.Cards[j + FromLoc].suit);
            }
            Debug.Assert(bWasUp);
#endif

            ToLoc = ThisColumn[ToStack].Cards.Count;
            if (ToLoc > 0)
            {
                RankBelow = sCol.Cards[FromLoc].rank;
                RankAbove = ThisColumn[ToStack].Cards[ToLoc - 1].rank;
                Debug.Assert((RankAbove - RankBelow) == 1);
                if ((RankAbove - RankBelow) != 1)
                {
                    ShowRawBoard();
                    return null;// 0;
                }
            }


            for (i = 0; i < NumMoved; i++)
            {
                NewCard = sCol.Cards[FromLoc + i];
                NewCard.iStack = ToStack;   // the and the following are needed in case this card needs to be
                NewCard.iCard = i + ToLoc;  // accessed before the ReScore is called
                ThisColumn[ToStack].Cards.Add(NewCard);
            }
            sCol.Cards.RemoveRange(FromLoc, NumMoved);
            i = (ThisColumn[FromStack].Cards.Count);
            if (i > 0)
            {
                card cCrd = sCol.Cards[i - 1];
                if (!cCrd.bFaceUp)
                {
                    int b = 1 << cCrd.rank;
                    cSuitedStatus css = SuitStatus[cCrd.suit];
                    css.SuitedRankBits |= b;
                    AreAnySuitsCompletable();
                    b = ~b;
                    b = b & 0xffff; // keep to int size
                    css.UnexposedRankBits &= b;    // remove from unexposed "bits" and the list
                    UnexposedTypes.Remove(cCrd.type);
                    if (css.MustExpose.Contains(cCrd.type))
                        SuitStatus[cCrd.suit].BonusExposes.Add(cCrd.type);
                    bWasFacedown = true;
                }
                cCrd.bFaceUp = true;
                CardExposed = cCrd;
            }
            MyMoves.AddMove(TopCardMoved.ID, FromStack, ToStack, score, bWasFacedown, NumMoved);
            return CardExposed;
        }




        public void AssignCompletedID()
        {
            GlobalClass.TypeCompletedBy cb = (GlobalClass.TypeCompletedBy)SaveCompletedBy.Pop();
             CompletedBy = utils.SetCompletedBy(cb);
        }

        public void AssignCompletedID(GlobalClass.TypeCompletedBy cb)
        {
            SaveCompletedBy.Push((int)cb);
            CompletedBy = utils.SetCompletedBy(cb);
        }

        public void ExplainNoRescoreBoard(string cmnt)
        {
            Console.WriteLine(cmnt);
            MyMoves.TraceBoard();
            ShowRawBoard();
        }

        public void ExplainBoard(string cmnt)
        {
            ReScoreBoard();
            ExplainNoRescoreBoard(cmnt);
        }
 

        public bool deal(int lBest, DateTime TimeOfLastDeal)
        {
            int i, n = ThisColumn[10].Cards.Count - 1;
            this.TimeOfLastDeal = TimeOfLastDeal;
            if (n < 0)
            {
                bOnLastDeal = true;
                return false;
            }
            DealCounter++;
            utils.InitSuitCompletions();
            for (i = 0; i < 10; i++)
            {
                card NewCard = new card();
                NewCard = ThisColumn[10].Cards[n - i];
                Debug.Assert(ThisColumn[i].Cards.Count > 0);
                ThisColumn[i].Cards.Add(NewCard);
            }
            ThisColumn[10].Cards.RemoveRange(n - 9, 10);
            bOnLastDeal = (ThisColumn[10].Cards.Count == 0) ;
            ReScoreBoard(); // replaced "lBest" with the local deal counter
            if (bOnLastDeal && NumEmptyColumns > 1)
                score += 500;   // nov2012 want to keep a few colums empty
            MyMoves.AddMoveInfo(GlobalClass.DEALT_A_CARD,DealCountIndex, DealCounter);  // 11nov2012 need to id the deal
            foreach (cSuitedStatus css in SuitStatus)
            {
                css.BonusExposes.Clear();
            }
            return true;
        }

        // form the result of the move as if it had occured
        // note that DesLoc is 1 beyond the end of the stack as that is where it
        // fits into the column

        public int FormVerify(ref int[] des, ref Int64 ChkValue)
        {
            int i, j;
            int desptr = 0;
            Int64 t;
            int WouldBeTop;
            ChkValue = 0;
            for (i = 0; i < 10; i++)
            {
                int n = ThisColumn[i].Cards.Count;
                WouldBeTop = ThisColumn[i].top;
                des[desptr++] = n - WouldBeTop;
                for (j = 0; j < n; j++)
                {
                    if (j >= WouldBeTop)
                    {
                        des[desptr++] = ThisColumn[i].Cards[j].GetValue();
                    }
                }
                t = WouldBeTop;
                t = (t & 31) << (i * 5);
                ChkValue |= t;
            }
            t = CalcNumAltSuits(desptr, ref des);
            t = t << 50;
            ChkValue |= t;
           // return desptr;
            return utils.ReOrderDups(desptr, ref des);
        }


        public int ComputeRemainingSuits()
        {
            int i, n = 0;
            for (i = 0; i < 11; i++)
                n += ThisColumn[i].Cards.Count;
            Debug.Assert(n != 0 && n <= 104);
            n /= 13;
            n = 8 - n;
            NumCompletedSuits = n;
            return 8 - n;
        }


        public void ShowRawBoard()
        {
            ShowRawBoard(null,false,0);
        }

        public void ShowRawBoardIDs()
        {
            ShowRawBoard(null, true,0);
        }

        // Show11 shows column 11 but only for "adeck" 
        public  void ShowRawBoard(StreamWriter sw, bool bShowID, int Show11)
        {
            int m;
            string strOL;
            bool bMore = true;
            int i, n, r = 0;
            int tValue = 0;
            string strCnt, strMove;
            if (Show11 > 0)
            {
                strOL = "";
                for (i = 10; i >0;  i--)
                {
                    strOL += String.Format("{0,6}", i);
                }
                strOL += "\n";
                for (i = 1; i < 11; i++)
                {
                    strOL += String.Format("{0,6}", i);
                }
                sw.WriteLine(strOL+"\n");
            }
            m = 0;
            for (i = 0; i < 10 + Show11; i++)
            {
                n = ThisColumn[i].Cards.Count;
                if (n > m) m = n;
            }

            while (bMore)
            {
                bMore = false;
                strOL = "";
                for (i = 0; i < 10 + Show11; i++)
                {
                    n = ThisColumn[i].Cards.Count;
                    if (n <= r) strOL += "      ";
                    else
                    {
                        if (bShowID) strOL += ThisColumn[i].Cards[r].GetFormattedID(6);
                        else strOL += ThisColumn[i].Cards[r].GetFormattedName(6);
                    }
                }
                strCnt = string.Format("{0,4:0}", 10-(r%10));
                r++;

                strMove = strOL + strCnt;
                if (sw == null)
                    Console.WriteLine(strMove);
                else
                {
                    sw.WriteLine(strMove);
                }

                bMore = (r < m);
            }

            strOL = "";
            if (ExtraPoints > 0) strOL = " E(" + ExtraPoints + ")";
            if (ID == 18040)
            {
                int jj = 0;
            }
            strMove = "ID: " + ID + "  From: " + from + GetSuitables();
            strMove += " NumMoves:" + (NumMoves + MyMoves.TheseMoves.Count) + " Score:" + score + strOL + " RemStacks:" + ThisColumn[10].Cards.Count/10;
            strOL = "";

            if (sw == null)
                Console.WriteLine(strMove);
            else
            {
                sw.WriteLine(strMove);
            }


            for (i = 0; i < (11+Show11); i++)
            {
                int v = ThisColumn[i].value;
                if (i == (10+Show11))
                {
                    v = score;
                }
                else
                {
                    tValue += v;
                }
                string strTemp = String.Format("{0,6}", v);
                strOL += strTemp;
            }

            if (sw == null)
                Console.WriteLine(strOL + "\nDeals:" + DealString + "\n");
            else
            {
                sw.WriteLine(strOL + "\nDeals:" + DealString);
            }
        }

        public bool Comparable(ref board nb)
        {
            if(nb.score != score)return false;
            if(nb.NumEmptyColumns != NumEmptyColumns) return false;
            if (nb.NumCompletedSuits != NumCompletedSuits) return false;
            if (nb.UnexposedTypes.Count != UnexposedTypes.Count) return false;
            return true;
        }
        

    }
}
