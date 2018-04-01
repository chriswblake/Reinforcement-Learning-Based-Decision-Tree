using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RLDT.DecisionTree
{
    /// <summary>
    /// A single node used for building a decision tree. Nodes are linked together in a "vertical" style
    /// with feature, value, and leaves. Together they explain the logic for deciding a classification label (leaf).
    /// </summary>
    public partial class TreeNode
    {
        //Fields
        /// <summary>
        /// Used for randomly picking the classification label (by percentage chance).
        /// </summary>
        private static Random rand = new Random();



        //Properties
        /// <summary>
        /// The node that pointed to this node.
        /// </summary>
        public TreeNode Parent { get; set; }

        /// <summary>
        /// The feature name or feature value.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// For tracking if the node is a feature, value, or the root node. Simply allows easier processing.
        /// </summary>
        public TreeNodeType Type { get; set; }

        /// <summary>
        /// All child nodes of this node. Usually the different values associated with a single feature.
        /// </summary>
        public List<TreeNode> SubNodes { get; set; }

        /// <summary>
        /// The possible outcomes at a node. Usually the different classifications at a feature's value.
        /// </summary>
        public List<TreeLeaf> Leaves { get; set; }



        //Contructors
        /// <summary>
        /// Builds a node with only name and node type (feature, value, root).
        /// </summary>
        /// <param name="groupName">The feature name, or the feature value.</param>
        /// <param name="type">If the node is a feature, value, or the root node.</param>
        public TreeNode(string groupName, TreeNodeType type)
        {
            this.Parent = null;
            this.Name = groupName;
            this.Type = type;
            this.SubNodes = new List<TreeNode>();
            this.Leaves = new List<TreeLeaf>();
        }



        //Methods
        /// <summary>
        /// Uses the logic of this node and its subnodes to classify a provided datavector.
        /// </summary>
        /// <param name="dataVector"></param>
        /// <returns></returns>
        public object Classify(DataVector dataVector)
        {
            TreeNode DecisionTree = this;

            //Get root node of decision tree, and set current best labels
            TreeNode currentNode = DecisionTree;
            List<TreeLeaf> labels = currentNode.Leaves;

            //Skip root (if able)
            if (currentNode.Type == TreeNodeType.Root && currentNode.SubNodes.Count == 1)
                currentNode = currentNode.SubNodes.First();

            //Cycle through tree
            if (currentNode.Type == TreeNodeType.Feature)
                while (true)
                {
                    //Determine feature
                    string currentFeature = currentNode.Name;

                    //Get the feature from the datavector
                    FeatureValuePair theFeature = dataVector[currentFeature];

                    //If feature not available, end loop. Use the current best labels.
                    if (theFeature == null)
                        break;

                    //Get the value from the datavector
                    object value = theFeature.Value;

                    //Pick next node
                    TreeNode valueNode = currentNode.SubNodes.Find(p => value.Equals(p.Name));

                    //Check if value node is not finished (incomplete tree)
                    if (valueNode == null)
                        break;

                    //Save current labels (they are more accurate)
                    if (valueNode.Leaves.Count != 0)
                        labels = valueNode.Leaves;

                    //If there is a subnode, there is another feature. Switch to it.
                    if (valueNode.SubNodes.Count == 1)
                        currentNode = valueNode.SubNodes.First();

                    //Check end condition. The end of the branch has been reached.
                    if (valueNode.SubNodes.Count == 0)
                        break;
                }

            //Get best label from labels list
            object bestLabelValue = PickLabelByProbability(labels);

            return bestLabelValue;
        }

        /// <summary>
        /// Upon reaching a leaf, a set of possible classification labels is used to return the
        /// best expected label. Each label has a percentage probability, which is used in a weighted
        /// random decision process.
        /// </summary>
        /// <param name="labels">A list of the possible classification labels and their percentage probability.</param>
        /// <returns></returns>
        private object PickLabelByProbability(List<TreeLeaf> labels)
        {
            double r = rand.NextDouble() * labels.Sum(p => p.ExpectedReward); // Random value between 0 and the sum of expected rewards
            double runningTotal = 0;
            object bestLabelValue = null;
            foreach (var label in labels.OrderBy(p => p.ExpectedReward))
            {
                runningTotal += label.ExpectedReward;
                if (r < runningTotal)
                {
                    bestLabelValue = label.LabelValue;
                    break;
                }
            }

            return bestLabelValue;
        }



        //Overrides
        public override string ToString()
        {
            return Name + "     " + "Subnodes=" + SubNodes.Count + "     " + "Leaves=" + Leaves.Count;
        }
    }

    /// <summary>
    /// Add visualization functionality to the TreeNode class.
    /// </summary>
    public partial class TreeNode
    {
        /// <summary>
        /// Converts this tree node to a visual tree in HTML store in a div with class='DecisionTree'.
        /// Default styling is provided.
        /// </summary>
        /// <returns></returns>
        public string ToHtmlTree()
        {
            return ToHtmlTree(true);
        }

        /// <summary>
        /// Converts this tree node to a visual tree in HTML store in a div with class='DecisionTree'.
        /// Default styling can be disabled.
        /// </summary>
        /// <param name="includeDefaultStyle">If true, default styling is included. If false, no styling is provided.</param>
        /// <returns></returns>
        public string ToHtmlTree(bool includeDefaultStyle)
        {
            string html = "";
            if (includeDefaultStyle)
            { 
                #region html styling
                html += @"
    <style>
    div.DecisionTree table {
        border-collapse: collapse;
        padding: 0px;
        margin: 0px;
        font: 8pt, arial, sans-serif;
        border: 0px solid #EEEEEE;
    }

    div.DecisionTree table td {
        vertical-align: top;
        text-align: center;
    }

    div.DecisionTree table td.Feature {
        border-bottom: 1px solid black;
        font-weight: bold;
    }

    div.DecisionTree table td div.Leaf {
        border: 1px solid #333333;
        background-color: #CCCCCC;
        padding: 3px 10px 3px 10px;
        white-space: nowrap;
    }

    </style>
    ";
                    #endregion
            }

            //Get tree at this node
            html += "<div class='DecisionTree'>\n";
            html += ToHtmlTree(this);
            html += "</div>\n";
            return html;
        }

        /// <summary>
        /// The recursive process of cycling through nodes, to convert to HTML.
        /// </summary>
        /// <param name="groupNode">The current node being converted.</param>
        /// <returns>HTML for the current node and its subnodes.</returns>
        private string ToHtmlTree(TreeNode groupNode)
        {
            //Check for root node. Skip if if not needed.
            if (groupNode.Type == TreeNodeType.Root && groupNode.SubNodes.Count == 1 && groupNode.Leaves.Count == 0)
                return ToHtmlTree(groupNode.SubNodes[0]);

            string nodeHtml = "";

            //Node title
            nodeHtml += "<table>";
            if (groupNode.Type == TreeNodeType.Feature)
                nodeHtml += "<tr><td class='Feature' colspan=100>" + groupNode.Name + "</td></tr>";
            else
                nodeHtml += "<tr><td class='Value' colspan=100>" + groupNode.Name + "</td></tr>";
            nodeHtml += "<tr>";

            //Show subnodes
            if (groupNode.SubNodes.Count > 0)
            {
                foreach (var subnode in groupNode.SubNodes)
                {
                    nodeHtml += "<td>";
                    nodeHtml += ToHtmlTree(subnode);
                    nodeHtml += "</td>";
                }
            }

            //Show leaves
            int leafCount = groupNode.Leaves.Count;
            if (leafCount > 0)
            {
                nodeHtml += "<td class='Leaf'>";
                nodeHtml += "<div class='Leaf'>\n";

                foreach (var leafItem in groupNode.Leaves)
                {
                    //Label
                    nodeHtml += leafItem.LabelValue;

                    //Label %
                    if (leafCount > 1)
                        nodeHtml += ": " + leafItem.ExpectedReward.ToString("N3") + "</br>\n";
                }
                nodeHtml += "</div>\n";
                nodeHtml += "</td>";

            }
            nodeHtml += "</tr>";
            nodeHtml += "</table>";


            return nodeHtml;
        }

        /// <summary>
        /// Converts this tree node to a simple text tabbed list.
        /// </summary>
        /// <returns></returns>
        public string ToTabbedList()
        {
            return ToTabbedList(this, "");
        }

        /// <summary>
        /// The recursive process of cycling through nodes, to convert to a simple text tabbed list.
        /// </summary>
        /// <param name="groupNode"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private string ToTabbedList(TreeNode groupNode, string offset)
        {
            int level = offset.Length / 2;

            string s = "";
            s += offset + level + ".Node: " + groupNode.Name + "\n";

            //Show subnodes
            if (groupNode.SubNodes.Count > 0)
            {
                foreach (var subnode in groupNode.SubNodes)
                    s += ToTabbedList(subnode, offset + "  ");
            }

            //Show leaves
            if (groupNode.Leaves.Count > 0)
            {
                foreach (var leafItem in groupNode.Leaves)
                    s += offset + "  " + level + ".Leaf: " + leafItem.LabelValue + ": " + leafItem.ExpectedReward.ToString("N3") + "\n";
            }

            return s;
        }
    }
}
