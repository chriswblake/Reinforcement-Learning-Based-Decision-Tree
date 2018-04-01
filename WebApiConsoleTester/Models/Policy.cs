using System;
using System.Collections.Generic;
using System.Text;

namespace WebApiConsoleTester.Models
{
    class Policy
    {
        //Properties
        public int PolicyId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int StateSpaceCount { get; set; }
    }
}
