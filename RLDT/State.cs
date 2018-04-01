using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;

namespace RLDT
{
    /// <summary>
    /// A single unique combination of feature-value pairs, that describes a portion of a datavector.
    /// Queries are used to indicate which are the most desirable next states, if that is a desired action.
    /// Labels are used for classifying a datavector that would visit this state.
    /// Queries and Labels can be compared to determine an action, such as moving to a next state or reporting a label.
    /// </summary>
    public class State
    {
        //Properties
        /// <summary>
        /// The labels and their occurence percentages, experienced at this state
        /// The dictionary key is the actual featurepair.
        /// The dictionary value is the expected reward (percentage occurence).
        /// </summary>
        public Dictionary<FeatureValuePair, double> Labels { get;} //double => expected reward

        /// <summary>
        /// A count of each label that has been experience at this state. If the count goes above 10000, it is automatically reduced by a factor of 10.
        /// The dictionary key is the feature-vale pair.
        /// THe dictionary key is the number of occurences.
        /// </summary>
        private Dictionary<FeatureValuePair, int> LabelsCount { get; set; } //int = instances seen

        /// <summary>
        /// The unique feature-value pairs that compose this state.
        /// </summary>
        public HashSet<FeatureValuePair> Features { get; set; }

        /// <summary>
        /// A summarized version of the features that have been inspected at this state.
        /// This is an efficiency variable, for the "Features" list, so the names do not need to be retrieved multiple times.
        /// </summary>
        private HashSet<string> FeatureNames { get; set; }

        /// <summary>
        /// The list of possible next states that are allowed from this state.
        /// The dictionary key is the actual query.
        /// THe dictionary value is the expected reward for chosing that query.
        /// </summary>
        public Dictionary<Query, double> Queries { get; set; }

        //Locks
        private Object labelsLock = new object();
        private Object featuresLock = new object();
        private Object queriesLock = new object();
        

        //Constructors
        /// <summary>
        /// Creates a new state by combining an existing state and new feature. Queries are updated using the datavector.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="additionalFeature"></param>
        /// <param name="dataVector"></param>
        public State(State original, FeatureValuePair additionalFeature, DataVectorTraining dataVector) : this(original, dataVector)
        {
            AddFeature(additionalFeature);
        }

        /// <summary>
        /// Creates a state from an existing state with no new features but a complete list of possible queries.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="dataVector"></param>
        public State(State original, DataVectorTraining dataVector) :this(dataVector)
        {
            //Add features from original. The list can't be directly copied because of synchronization with the queries list.
            foreach (FeatureValuePair theFeature in original.Features)
                AddFeature(theFeature);
        }

        /// <summary>
        /// Creates a new state with the specified features. Appropriate queries are built using the datavector.
        /// </summary>
        /// <param name="features">The features to be added to this state.</param>
        /// <param name="dataVector">An example data vector to create initial queries.</param>
        public State(List<FeatureValuePair> features, DataVectorTraining dataVector) : this(dataVector)
        {
            //Add features
            foreach (FeatureValuePair theFeature in features)
                AddFeature(theFeature);
        }

        /// <summary>
        /// Creates a "root" state with only queries.
        /// </summary>
        /// <param name="dataVector"></param>
        public State(DataVectorTraining dataVector)
        {
            this.Features = new HashSet<FeatureValuePair>();
            this.FeatureNames = new HashSet<string>();
            this.Queries = new Dictionary<Query, double>();
            this.Labels = new Dictionary<FeatureValuePair, double>();
            this.LabelsCount = new Dictionary<FeatureValuePair, int>();

            //Add missing details
            AddMissingQueriesAndLabels(dataVector);
        }



        //Methods
        /// <summary>
        /// Adds the given feature to the state and removes related queries. This feature is
        /// additionaly marked as the "MostRecentFeature" for convienance.
        /// </summary>
        /// <param name="theFeature"></param>
        private void AddFeature(FeatureValuePair theFeature)
        {
            lock(featuresLock)
            { 
                //Add to list of features
                Features.Add(new FeatureValuePair(theFeature.Name, theFeature.Value)); //to prevent storing derived classes
                FeatureNames.Add(theFeature.Name);
            }

            //Remove queries with same feature name
            lock (queriesLock)
            { 
                foreach (var q in Queries.ToList())
                {
                    if (q.Key.Feature.Name == theFeature.Name)
                        Queries.Remove(q.Key);
                }
            }
        }

        /// <summary>
        /// Updates the percentage probability of each label at this state.
        /// </summary>
        /// <param name="correctLabel"></param>
        public void AdjustLabels(FeatureValuePair correctLabel)
        {
            lock(labelsLock)
            { 
                //Add missing label
                if (!Labels.ContainsKey(correctLabel))
                    Labels.Add(correctLabel, 0.0);
                if (!LabelsCount.ContainsKey(correctLabel))
                    LabelsCount.Add(correctLabel, 0);

                //Increase experiences of label
                LabelsCount[correctLabel]++;

                //Reduce label counts occasionally (to prevent going to infinity)
                if (LabelsCount.Sum(p => p.Value) > 10000)
                    foreach(FeatureValuePair label in Labels.Select(p=>p.Key))
                    {
                        LabelsCount[label] /= 10;
                    }

                //Recalculate percentage
                int sumCount = LabelsCount.Sum(p=> p.Value);
                foreach (var l in Labels.ToList())
                {
                    Labels[l.Key] = LabelsCount[l.Key] / ((double) sumCount);
                }
            }
        }

