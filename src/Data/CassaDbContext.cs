using CassaEventiAI.Models;
using Microsoft.EntityFrameworkCore;

namespace CassaEventiAI.Data;

public class CassaDbContext : DbContext
{
    private readonly string _dbPath;

    public CassaDbContext(string dbPath) { _dbPath = dbPath; }
    public CassaDbContext() { _dbPath = "cassa_default.db"; }

    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<OperatorShift> OperatorShifts => Set<OperatorShift>();

    protected override void OnConfiguring(DbContextOptionsBuilder o)
    {
        if (!o.IsConfigured)
            o.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Sale>()
            .HasMany(s => s.Items).WithOne(i => i.Sale)
            .HasForeignKey(i => i.SaleId).OnDelete(DeleteBehavior.Cascade);

        m.Entity<Sale>().Property(s => s.Total).HasColumnType("decimal(10,2)");
        m.Entity<Sale>().Property(s => s.Subtotal).HasColumnType("decimal(10,2)");
        m.Entity<SaleItem>().Property(i => i.LineTotal).HasColumnType("decimal(10,2)");
        m.Entity<OperatorShift>().Property(s => s.TotalAmount).HasColumnType("decimal(10,2)");
    }

    public static CassaDbContext Create(string path)
    {
        var ctx = new CassaDbContext(path);
        ctx.Database.EnsureCreated();
        return ctx;
    }
}
