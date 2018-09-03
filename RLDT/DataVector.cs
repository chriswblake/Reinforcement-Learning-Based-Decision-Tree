using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RLDT
{
    public class DataVector
    {
        //Properties
        /// <summary>
        /// The list of all features and their respective values.
        /// </summary>
        public List<FeatureValuePair> Features { get; protected set; }

        //Indexer
        /// <summary>
        /// Retrieves a feature within this datavector, by name.
        /// </summary>
        /// <param name="featureName"></param>
        /// <returns></returns>
        public FeatureValuePair this[string featureName]
        {
            get
            {
                return Features.Find(f => f.Name == featureName);
            }
        }

        //Constructors
        /// <summary>
        /// Allows derived classes to work without requiring the base constructor.
        /// </summary>
        protected DataVector()
        {
            Features = new List<FeatureValuePair>();
        }

        /// <summary>
        /// A list of feature-value pairs for storing the complete information about a single data point.
        /// </summary>
        /// <param name="headers">The names of the features.</param>
        /// <param name="dataobjects">The actual values of each feature.</param>
        public DataVector(string[] headers, object[] dataobjects)
        {
            //Check number of headers matches number of data
            if (headers == null || headers.Length != dataobjects.Length)
                throw new FormatException("Number of headers and data objects per line do not match. Ensure there is a header for each value.");

            //Create list
            Features = new List<FeatureValuePair>();

            //Build list of features. 
            for (int i = 0; i < headers.Length; i++)
            {
                //Skip nulls
                if (headers[i] == null || dataobjects[i] == null)
                    continue;

                AddFeature(headers[i], dataobjects[i]);
            }
        }

        //Methods
        public void AddFeature(string featureName, object value)
        {
            FeatureValuePair fvp = new FeatureValuePair(featureName, value);
            Features.Add(fvp);
        }

        //Overrides
        public override string ToString()
        {
            string s = "";
            s += " Features=" + this.Features.Count;
            return s;

        }
    }
}