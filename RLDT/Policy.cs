using System;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using RLDT.DecisionTree;

namespace RLDT
{
    /// <summary>
    /// The complete network of states that describes a reinforcement learning based decision tree.
    /// Learning is performed by the "Learn" method, by providing many data samples in the form of a
    /// DataVectorTraining class. After learning is finished, a new data point can
    /// be classified. The property "DecisionTree" provides a node based version of the resulting logic.
    /// </summary>
    public class Policy
    {
        //Fields
        /// <summary>
        /// Used for deciding a random query or the query with highest expected reward.
        /// </summary>
        private Random rand = new Random();
        private double _ExplorationRate = 0.0;
        private double _DiscountFactor = 0.8;
        private bool _TruncatedExplorationEnabled = false;
        private bool _ParallelPathUpdatesEnabled = true;
        private int _QueriesLimit = 10000;
        private TreeNode _DecisionTree = null;



        //Properties
        /// <summary>
        /// All of the possible states that have been explored. All states are stored using their hashcode, to avoid enumerating a list.
        /// </summary>
        private Dictionary<int, State> StateSpace = new Dictionary<int, State>();

        /// <summary>
        /// Provides the count of the states included in the policy.
        /// Note: Only a reference variable. Not used for functionality.
        /// </summary>
        public int StateSpaceCount
        {
            get
            {
                lock(stateSpacesLock)
                    return StateSpace.Count;
            }
        }

        /// <summary>
        /// A percentage value (0 to 0.9) of how often the learning should explore non-recommended paths.
        /// Low values are recommended for higher speed and lower accuracy.
        /// Higher values are recommended for  lower speed but higher accuracy.
        /// Warning: The search space is exponential.
        /// Default: 0.0
        /// </summary>
        public double ExplorationRate
        {
            get
            {
                return _ExplorationRate;
            }
            set
            {
                if (value < 0.0)
                    _ExplorationRate = 0;
                else if (value > 0.9)
                    _ExplorationRate = 0.9;
                else
                _ExplorationRate = value;
            }
        }

        /// <summary>
        /// The transfer rate (0.0 to 1.0) of information during learning. Default: 0.8.
        /// High values provides faster convergence but lower accuracy.
        /// Low values provides slower convergence but higher accuracy.
        /// Default: 0.8
        /// </summary>
        public double DiscountFactor
        {
            get
            {
                return _DiscountFactor;
            }
            set
            {
                if (value < 0.0 || value > 1.0)
                    throw new ArgumentException("DiscountFactor must be between 0.000 and 1.00. Higher numbers, near 1.0, are recommended.");
                _DiscountFactor = value;
            }
        }

        /// <summary>
        /// Enables truncated exploration. This decreases convergence time by updating the label
        /// percentages during each state visit. Howerver, it may cause unexplored branches.
        /// Default: false
        /// </summary>
        public bool ParallelReportUpdatesEnabled
        {
            get
            {
                return _TruncatedExplorationEnabled;
            }
            set { _TruncatedExplorationEnabled = value; }
        }

        /// <summary>
        /// Decreases convergence time by updating parallel paths in the state space.
        /// Default: true
        /// </summary>
        public bool ParallelQueryUpdatesEnabled
        {
            get
            {
                return _ParallelPathUpdatesEnabled;
            }

            set
            {
                _ParallelPathUpdatesEnabled = value;
            }
        }

        /// <summary>
        /// The maximum number of allowed queries before the classification label must be reported.
        /// </summary>
        public int QueriesLimit
        {
            get { return _QueriesLimit; }
            set
            {
                if (value < 1)
                    _QueriesLimit = 1;
                else
                    _QueriesLimit = value;
            }
        }

        /// <summary>
        /// A node based representation of the policy's resulting logic from training. It can be used for classifying
        /// a new datavector as well as displaying via HTML or simple text.
        /// </summary>
        public TreeNode DecisionTree
        {
            get
            {
                if (_DecisionTree == null)
                {
                    TreeSettings treeSettings = new TreeSettings()
                    {
                        ShowBlanks = true,
                        ShowSubScores = true
                    };
                    _DecisionTree = ToDecisionTree(treeSettings);

                }
                return _DecisionTree;
            }
            private set { _DecisionTree = value; }
        }



