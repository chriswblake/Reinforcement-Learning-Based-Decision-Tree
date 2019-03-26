using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace System.Data
{
    public static class DataTableExtensions
    {
        public static void ToCsv(this DataTable dataTable, string address)
        {
            //Adjust file address
            if (Path.GetExtension(address).ToLower() != ".csv") address = address + ".csv";

            //Create file
            StreamWriter swCSV = new StreamWriter(address);

            //Get Columns
            swCSV.WriteLine(String.Join(",", dataTable.Columns.Cast<DataColumn>().Select(p => p.ColumnName)));

            //Get Data
            foreach (DataRow r in dataTable.Rows)
                swCSV.WriteLine(String.Join(",", r.ItemArray));

            //Close file
            swCSV.Close();
        }
    }
}
