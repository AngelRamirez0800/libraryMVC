namespace libraryMVC.Security;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Librarian = "Librarian";
    public const string User = "User";

    public const string BookReaders = Admin + "," + Librarian + "," + User;
    public const string BookManagers = Admin + "," + Librarian;
}
