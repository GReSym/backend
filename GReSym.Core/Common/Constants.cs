namespace GReSym.Core.Common;

public static class Constants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string Moderator = "Moderator";
    }
    
    public static class Validation
    {
        public const int MaxEmailLength = 100;
        public const int MaxPasswordHashLength = 100;
        public const int MaxDisplayNameLength = 64;
        public const int MaxGameTitleLength = 255;
    }
    
    public static class Api
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
    }
}