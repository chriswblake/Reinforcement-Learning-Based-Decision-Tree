using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using RLDT;
using RLDT.Diagnostics;


namespace ConsoleApp_Playground
{
    class Program
    {
        //Main
        static void Main(string[] args)
        {
            bool repeat = true;
            while (repeat==true)
            {
                //Create the policy
                Console.WriteLine("Creating policy...");
                Policy thePolicy = new Policy();

                //Train the policy
                for (int pass = 1; pass <= 3; pass++)
                {
                    TrainFromCSV(thePolicy, "class", @"mushrooms.csv", 500);
                    Console.WriteLine("Pass: " + pass);
                }

                //Print Diagnostic info
                File.WriteAllText("Policy Diagnostics.html", thePolicy.DiagnosticHtml());
                //File.WriteAllText("State Diagnostics.txt", thePolicy.StateSpace.Values.ElementAt(5).DiagnosticInfo());

                //Ask about repeating
                Console.WriteLine();
                Console.Write("Repeat? (y/n):");
                string ans = Console.ReadLine();
                if (ans.StartsWith("n"))
                    repeat = false;
            }
        }

        //Read data, line by line. Simulate data coming in 1 item at a time.
        public static void TrainFromCSV(Policy thePolicy, string labelFeaturName, string csvAddress)
        {
            TrainFromCSV(thePolicy, labelFeaturName, csvAddress, int.MaxValue);
        }
        public static void TrainFromCSV(Policy thePolicy, string labelFeaturName, string csvAddress, int readLimit)
        {
            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(csvAddress);

            //Read headers
            string[] headers = file.ReadLine().Split(',');
            double[] rewards = file.ReadLine().Split(',').Select(double.Parse).ToArray();

            //Read data into feature vector
            string line; int counter = 0;
            while ((line = file.ReadLine()) != null)
            {
                //increment counter to use as import id
                counter++;

                //Read a line to a string array
                string[] dataobjects = line.Split(',');

                //Create a data vector from the headers and read data line
                DataVectorTraining dataVector = new DataVectorTraining(headers, dataobjects, rewards, labelFeaturName);
                
                //Submit to the reinforcement learner
                thePolicy.Learn(dataVector);

                //If limit reached, end early
                if (counter == readLimit) break;
            }

            file.Close();
        }
    }
}
