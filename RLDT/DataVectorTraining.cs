using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RLDT
{
    /// <summary>
    /// An extension of the DataVector class. It includes a classification label for this datavector. Each feature also contains its relative reward (or cost).
    /// </summary>
    public class DataVectorTraining : DataVector
    {
        //Properties
        /// <summary>
        /// The classification label of this datavector.
        /// </summary>
        public FeatureValuePair Label { get; set; }

        //Indexer
        /// <summary>
        /// Retrieves a feature within this datavector, by name.
        /// </summary>
        /// <param name="featureName"></param>
        /// <returns></returns>
        new public FeatureValuePairWithImportance this[string featureName]
        {
            get
            {
                return (FeatureValuePairWithImportance) Features.Find(f => f.Name == featureName);
            }
        }

        //Constructors
        public DataVectorTraining()
        {
            Features = new List<FeatureValuePairWithImportance>().Cast<FeatureValuePair>().ToList();
        }
        /// <summary>
        /// Creates a Datavector, and adds additional information about feature rewards (or costs) and the classification label.
        /// </summary>
        /// <param name="headers">The names of the features.</param>
        /// <param name="dataobjects">The actual values of each feature.</param>
        /// <param name="rewards">tThe relative importance of each feature (-1 to 1).</param>
        /// <param name="labelFeatureName">The feature to use as the label. It will be shifted out of the headers and dataobjects and stored as "Label".</param>
        public DataVectorTraining(string[] headers, object[] dataobjects, double[] rewards, string labelFeatureName)
        {
            //Check number of headers matches number of data
            if (headers.Length != dataobjects.Length)
                throw new FormatException("Number of headers and data per line do not match. Ensure there is a header for each value.");

            //Build list of features. (Note: there is nothing about the reward yet. It is just hardcoded as -10.) 
            Features = new List<FeatureValuePairWithImportance>().Cast<FeatureValuePair>().ToList();
            for (int i = 0; i < headers.Length; i++)
                Features.Add(new FeatureValuePairWithImportance(headers[i], dataobjects[i], rewards[i]));

            //Find feature with label
            FeatureValuePair labelFeature = Features.Find(f => f.Name == labelFeatureName);

            //Copy to data label
            Label = new FeatureValuePair(labelFeature.Name, labelFeature.Value);

            //Remove from the list of features
            Features.RemoveAll(p => p.Name == Label.Name);
        }

        //Debug
        public override string ToString()
        {
            string s = "";
            s += " Features=" + this.Features.Count;
            s += " LabelFeatureName=" + this.Label.Name;
            s += " LabelValue=" + this.Label.Value;
            return s;

        }
    }
}