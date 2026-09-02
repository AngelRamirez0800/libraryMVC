using libraryMVC.Abstractions;

namespace libraryMVC.Models
{
    public class Entity<T> : IEntity<T>
    {
        public T Id { get; protected set; } = default!;
        public DateTime CreatedAt { get; protected set; }
        public DateTime? UpdatedAt { get; protected set; }
        public DateTime? DeletedAt { get; protected set; }
        public bool IsActive { get; protected set; } = true;

        public void MarkCreated()
        {
            CreatedAt = DateTime.Now;
        }



        public virtual void Deactivate() {

            IsActive = false;
            DeletedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }

        public virtual void Activate() {
            IsActive = true;
            DeletedAt = null;
            UpdatedAt = DateTime.Now;
        }
    }
}
