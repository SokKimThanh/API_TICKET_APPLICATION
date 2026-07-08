using System.Collections.Generic;

namespace API_TICKET_APPLICATION.Models
{
    public class PagedData<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public IEnumerable<T> Data { get; set; } = new List<T>();
    }
}
