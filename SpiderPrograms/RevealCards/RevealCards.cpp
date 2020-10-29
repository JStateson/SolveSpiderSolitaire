// RevealCards.cpp : Defines the entry point for the application.
//

#include "RevealCards.h"

using namespace std;



const int BUFFER_SIZE = 1024 * 256;
unsigned char buffer[BUFFER_SIZE];
int n, MaxIndex, iOffset = 23 * 2;
int TraversePtr = 0;

static short int FaceDown[23] = {
0x3c,0x46,0x61,0x63,0x65,0x55,0x70,0x3e,0x66,0x61,0x6c,0x73,0x65,0x3c,0x2f,0x46,0x61,0x63,0x65,0x55,0x70,0x3e,0x0a };


static short int FaceUp[23] = {
0x3c,0x46,0x61,0x63,0x65,0x55,0x70,0x3e,0x74,0x72,0x75,0x65,0x3c,0x2f,0x46,0x61,0x63,0x65,0x55,0x70,0x3e,0x0a,0x20 };

bool bCompareChars()
{
	int iBuf;
	for (int i = 0; i < 23; i++)
	{
		iBuf = i * 2;
		if (FaceDown[i] != buffer[TraversePtr + iBuf])
		{
			return false;
		}
	}
	return true;
}

// look for "0x3c" as a byte in an integer array ie: 3c 00 46 00, etc
int findFD()
{
	bool bFound;
	int iStart;
	if (TraversePtr >= MaxIndex)return 0; // no more in file
	TraversePtr--;
	do
	{
		TraversePtr++;
		iStart = TraversePtr;
		bFound = bCompareChars();
		if (bFound)
		{
			memcpy(&buffer[iStart], (char *)FaceUp, sizeof(FaceUp));
			TraversePtr = iStart + iOffset;
		}
	} while (TraversePtr < MaxIndex);

	return 0;
}

int main()
{


	char *strSrc = "Spider Solitaire.SpiderSolitaireSave-ms";
	char *strDes = "Faceup Spider Solitaire.SpiderSolitaireSave-ms";
	int m;

	FILE *fin = fopen(strSrc, "rb");
	FILE *fout = fopen(strDes, "wb");

	if (fin == NULL)
	{
		printf(" file %s cannot be found\n", strSrc);
		exit(0);
	}

	if (fout == NULL)
	{
		printf(" file %s cannot be written to\n",strDes);
		exit(0);
	}

	n = (int) fread(buffer, 1, BUFFER_SIZE, fin);
	if(n <= 0)
	{
		printf("file  %s is empty or not readable\n", strSrc);
		exit(0);
	}

	MaxIndex = n - iOffset;

	// find pattern in array
	m = 0;
	do {
		int iLoc = findFD();
		int j;
		if (iLoc == 0)break;
		m++;
		for (int i = 0; i < 23; i++)
		{
			j = i * 2;
			buffer[iLoc + j] = FaceUp[i];
		}
		printf("Found %d\n", m);
		TraversePtr = iLoc + iOffset;
	} while (true);

	fwrite(buffer, 1, n, fout);
	return 0;
}
