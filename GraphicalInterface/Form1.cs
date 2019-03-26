using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using RLDT;
using RLDT.DecisionTree;

namespace GraphicalInterface
{
    public partial class Form1 : Form
    {
        //Fields
        DataTable results = new DataTable();
        //public List<TrainingStats> policyVsStep = new List<TrainingStats>();
        private Object dataBindLock = new Object();

        //Constructors
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            btnReset_Click(null, null);

            //Set columns for results data table
            results.Columns.Add("Id", typeof(int));
            results.Columns.Add("Instance Id", typeof(int));
            results.Columns.Add("States Total", typeof(int));
            results.Columns.Add("States Created", typeof(int));
            results.Columns.Add("Queries Total", typeof(int));
            results.Columns.Add("Testing Accuracy", typeof(double));

            //Clear sample series for chart
            chartValues.Series.Clear();

            //Set datasource for chart
            chartValues.DataSource = results;

            //Load Defaults
            setDefaultParameters_Testing();
            setDefaultParameters_Training();
            setDefaultParameters_Display();
        }

        #region Training
        //Fields
        Policy thePolicy = null;
        int correctCount = 0;
        string trainingFileAddress = "../../DataSets/mushrooms.csv";
        string trainingClassFeatureName = "class";
        double explorationRate = 0.00;
        double discountFactor = 0.85;
        int numTrainingPoints = 10000;
        int passes = 1;
        int totalPasses = 0;
        bool parallelQueryUpdates = true;
        bool parallelReportUpdates = false;
        int queriesLimit = 1000;
        Stopwatch stopwatchTraining = new Stopwatch();

        //Controls - Text Boxes
        private void txtBoxExplorationRate_TextChanged(object sender, EventArgs e)
        {
            try { explorationRate = Convert.ToDouble(txtBoxExplorationRate.Text); } catch { }
        }
        private void txtboxDiscountFactor_TextChanged(object sender, EventArgs e)
        {
            try { discountFactor = Convert.ToDouble(txtboxDiscountFactor.Text); } catch { }
        }
        private void txtboxNumTrainingPoints_TextChanged(object sender, EventArgs e)
        {
            try { numTrainingPoints = Convert.ToInt32(txtboxNumTrainingPoints.Text); } catch { }
        }
        private void txtboxPasses_TextChanged(object sender, EventArgs e)
        {
            try { passes = Convert.ToInt32(txtboxPasses.Text); } catch { }
        }
        private void txtboxClassFeature_TextChanged(object sender, EventArgs e)
        {
            trainingClassFeatureName = txtboxClassFeature.Text;
            testingClassFeatureName = trainingClassFeatureName;
        }
        private void txtboxQueriesLimit_TextChanged(object sender, EventArgs e)
        {
            try { queriesLimit = Convert.ToInt32(txtboxQueriesLimit.Text); } catch { }
        }

        //Controls - Check boxes
        private void chkboxParallelReportUpdates_CheckedChanged(object sender, EventArgs e)
        {
            parallelReportUpdates = chkboxParallelReportUpdates.Checked;
        }
        private void chkboxParallelQueryUpdates_CheckedChanged(object sender, EventArgs e)
        {
            parallelQueryUpdates = chkboxParallelQueriesUpdates.Checked;
        }

