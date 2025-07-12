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
                entity.HasKey(e => e.ContainerId);
                // For Guid primary keys, don't use ValueGeneratedOnAdd() - let the application generate the Guid
                entity.Property(e => e.ContainerId).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.ContainerName)
                      .IsRequired()
                      .HasMaxLength(255); // Set maximum length for string property
                entity.Property(e => e.PurchaseDateTime).IsRequired();
                entity.Property(e => e.TotalQuantity).IsRequired();
                entity.Property(e => e.RemainingQuantity).IsRequired();
                entity.Property(e => e.Amount)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)"); // Define precision for decimal type

                // Relationships (Foreign Keys)
                // Container to User (Buyer)
                entity.HasOne(d => d.Buyer)
                      .WithMany(p => p.Containers) // User has many Containers (as a buyer)
                      .HasForeignKey(d => d.BuyerId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent deleting a User if they have associated Containers

                // Container to Room
                entity.HasOne(d => d.Room)
                      .WithMany(p => p.Containers) // Room has many Containers
                      .HasForeignKey(d => d.RoomId)
                      .OnDelete(DeleteBehavior.Cascade); // Deleting a Room will delete its associated Containers
            });

            // Configure the Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.OrderId);
                entity.Property(e => e.OrderId).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.OrderName)
                      .IsRequired()
                      .HasMaxLength(255);
                entity.Property(e => e.Datestamp).IsRequired();
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.Amount)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");
                // Assuming OrderType and OrderStatus are enums, EF Core maps them to integer columns by default
                entity.Property(e => e.OrderType).IsRequired();
                entity.Property(e => e.OrderStatus).IsRequired();

                // Relationships (Foreign Keys)
                // Order to User
                entity.HasOne(d => d.User)
                      .WithMany(p => p.Orders) // User has many Orders
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent deleting a User if they have associated Orders
            });

            // Configure the OrderDetail entity
            modelBuilder.Entity<OrderDetail>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.OrderDetailId);
                entity.Property(e => e.OrderDetailId).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.DetailQuantity).IsRequired();
                entity.Property(e => e.Price)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");
                entity.Property(e => e.Amount)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");
                entity.Property(e => e.OrderDetailStatus).IsRequired();

                // Relationships (Foreign Keys)
                // OrderDetail to Order
                entity.HasOne(d => d.Order)
                      .WithMany(p => p.OrderDetails) // Order has many OrderDetails
                      .HasForeignKey(d => d.OrderId)
                      .OnDelete(DeleteBehavior.Cascade); // Deleting an Order will delete its associated OrderDetails

                // OrderDetail to Container
                entity.HasOne(d => d.Container)
                      .WithMany(p => p.OrderDetails) // Container has many OrderDetails
                      .HasForeignKey(d => d.ContainerId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent deleting a Container if it's part of an OrderDetail
            });

            // Configure the RefreshToken entity
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.Token)
                      .IsRequired()
                      .HasMaxLength(500); // Sufficient length for JWT or similar tokens
                entity.Property(e => e.Expires).IsRequired();
                entity.Property(e => e.Created).IsRequired();
                entity.Property(e => e.CreatedByIp)
                      .HasMaxLength(45); // Max length for IPv6 address
                entity.Property(e => e.Revoked).IsRequired(false); // Nullable property
                entity.Property(e => e.RevokedByIp)
                      .HasMaxLength(45);
                entity.Property(e => e.ReplacedByToken)
                      .HasMaxLength(500);

                // Relationships (Foreign Keys)
                // RefreshToken to User
                entity.HasOne(d => d.User)
                      .WithMany(p => p.RefreshTokens) // User has many RefreshTokens
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade); // Deleting a User will delete their RefreshTokens
            });

            // Configure the Room entity
            modelBuilder.Entity<Room>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.RoomId);
                entity.Property(e => e.RoomId).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.RoomName)
                      .IsRequired()
                      .HasMaxLength(255);
                entity.Property(e => e.RoomCode).IsRequired();
                // Add a unique index to RoomCode to ensure uniqueness
                entity.HasIndex(e => e.RoomCode).IsUnique();
                entity.Property(e => e.IsPublic).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            // Configure the Transaction entity
            modelBuilder.Entity<Transaction>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.TransactionId);
                entity.Property(e => e.TransactionId).ValueGeneratedNever();

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
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.FirstName)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(e => e.LastName)
                      .HasMaxLength(100);
                entity.Property(e => e.Email)
                      .IsRequired()
                      .HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role).IsRequired(); // Assuming Role is an integer or enum
                entity.Property(e => e.Provider)
                      .HasMaxLength(50);
            });

            // Configure the UserPassword entity
            modelBuilder.Entity<UserPassword>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.Id);
                // For Guid primary keys, don't use ValueGeneratedOnAdd() - let the application generate the Guid
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.PasswordHash)
                      .IsRequired()
                      .HasMaxLength(500); // Sufficient length for hashed passwords

                // Add a unique index to UserId to ensure one password per user
                entity.HasIndex(e => e.UserId).IsUnique();

                // Relationships (Foreign Keys)
                // UserPassword to User (One-to-One relationship)
                entity.HasOne(d => d.User)
                      .WithOne(p => p.UserPassword) // User has one UserPassword
                      .HasForeignKey<UserPassword>(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade); // Deleting a User will delete their UserPassword
            });

            // Configure the UserRoom entity
            modelBuilder.Entity<UserRoom>(entity =>
            {
                // Primary Key configuration
                entity.HasKey(e => e.Id);
                // For Guid primary keys, don't use ValueGeneratedOnAdd() - let the application generate the Guid
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Property configurations
                entity.Property(e => e.IsAdmin).IsRequired();
                entity.Property(e => e.JoinedAt).IsRequired();

                // Composite unique index
                entity.HasIndex(ur => new { ur.UserId, ur.RoomId }).IsUnique();

                // Relationships (Foreign Keys)
                entity.HasOne(d => d.User)
                      .WithMany(p => p.UserRooms)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Room)
                      .WithMany(p => p.UserRooms)
                      .HasForeignKey(d => d.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}