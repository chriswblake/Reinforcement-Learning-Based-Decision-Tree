using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

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

        //Properties (pdf only)
        public double width { get; set; } = 800;
        public double height { get; set; } = 400;
        public double? xMin { get; set; } = null;
        public double? xMax { get; set; } = null;
        public double? yMin { get; set; } = null;
        public double? yMax { get; set; } = null;
        public bool xLogarithmic { get; set; } = false;
        public bool yLogarithmic { get; set; } = false;

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
        public void ToHtml(string address)
        {
            if (Path.GetExtension(address).ToLower() != ".html") address = address + ".html";
            File.WriteAllText(address, ToHtml());
        }
        public string ToHtml()
        {
            // Get Templates
            string chartHtml = File.ReadAllText("ChartTemplate.html");

            //Insert titles
            chartHtml = chartHtml.Replace("<chart title>", this.title);
            chartHtml = chartHtml.Replace("<x title>", this.xAxisTitle);
            chartHtml = chartHtml.Replace("<y title>", this.yAxisTitle);

            //Adjust axes type
            if(this.xLogarithmic)
                chartHtml = chartHtml.Replace("<x type>", "logarithmic");
            else
                chartHtml = chartHtml.Replace("<x type>", "linear");
            if (this.yLogarithmic)
                chartHtml = chartHtml.Replace("<y type>", "logarithmic");
            else
                chartHtml = chartHtml.Replace("<y type>", "linear");

            //Insert datasets
            string datasets = "";
            datasets += "datasets: [" + Environment.NewLine;
            foreach(var series in this.chartSeriesData)
                datasets += ToDataset(series.Key) + "," + Environment.NewLine;
            datasets += "]";
            chartHtml = chartHtml.Replace("datasets: []", datasets);

            return chartHtml;
        }
        public void ToPdf(string address)
        {
            //Create empty chart
            var myModel = new PlotModel()
            {
                Title = this.title,
                Background = OxyColor.FromArgb(255, 255, 255, 255),
                PlotAreaBorderColor =OxyColor.FromArgb(25, 0, 0, 0),
            };


            //Configure X Axis
            Axis xAxis = null;
            if (xLogarithmic)
                xAxis = new LogarithmicAxis();
            else
                xAxis = new LinearAxis();
            xAxis.Title = this.xAxisTitle;
            xAxis.Position = AxisPosition.Bottom;
            xAxis.IsAxisVisible = true;
            xAxis.MajorGridlineStyle = LineStyle.Solid;
            xAxis.MajorGridlineColor = OxyColor.FromArgb(25, 0, 0, 0);
            if (xMin.HasValue) xAxis.Minimum = xMin.Value;
            if (xMax.HasValue) xAxis.Maximum = xMax.Value;
            myModel.Axes.Add(xAxis);

            //Configure Y Axis
            Axis yAxis = null;
            if (yLogarithmic)
                yAxis = new LogarithmicAxis();
            else
                yAxis = new LinearAxis();
            yAxis.Title = this.yAxisTitle;
            yAxis.Position = AxisPosition.Left;
            yAxis.IsAxisVisible = true;
            yAxis.MajorGridlineStyle = LineStyle.Solid;
            yAxis.MajorGridlineColor = OxyColor.FromArgb(25, 0, 0, 0);
            if (yMin.HasValue) yAxis.Minimum = yMin.Value;
            if (yMax.HasValue) yAxis.Maximum = yMax.Value;
            myModel.Axes.Add(yAxis);

            //Hide legend if only 1 series
            if (chartSeriesData.Count < 2)
            { 
                myModel.IsLegendVisible = false;
                myModel.Title = "";
            }

            //Create series
            foreach (var seriesData in chartSeriesData)
            {
                //Add group
                var series = new ScatterSeries
                {
                    Title = seriesData.Key,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 3
                };
                //Add data
                foreach(var datapoint in seriesData.Value)
                    series.Points.Add(new ScatterPoint(datapoint.Item1, datapoint.Item2));

                //Add to chart
                myModel.Series.Add(series);
            }

            //Save chart to file as PDF
            if (Path.GetExtension(address).ToLower() != ".pdf") address = address + ".pdf";
            using (var stream = File.Create(address))
            {
                var exporter = new PdfExporter { Width = width, Height = height };
                exporter.Export(myModel, stream);
            }
        }
    }
}
