using System.IO;
using System.Linq;
using RLDT;

namespace RLDT.Experiments
{
    public class CsvStreamReader
    {
        //Fields
        private StreamReader file = null;
        private string[] headers = null; //first line of csv file
        private double[] weights = null; //second line of csv file (-1 to +1)

        //Constructor
        public CsvStreamReader(string csvAddress)
        {
            //Open csv file
            file = new System.IO.StreamReader(csvAddress);

            //Read headers and weights
            headers = file.ReadLine().Split(',');
            weights = file.ReadLine().Split(',').Select(double.Parse).ToArray();
        }

        //Methods
        public DataVector ReadLine()
        {
            //Try to read a line
            string line = file.ReadLine();
            if (line == null)
                return null;

            //Read a line to a string array
            string[] dataobjects = line.Split(',');

            //Create a data vector from the headers and read data line
            return new DataVector(headers, dataobjects);
        }
        public DataVectorTraining ReadLine(string labelFeatureName)
        {
            //Try to read a line
            string line = file.ReadLine();
            if (line == null)
                return null;

            //Read a line to a string array
            string[] dataobjects = line.Split(',');

            //Create a data vector from the headers and read data line
            return new DataVectorTraining(headers, dataobjects, weights, labelFeatureName);
        }
        public void Close()
        {
            file.Close();
        }
        public void SeekOriginBegin()
        {
            file.BaseStream.Seek(0, SeekOrigin.Begin);
            file.ReadLine();
            file.ReadLine();
        }
    }
}