        //Controls - Buttons
        private void btnOpenTrainingDataFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select CSV file for training";
            openFileDialog.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                //Save to settings
                trainingFileAddress = openFileDialog.FileName;
                //Show filename on interface
                lblTrainingDataFile.Text = Path.GetFileName(trainingFileAddress);
            }
        }
        private void btnTrain_Click(object sender, EventArgs e)
        {
            //Disable train button
            btnTrain.Enabled = false;

            //Clear status
            lblCurrentPass.Text = "-";
            lblCurrentLine.Text = "-";

            //Do training in another thread
            new Thread(delegate ()
            {
                //Run training
                thePolicy.DiscountFactor = discountFactor;
                thePolicy.ExplorationRate = explorationRate;
                thePolicy.ParallelQueryUpdatesEnabled = parallelQueryUpdates;
                thePolicy.ParallelReportUpdatesEnabled = parallelReportUpdates;
                thePolicy.QueriesLimit = queriesLimit;
                startStopwatchTraining_InThread();
                for (int i = 1; i <= passes; i++)
                {
                    updateProgressPass(i);
                    TrainFromCSV(thePolicy, trainingClassFeatureName, trainingFileAddress, numTrainingPoints);
                }
                stopwatchTraining.Stop();

                //Test correctness
                correctCount = TestFromCSV(thePolicy, testingClassFeatureName, testingFileAddress, numTestPoints);

                //GUI update must be made on GUI thread
                this.InvokeAction(delegate ()
                {
                    //Show tree
                    DrawTree();

                    //Refresh data on chart
                    lock (dataBindLock)
                        chartValues.DataBind();

                    //Enable buttons
                    btnTrain.Enabled = true;
                    btnReset.Enabled = true;
                    btnRedraw.Enabled = true;

                    //Update total passes
                    totalPasses += passes;
                    lblTotalPasses.Text = totalPasses.ToString();

                    //Clear status
                    lblCurrentPass.Text = "Done";
                    lblCurrentLine.Text = "Done";
                });

            }).Start();
        }
        private void btnReset_Click(object sender, EventArgs e)
        {
            //Clear existing data
            results.Clear();
            thePolicy = new Policy();

            //Clear charts and reset
            chartValues.DataBind();

            //Clear tables (web browser)
            webviewerTree.DocumentText = "";

            //Disable buttons
            btnReset.Enabled = false;
            btnRedraw.Enabled = false;

            //Clear total passes
            totalPasses = 0;
            lblTotalPasses.Text = "0";

            //Clear Status
            lblCurrentPass.Text = "-";
            lblCurrentLine.Text = "-";
            lblTimer.Text = "-";

        }


        //Methods
        public void setDefaultParameters_Training()
        {
            //Controls
            txtBoxExplorationRate.Text = explorationRate.ToString("N2");
            txtboxDiscountFactor.Text = discountFactor.ToString("N2");
            txtboxNumTrainingPoints.Text = numTrainingPoints.ToString();
            txtboxPasses.Text = passes.ToString();
            lblTrainingDataFile.Text = trainingFileAddress;
            txtboxClassFeature.Text = trainingClassFeatureName;
            chkboxParallelQueriesUpdates.Checked = parallelQueryUpdates;
            chkboxParallelReportUpdates.Checked = parallelReportUpdates;

            //Charts
            //StatesTotal
            Series seriesStatesTotal = chartValues.Series.Add("States Total");
            seriesStatesTotal.XValueMember = "Id";
            seriesStatesTotal.YValueMembers = "StatesTotal";
            seriesStatesTotal.ToolTip = "States Total: #VALY";
            seriesStatesTotal.ChartType = SeriesChartType.FastPoint;
            seriesStatesTotal.YAxisType = AxisType.Primary;

            ////StatesVisited
            //Series seriesStatesVisited = chartValues.Series.Add("Queries Total");
            //seriesStatesVisited.XValueMember = "Id";
            //seriesStatesVisited.YValueMembers = "QueriesTotal";
            //seriesStatesVisited.ToolTip = "Queries Total: #VALY";
            //seriesStatesVisited.ChartType = SeriesChartType.FastPoint;

            ////StatesCreated
            //Series seriesStatesCreated = chartValues.Series.Add("States Created");
            //seriesStatesCreated.XValueMember = "Id";
            //seriesStatesCreated.YValueMembers = "StatesCreated";
            //seriesStatesCreated.ToolTip = "States Created: #VALY";
            //seriesStatesCreated.ChartType = SeriesChartType.FastPoint;
        }
        public void TrainFromCSV(Policy thePolicy, string labelFeaturName, string csvAddress)
        {
            TrainFromCSV(thePolicy, labelFeaturName, csvAddress, int.MaxValue);
        }
        public void TrainFromCSV(Policy thePolicy, string labelFeaturName, string csvAddress, int readLimit)
        {
            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(csvAddress);

            //Read headers
            string[] headers = file.ReadLine().Split(',');
            double[] rewards = file.ReadLine().Split(',').Select(double.Parse).ToArray();

            //Determine id for statistics
            int initialId = 0;
            if (results.Rows.Count != 0)
                initialId = results.Rows.Cast<DataRow>().Max(p => (int)p["Id"]);

            //Read data into feature vector
            string line; int lineCounter = 0;
            while ((line = file.ReadLine()) != null)
            {
                //increment counter to use as import id
                lineCounter++;

                //Read a line to a string array
                string[] dataobjects = line.Split(',');

                //Create a data vector from the headers and read data line
                DataVectorTraining dataVector = new DataVectorTraining(headers, dataobjects, rewards, labelFeaturName);

                //Submit to the reinforcement learner
                TrainingStats trainingStats = thePolicy.Learn(dataVector);
                DataRow dr = results.NewRow();
                results.Rows.Add(dr);
                dr["Id"] = initialId + lineCounter;
                dr["Instance Id"] = lineCounter;
                dr["States Total"] = trainingStats.StatesTotal;
                dr["States Created"] = trainingStats.StatesCreated;
                dr["Queries Total"] = trainingStats.QueriesTotal;

                //Copy data for chart
                if (lineCounter % sampleNthPoint == 0)
                {
                    //Save policy snapshot
                    lock (dataBindLock)
                        results.Rows.Add(dr);

                    //Save testing accuracy
                    if (chboxTestPolicy.Checked)
                    {
                        int correctClassifications = TestFromCSV(thePolicy, testingClassFeatureName, testingFileAddress, numTestPoints);
                        dr["Testing Accuracy"] = (double) correctClassifications / lineCounter;
                    }
                }

                //Update processing status
                updateProgressTraining(lineCounter);

                //If limit reached, end early
                if (lineCounter == readLimit) break;
            }

            file.Close();
        }
        #endregion

        #region Testing
        //Fields
        string testingFileAddress = "../../DataSets/mushrooms.csv";
        string testingClassFeatureName = "class";
        int sampleNthPoint = 100; //Record every Nth policy's details
        int numTestPoints = 10000; //Reads up to first 100 items in data file
        bool testPolicy = true;
        int totalPointsRead = 0;

        //Controls - Settings
        private void txtboxSampleNthPoint_TextChanged(object sender, EventArgs e)
        {
            try { sampleNthPoint = Convert.ToInt32(txtboxSampleNthPoint.Text); } catch { }
        }
        private void txtboxNumTestPoints_TextChanged(object sender, EventArgs e)
        {
            try { numTestPoints = Convert.ToInt32(txtboxNumTestPoints.Text); } catch { }

        }
        private void chboxTestPolicy_CheckedChanged(object sender, EventArgs e)
        {
            testPolicy = chboxTestPolicy.Checked;
            try
            {
                chartValues.Series["CorrectClassifications"].Enabled = testPolicy;
            }
            catch { }
        }

        //Controls - Buttons
        private void btnOpenTestingDataFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select CSV file for testing";
            openFileDialog.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                //Save to settings
                testingFileAddress = openFileDialog.FileName;
                //Show filename on interface
                lblTestingDataFile.Text = Path.GetFileName(testingFileAddress);
            }
        }
        private void btnTestPolicy_Click(object sender, EventArgs e)
        {
            int correctCount = TestFromCSV(thePolicy, testingClassFeatureName, testingFileAddress, numTestPoints);
            MessageBox.Show(correctCount.ToString());
        }

        //Methods
        public void setDefaultParameters_Testing()
        {
            //Controls
            chboxTestPolicy.Checked = testPolicy;
            txtboxSampleNthPoint.Text = sampleNthPoint.ToString();
            txtboxNumTestPoints.Text = numTestPoints.ToString();
            lblTestingDataFile.Text = testingFileAddress;

            //Chart
            //Correct Classifications
            chartValues.ChartAreas[0].AxisY2.Enabled = AxisEnabled.Auto;
            Series seriesCorrectClassifications = chartValues.Series.Add("Correct Classifications");
            seriesCorrectClassifications.YAxisType = AxisType.Secondary;
            seriesCorrectClassifications.XValueMember = "Id";
            seriesCorrectClassifications.YValueMembers = "CorrectClassifications";
            seriesCorrectClassifications.ToolTip = "Correct Classifications: #VALY";
            seriesCorrectClassifications.Enabled = testPolicy;
            seriesCorrectClassifications.ChartType = SeriesChartType.FastPoint;
        }
        public int TestFromCSV(Policy thePolicy, string labelFeaturName, string csvAddress)
        {
            return TestFromCSV(thePolicy, labelFeaturName, csvAddress, int.MaxValue);
        }
        public int TestFromCSV(Policy thePolicy, string labelFeaturName, string csvAddress, int readLimit)
        {
            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(csvAddress);

            //Read headers
            string[] headers = file.ReadLine().Split(',');

            //Skip Rewards
            double[] rewards = file.ReadLine().Split(',').Select(double.Parse).ToArray();
            //file.ReadLine();

            //Read data into feature vector
            string line; int lineCounter = 0; int correctCount = 0;
            while ((line = file.ReadLine()) != null)
            {
                lineCounter++;

                //Read a line to a string array
                string[] dataobjects = line.Split(',');

                //Create a data vector from the headers and read data line
                DataVector dataVector = new DataVector(headers, dataobjects);

                //Submit to the classifier
                object classification = thePolicy.Classify_ByTree(dataVector);

                //Count correct answers
                if (classification.Equals(dataVector[labelFeaturName].Value))
                    correctCount++;

                //If limit reached, end early
                if (lineCounter == readLimit)
                    break;
            }
            totalPointsRead = lineCounter;

            file.Close();

            return correctCount;
        }
        #endregion

        #region Display
        //Fields
        public TreeSettings treeSettings = new TreeSettings()
        {
            ShowBlanks = true,
            ShowSubScores = false
        };

        //Controls
        private void setDefaultParameters_Display()
        {
            chboxShowBlanks.Checked = treeSettings.ShowBlanks;
            chBoxShowSubScores.Checked = treeSettings.ShowSubScores;
        }
        private void btnRedraw_Click(object sender, EventArgs e)
        {
            DrawTree();
        }
        private void chboxShowBlanks_CheckedChanged(object sender, EventArgs e)
        {
            treeSettings.ShowBlanks = chboxShowBlanks.Checked;
            DrawTree();
        }
        private void chBoxShowSubScores_CheckedChanged(object sender, EventArgs e)
        {
            treeSettings.ShowSubScores = chBoxShowSubScores.Checked;
            DrawTree();
        }

        //Methods
        private void DrawTree()
        {
            //Display the Decision Tree in the webbrowser
            var theTree = thePolicy.ToDecisionTree(treeSettings);
            string html = "<html>";
            html += "<body style='background-color:#EEEEEE;'>";
            html += "Total States: " + thePolicy.StateSpaceCount + "<br/>\n";
            html += "Correct Count: " + correctCount.ToString() + "/" + totalPointsRead.ToString() + " (" + (100.0 * correctCount / totalPointsRead).ToString("N1") + "%)\n";
            html += theTree.ToHtmlTree();
            //html += "<pre>";
            //html += theTree.ToTabbedList();
            //html += "</pre>";
            html += "</body>";
            html += "</html>";
            webviewerTree.DocumentText = html;
        }
        #endregion

        #region Status
        public void updateProgressPass(int currentPass)
        {
            //Thread safe
            if (lblCurrentPass.InvokeRequired)
            {
                lblCurrentPass.Invoke(new Action<int>(updateProgressPass), currentPass);
                return;
            }

            //Update label
            lblCurrentPass.Text = currentPass.ToString();
        }
        public void updateProgressTraining(int currentLine)
        {
            //Only show update every 10th line (relative)
            if (currentLine % (numTrainingPoints / 10) == 0)
            {
                //Thread safe
                if (lblCurrentLine.InvokeRequired)
                {
                    lblCurrentLine.Invoke(new Action<int>(updateProgressTraining), currentLine);
                    return;
                }

                //Update label
                lblCurrentLine.Text = currentLine.ToString();
            }
        }
        public void updateTimer(string elapsedTime)
        {
            //Thread safe
            if (lblTimer.InvokeRequired)
            {
                lblTimer.Invoke(new Action<string>(updateTimer), elapsedTime);
                return;
            }

            //Update label
            lblTimer.Text = elapsedTime;
        }
        public void startStopwatchTraining_InThread()
        {
            //Start the stopwatch
            stopwatchTraining.Reset();
            stopwatchTraining.Start();

            //Show regular updates
            new Thread(delegate ()
            {
                //Run until stopwatch is stopped (by training)
                while (stopwatchTraining.IsRunning)
                {
                    //Get elapsed time
                    TimeSpan ts = stopwatchTraining.Elapsed;
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:000}",
                    ts.Minutes, ts.Seconds, ts.Milliseconds);

                    //Update timer label
                    updateTimer(elapsedTime);

                    //Wait
                    Thread.Sleep(100);
                }

                
            }).Start();
        }
        #endregion

        //Chart controls
        double currentPositionX = 0;
        double currentPositionY = 0;
        private void chartValues_MouseMove(object sender, MouseEventArgs e)
        {
            //if (e.Button == MouseButtons.Left)
            //{
            //    Axis xAxis = chartValues.ChartAreas[0].AxisX;

            //    //Shift Horizontally
            //    double shiftAmount = currentPositionX - e.X;
            //    xAxis.Minimum += shiftAmount;
            //    xAxis.Maximum += shiftAmount;

            //    //Scalling Horizontally
            //    double stretchAmount = e.Y / currentPositionY;
            //    xAxis.Minimum *= stretchAmount;
            //    xAxis.Maximum *= stretchAmount;

            //    //Update position
            //    currentPositionX = e.X;
            //    currentPositionY = e.Y;
            //}
            //else
            //{
            //    //Update position
            //    currentPositionX = e.X;
            //    currentPositionY = e.Y;
            //}
        }
        private void chartValues_DoubleClick(object sender, EventArgs e)
        {
            //Toggle legend visibility
            //chartValues.Legends[0].Enabled = !chartValues.Legends[0].Enabled;
        }

        private void btnSaveData_Click(object sender, EventArgs e)
        {
            //Ask for save location
            string fileDir = "";
            string fileName = "";
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "csv files (*.csv)|*.csv";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                fileDir = Path.GetDirectoryName(saveFileDialog1.FileName);
                fileName = Path.GetFileNameWithoutExtension(saveFileDialog1.FileName);
            }
            else
            {
                return;
            }

            //Convert data to csv lines
            List<string> lines = new List<string>();
            lines.Add("Id,StatesTotal,StatesCreated,QueriesTotal,CorrectClassifications");
            foreach (DataRow r in results.Rows)
            {
                string line = r["Id"] + "," + r["States Total"] + "," + r["States Created"] + "," + r["Queries Total"] + "," + r["Testing Accuracy"];
                lines.Add(line);
            }

            //Create metadata file for test
            List<string> parameters = new List<string>();
            parameters.Add("Training File: " + Path.GetFileName(trainingFileAddress));
            parameters.Add("Exporation Rate: " + explorationRate);
            parameters.Add("Discount Factor: " + discountFactor);
            parameters.Add("Parallel Query Updates: " + parallelQueryUpdates);
            parameters.Add("Parallel Report Updates: " + parallelReportUpdates);
            parameters.Add("Total Passes: " + totalPasses);

            parameters.Add("");

            parameters.Add("Testing File: " + Path.GetFileName(testingFileAddress));
            parameters.Add("Correct Count: " + correctCount);
            parameters.Add("Points Checked: " + totalPointsRead);
            parameters.Add("Percent Correct: " + (100.0 * correctCount / totalPointsRead).ToString("N2"));

            //Take picture of chart
            Control theControl = chartValues;
            Bitmap bmp = new Bitmap(theControl.Width, theControl.Height);
            theControl.DrawToBitmap(bmp, new Rectangle(0, 0, theControl.Width, theControl.Height));
            bmp.Save(Path.Combine(fileDir, fileName + "-chart.png"), System.Drawing.Imaging.ImageFormat.Png);

            //Take picture of tree
            theControl = webviewerTree;
            bmp = new Bitmap(theControl.Width, theControl.Height);
            theControl.DrawToBitmap(bmp, new Rectangle(0, 0, theControl.Width, theControl.Height));
            bmp.Save(Path.Combine(fileDir, fileName + "-tree.png"), System.Drawing.Imaging.ImageFormat.Png);

            //Save files
            System.IO.File.WriteAllLines(Path.Combine(fileDir, fileName + ".csv"), lines);
            System.IO.File.WriteAllLines(Path.Combine(fileDir, fileName + "-details.txt"), parameters);
        }

        
    }

    public static class extentions
    {
        public static void InvokeAction(this Control ctl, Action a)
        {
            if (!ctl.InvokeRequired)
            {
                a();
            }
            else
            {
                ctl.BeginInvoke(new MethodInvoker(a));
            }
        }
    }
}