        /// <summary>
        /// Updates the specified query's expected reward with a portion of the
        /// next state's expected reward.
        /// </summary>
        /// <param name="theQuery">The query to update.</param>
        /// <param name="nextStateLabelExpectedReward">The value of the label obtained from the next state.</param>
        /// <param name="featureReward">The reward for this feature, from the datavector. (-1 to 1)</param>
        /// <param name="discountFactor">The rate of value transfer. (0 to 1)</param>
        public void AdjustQuery(Query theQuery, double nextStateLabelExpectedReward, double featureReward, double discountFactor)
        {
            lock(queriesLock)
            { 
                //Convert feature reward to multiplier
                double featureMultiplier = featureReward + 1; // Example: -0.3 => 0.7 (less desireable feature)      0 => 1 (neutral)        0.9 => 1.9 (more desireable feature)

                //Adjust the value of the query
                Queries[theQuery] = featureMultiplier * discountFactor * nextStateLabelExpectedReward;
            }
        }

        /// <summary>
        /// Selects the best group of queries, then compares them to the appropriate label.
        /// If the query's expected reward is better than the label, it returns the query.
        /// If the label's expected reward is better, than it returns null, to indicate querying is not the recommended action.
        /// </summary>
        /// <param name="dataVector"></param>
        /// <returns></returns>
        public Query GetBestQuery(DataVector dataVector)
        {
            //Try to add new details
            if (dataVector.GetType() == typeof(DataVectorTraining))
                AddMissingQueriesAndLabels((DataVectorTraining)dataVector);

            //Get best queries (general)
            var bestQueriesGroup = GetAverageGroupQueries();

            //Build list of possible queries, that match datavector
            var possibleQueries = bestQueriesGroup.Where(q =>
                dataVector.Features.Find(f => q.Key.Feature.Equals(f))
                != null
            ).ToList();

            //If no possibilities
            if (possibleQueries.Count == 0)
                return null;

            //Result variable
            Query bestQueryResult = null; //Default: don't query, because the labels provide the best reward.

            #region Find best query, Version 1
            //Find best query for each label by expected reward
            List<KeyValuePair<Query, double>> bestQueries = new List<KeyValuePair<Query, double>>();
            
            foreach (var labelPair in Labels.ToList())
            {
                //Get label details
                FeatureValuePair theLabel = labelPair.Key;
                double theLabelExpectedReward = labelPair.Value;

                //Filter list by label
                var bestQueriesByLabel = possibleQueries.Where(q => q.Key.Label.Equals(theLabel)).ToList();
                if (bestQueriesByLabel.Count == 0) continue;

                //Get best query details
                var bestQueryPair = bestQueriesByLabel.OrderByDescending(p => p.Value).First();
                Query bestQuery = bestQueryPair.Key;
                double bestQueryExpectedReward = bestQueryPair.Value;

                //Is query better than label
                if (bestQueryExpectedReward > theLabelExpectedReward)
                    bestQueries.Add(bestQueryPair);
            }

            //Pick final answer
            if (bestQueries.Count > 0)
                bestQueryResult = bestQueries.OrderByDescending(q => q.Value).First().Key;
            #endregion

            #region Find best query, Version 2 -- this may work, and would be faster.
            ////Find best query pair
            //var bestQueryPair2 = possibleQueries.OrderByDescending(p => p.Value).First();
            //Query bestQuery2 = bestQueryPair2.Key;
            //double bestQuery2ExpectedReward = bestQueryPair2.Value;

            ////Find label
            //var labelPair2 = Labels.ToList().Find(p => p.Key.Equals(bestQuery2.Label));
            //Feature theLabel2 = labelPair2.Key;
            //double theLabel2ExpectedReward = labelPair2.Value;

            ////If query has higher expected reward, select it as the option.
            //Query bestQueryResult2 = null;
            //if (bestQuery2ExpectedReward > theLabel2ExpectedReward)
            //    bestQueryResult2 = bestQuery2;
            #endregion

            return bestQueryResult;
        }

        /// <summary>
        /// Picks a random query from the list of queries. However, it filters the list of
        /// queries to only be relevant to a provided datavector.
        /// </summary>
        /// <param name="dataVector"></param>
        /// <param name="rand"></param>
        /// <returns></returns>
        public Query GetRandomQuery(DataVectorTraining dataVector, Random rand)
        {
            //Try to add new details
            AddMissingQueriesAndLabels(dataVector);

            //If no queries available, all features are in the the state.
            if (Queries.Count == 0)
                return null;

            lock (queriesLock)
            {
                //Build list of possible queries, that match datavector
                var possibleQueries = Queries.Where(q =>
                    dataVector.Features.Find(f => q.Key.Feature.Equals(f))
                    != null
                ).ToList();

                //If no possibilities
                if (possibleQueries.Count == 0)
                    return null;

                //Pick random query from possibilities
                return possibleQueries[rand.Next(possibleQueries.Count)].Key;
            }
        }

