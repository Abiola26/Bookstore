namespace Bookstore.Application.Common;

public static class ApplicationConstants
{
    public static class Pagination
    {
        public const int DefaultPageNumber = 1;
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 100;
    }

    public static class Validation
    {
        public const int MinSearchLength = 2;
        public const int MaxSearchLength = 100;
        public const int MaxEmailLength = 256;
    }

    public static class Cache
    {
        public const int DefaultExpirationSeconds = 60;
        public const int CategoryExpirationSeconds = 300;
    }

    public static class ErrorMessages
    {
        public const string GenericError = "An unexpected error occurred. Please try again later.";
        public const string NotFound = "The requested resource was not found.";
        public const string Unauthorized = "You are not authorized to perform this action.";
        public const string Forbidden = "Access denied.";
        public const string BadRequest = "The request was invalid.";
        public const string Conflict = "A conflict occurred with the current state of the resource.";
        public const string InternalServerError = "A server error occurred while processing your request.";
    }
}
