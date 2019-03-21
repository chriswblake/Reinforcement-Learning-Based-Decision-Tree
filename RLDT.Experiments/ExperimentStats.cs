using System;
using System.Collections.Generic;
using System.Text;

namespace RLDT.Experiments
{
    public class ExperimentStats : RLDT.TrainingStats
    {
        //Constructors
        public ExperimentStats() { }
        public ExperimentStats(TrainingStats ts)
        {
            this.Id = ts.Id;
            this.StatesTotal = ts.StatesTotal;
            this.QueriesTotal = ts.QueriesTotal;
            this.StatesCreated = ts.StatesCreated;
        }

        //Properties
        /// <summary>
        /// A tracking variable to know which pass of the data this is recorded from.
        /// </summary>
        public int Pass { get; set; }

        /// <summary>
        /// The unique ID of a training instance.
        /// </summary>
        public int InstanceID { get; set; }

        /// <summary>
        /// The number of correct classifications, usually from testing.
        /// </summary>
        public int CorrectClassifications { get; set; }

        //Methods
        public override string ToString()
        {
            string s = base.ToString();
            s += "  Correct=" + CorrectClassifications;

            return s;
        }
    }
}