        //Locks
        private Object stateSpacesLock = new object();



        //Methods - Training
        /// <summary>
        /// Updates the network of states used for deciding the label of a future datavector. A training datavector
        /// is provided which contains all features, values, relative rewards, and the correct classification label.
        /// </summary>
        /// <param name="dataVector">A sample data point to learn from with features, values, relative rewards, and correct classification label.</param>
        /// <returns>The statistics of the learning process. See "TrainingStats" class for more details.</returns>
        public TrainingStats Learn(DataVectorTraining dataVector)
        {
            //Clear current decision tree
            DecisionTree = null;

            //Training statistics
            TrainingStats trainingDetails = new TrainingStats();

            //Create root state, if it does not exist
            lock(stateSpacesLock)
            { 
                if (StateSpace.Count == 0)
                {
                    State newState = new State(dataVector);
                    StateSpace.Add(newState.GetHashCode(), newState); trainingDetails.StatesCreated++;
                }
            }

            //Start with root state
            State rootState = StateSpace[0]; // 0 is the hashcode for a state with no features.
            Learn(rootState, dataVector, 0, trainingDetails);

            //Statistics
            trainingDetails.StatesTotal = StateSpace.Count;

            //Return
            return trainingDetails;
        }

        /// <summary>
        /// A recursive learning process. A state is updated and analised using a training datavector.
        /// The labels are initially updated, then it is determined if the current label or visiting another
        /// state provides greater reward.
        /// </summary>
        /// <param name="currentState">The state to be updated and analized.</param>
        /// <param name="dataVector">The list of features, rewards and label to update with</param>
        /// <param name="trainingDetails">Provides statistics of the learning process.</param>
        private void Learn(State currentState, DataVectorTraining dataVector, int totalQueries, TrainingStats trainingDetails)
        {
            //Choose random or best query
            Query recommendedQuery = null;
            if (rand.NextDouble() < ExplorationRate) //Pick random query 10% of the time 
            {
                //Pick random query
                Query randomQuery = currentState.GetRandomQuery(dataVector, rand);
                recommendedQuery = randomQuery;
            }
            else
            {
                //Find best query
                Query bestQuery = currentState.GetBestQuery(dataVector);
                recommendedQuery = bestQuery;
            }

            //Check total queries
            if (totalQueries > QueriesLimit)
                recommendedQuery = null;

            //Adjust expected reward of labels
            if (recommendedQuery == null || ParallelReportUpdatesEnabled)
                currentState.AdjustLabels(dataVector.Label);

            //If no query, then end training for this datapoint
            if (recommendedQuery == null)
                return;

            //Search for next state, or create it
            State nextState = null;
            int nextHashCode = currentState.GetHashCodeWith(recommendedQuery.Feature);
            lock (stateSpacesLock)
            { 
                if (StateSpace.ContainsKey(nextHashCode))
                {
                    //Get existing state
                    nextState = StateSpace[nextHashCode];
                }
                else
                {
                    //Create a new state
                    nextState = new State(currentState, recommendedQuery.Feature, dataVector);
                    StateSpace.Add(nextState.GetHashCode(), nextState); trainingDetails.StatesCreated++;
                }
            }

            //Process next state, to get adjustment for selected query
            Learn(nextState, dataVector, totalQueries+1, trainingDetails); trainingDetails.QueriesTotal++;

            //Update State's Query's expected reward
            if (ParallelQueryUpdatesEnabled)
            {
                //Update all queries that lead to this next state
                ParallelPathsUpdate(nextState, dataVector, trainingDetails);
            }
            else
            {
                //Update just current state's query
                double featureReward = dataVector[recommendedQuery.Feature.Name].Importance;
                double nextStateLabelReward = nextState.Labels[dataVector.Label];
                currentState.AdjustQuery(recommendedQuery, nextStateLabelReward, featureReward, DiscountFactor);
            }
            
            //Return
            return;
        }

