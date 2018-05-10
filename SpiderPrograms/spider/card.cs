using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace spider
{
    public class cRankIndex
    {
        public int rank;
        public int index;
        public string PatLet;
        public cRankIndex(int rank, int index)
        {
            this.rank = rank;
            this.index = index;
            PatLet = "";
        }
    }

    public class cCardMeta
    {
        public int Type;
        public int Suit;
        public int Rank;
    }

    public class pseudoCard
    {
        public int ID;
        public int iStack;  // this is computed at the time it is to be moved
        public int iCard;	// this is computed at the time it is to be moved
        public bool desID_is_iStack;
        public int desID;	// ID of destination card to fit under or stack#
        public int size;	// number of cards to move (all cards of same suit underneath and rankedl)
        public int score;


        public pseudoCard(int CardID)
        {
            this.ID = CardID;                        
        }
        public pseudoCard(int CardID, int desStack)
        {
            this.ID = CardID;
            desID = desStack;
            desID_is_iStack = true;
        }
        public pseudoCard(int CardID, int desCardID, bool bDestination_is_CardID)
        {
            ID = CardID;
            this.desID = desCardID;
            this.desID_is_iStack = false;
        }
    }


    public class card
    {

        static cCardMeta[] CardMeta = GlobalClass.LocMeta;
        public int ID;
        public  int suit
        {
            get { return CardMeta[ID].Suit; }
            set { CardMeta[ID].Suit = suit; }
        }
        public int rank
        {
            get { return CardMeta[ID].Rank; }
            set { CardMeta[ID].Rank = rank; }
        }
        public int type
        {
            get { return CardMeta[ID].Type; }
            set { CardMeta[ID].Type = type; }
        }
        public bool bFaceUp;
        public int iCard;   // position of card in column
        public int GapAbove;    // if 0 then rank above == rank below, if +1 then exactly fits under card above
                                // useful for top card of a series.  A move is reversible if the gap is 1.
                                // also, when swapping like suits the cards moved from one column can go under 
                                // the swapped columns cards and no empty column is needed
        public int iStack;  // which column card is in

        public int WhichSEQSeries;   // which series of identical suits this card is in

        public int next;    // number of cards that follow this on that are in a series of identical suits

        public int tag;         // used to provide the number of cards that are to be moved when this card is moved
        // it also provides the cost of removing the cards that are underneath this card
        //    think these are one and the same
        // in addition , it provides the number of cards that are to be moved in a PerformMove
        // so as to avoid calculating the number of cards in the PerformMove program     

        public bool PartOfStackables;   // if true, then this card is part of an SEQ series that is being unstacked
        // and it must be excluded from any SOS series being built for discard purposes
        // make it a separate series from the other ones in the same column (stack)

        public bool ExcludePlaceholder;
        // if true then this card (or the stack it is on) cannot be used to temporarily hold a card
        // during an unstack or temporary move.  ie:  it is either the target of the final move or is
        // already holding a card in an unstack operation

        public card(ref card oldCard, int iStack, int iCard)
        {
            
            ID = oldCard.ID;
            this.iStack = iStack;
            this.iCard = iCard;
            ExcludePlaceholder = false;
        }
        public card()
        {
            bFaceUp = true;
            ExcludePlaceholder = false;
        }
        private char GetSuitChar()
        {
            char ch = Convert.ToChar(GlobalClass.cSuits.Substring(suit, 1));
            ch = bFaceUp ? char.ToUpper(ch) : char.ToLower(ch);
            return ch;
        }
        private string GetRankChar()
        {
            string strName = GlobalClass.rNames.Substring(rank * 2, 2);
            if (bFaceUp) strName = strName.ToUpper();
            return strName.ToString();
        }

        public string GetFormattedName(int PadTo)
        {
            string strCardName = GetRankChar() + GetSuitChar();
            int i, j = strCardName.Length;
            i = PadTo - j;
            if (i <= 0) return strCardName;
            return "                ".Substring(0, i) + strCardName;
        }


        public string GetFormattedID(int PadTo)
        {
            string strCardName = ID.ToString();
            int i, j = strCardName.Length;
            i = PadTo - j;
            if (i <= 0) return strCardName;
            return "                ".Substring(0, i) + strCardName;
        }


        //public void SetSuitChar(char ch)
        //{
        //    char achar = char.ToLower(ch);
        //    suit = GlobalClass.cSuits.IndexOf(ch);
        //}
        public int GetValue()
        {
            // there are 2 unused bits and no room for the "type"
            int b14_8;      // ID 0..103 (fits into 127 or 7 bits)
            int b7_6;       // the faceup bit
            int b5_4;       // suit fits into 0x3 (2 bits)
            int b3_0;       // rank fits into 0xf (4 bits: 0..13) with 0 unused
            b3_0 = rank;
            b5_4 = suit * 16;
            b7_6 = 0;
            if (bFaceUp) b7_6 = 64;
            b14_8 = type * 256;
            return ((b14_8 | b7_6 | b5_4 | b3_0));
        }

  
    }

}
