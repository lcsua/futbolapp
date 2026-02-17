using System;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class Role : Entity
    {
        public string Name { get; private set; }
        public string Description { get; private set; }

        protected Role() { }

        public Role(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
