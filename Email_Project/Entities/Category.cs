namespace Email_Project.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }
        public String CategoryName { get; set; }
        public String Keywords { get; set; }

        public virtual ICollection<Message> Messages { get; set; }
    }
}
