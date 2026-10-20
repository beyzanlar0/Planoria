using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planoria.Models;
using Microsoft.EntityFrameworkCore;

namespace Planoria.Data
{
   
    
    public class AppDbContext : DbContext
    {
        public AppDbContext()
        {
        }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<GroupTask> GroupTasks { get; set; }
        public DbSet<GroupTaskAssignment> GroupTaskAssignments { get; set; }
        public DbSet<DirectConversation> DirectConversations { get; set; }
        public DbSet<DirectMessage> DirectMessages { get; set; }
        public DbSet<GroupInvite> GroupInvites { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // FriendRequest: Sender ve Receiver ilişkilerinin
            // birbirini silmesini (cascade delete çakışmasını) önlüyoruz
            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Sender)
                .WithMany(u => u.SentFriendRequests)
                .HasForeignKey(fr => fr.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany(u => u.ReceivedFriendRequests)
                .HasForeignKey(fr => fr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GroupTaskAssignment>()
    .HasIndex(a => new { a.GroupTaskId, a.UserId })
    .IsUnique();

            modelBuilder.Entity<DirectConversation>()
    .HasOne(c => c.Initiator).WithMany().HasForeignKey(c => c.InitiatorId)
    .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DirectConversation>()
                .HasOne(c => c.Recipient).WithMany().HasForeignKey(c => c.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DirectMessage>()
                .HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<GroupInvite>()
                .HasOne(i => i.InvitedUser).WithMany().HasForeignKey(i => i.InvitedUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<GroupInvite>()
                .HasOne(i => i.InvitedBy).WithMany().HasForeignKey(i => i.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<GroupInvite>()
                .HasIndex(i => new { i.GroupId, i.InvitedUserId }).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<GroupMember>().HasIndex(gm => new { gm.GroupId, gm.UserId }).IsUnique();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(DbSecrets.ConnectionString);
            }


        }
    }
}

