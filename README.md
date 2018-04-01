# Reinforcement-Learning-Based-Decision-Tree

## 1 Introduction
A classification process, based on (Garlapati et al. 2015), is developed by using reinforcement learning to produce decision trees and then evaluated by altering various parameters. This use of reinforcement learning enables the creation of a decision tree without the downsides of traditional methods, which usually require prior knowledge, greedy separation techniques, and pruning. Additionally the use of reinforcement learning enables extended functionalities such as online learning, limited drift resistance, per-feature importance weights, and handling of partial data vectors.

The reinforcement learning process develops a state space created from the experienced combinations of features, feature values, and classification labels. As transitions occur between states (a combination of features), expected rewards are propagated. Finally, upon reaching an end condition state, a classification label is predicted and compared, thereby producing the reward, that will eventually be maximized. As learning increases the best end states are identified, leading to the shortest and most accurate decision tree.

To gain better understanding and the limitations of reinforcement learning based decision trees, various topics are evaluated. Such topics include data type, data order, overfitting, discount factor, exploration rate, drifting, and feature space.

## 2 MDP Formulation
The Markov Decision Process (MDP) is modeled similar to the process described in (Garlapati et al. 2015), although with minor modifications. As such, the important components are defined as follows:

- **State Space** – All of the possible states of the MDP.
- **State** – A combination of features and their respective values that define a portion of a data vector. It may include all or some of the features of the feature space.
- **Data vector** – A set of features and their associated values which fully define a single data point within a data set.
- **Feature** – A single, ideally unique, characteristic of a data point.
- **Query** – An action possibility at each state, which reads another feature’s value from the data vector, and causes transition to another state. Naturally, a state cannot query a feature that it already contains.
- **Report** – An action possibility at each state, which predicts the classification label.

The expected rewards of query actions are updated after each transition to a new state, while the expected rewards of report actions are only updated when a classification label is reported and checked. Determination of each state’s expected reward is thereby a natural propagation of the expected reward via queries from end states, where a report occurred, to the root state, where the first query is decided. Naturally, two factors must be considered during these transitions. First being the transfer of knowledge between states, solved by using a traditional discount factor as described in (Sutton and Barto 1998). Second being the feature’s importance or “cost”, included in the training data vector.

1. **Discount Factor** – The base transfer rate of rewards between a state’s report rewards and another state’s queries, usually between 0.8 and 0.99.
2. **Feature Importance** – A normalized weighting to encourage or discourage feature inclusion during training, aiding support for partial data vectors.
   - +1 indicates a more desirable feature
   -  0 indicates neutral importance
   - -1 indicates a less desirable feature

