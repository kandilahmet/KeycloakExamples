using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossCutting.Configuration
{
    public class CookieSetttings
    {
        public string Name { get; set; }
        public string AccessDeniedPath { get; set; }
        public string LoginPath { get; set; }        
        public string HttpOnly { get; set; }  
        public  int  ExpireTimeSpan { get; set; }
        //public string Cookie.MaxAge { get; set; } 
    }
}
