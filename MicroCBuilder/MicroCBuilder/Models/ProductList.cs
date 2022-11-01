using MicroCLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCBuilder.Models
{
    public class ProductList
    {
        public Guid Guid { get; set; }
        public DateTime Created { get; set; }

        public string Name { get; set; }
        public string Author { get; set; }

        public List<BuildComponent> Components { get; set; }

        public float Price => Components.Sum(c => c.Item.Price * c.Item.Quantity);

        public ProductList()
        {
            Guid = Guid.NewGuid();
            Created = DateTime.Now;
        }

        public ProductList(List<BuildComponent> components)
            : this()
        {
            Components = components;
        }
    }
}