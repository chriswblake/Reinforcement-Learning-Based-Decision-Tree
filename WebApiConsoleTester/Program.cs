using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections;
using System.Collections.Generic;
using WebApiConsoleTester.Models;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace WebApiConsoleTester
{
    class Program
    {
        static HttpClient client = new HttpClient();

        static void Main(string[] args)
        {
            //Http Client, configuration
            client.BaseAddress = new Uri("http://localhost:60000/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            #region Menu
            string menu = @"
1 - Get policies
2 - Create a Policy
3 - Learn policy
exit - end program
";
            #endregion

            while (true)
            {
                //Ask for action
                Console.WriteLine(menu);
                Console.Write("Select an action: ");
                string selectedAction = Console.ReadLine();

                //Perform action
                switch (selectedAction)
                {
                    //Get policies
                    case "1":
                        List<Policy> policies = GetPoliciesAsync().Result;
                        if (policies == null || policies.Count == 0)
                        {
                            Console.WriteLine("No policies available.");
                            break;
                        }

                        //Table header
                        Console.WriteLine("ID" + "\t\t" + "Name" + "\t\t" + "Description" + "\t\t" + "States");

                        //Table rows
                        foreach (Policy thePolicy in policies)
                            Console.WriteLine(thePolicy.PolicyId + "\t\t" + thePolicy.Name + "\t\t" + thePolicy.Description + "\t\t\t" + thePolicy.StateSpaceCount);
                        break;





                    //Create policy
                    case "2":

                        //Request name and description
                        Console.Write("Enter new policy's name: ");
                        string name = Console.ReadLine();
                        Console.Write("Enter new policy's description: ");
                        string description = Console.ReadLine();

                        //Submit to server
                        int policyId = CreatePolicyAsync(name, description).Result;

                        //Show user assigned Id
                        Console.WriteLine("Policy created. Assigned Id = " + policyId);
                        break;




                    //Learn policy
                    case "3":
                        //Request name and description
                        Console.Write("Enter policy's Id: ");
                        int id = Convert.ToInt32(Console.ReadLine());

                        TrainFromCsvAsync(id, "class", "mushrooms.csv", 1000).Wait();

                        Console.WriteLine("Finished training");

                        break;


                    //Exit the program
                    case "exit":
                        return;

                }


            }
        }

        static async Task<List<Policy>> GetPoliciesAsync()
        {
            HttpResponseMessage response = await client.GetAsync("api/policies");
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                List<Policy> policies = JsonConvert.DeserializeObject<List<Policy>>(jsonResponse);

                return policies;
            }else
            {
                return null;
            }
        }
        static async Task<int> CreatePolicyAsync(string name, string description)
        {

            //Convert parameters to json string
            var parameters = new {
                Name = name,
                Description = description
            };
            string content = JsonConvert.SerializeObject(parameters);

            //Send put request
            HttpContent httpContent = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync("api/policies/create", httpContent);

            //Get response and convert to integer
            int policyId = -1;
            if (response.IsSuccessStatusCode)
            {
                string strPolicyId = await response.Content.ReadAsStringAsync();
                policyId = Convert.ToInt32(strPolicyId);
            }

            //return policy id
            return policyId;
        }
        static async Task<bool> LearnPolicyAsync(int policyId, RLDT.DataVectorTraining dataVector)
        {
            //Convert parameters to json string
            JsonSerializerSettings jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Formatting = Formatting.Indented
            };
            string content = JsonConvert.SerializeObject(dataVector, jsonSettings);

            //Send put request
            HttpContent httpContent = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync("api/policies/"+policyId+"/learn", httpContent);

            //Get response and convert to integer
            if (response.IsSuccessStatusCode)
                return true;
            else
                return false;
        }

        public async Task TrainFromCsvAsync(int policyId, string labelFeaturName, string csvAddress)
        {
            await TrainFromCsvAsync(policyId, labelFeaturName, csvAddress, int.MaxValue);
        }
        static async Task TrainFromCsvAsync(int policyId, string labelFeaturName, string csvAddress, int readLimit)
        {
            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(csvAddress);

            //Read headers
            string[] headers = file.ReadLine().Split(',');
            double[] rewards = file.ReadLine().Split(',').Select(double.Parse).ToArray();

            //Read data into feature vector
            string line; int lineCounter = 0;
            while ((line = file.ReadLine()) != null)
            {
                //increment counter to use as import id
                lineCounter++;

                //Read a line to a string array
                string[] dataobjects = line.Split(',');

                //Create a data vector from the headers and read data line
                RLDT.DataVectorTraining dataVector = new RLDT.DataVectorTraining(headers, dataobjects, rewards, labelFeaturName);

                //Submit to the reinforcement learner
                LearnPolicyAsync(policyId, dataVector);

                //If limit reached, end early
                if (lineCounter == readLimit) break;
            }

            file.Close();
        }

    }
}
