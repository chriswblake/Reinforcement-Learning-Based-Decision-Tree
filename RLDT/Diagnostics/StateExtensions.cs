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

        /// <summary>
        /// Returns a string of plain-text with detailed state information.
        /// </summary>
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

        /// <summary>
        /// Returns an HTML formatted string with detailed state information.
        /// </summary>
        public static string DiagnosticHtml(this State theState)
        {
            StringBuilder s = new StringBuilder();
            int stateID = theState.GetHashCode();

            //Front Matter
            s.AppendLine(@"
            <style>
                .card-body {
                    margin:0;
                    padding:0;
}
</style>
");
            s.AppendLine("<div class='accordion'>");

            #region Features
            s.AppendLine("<div class='card'>");

            //Card Header
            s.AppendFormat(@"
                <div class='card-header'>
                  <h5 class='mb-0'>
                    <button class='btn btn-link' type='button' data-toggle='collapse' data-target='#ID{0}-Features'>
                      Features ({1:F0})
                    </button>
                  </h5>
                </div>
            ", stateID, theState.Features.Count);

            //Card Body
            s.AppendFormat("<div id='ID{0}-Features' class='collapse'>", stateID).AppendLine();
            s.AppendLine("<div class='card-body'>");
            s.AppendLine("<table class='table'>");
            s.AppendLine("<tr>");
            s.AppendLine("<th>Name</th><th>Value</th>");
            s.AppendLine("</tr>");
            foreach (var theFeature in theState.Features)
            {
                s.AppendLine("<tr>");
                s.AppendFormat("<td>{0}</td>", theFeature.Name.ToString());
                s.AppendFormat("<td>{0}</td>", theFeature.Value);
                s.AppendLine("</tr>");
            }
            s.AppendLine("</table>");
            s.AppendLine("</div>");
            s.AppendLine("</div>");

            s.AppendLine("</div>");
            #endregion

            #region Labels
            s.AppendLine("<div class='card'>");

            //Card Header
            s.AppendFormat(@"
                <div class='card-header'>
                  <h5 class='mb-0'>
                    <button class='btn btn-link' type='button' data-toggle='collapse' data-target='#ID{0}-Labels'>
                      Labels ({1:F0})
                    </button>
                  </h5>
                </div>
            ", stateID, theState.Labels.Count);

            //Card Body
            s.AppendFormat("<div id='ID{0}-Labels' class='collapse'>", stateID).AppendLine();
            s.AppendLine("<div class='card-body'>");
            s.AppendLine("<table class='table'>");
            s.AppendLine("<tr>");
            s.AppendLine("<th>Feature Name</th><th>Feature Value</th><th>Reward</th>");
            s.AppendLine("</tr>");
            foreach (var labelRewardPair in theState.Labels.OrderByDescending(p => p.Value))
            {
                FeatureValuePair theLabel = labelRewardPair.Key;
                double reward = labelRewardPair.Value;

                s.AppendLine("<tr>");
                s.AppendFormat("<td>{0}</td>", theLabel.Name);
                s.AppendFormat("<td>{0}</td>", theLabel.Value);
                s.AppendFormat("<td>{0:F3}</td>", reward);
                s.AppendLine("</tr>");
            }
            s.AppendLine("</table>");
            s.AppendLine("</div>");
            s.AppendLine("</div>");

            s.AppendLine("</div>");
            #endregion

            #region Queries
            s.AppendLine("<div class='card'>");

            //Card Header
            s.AppendFormat(@"
                <div class='card-header'>
                  <h5 class='mb-0'>
                    <button class='btn btn-link' type='button' data-toggle='collapse' data-target='#ID{0}-Queries'>
                      Queries ({1:F0})
                    </button>
                  </h5>
                </div>
            ", stateID, theState.Queries.Count);

            //Card Body
            s.AppendFormat("<div id='ID{0}-Queries' class='collapse'>", stateID).AppendLine();
            s.AppendLine("<div class='card-body'>");
            s.AppendLine("<table class='table'>");
            s.AppendLine("<tr>");
            s.AppendLine("<th>Relevant Label</th><th>Feature Name</th><th>Feature Value</th><th>Reward</th>");
            s.AppendLine("</tr>");
            foreach (var queryRewardPair in theState.Queries.OrderBy(p => p.Key.Label.Value).ThenBy(p => p.Key.Feature.Name).ThenBy(p => p.Key.Feature.Value).ThenByDescending(p => p.Value))
            {
                Query theQuery = queryRewardPair.Key;
                double reward = queryRewardPair.Value;
                s.AppendLine("<tr>");
                s.AppendFormat("<td>{0}</td>", theQuery.Label.Value);
                s.AppendFormat("<td>{0}</td>", theQuery.Feature.Name);
                s.AppendFormat("<td>{0}</td>", theQuery.Feature.Value);
                s.AppendFormat("<td>{0:F3}</td>", reward);
                s.AppendLine("</tr>");
            }
            s.AppendLine("</table>");
            s.AppendLine("</div>");
            s.AppendLine("</div>");

            s.AppendLine("</div>");
            #endregion

            //Back Matter
            s.AppendLine("</div>");

            return s.ToString();
        }

    }
}
