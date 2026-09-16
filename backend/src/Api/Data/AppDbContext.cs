using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<Itemtype> Itemtypes { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Userrole> Userroles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Itemid).HasName("items_pkey");

            entity.ToTable("items");

            entity.HasIndex(e => e.Itemtypeid, "ix_items_itemtypeid");

            entity.HasIndex(e => e.Projectid, "ix_items_projectid");

            entity.HasIndex(e => e.Statusid, "ix_items_statusid");

            entity.HasIndex(e => e.Title, "ix_items_title");

            entity.Property(e => e.Itemid).UseIdentityAlwaysColumn().HasColumnName("itemid");
            entity.Property(e => e.Assignedtouserid).HasColumnName("assignedtouserid");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Itemtypeid).HasColumnName("itemtypeid");
            entity.Property(e => e.Projectid).HasColumnName("projectid");
            entity.Property(e => e.Statusid).HasColumnName("statusid");
            entity.Property(e => e.Title).HasMaxLength(200).HasColumnName("title");

            entity
                .HasOne(d => d.Assignedtouser)
                .WithMany(p => p.ItemAssignedtousers)
                .HasForeignKey(d => d.Assignedtouserid)
                .HasConstraintName("items_assignedtouserid_fkey");

            entity
                .HasOne(d => d.Itemtype)
                .WithMany(p => p.Items)
                .HasForeignKey(d => d.Itemtypeid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("items_itemtypeid_fkey");

            entity
                .HasOne(d => d.Project)
                .WithMany(p => p.Items)
                .HasForeignKey(d => d.Projectid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("items_projectid_fkey");

            entity
                .HasOne(d => d.Status)
                .WithMany(p => p.Items)
                .HasForeignKey(d => d.Statusid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("items_statusid_fkey");
        });

        modelBuilder.Entity<Itemtype>(entity =>
        {
            entity.HasKey(e => e.Itemtypeid).HasName("itemtypes_pkey");

            entity.ToTable("itemtypes");

            entity.HasIndex(e => e.Name, "itemtypes_name_key").IsUnique();

            entity
                .Property(e => e.Itemtypeid)
                .UseIdentityAlwaysColumn()
                .HasColumnName("itemtypeid");
            entity.Property(e => e.Isactive).HasDefaultValue(true).HasColumnName("isactive");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Projectid).HasName("projects_pkey");

            entity.ToTable("projects");

            entity.Property(e => e.Projectid).UseIdentityAlwaysColumn().HasColumnName("projectid");
            entity.Property(e => e.Createdbyuserid).HasColumnName("createdbyuserid");
            entity
                .Property(e => e.Description)
                .HasMaxLength(Project.MaxDescriptionLength)
                .HasColumnName("description");
            entity.Property(e => e.Isactive).HasDefaultValue(true).HasColumnName("isactive");
            entity
                .Property(e => e.Name)
                .HasMaxLength(Project.MaxNameLength)
                .HasColumnName("name");

            entity
                .HasOne(d => d.Createdbyuser)
                .WithMany(p => p.Projects)
                .HasForeignKey(d => d.Createdbyuserid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("projects_createdbyuserid_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Roleid).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Name, "roles_name_key").IsUnique();

            entity.Property(e => e.Roleid).UseIdentityAlwaysColumn().HasColumnName("roleid");
            entity.Property(e => e.Description).HasMaxLength(200).HasColumnName("description");
            entity.Property(e => e.Name).HasMaxLength(50).HasColumnName("name");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.Statusid).HasName("statuses_pkey");

            entity.ToTable("statuses");

            entity.HasIndex(e => e.Name, "statuses_name_key").IsUnique();

            entity.Property(e => e.Statusid).UseIdentityAlwaysColumn().HasColumnName("statusid");
            entity.Property(e => e.Isactive).HasDefaultValue(true).HasColumnName("isactive");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.Sortorder).HasColumnName("sortorder");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Userid).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Userid).UseIdentityAlwaysColumn().HasColumnName("userid");
            entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("email");
            entity.Property(e => e.Fullname).HasMaxLength(150).HasColumnName("fullname");
            entity.Property(e => e.Isactive).HasDefaultValue(true).HasColumnName("isactive");
            entity.Property(e => e.Passwordhash).HasMaxLength(255).HasColumnName("passwordhash");
        });

        modelBuilder.Entity<Userrole>(entity =>
        {
            entity.HasKey(e => new { e.Userid, e.Roleid }).HasName("userroles_pkey");

            entity.ToTable("userroles");

            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Roleid).HasColumnName("roleid");

            entity
                .HasOne(d => d.Role)
                .WithMany(p => p.Userroles)
                .HasForeignKey(d => d.Roleid)
                .HasConstraintName("userroles_roleid_fkey");

            entity
                .HasOne(d => d.User)
                .WithMany(p => p.Userroles)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("userroles_userid_fkey");
        });
    }
}
