namespace ContosoUniversity.Data
{
    using System.Linq;
    using System.Threading.Tasks;

    using Domain.Contracts.Paging;

    using Microsoft.EntityFrameworkCore;

    public static class PagingExtensions
    {
        public static async Task<PagedResult<T>> ToPageAsync<T>(
            this IQueryable<T> source, 
            PageRequest request) 
            => new(
                await source.TakePage(request).ToArrayAsync(), 
                new PageInfo(
                    request,
                    await source.CountAsync()));

        private static IQueryable<T> TakePage<T>(
            this IQueryable<T> source,
            PageRequest request) 
            => source
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);
    }
}