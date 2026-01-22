


namespace authModule.Common;


public class  Paginations
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;


    public int TotalCount { get; set; }
    public int TotalPages 
    { 
        get 
        {
            return (int)Math.Ceiling((double)TotalCount / Limit);
        }
    }
    
    public bool HasPreviousPage 
    { 
        get 
        {
            return Page > 1;
        }
    }
    public bool HasNextPage 
    { 
        get 
        {
            return Page < TotalPages;
        }
    }
}
public static class PaginationUtils
{
    public static (int Page, int Limit) GetValidPagination(int page, int limit, IConfiguration config)
    {
        // Đọc config
        var defaultPage = config.GetValue<int>("Pagination:DefaultPage"); // Thường là 1
        var defaultLimit = config.GetValue<int>("Pagination:DefaultPageSize"); // Thường là 10
        var maxLimit = config.GetValue<int>("Pagination:MaxPageSize"); // Thường là 50

        // Validate Page
        if (page < 1) page = defaultPage != 0 ? defaultPage : 1;

        // Validate Limit
        if (limit < 1) limit = defaultLimit != 0 ? defaultLimit : 10;

        // Prevent excessive limit
        if (maxLimit > 0 && limit > maxLimit) limit = maxLimit;

        return (page, limit);
    }
}
