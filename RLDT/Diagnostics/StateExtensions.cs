using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using RLDT;

namespace RLDT.Diagnostics
{
    public static class StateExtensions
    {
        public static string delimeter = ",";

        public static string DiagnosticInfo(this State theState)
        {
            StringBuilder s = new StringBuilder();

            //General
            s.AppendLine("#### State ####");
            s.AppendFormat("ID: {0}", theState.GetHashCode()).AppendLine();
            s.AppendFormat("Features: {0}", theState.Features.Count).AppendLine();
            s.AppendFormat("Queries: {0}", theState.Queries.Count).AppendLine();
            s.AppendFormat("Labels: {0}", theState.Labels.Count).AppendLine();
            s.AppendFormat("Gini: {0:F5}", theState.GiniImpurity).AppendLine();

            s.AppendLine();

            //Features
            s.AppendLine("#### Features ####");
            s.AppendFormat("Name{0}Value", delimeter).AppendLine();
            foreach (var theFeature in theState.Features)
            {
                s.AppendFormat("{0}", theFeature.Name.ToString());
                s.Append(delimeter).AppendFormat("{0}", theFeature.Value);
                s.AppendLine();
            }

            s.AppendLine();

            //Labels
            s.AppendLine("#### Labels ####");
            s.AppendFormat("Feature Name{0}Feature Value{0}Reward", delimeter).AppendLine();
            foreach (var labelRewardPair in theState.Labels.OrderByDescending(p => p.Value))
            {
                FeatureValuePair theLabel = labelRewardPair.Key;
                double reward = labelRewardPair.Value;

                s.AppendFormat("{0}", theLabel.Name.ToString());
                s.Append(delimeter).AppendFormat("{0}", theLabel.Value);
                s.Append(delimeter).AppendFormat("{0:F5}", reward);
                s.AppendLine();
            }

            s.AppendLine();

            //Queries
            s.AppendLine("#### Queries ####");
            s.AppendFormat("Relevant Label{0}Feature Name{0}Feature Value{0}Reward", delimeter).AppendLine();
            foreach (var queryRewardPair in theState.Queries.OrderBy(p => p.Key.Label.Value).ThenBy(p => p.Key.Feature.Name).ThenBy(p => p.Key.Feature.Value).ThenByDescending(p => p.Value))
            {
                Query q = queryRewardPair.Key;
                double reward = queryRewardPair.Value;
                s.AppendFormat("{0}", q.Label.Value.ToString());
                s.Append(delimeter).AppendFormat("{0}", q.Feature.Name);
                s.Append(delimeter).AppendFormat("{0}", q.Feature.Value.ToString());
                s.Append(delimeter).AppendFormat("{0:F5}", reward);
                s.AppendLine();
            }

            s.AppendLine();


            return s.ToString();
        }

    }
}