        /// <summary>
        /// Calculates the average of every feature. Returns the group of queries with the best
        /// average feature expected reward.
        /// </summary>
        /// <returns></returns>
        public List<KeyValuePair<Query, double>> GetAverageGroupQueries()
        {
            if (Queries == null) return new List<KeyValuePair<Query, double>>();
            if (Queries.Count == 0) return new List<KeyValuePair<Query, double>>();

            lock(queriesLock)
            { 
                //Find all feature groups
                List<string> queryFeatureNames = Queries.Select(p => p.Key.Feature.Name).Distinct().ToList();

                //Get average reward for each feature
                Dictionary<string, double> featuresExpectedReward = new Dictionary<string, double>();
                foreach(string theFeatureName in queryFeatureNames)
                {
                    double avgExpectedReward = Queries.ToList().FindAll(p => p.Key.Feature.Name == theFeatureName).Average(q => q.Value);
                    featuresExpectedReward.Add(theFeatureName, avgExpectedReward);
                }

                //Find best query
                string bestFeature = featuresExpectedReward.OrderByDescending(p => p.Value).First().Key;

                //Find queries with same feature name
                List<KeyValuePair<Query, double>> queryGroup = Queries.ToList().FindAll(q => q.Key.Feature.Name == bestFeature).ToList();
                return queryGroup;
            }
        }

        /// <summary>
        /// All features of the datavector are inspected. If a feature-value combination is not
        /// in the list of queries, it is added. If a new label is encountered, it is also added.
        /// New queries are created optomistically with an expected reward of 1.
        /// New labels are created with an expected reward of 0;
        /// </summary>
        /// <param name="dataVector"></param>
        public void AddMissingQueriesAndLabels(DataVectorTraining dataVector)
        {
            lock(featuresLock)
            {
                lock(queriesLock)
                {
                    //Try to create new queries
                    foreach (FeatureValuePairWithImportance theFeature in dataVector.Features)
                    {
                        //Skip features names that are already in feature list.
                        if (FeatureNames.Contains(theFeature.Name))
                            continue;

                        //Create the possibly new query
                        Query newQuery = new Query(theFeature, dataVector.Label);

                        //Try to add the querry
                        if (!Queries.ContainsKey(newQuery))
                            Queries.Add(newQuery, 1);
                    }

                }
            }

            lock(labelsLock)
            {
                //Try to add the label
                AdjustLabels(dataVector.Label);
            }
        }
        
        /// <summary>
        /// Generates the hashcode of this state as if it includes an additional feature.
        /// </summary>
        /// <param name="withFeature"></param>
        /// <returns></returns>
        public int GetHashCodeWith(FeatureValuePair withFeature)
        {
            lock(featuresLock)
            { 
                var newFeatureHashSet = new HashSet<FeatureValuePair>(Features);
                newFeatureHashSet.Add(withFeature);
                return GenerateId(newFeatureHashSet).GetHashCode();
            }
        }

        /// <summary>
        /// Generates the hashcode of this state as if the specied feature is removed.
        /// </summary>
        /// <param name="WithoutFeature"></param>
        /// <returns></returns>
        public int GetHashCodeWithout(FeatureValuePair WithoutFeature)
        {
            lock(featuresLock)
            {
                //Copy list of features
                var newFeatureHashSet = new HashSet<FeatureValuePair>(Features);

                //Remove specified feature
                newFeatureHashSet.Remove(WithoutFeature);

                //Return zero if no features
                if (newFeatureHashSet.Count == 0) return 0;
                return GenerateId(newFeatureHashSet).GetHashCode();
            }
        }




        //Overrides
        /// <summary>
        /// Displays the number of features, queries, and labels as a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string s = "";
            //s += " ExpReward=" + this.ExpectedReward.ToString("N3").PadRight(8, ' ');
            s += " Features=" + this.Features.Count.ToString().PadRight(10, ' ');
            s += " Queries=" + this.Queries.Count.ToString().PadRight(10, ' ');
            s += " Labels=" + this.Labels.Count.ToString().PadRight(10, ' ');
            return s;
        }
        
        /// <summary>
        /// Generates the hashcode based only on what features are present. All other data about the state
        /// is ignored. This facilitates easy search of states, for example using a dictionary.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (Features == null) return 0;
            if (Features.Count == 0) return 0;

            return GenerateId(Features).GetHashCode();
        }
        
        /// <summary>
        /// The list of features is converted into a delimited text string. The features are
        /// ordered alphabetically by name to ensure the same Id.
        /// </summary>
        /// <param name="features"></param>
        /// <returns></returns>
        private string GenerateId(HashSet<FeatureValuePair> features)
        {
            lock(featuresLock)
            { 
                return string.Join(";", features.OrderBy(p => p.Name).Select(p => (p.Name + ":" + p.Value)));
            }
        }
    }
}
