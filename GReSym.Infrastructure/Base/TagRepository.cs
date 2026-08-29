using GReSym.Core.Common;
using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Interfaces;
using GReSym.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GReSym.Infrastructure.Base;

public class TagRepository : RepositoryBase<Tag>, ITagRepository
{
    public TagRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Tag?> GetByNameAsync(string name)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _dbSet
            .AnyAsync(t => t.Name.ToLower() == name.ToLower());
    }

    public async Task<IEnumerable<Tag>> SearchByNameAsync(string searchTerm, int limit = Constants.Api.DefaultPageSize)
    {
        return await _dbSet
            .Where(t => EF.Functions.Like(t.Name, $"%{searchTerm}%"))
            .OrderBy(t => t.Name)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Tag> GetOrCreateAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name cannot be empty", nameof(name));

        var normalizedName = name.Trim();

        // Пытаемся найти существующий тег
        var existingTag = await _dbSet
            .FirstOrDefaultAsync(t => t.Name.ToLower() == normalizedName.ToLower(), cancellationToken);

        if (existingTag != null)
            return existingTag;

        // Создаем новый тег
        var newTag = new Tag { Name = normalizedName };
        _dbSet.Add(newTag);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return newTag;
        }
        catch (DbUpdateException ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("Duplicate entry"))
        {
            // Если кто-то уже вставил тег параллельно
            // Просто возвращаем существующий тег из базы
            existingTag = await _dbSet
                .FirstAsync(t => t.Name.ToLower() == normalizedName.ToLower(), cancellationToken);
            return existingTag;
        }
    }
}