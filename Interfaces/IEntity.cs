namespace libraryMVC.Abstractions
{
    public interface IEntity<T>
    {
        public T Id { get; }
        public DateTime CreatedAt { get; }
        public DateTime? UpdatedAt { get; }
        public DateTime? DeletedAt { get; }
        public bool IsActive { get; }

    }
}
