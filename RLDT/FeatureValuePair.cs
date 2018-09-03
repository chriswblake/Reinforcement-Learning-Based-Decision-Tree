using System;
using System.Collections.Generic;
using System.Reflection;
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
        public string Name { get; private set; }
        /// <summary>
        /// The value associated with this feature name, usually from a Datavactor.
        /// </summary>
        public object Value { get; private set; }

        //Constructors
        /// <summary>
        /// Creates a paired object of feature name and its value.
        /// </summary>
        /// <param name="name">The name of the feature.</param>
        /// <param name="value">The value of the feature.</param>
        public FeatureValuePair(string name, object value)
        {
            //Check for nulls
            if (name == null || value == null)
                throw new ArgumentException("Parameters cannot be null.");

            //Save parameters
            this.Name = name;
            this.Value = value;

            //Subscribe to RemoveSelf event, if it exists.
            if (value.GetType().GetInterface("IRemoveSelf") != null)
            {
                //Retrieve event from value
                EventInfo eventInfo = value.GetType().GetEvent("OnRemoveSelf");

                //Convert to delegate
                Type tDelegate = eventInfo.EventHandlerType;
                MethodInfo miHandler = typeof(FeatureValuePair).GetMethod("Value_OnRemoveSelf",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                Delegate d = Delegate.CreateDelegate(tDelegate, this, miHandler);
                
                //Subscribe to the event
                eventInfo.GetAddMethod().Invoke(value, new object[] { d });
            }
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
