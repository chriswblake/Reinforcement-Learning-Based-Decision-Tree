using System;
using System.Collections.Generic;
using System.Text;

namespace RLDT.DecisionTree
{
    /// <summary>
    /// A combination of classification label and expected reward (usually percentage probability).
    /// </summary>
    public class TreeLeaf
    {
        //Properties
        /// <summary>
        /// The classification label value.
        /// </summary>
        public object LabelValue { get; set; }
        /// <summary>
        /// The expected reward, usually as a percentage probability.
        /// </summary>
        public double ExpectedReward { get; set; }

        //Constructors
        /// <summary>
        /// Creates a pair of label and expected reward (usually percentage probability).
        /// </summary>
        /// <param name="labelValue">The classification label value.</param>
        /// <param name="expectedReward">The expected reward (usually percentage probability) for choosing this label.</param>
        public TreeLeaf(object labelValue, double expectedReward)
        {
            this.LabelValue = labelValue;
            this.ExpectedReward = expectedReward;
        }

        //Overrides
        public override string ToString()
        {
            return LabelValue + ": " + ExpectedReward.ToString("N3");
        }
    }
}
