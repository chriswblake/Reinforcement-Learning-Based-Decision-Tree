using System;
using System.Collections.Generic;
using System.Text;

namespace RLDT.DecisionTree
{
    /// <summary>
    /// Root = The first node which has no parents. It usualy only has 1 node, the first feature.
    /// Feature = The feature name to be examined. It usually has several subnodes which represent the possible values of this feature.
    /// Value = A single value at a feature. It may contain leaves or subnodes that point to another feature to be inspected.
    /// </summary>
    public enum TreeNodeType
    {
        Root,
        Feature, 
        Value
    }
}
