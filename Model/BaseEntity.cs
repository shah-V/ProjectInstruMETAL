namespace Model
{
    // מחלקת בסיס מופשטת לכל הישויות בפרויקט
    public abstract class BaseEntity
    {
        public int Id { get; set; }

        // בנאים מוגדרים כ-protected כי רק מחלקות יורשות משתמשות בהם
        protected BaseEntity() { }

        protected BaseEntity(int id)
        {
            Id = id;
        }
    }
}