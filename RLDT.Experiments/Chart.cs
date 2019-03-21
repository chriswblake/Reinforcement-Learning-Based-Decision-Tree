using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace RLDT.Experiments
{
    public class Chart
    {
        //Fields
        private string title = "";
        private string xAxisTitle = "";
        private string yAxisTitle = "";
        private List<string> seriesNames = new List<string>();
        private Dictionary<string, List<Tuple<double, double>>> chartSeriesData = new Dictionary<string, List<Tuple<double, double>>>(); //Series Name, X Value, Y Value

        //Constructor
        public Chart(string title, string xAxisTitle, string yAxisTitle)
        {
            this.title = title;
            this.xAxisTitle = xAxisTitle;
            this.yAxisTitle = yAxisTitle;
        }

        //Methods - Private
        private string ToDataset(string SeriesName)
        {
            string datasetTemplate = @"
              {
                label: '<seriesName>',
                hidden: false,
                pointRadius: pointSize,
                pointBackgroundColor: colors[<seriesNum>],
                pointBorderColor: colors[<seriesNum>],
                backgroundColor: colors[<seriesNum>],
                borderColor: colors[<seriesNum>],
                borderWidth: pointSize / 2,
                showLine: false,
                lineTension: 0.0,
                fill: false,
                data: <seriesData>
              }
            ";

            //Copy template
            string dataset = datasetTemplate;

            //Insert name
            dataset = dataset.Replace("<seriesName>", SeriesName);

            //Insert series number (for color)
            dataset = dataset.Replace("<seriesNum>", seriesNames.IndexOf(SeriesName).ToString());

            //Insert data
            string seriesData = "[" + Environment.NewLine;
            foreach(var xyPair in chartSeriesData[SeriesName])
                seriesData += String.Format("{{x:{0}, y:{1}}},", xyPair.Item1, xyPair.Item2) + Environment.NewLine;
            seriesData += "]";
            dataset = dataset.Replace("<seriesData>", seriesData);

            return dataset;
        }

        //Methods
        public void Add(string seriesName, double x, double y)
        {
            if (!chartSeriesData.ContainsKey(seriesName))
            {
                seriesNames.Add(seriesName);
                chartSeriesData.Add(seriesName, new List<Tuple<double, double>>());
            }
            chartSeriesData[seriesName].Add(new Tuple<double, double>(x, y));
        }
        public string ToHtml()
        {
            // Get Templates
            string chartHtml = File.ReadAllText("ChartTemplate.html");

            //Insert titles
            chartHtml = chartHtml.Replace("<chart title>", this.title);
            chartHtml = chartHtml.Replace("<x title>", this.xAxisTitle);
            chartHtml = chartHtml.Replace("<y title>", this.yAxisTitle);

            //Insert datasets
            string datasets = "";
            datasets += "datasets: [" + Environment.NewLine;
            foreach(var series in this.chartSeriesData)
                datasets += ToDataset(series.Key) + "," + Environment.NewLine;
            datasets += "]";
            chartHtml = chartHtml.Replace("datasets: []", datasets);

            return chartHtml;
        }
    }
}
