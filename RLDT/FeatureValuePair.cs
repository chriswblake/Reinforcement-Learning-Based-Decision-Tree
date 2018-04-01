using System;
using System.Collections.Generic;
using System.Text;

namespace RLDT
{
    /// <summary>
    /// A combination of feature name and its respective value.
    /// </summary>
    public class FeatureValuePair
    {
        //Properties
        /// <summary>
        /// The name of a single feature, usually from a DataVector.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The value associated with this feature name, usually from a Datavactor.
        /// </summary>
        public object Value { get; }

        //Constructors
        /// <summary>
        /// Creates a paired object of feature name and its value.
        /// </summary>
        /// <param name="name">The name of the feature.</param>
        /// <param name="value">The value of the feature.</param>
        public FeatureValuePair(string name, object value)
        {
            this.Name = name;
            this.Value = value;
        }

        //Overrides
        public override string ToString()
        {
            string s = "";
            s += " Name=" + this.Name.ToString().PadRight(20, ' ');
            s += " Value=" + this.Value.ToString().PadRight(8, ' ');
            //s += ", " + this.GetHashCode();
            return s;
        }
        public override int GetHashCode()
        {
            return Tuple.Create(Name, Value).GetHashCode();
        }
        public override bool Equals(object obj)
        {
            FeatureValuePair that = (FeatureValuePair)obj;
            return this.Name.Equals(that.Name)
                && this.Value.Equals(that.Value);
        }
    }

}
