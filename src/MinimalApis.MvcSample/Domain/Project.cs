using System.ComponentModel.DataAnnotations;

namespace MinimalApis.MvcSample.Domain
{
    public class Project
    {
        public long Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public Client Client { get; set; }
    }
}
