using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    public partial class PoliciesController : Controller
    {
        //Fields
        public static List<Policy> Policies = new List<Policy>();

        //GET api/policies
        public IEnumerable<object> Get()
        {
            return Policies.Select(p=> new { p.PolicyId, p.Name, p.Description, p.StateSpaceCount });
        }

        //POST api/Create
        [HttpPost("Create")]
        public int Create([FromBody] CreatePolicyParameters parameters)
        {
            //Create and add the new policy
            Policy newPolicy = new Policy(Policies.Count + 1, parameters.Name, parameters.Description);
            Policies.Add(newPolicy);

            //return the generated Id
            return newPolicy.PolicyId;
        }

        //POST api/{id}/Learn
        [HttpPost("{policyId}/Learn")]
        public void Learn(int policyId, [FromBody] RLDT.DataVectorTraining dataVector)
        {
            new System.Threading.Thread(delegate ()
            {

                //Find the policy, by ID
                Policy thePolicy = Policies.Find(p => p.PolicyId == policyId);

                //if null, end early
                if (thePolicy == null)
                    return;

                //Learn
                thePolicy.Learn(dataVector);

            }).Start();
            
        }

        //// GET api/values/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST api/values
        //[HttpPost]
        //public void Post([FromBody]string value)
        //{
        //}

        //// PUT api/values/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        //// DELETE api/values/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }

    [Route("viewer/[controller]")]
    public partial class PoliciesController
    {
        //Index
        [Route("list")]
        public ActionResult Index(int policyId)
        {
            return View("List", Policies);
        }

        [Route("{policyId}/Details")]
        public ActionResult Details(int policyId)
        {
            Policy thePolicy = Policies.Find(p => p.PolicyId == policyId);

            return View("Details", thePolicy);
        }
    }
}
