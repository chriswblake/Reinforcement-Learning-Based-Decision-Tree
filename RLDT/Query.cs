using System;
using System.Collections.Generic;
using System.Text;

namespace RLDT
{
    /// <summary>
    /// A combination of feature and related label. A list of queries is usually used for tracking possible
    /// transitions from state to state.
    /// </summary>
    public class Query : IRemoveSelf, IDisposable
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
            this.Feature = new FeatureValuePair(datavectorFeature.Name, datavectorFeature.Value); //To prevent additional details being stored by a derived object.
            this.Label = new FeatureValuePair(label.Name, label.Value);

            this.Feature.OnRemoveSelf += Feature_OnRemoveSelf;
            this.Label.OnRemoveSelf += Label_OnRemoveSelf;
        }

        //Events
        private void Feature_OnRemoveSelf(object sender, EventArgs e)
        {
            //Remove the feature
            this.Feature = null;

            //This query is no longer valid, so tell any parent to remove it.
            RemoveSelf();
            Dispose();
        }
        private void Label_OnRemoveSelf(object sender, EventArgs e)
        {
            //Remove the label
            this.Label = null;
            //This query is no longer valid, so tell any parent to remove it.
            RemoveSelf();
            Dispose();
        }
        public void RemoveSelf()
        {
            OnRemoveSelf?.Invoke(this, new EventArgs());
        }
        public event EventHandler OnRemoveSelf;

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
