﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nntpPoster
{
    public class RarAndRecoveryRecommendation
    {
        public Int64 FromFileSize { get; set; }
        public Int32 ReccomendedRarSize { get; set; }
        public Int32 ReccomendedRecoveryPercentage { get; set; }
    }
}