        /// <summary>
        /// Updates all queries in other states that lead to this state.
        /// </summary>
        /// <param name="nextState">The state that comes after the query is performed.</param>
        /// <param name="dataVector">The relevant datavector for trainging.</param>
        private void ParallelPathsUpdate(State nextState, DataVectorTraining dataVector, TrainingStats trainingDetails)
        {
            //Get current expected reward of label
            double nextStateLabelReward = nextState.Labels[dataVector.Label];

            //Adjust queries in states that point to this "nextState".
            List<FeatureValuePair> nextStateFeatures = nextState.Features.ToList();
            foreach (FeatureValuePair theFeature in nextStateFeatures)
            {
                //Generate hashcode of a state that is missing this feature. i.e. A state that is only different by one feature, so it could lead to this state.
                int stateHashcode = nextState.GetHashCodeWithout(theFeature);

                //If the state exists, get it.
                State prevState = null;
                lock (stateSpacesLock)
                { 
                    if (StateSpace.ContainsKey(stateHashcode))
                    {
                        //Get the state
                        prevState = StateSpace[stateHashcode];
                    }
                    else
                    {
                        //Copy list of features
                        List<FeatureValuePair> prevStateFeatures = nextStateFeatures.ToList();

                        //Remove unwanted feature
                        prevStateFeatures.Remove(theFeature);

                        //Create a new state
                        prevState = new State(prevStateFeatures, dataVector);
                        StateSpace.Add(prevState.GetHashCode(), prevState); trainingDetails.StatesCreated++;
                        continue;
                    }
                }

                //Create the query to update
                Query theQuery = new Query(theFeature, dataVector.Label);

                //Get reward from datavector for querying this feature
                double featureReward = dataVector[theFeature.Name].Importance;

                //Adjust the query
                prevState.AdjustQuery(theQuery, nextStateLabelReward, featureReward, DiscountFactor);
            }
        }



        //Methods - Classification
        /// <summary>
        /// Uses the resulting "DecisionTree", summarized from the policy, to quickly classify a given datavector.
        /// </summary>
        /// <param name="dataVector"></param>
        /// <returns>Classification label</returns>
        public object Classify_ByTree(DataVector dataVector)
        {
            return DecisionTree.Classify(dataVector);
        }

        /// <summary>
        /// Uses the current policy to classify a datavector and return the best classification label.
        /// </summary>
        /// <param name="dataVector"></param>
        /// <returns>Classification label</returns>
        public object Classify_ByPolicy(DataVector dataVector)
        {
            //Start with root state
            State rootState = StateSpace[0]; // 0 is the hashcode for a state with no features.
            var labels = rootState.Labels;

            State currentState = rootState;
            while (true)
            {
                //Set current state's labels
                labels = currentState.Labels;

                //Find best query
                Query recommendedQuery = currentState.GetBestQuery(dataVector);

                //If no query, then end search. Use current labels.
                if (recommendedQuery == null)
                    break;

                //Search for next state
                State nextState = null;
                int nextHashCode = currentState.GetHashCodeWith(recommendedQuery.Feature);
                if (StateSpace.ContainsKey(nextHashCode))
                    nextState = StateSpace[nextHashCode];
                else
                    break;

                //Go to next state
                currentState = nextState;

            }

            //Select best label, by percentage probabilty
            double r = rand.NextDouble() * labels.Sum(p => p.Value); // Random value between 0 and the sum of expected rewards
            double runningTotal = 0;
            object bestLabelValue = null;
            foreach (var label in labels.OrderBy(p => p.Value))
            {
                runningTotal += label.Value;
                if (r < runningTotal)
                {
                    bestLabelValue = label.Key.Value;
                    break;
                }
            }

            //Return value of appropriate label
            return bestLabelValue;
        }

