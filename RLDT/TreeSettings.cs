using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RLDT.DecisionTree
{
    /// <summary>
    /// A helper class for providing settings during Policy to TreeNode creation. It allows determining
    /// what 'extra' information will be included.
    /// </summary>
    public class TreeSettings
    {
        //Properties
        /// <summary>
        /// Displays incomplete tree paths, that learning did not finish exploring.
        /// </summary>
        public bool ShowBlanks { get; set; }

        /// <summary>
        /// Displays summary scores at each decision level, before another feature would be read.
        /// </summary>
        public bool ShowSubScores { get; set; }
    }
}
