using System;
using System.Collections.Generic;
using System.Text;

namespace RLDT
{
    /// <summary>
    /// A combination of feature and related label. A list of queries is usually used for tracking possible
    /// transitions from state to state.
    /// </summary>
    public class Query
    {
        //Properties
        /// <summary>
        /// The feature that will be queried during training.
        /// </summary>
        public FeatureValuePair Feature { get; set; }

        /// <summary>
        /// The label provides context for comparing this query, for getting the expected reward. 
        /// </summary>
        public FeatureValuePair Label { get; set; }

        //Constructors
        public Query(FeatureValuePair datavectorFeature, FeatureValuePair label)
        {
            //Check for nulls
            if (datavectorFeature == null || label == null)
                throw new ArgumentException("Parameters cannot be null.");
            if (datavectorFeature.Name == null || datavectorFeature.Value == null)
                throw new ArgumentException("DatavectorFeature's Name and Value parameters cannot be null.");
            if (label.Name == null || label.Value == null)
                throw new ArgumentException("Labels's Name and Value parameters cannot be null.");

            //Save parameters
            this.Feature = new FeatureValuePair(datavectorFeature.Name, datavectorFeature.Value); //To prevent additional details being stored by a derived object.
            this.Label = new FeatureValuePair(label.Name, label.Value);
        }

        //Overrides
        public override string ToString()
        {
            string s = "";
            s += "" + this.Feature.ToString().PadRight(20, ' ');
            s += ", " + this.Label.Value.ToString().PadRight(8, ' ');
            //s += ", " + this.GetHashCode();
            return s;
        }
        public override int GetHashCode()
        {
            return Tuple.Create(Feature, Label.Value).GetHashCode();
        }
        public override bool Equals(object obj)
        {
            Query that = (Query)obj;
            return this.Feature.Equals(that.Feature)
                && this.Label.Equals(that.Label);
        }
    }
}
