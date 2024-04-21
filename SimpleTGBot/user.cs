using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTGBot
{
    public class user
    {
        public long id { get; set; }
        public bool bugs { get; set; }
        public bool automatons { get; set; }
        public user(long id, bool bugs, bool automatons)
        {
            this.id = id;
            this.bugs = bugs;
            this.automatons = automatons;
        }
    }
}
