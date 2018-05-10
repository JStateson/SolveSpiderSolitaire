using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;

/*
 * The purpose of this program is to make either XML or Spider "saved games" using an arbitary or made up deck
 * program can have 0, 1 or 3 arguments
 * 0 args: find and convert a saved file into an xml image
 * 1 arg:  saved file is supplied as first arguement to program
 *    output of 0 or 1 is same filename with extension of .xml
 * rest are not coded yet but will be
 * if 3 args the first is input saved file and second is input XML we want to use
 * 1: game saved file for use as a templete
 * 2: xml file we want to put into the templete
 * 3: game filename that will have the xml file from (2) that was put into (1)
 * */

namespace spider
{
    public class Program
    {
        static int XMLtoRead = 0;
        static void Main(string[] args)
        {
            bool bIsThere = false;
            string stTemp, strSpiderBin0;
            string PathToDirectory;
            string stName = "\\Spider Solitaire.SpiderSolitaireSave-ms";
            stTemp = System.Reflection.Assembly.GetEntryAssembly().Location; // path to executable
            if (args.Count() > 0)
            {
                stName = "\\" + args[0];
            }
            strSpiderBin0 = System.IO.Path.GetDirectoryName(stTemp) + stName;
            bIsThere = File.Exists(strSpiderBin0);
            if (bIsThere)
            {
                PathToDirectory = Path.GetDirectoryName(strSpiderBin0);
                GlobalClass.strSpiderBin = strSpiderBin0;
                cSpinControl cSC = new cSpinControl();
                board InitialBoard = new board();
                cSC.Deck = new cBuildDeck(strSpiderBin0, XMLtoRead, ref cSC);
                cSC.Deck.GetBoardFromSpiderSave(ref InitialBoard);
            }
        }
    }
}