        /// <summary>
        /// Converts the learned logic of the state space into a simple node based decision tree.
        /// </summary>
        /// <param name="treeSettings">Settings for defining tree creation.</param>
        /// <returns></returns>
        public TreeNode ToDecisionTree(TreeSettings treeSettings)
        {
            if (StateSpace.Count == 0)
                return new TreeNode("root", TreeNodeType.Root);

            //Start with root state
            State rootState = StateSpace[0]; // 0 is the hashcode for a state with no features.

            TreeNode rootNode = new TreeNode("root", TreeNodeType.Root);
            ToDecisionTree(rootState, rootNode, treeSettings);

            return rootNode;
        }
        private void ToDecisionTree(State currentState, TreeNode parentNode, TreeSettings treeSettings)
        {
            //Find best group of queries
            List<KeyValuePair<Query, double>> bestGroupQueries = currentState.GetAverageGroupQueries();

            //Filter queries. (expected value must be greater than the label's expected value)
            List<KeyValuePair<Query, double>> bestGroupQueriesFiltered = new List<KeyValuePair<Query, double>>();
            foreach (KeyValuePair<Query, double> queryPair in bestGroupQueries.ToList())
            {
                double queryExpectedReward = queryPair.Value;
                double labelExpectedReward = currentState.Labels[queryPair.Key.Label];

                if (queryExpectedReward > labelExpectedReward)
                    bestGroupQueriesFiltered.Add(queryPair);
            }

            //Check if all features can be queried. If not, remove all.
            if (!treeSettings.ShowBlanks)
            {
                List<FeatureValuePair> uniqueFeatures = bestGroupQueriesFiltered.Select(p => p.Key.Feature).Distinct().ToList();
                foreach (FeatureValuePair uniqueFeature in uniqueFeatures)
                {
                    if (uniqueFeature.Name.ToString() == "bruises")
                    {

                    }

                    //Find next state and modify node
                    int nextStateHashCode = currentState.GetHashCodeWith(uniqueFeature);
                    if (!StateSpace.ContainsKey(nextStateHashCode))
                    {
                        bestGroupQueriesFiltered.Clear();
                        break;
                    }
                }
            }

            //Create group node
            TreeNode groupNode = parentNode;
            if (bestGroupQueriesFiltered.Count > 0)
            { 
                string groupName = bestGroupQueries.First().Key.Feature.Name;
                groupNode = new TreeNode(groupName, TreeNodeType.Feature);
                groupNode.Parent = parentNode;
                parentNode.SubNodes.Add(groupNode);
            }

            //Add queries
            if (bestGroupQueriesFiltered.Count > 0)
            {
                foreach (FeatureValuePair uniqueFeature in bestGroupQueries.Select(p=>p.Key.Feature).Distinct())
                {
                    //Create node
                    TreeNode valueNode = new TreeNode(uniqueFeature.Value.ToString(), TreeNodeType.Value);

                    //Add group
                    groupNode.SubNodes.Add(valueNode);
                    valueNode.Parent = groupNode;

                    //Find next state and modify node
                    int nextStateHashCode = currentState.GetHashCodeWith(uniqueFeature);
                    lock(stateSpacesLock)
                    { 
                        if(StateSpace.ContainsKey(nextStateHashCode))
                        { 
                            State nextState = StateSpace[nextStateHashCode];
                            ToDecisionTree(nextState, valueNode, treeSettings);
                        }
                    }

                }
            }

            //Add Labels
            if (bestGroupQueriesFiltered.Count == 0 || treeSettings.ShowSubScores)
                foreach (var theLabel in currentState.Labels)
                {
                    object labelValue = theLabel.Key.Value;
                    double expReward = theLabel.Value;

                    parentNode.Leaves.Add(new TreeLeaf(labelValue, expReward));
                }

            //Order subnodes and leaves by value
            groupNode.SubNodes = groupNode.SubNodes.OrderBy(p => p.Name).ToList();
            groupNode.Leaves = groupNode.Leaves.OrderBy(p => p.LabelValue).ToList();
            parentNode.Leaves = parentNode.Leaves.OrderBy(p => p.LabelValue).ToList();
        }



        //Overrides
        public override string ToString()
        {
            string s = "";
            s += " States=" + this.StateSpace.Count.ToString().PadRight(5, ' ');
            s += " DiscountFactor=" + this.DiscountFactor.ToString().PadRight(5, ' ');
            return s;
        }
    }
}