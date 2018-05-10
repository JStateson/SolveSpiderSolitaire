using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace spider
{
    public class cSeed
    {
        public List<int> Seeds;
        public List<int> SeedIndex;
        public cSeed()
        {
            Seeds = new List<int>(512);
            SeedIndex = new List<int>(512);
        }
        public void Clear()
        {
            Seeds.Clear();
            SeedIndex.Clear();
        }
        public void Add(ref board tb)
        {
            Seeds.Add(tb.UniqueID);
            SeedIndex.Add(tb.ID);
        }
        public bool bSameSeed(ref cSeed PrevSeed)
        {
            int i;
            int n = Seeds.Count;
            if (n != PrevSeed.Seeds.Count) return false;
            for (i = 0; i < n; i++)
            {
                if(!PrevSeed.Seeds.Contains(Seeds[i]))return false;
            }
            return true;
        }
        public void CopySeeds(ref cSeed ThisSeed)
        {
            Seeds.Clear();
            SeedIndex.Clear();
            for (int i = 0; i < ThisSeed.Seeds.Count; i++)
            {
                Seeds.Add(ThisSeed.Seeds[i]);
                SeedIndex.Add(ThisSeed.SeedIndex[i]);
            }
        }

  

    }
}
