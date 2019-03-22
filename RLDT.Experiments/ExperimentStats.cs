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
        /// A tracking variable for having a unique identifier. Deliberately hides the base class version
        /// so Id, Pass, and InstanceId appear together during reflection.
        /// </summary>
        public new int Id { get; set; }

        /// <summary>
        /// A tracking variable to know which pass of the data this is recorded from.
        /// </summary>
        public int Pass { get; set; }

        /// <summary>
        /// The unique ID of a training instance.
        /// </summary>
        public int InstanceID { get; set; }

        /// <summary>
        /// The percentage of correct classifications (0 to 1).
        /// </summary>
        public double TestingAccuracy { get; set; }

        /// <summary>
        /// Records the time required for the policy to learn a single instance.
        /// </summary>
        public long TrainingTime { get; set; }

        /// <summary>
        /// Records the time required to test all instances from the testing file.
        /// </summary>
        public long TestingTime { get; set; }

        //Methods
        public override string ToString()
        {
            string s = "";
            s += " Id=" + this.Id;
            s += " Accuracy=" + this.TestingAccuracy;
            s += base.ToString();
            return s;
        }
    }
}