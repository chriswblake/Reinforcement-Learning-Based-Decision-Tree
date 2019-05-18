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

        /// <summary>
        /// Returns a string of plain-text with policy and summarized state information.
        /// </summary>
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

        /// <summary>
        /// Returns an HTML formatted string with policy and detailed state information.
        /// </summary>
        public static string DiagnosticHtml(this Policy thePolicy)
        {
            StringBuilder s = new StringBuilder();

            //Front Matter
            s.AppendLine("<html>");
            s.AppendLine(@"
            <head>
	            <link rel='stylesheet' href='https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/css/bootstrap.min.css' integrity='sha384-ggOyR0iXCbMQv3Xipma34MD+dH/1fQ784/j6cY/iJTQUOhcWr7x9JvoRxT2MZw1T' crossorigin='anonymous'>
	            <script src='https://code.jquery.com/jquery-3.3.1.slim.min.js' integrity='sha384-q8i/X+965DzO0rT7abK41JStQIAqVgRVzpbzo5smXKp4YfRvH+8abtTE1Pi6jizo' crossorigin='anonymous'></script>
	            <script src='https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.14.7/umd/popper.min.js' integrity='sha384-UO2eT0CpHqdSJQ6hJty5KVphtPhzWj9WO1clHTMGa3JDZwrnQq4sF86dIHNDz0W1' crossorigin='anonymous'></script>
	            <script src='https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/js/bootstrap.min.js' integrity='sha384-JjSmVgyd0p3pXB1rRibZUAYoIIy6OrQ6VrjIEaFf/nJGzIxFDsf4x0xIM+B07jRM' crossorigin='anonymous'></script>
            </head>
            ");
            s.AppendLine("<body>");

            //Policy Details
            s.AppendLine("<b>Policy Details</b>").AppendLine("</br>");
            s.AppendFormat("Exploration Rate: {0:F3}", thePolicy.ExplorationRate).AppendLine("</br>");
            s.AppendFormat("Discount Factor: {0:F3}", thePolicy.DiscountFactor).AppendLine("</br>");
            s.AppendFormat("Parallel Report Updates: {0}", thePolicy.ParallelReportUpdatesEnabled).AppendLine("</br>");
            s.AppendFormat("Parallel Query Updates: {0}", thePolicy.ParallelQueryUpdatesEnabled).AppendLine("</br>");
            s.AppendFormat("Queries Limit: {0:F0}", thePolicy.QueriesLimit).AppendLine("</br>");
            s.AppendLine("</br>");

            //State Details
            s.AppendFormat("<b>State Details</b> ({0:F0})", thePolicy.StateSpace.Count).AppendLine("</br>");
            s.AppendLine("<table class='table table-hover'>");
            s.AppendLine("<tr>");
            s.AppendLine("" +
                "<th>ID</th>" +
                "<th>Features</th>" +
                "<th>Queries</th>" +
                "<th>Labels</th>" +
                "<th>Gini</th>");
            s.AppendLine("</tr>");
            foreach (var keyStatePair in thePolicy.StateSpace.OrderBy(p => p.Key))
            {
                int stateID = keyStatePair.Key;
                State theState = keyStatePair.Value;
                
                //State Overview
                s.AppendFormat("<tr data-toggle='collapse' data-target='#ID{0}' class='clickable'>", stateID).AppendLine();
                s.AppendFormat("<td>{0}</td>", stateID).AppendLine();
                s.AppendFormat("<td>{0}</td>", theState.Features.Count).AppendLine();
                s.AppendFormat("<td>{0}</td>", theState.Queries.Count).AppendLine();
                s.AppendFormat("<td>{0}</td>", theState.Labels.Count).AppendLine();
                s.AppendFormat("<td>{0:F5}</td>", theState.GiniImpurity).AppendLine();
                s.AppendLine("</tr>");

                //State Details
                s.AppendFormat("<tr id='ID{0}' class='collapse'>",stateID).AppendLine();
                s.AppendLine("<td colspan='5'>");
                s.AppendFormat("<div id='ID{0}' class='collapse'>", stateID).AppendLine();
                s.AppendLine(theState.DiagnosticHtml()).AppendLine();
                s.AppendFormat("</div>", stateID).AppendLine();
                s.AppendLine("</td>");
                s.AppendLine("</tr>");
            }
            s.AppendLine("</table>");

            //Back Matter
            s.AppendLine("</body>");
            s.AppendLine("</html>");

            return s.ToString();
        }

    }
}
