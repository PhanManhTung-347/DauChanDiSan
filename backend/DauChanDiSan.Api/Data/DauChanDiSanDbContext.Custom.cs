using DauChanDiSan.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DauChanDiSan.Api.Data;

public partial class DauChanDiSanDbContext
{
    public DbSet<DongGopDiSan> DongGopDiSans => Set<DongGopDiSan>();

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DongGopDiSan>(entity =>
        {
            entity.ToTable("DongGopDiSan", "dbo");
            entity.HasKey(x => x.DongGopId);
            entity.HasIndex(x => new { x.NguoiDungId, x.MaYeuCau }).IsUnique();
            entity.Property(x => x.PhienBan).IsRowVersion();
            entity.Property(x => x.Loai).HasMaxLength(16).IsUnicode(false);
            entity.Property(x => x.TrangThai).HasMaxLength(16).IsUnicode(false);
            entity.Property(x => x.ViDo).HasPrecision(9, 6);
            entity.Property(x => x.KinhDo).HasPrecision(9, 6);
        });
        var utc = new ValueConverter<DateTime, DateTime>(
            value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(type => type.GetProperties()))
        {
            if (property.Name.EndsWith("Utc", StringComparison.Ordinal)
                && (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?)))
                property.SetValueConverter(utc);
        }
    }
}
