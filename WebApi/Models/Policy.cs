using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RLDT;

namespace WebApi.Models
{
    public class Policy : RLDT.Policy
    {
        //Properties
        public int PolicyId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        //Constructors
        public Policy(int id, string name, string description, double explorationRate, double discountFactor) : this(id, name, description)
        {
            base.ExplorationRate = explorationRate;
            base.DiscountFactor = discountFactor;
        }
        public Policy(int id, string name, string description)
        {
            this.PolicyId = id;
            this.Name = name;
            this.Description = description;
        }
    }
}
