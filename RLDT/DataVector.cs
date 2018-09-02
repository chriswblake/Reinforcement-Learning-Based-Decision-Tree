using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RLDT
{
    public class DataVector : IDisposable, IRemoveSelf
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
                FeatureValuePair fvp = new FeatureValuePair(headers[i], dataobjects[i]);
                Features.Add(fvp);
                fvp.OnRemoveSelf += Feature_OnRemoveSelf;

            }
        }

        //Overrides
        public override string ToString()
        {
            string s = "";
            s += " Features=" + this.Features.Count;
            return s;

        }

        //Events
        protected void Feature_OnRemoveSelf(object sender, EventArgs e)
        {
            //Remove the feature from this datavector
            FeatureValuePair theFeature = (FeatureValuePair)sender;
            Features.Remove(theFeature);

            //This datavector is invalid. Tell any parent items to remove it.
            RemoveSelf();
            Dispose();
        }
        public event EventHandler OnRemoveSelf;
        public void RemoveSelf()
        {
            OnRemoveSelf?.Invoke(this, new EventArgs());
        }

        #region IDisposable Support
        public bool IsDisposed { get { return disposedValue; } }
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DataVector() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}