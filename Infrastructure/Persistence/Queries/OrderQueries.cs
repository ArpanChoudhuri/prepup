using Infra.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infra.Persistence.Queries;    
public static class OrderQueries
{
    // Compiled query: latest 10 orders by customer
    public static readonly Func<AppDbContext, string, IAsyncEnumerable<Order>> GetLatestOrdersByCustomer =
        EF.CompileAsyncQuery((AppDbContext ctx, string customerId) =>
            ctx.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == customerId)
                .Include(o=>o.Items)
                .OrderByDescending(o => o.OrderDate)
                .Take(10));
}
