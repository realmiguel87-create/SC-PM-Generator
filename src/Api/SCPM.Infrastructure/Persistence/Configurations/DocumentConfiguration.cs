using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Document", "Documents", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("Document_History", "Documents");
        }));

        builder.Property(d => d.Title).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Category).HasMaxLength(100).IsRequired();

        builder.HasOne(d => d.Project).WithMany().HasForeignKey(d => d.ProjectId);
        builder.HasMany(d => d.Versions).WithOne(v => v.Document).HasForeignKey(v => v.DocumentId);
    }
}

public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("DocumentVersion", "Documents", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("DocumentVersion_History", "Documents");
        }));

        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(v => v.VersionLabel);

        // Snapshot also cascades from Project (via DocumentVersion -> Document -> Project and
        // DocumentVersion -> Snapshot -> Project both reaching Project) — same multiple-cascade-
        // paths issue as Gateway.RibaStageInstance and Escalation.Risk/Issue.
        builder.HasOne(v => v.Snapshot).WithMany().HasForeignKey(v => v.SnapshotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(v => v.Files).WithOne(f => f.DocumentVersion).HasForeignKey(f => f.DocumentVersionId);

        builder.HasIndex(v => new { v.DocumentId, v.MajorVersion, v.MinorVersion }).IsUnique();
    }
}

public class DocumentFileConfiguration : IEntityTypeConfiguration<DocumentFile>
{
    public void Configure(EntityTypeBuilder<DocumentFile> builder)
    {
        // Physical files are immutable once created (a new export is a new row, never an
        // overwrite — see Document.cs) so, unlike Document/DocumentVersion, there is nothing
        // to version here.
        builder.ToTable("DocumentFile", "Documents");

        builder.Property(f => f.FileType).HasMaxLength(20).IsRequired();
        builder.Property(f => f.Category).HasMaxLength(100).IsRequired();
        builder.Property(f => f.FileName).HasMaxLength(260).IsRequired();
        builder.Property(f => f.StorageUrl).HasMaxLength(2000);
        builder.Property(f => f.BlobArchiveUrl).HasMaxLength(2000);
    }
}
