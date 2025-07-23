using EggLedger.Models.Models;
using Microsoft.EntityFrameworkCore;

namespace EggLedger.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Container> Containers { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserPassword> UserPasswords { get; set; } = null!;
        public DbSet<UserRoom> UserRooms { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the Container entity
            modelBuilder.Entity<Container>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PurchaseDateTime).IsRequired();
                entity.Property(e => e.TotalQuantity).IsRequired();
                entity.Property(e => e.RemainingQuantity).IsRequired();
                entity.Property(e => e.Amount).IsRequired().HasColumnType("decimal(18,2)");

                // Relationships (Foreign Keys)
                // Container to User (Buyer)
                entity.HasOne(d => d.Buyer)
                      .WithMany(p => p.Containers) // User has many Containers (as a buyer)
                      .HasForeignKey(d => d.BuyerId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Container to Room
                entity.HasOne(d => d.Room)
                      .WithMany(p => p.Containers) // Room has many Containers
                      .HasForeignKey(d => d.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure the Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Datestamp).IsRequired();
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.Amount).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Status).IsRequired();

                // Relationships (Foreign Keys)
                // Order to User
                entity.HasOne(d => d.User)
                      .WithMany(p => p.Orders)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure the OrderDetail entity
            modelBuilder.Entity<OrderDetail>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.Amount).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).IsRequired();

                // Relationships (Foreign Keys)
                // OrderDetail to Order
                entity.HasOne(d => d.Order)
                      .WithMany(p => p.OrderDetails)
                      .HasForeignKey(d => d.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                // OrderDetail to Container
                entity.HasOne(d => d.Container)
                      .WithMany(p => p.OrderDetails)
                      .HasForeignKey(d => d.ContainerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure the RefreshToken entity
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Expires).IsRequired();
                entity.Property(e => e.Created).IsRequired();
                entity.Property(e => e.CreatedByIp).HasMaxLength(45);
                entity.Property(e => e.Revoked).IsRequired(false);
                entity.Property(e => e.RevokedByIp).HasMaxLength(45);
                entity.Property(e => e.ReplacedByToken).HasMaxLength(500);

                // Relationships (Foreign Keys)
                // RefreshToken to User
                entity.HasOne(d => d.User)
                      .WithMany(p => p.RefreshTokens)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure the Room entity
            modelBuilder.Entity<Room>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Code).IsRequired();
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.IsPublic).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            // Configure the Transaction entity
            modelBuilder.Entity<Transaction>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.Datestamp).IsRequired();
                entity.Property(e => e.Amount).IsRequired();
                entity.Property(e => e.Status).IsRequired();

                // Relationships (Foreign Keys)
                // Transaction to Order
                entity.HasOne(d => d.Order)
                      .WithMany(p => p.Transactions)
                      .HasForeignKey(d => d.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Transaction to User (Payer)
                entity.HasOne(d => d.Payer)
                      .WithMany()
                      .HasForeignKey(d => d.PayerId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("FK_Transaction_Payer");

                // Transaction to User (Receiver)
                entity.HasOne(d => d.Receiver)
                      .WithMany()
                      .HasForeignKey(d => d.ReceiverId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("FK_Transaction_Receiver");
            });

            // Configure the User entity
            modelBuilder.Entity<User>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role).IsRequired(); // Assuming Role is an integer or enum
                entity.Property(e => e.Provider).HasMaxLength(50);
            });

            // Configure the UserPassword entity
            modelBuilder.Entity<UserPassword>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);

                // Add a unique index to UserId to ensure one password per user
                entity.HasIndex(e => e.UserId).IsUnique();

                // Relationships (Foreign Keys)
                // UserPassword to User (One-to-One relationship)
                entity.HasOne(d => d.User)
                      .WithOne(p => p.UserPassword)
                      .HasForeignKey<UserPassword>(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure the UserRoom entity
            modelBuilder.Entity<UserRoom>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.IsAdmin).IsRequired();
                entity.Property(e => e.JoinedAt).IsRequired();

                // Composite unique index
                entity.HasIndex(ur => new { ur.UserId, ur.RoomId }).IsUnique();

                // Relationships (Foreign Keys)
                // UserRoom to User
                entity.HasOne(d => d.User)
                      .WithMany(p => p.UserRooms)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                // UserRoom to Room
                entity.HasOne(d => d.Room)
                      .WithMany(p => p.UserRooms)
                      .HasForeignKey(d => d.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}