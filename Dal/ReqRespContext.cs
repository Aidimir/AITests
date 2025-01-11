using Microsoft.EntityFrameworkCore;

namespace Dal;

public class ReqRespContext : DbContext
{
    public ReqRespContext(DbContextOptions<ReqRespContext> options) : base(options)
    {
    }

    private DbSet<ReqRespModel> _reqRespModels { get; set; }

    public async Task AddToDbAsync(ReqRespModel model)
    {
        await _reqRespModels.AddAsync(model);
        await SaveChangesAsync();
    }

    public async Task<IEnumerable<ReqRespModel>> GetAllUsersModelAsync(Guid userId)
    {
        return await _reqRespModels.Where(m => m.UserId == userId).ToListAsync();
    }
}