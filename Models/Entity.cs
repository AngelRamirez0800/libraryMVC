using libraryMVC.Abstractions;

namespace libraryMVC.Models
{
    public class Entity<T> : IEntity<T>
    {
        public T Id { get; protected set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsActive { get; set; } = true;



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
