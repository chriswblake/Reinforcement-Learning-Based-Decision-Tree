using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using RLDT;

namespace RLDT.Diagnostics
{
    public static class PolicyExtensions
    {
        public static string delimeter = ",";

        public static string DiagnosticInfo(this Policy thePolicy)
        {
            StringBuilder s = new StringBuilder();

            s.AppendLine("#### Policy ####");
            s.AppendFormat("States: {0:F0}", thePolicy.StateSpaceCount).AppendLine();
            s.AppendFormat("Exploration Rate: {0:F3}", thePolicy.ExplorationRate).AppendLine();
            s.AppendFormat("Discount Factor: {0:F3}", thePolicy.DiscountFactor).AppendLine();
            s.AppendFormat("Parallel Report Updates: {0}", thePolicy.ParallelReportUpdatesEnabled).AppendLine();
            s.AppendFormat("Parallel Query Updates: {0}", thePolicy.ParallelQueryUpdatesEnabled).AppendLine();
            s.AppendFormat("Queries Limit: {0:F0}", thePolicy.QueriesLimit).AppendLine();

            s.AppendLine();

            s.AppendLine("#### States ####");
            s.AppendFormat("ID{0}Features{0}Queries{0}Labels{0}Gini", delimeter).AppendLine();
            foreach (var keyStatePair in thePolicy.StateSpace.OrderBy(p=> p.Key))
            {
                int stateID = keyStatePair.Key;
                State theState = keyStatePair.Value;

                s.AppendFormat("{0}", stateID);
                s.Append(delimeter).AppendFormat("{0}", theState.Features.Count);
                s.Append(delimeter).AppendFormat("{0}", theState.Queries.Count);
                s.Append(delimeter).AppendFormat("{0}", theState.Labels.Count);
                s.Append(delimeter).AppendFormat("{0:F3}", theState.GiniImpurity);
                s.AppendLine();
            }

            return s.ToString();
        }

    }
}
