namespace libraryMVC.Abstractions
{
    public interface IEntity <T> 
    {
        public T Id { get; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsActive { get; set; }

    }
}
