﻿using System;
using System.Collections.Generic;
using System.Text;

namespace AdsAgregator.SearchEngine.Database.Tables
{
    class ApplicationUser
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string MobileAppToken { get; set; }
    }
}