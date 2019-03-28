using System;
using System.Collections.Generic;
using System.Text;

namespace RLDT.Experiments
{
    public class ConfusionMatrix
    {
        //Fields
        private HashSet<object> knownObjects = new HashSet<object>();
        private Dictionary<object, Dictionary<object, int>> Counts = new Dictionary<object, Dictionary<object, int>>();
        private int totalEntries = 0;

        //Properties
        public static string HtmlStyling
        {
            get
            {
                return @"
                    <style>
                        table.confusionMatrix {
                            border-collapse: collapse;
                            --color-correct: 63,127,191;
                            --color-wrong: 191,127,63;
                        }
                        table.confusionMatrix th, td {
                            background-color: #eeeeee;
                            border: 1px solid black;
                        }
                        table.confusionMatrix th {
                            background-color: #999999;
                        }
                        table.confusionMatrix td.correct {
                            background-color: rgb(0,0,100);
                        }
                       table.confusionMatrix  td.wrong {
                            background-color: #ffcc88;
                        }
                        .hidden{
                            visibility: hidden;
                        }
                    </style>
                    ";
            }
        }
        public string ToHtml(bool inPage)
        {
            //If false send without styling
            if (!inPage)
                return ToHtml();

            //Build in html page and return
            string html = "";
            html = "<html>";
            html += HtmlStyling;
            html += "<body>";
            html += ToHtml();
            html += "</body>";
            html += "</html>";
            return html;
        }
        public string ToHtml()
        {
            string html = "<table class='confusionMatrix'>";
            List<object> fields = new List<object>(knownObjects);

            //Create top headers
            html += "<tr>";
            html += "<th class='hidden'></th>";
            foreach (object column in fields)
                html += "<th>" + column.ToString() + "</th>";
            html += "</tr>";
            
            //Add rows of data
            foreach (object row in fields)
            {
                //Start row
                html += "<tr>";

                //Add header
                html += "<th>"+row.ToString()+"</th>";

                //Add counts
                int rowTotal = 0;
                int rowCorrect = 0;
                int rowWrong = 0;
                foreach (object column in fields)
                {
                    //Default to zero for missing entry
                    int cellCount = 0;
                    if (Counts.ContainsKey(row))
                        if (Counts[row].ContainsKey(column))
                            cellCount = Counts[row][column];

                    //Highlight cell if on diagonal
                    rowTotal += cellCount;
                    string cellClass = "";
                    if (row.Equals(column))
                    {
                        cellClass = "correct";
                        rowCorrect += cellCount;
                    }
                    else
                    {
                        cellClass = "wrong";
                        rowWrong += cellCount;
                    }
                    html += String.Format("<td class='{1}' style='background-color: rgba(var(--color-{1}),{2:N2});'>{0}</td>", cellCount, cellClass, (double)cellCount / totalEntries);

                }
                double percentCorrect = 0;
                double percentWrong = 0;
                if (rowTotal > 0)
                { 
                    percentCorrect = (double) rowCorrect / rowTotal * 100;
                    percentWrong = (double) rowWrong / rowTotal * 100;
                }
                html += String.Format("<td class='correct' style='background-color: rgba(var(--color-correct), {1:N2})'>{0:N1}%</td>", percentCorrect, percentCorrect/100);
                html += String.Format("<td class='wrong' style='background-color: rgba(var(--color-wrong), {1:N2})'>{0:N1}%</td>", percentWrong, percentWrong/100);
                html += "</tr>";
            }

            //Add column summaries
            string htmlRowCorrect = "<tr><th class='hidden'></th>";
            string htmlRowWrong = "<tr><th class='hidden'></th>";
            foreach (object column in fields)
            {
                int colTotal = 0;
                int colCorrect = 0;
                int colWrong = 0;

                //Count values
                foreach (object row in fields)
                {
                    //Default to zero for missing entry
                    int cellCount = 0;
                    if (Counts.ContainsKey(row))
                        if (Counts[row].ContainsKey(column))
                            cellCount = Counts[row][column];

                    //Highlight cell if on diagonal
                    colTotal += cellCount;
                    if (column.Equals(row))
                        colCorrect += cellCount;
                    else
                        colWrong += cellCount;
                }

                //Calculate percentages
                double percentCorrect = 0;
                double percentWrong = 0;
                if (colTotal > 0)
                {
                    percentCorrect = (double)colCorrect / colTotal * 100;
                    percentWrong = (double)colWrong / colTotal * 100;
                }

                //Add cells to html
                htmlRowCorrect += String.Format("<td class='correct' style='background-color: rgba(var(--color-correct),{1:N1});'>{0:N2}%</td>", percentCorrect, percentCorrect/100);
                htmlRowWrong += String.Format("<td class='wrong' style='background-color: rgba(var(--color-wrong),{1:N1});'>{0:N2}%</td>", percentWrong, percentWrong/100);
            }
            htmlRowCorrect += "</tr>";
            htmlRowWrong += "</tr>";
            html += htmlRowCorrect;
            html += htmlRowWrong;

            html += "</table>";
            return html;
        }

        //Methods
        public void AddEntry(object correct, object prediction)
        {
            //Track all seen objects
            knownObjects.Add(correct);
            knownObjects.Add(prediction);

            //Create empty entry if missing
            if (!Counts.ContainsKey(correct))
                Counts.Add(correct, new Dictionary<object, int>());
            if (!Counts[correct].ContainsKey(prediction))
                Counts[correct].Add(prediction, 0);

            //Increase counter for correct/predicted combination
            Counts[correct][prediction]++;

            //Increase total count
            totalEntries++;
        }
    }
}
