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
        /// <param name="importance">The relative importance of each feature (-1 to 1).</param>
        /// <param name="labelFeatureName">The feature to use as the label. It will be shifted out of the headers and dataobjects and stored as "Label".</param>
        public DataVectorTraining(string[] headers, object[] dataobjects, double[] importance, string labelFeatureName)
        {
            //Check number of headers matches number of data
            if ((headers.Length != dataobjects.Length) || (headers.Length != importance.Length))
                throw new FormatException("Number of headers, importance, and data per line do not match. Ensure there is a header and importance for each value.");

            //Check label feature is valid
            if (!headers.Contains(labelFeatureName))
                throw new ArgumentException("'labelFeatureName' must exist in the list of headers.");

            //Build list of features.
            Features = new List<FeatureValuePairWithImportance>().Cast<FeatureValuePair>().ToList();
            for (int i = 0; i < headers.Length; i++)
            {
                FeatureValuePairWithImportance fvpw = new FeatureValuePairWithImportance(headers[i], dataobjects[i], importance[i]);
                Features.Add(fvpw);
                fvpw.OnRemoveSelf += FeatureWithImportance_OnRemoveSelf;
            }

            //Find feature with label
            FeatureValuePair labelFeature = Features.Find(f => f.Name == labelFeatureName);
            labelFeature.OnRemoveSelf += Label_OnRemoveSelf;

            //Copy to data label
            Label = new FeatureValuePair(labelFeature.Name, labelFeature.Value);

            //Remove from the list of features
            Features.RemoveAll(p => p.Name == Label.Name);
        }

        //Events
        private void Label_OnRemoveSelf(object sender, EventArgs e)
        {
            //Remove the label
            this.Label = null;

            //Tell any parent items to remove this datavector.
            RemoveSelf();

            //Mark this datavector as disposed
            Dispose();
        }
        private void FeatureWithImportance_OnRemoveSelf(object sender, EventArgs e)
        {
            //Remove the feature from this datavector
            FeatureValuePairWithImportance fpvw = (FeatureValuePairWithImportance)sender;
            Features.Remove(fpvw);

            //Tell any parent items to remove this datavector.
            RemoveSelf();

            //Mark this datavector as disposed
            Dispose();
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