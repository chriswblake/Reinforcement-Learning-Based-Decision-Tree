using System;
using System.Collections.Generic;
using System.Text;

namespace RLDT
{
    public class PolicyChangeEventArgs : EventArgs
    {
        public State State { get; set; }
        public Query Query { get; set; }
        public FeatureValuePair Label { get; set; }
    }
}
