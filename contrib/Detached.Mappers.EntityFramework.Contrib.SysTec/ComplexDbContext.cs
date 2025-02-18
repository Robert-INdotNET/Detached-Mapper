﻿using Detached.Mappers.EntityFramework;
using Detached.Mappers.EntityFramework.Contrib.SysTec.ComplexModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Detached.Mappers.EntityFramework.Contrib.SysTec.DeepModel;

namespace Detached.Mappers.EntityFramework.Contrib.SysTec
{
    public class ComplexDbContext : DbContext
    {
        public DbSet<OrganizationList> OrganizationLists { get; set; }
        public DbSet<OrganizationBase> Organizations { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Government> Governments { get; set; }
        public DbSet<SubGovernment> SubGovernments { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Country> Countries { get; set; }

        public DbSet<OrganizationNotes> OrganizationNotes { get; set; }
        public DbSet<CustomerKind> CustomerKinds { get; set; }
        public DbSet<Recommendation> Recommendations { get; set; }

        public DbSet<TodoItem> TodoItems { get; set; }
        public DbSet<ReusedLinkedItem> ReusedLinkedItems { get; set; }
        public DbSet<UploadedFile> UploadedFiles { get; set; }

        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite("Data Source=ComplexDB.db;")
                .EnableSensitiveDataLogging()
                .LogTo(message => Debug.WriteLine(message), LogLevel.Information)
                .EnableDetailedErrors()
                .UseDetached(options =>
                {
                    // not what we want, because OrganizationBase can be Customer or Government
                    // but this line is a test to drive it further
                    //options.Configure<OrganizationBase>().Constructor(x => new Customer());

                    //options.Type<OrganizationBase>().Discriminator(o => o.OrganizationType)
                    //    .Value(nameof(Customer), typeof(Customer))
                    //    .Value(nameof(Government), typeof(Government))
                    //    .Value(nameof(SubGovernment), typeof(GovernmentLeader));
                });

            //optionsBuilder.ConfigureWarnings(
            //    w => w.Ignore(CoreEventId.NavigationBaseIncludeIgnored)
            //);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrganizationBase>()
                .HasDiscriminator(o => o.OrganizationType)
                .HasValue<Customer>(nameof(Customer))
                .HasValue<Government>(nameof(Government))
                .HasValue<SubGovernment>(nameof(SubGovernment));

            modelBuilder.Entity<OrganizationBase>()
                .HasOne<Address>(o => o.PrimaryAddress);

            modelBuilder.Entity<OrganizationBase>()
                .HasOne<Address>(o => o.ShipmentAddress);

            // Back-references don't work
            // Exception:
            // System.InvalidOperationException : An error was generated for warning
            // 'Microsoft.EntityFrameworkCore.Query.NavigationBaseIncludeIgnored': The navigation
            // 'OrganizationNotes.Organization' was ignored from 'Include' in the query since the fix-up
            // will automatically populate it. If any further navigations are specified in 'Include' afterwards
            // then they will be ignored. Walking back include tree is not allowed. This exception can be suppressed
            // or logged by passing event ID 'CoreEventId.NavigationBaseIncludeIgnored' to the 'ConfigureWarnings' method
            // in 'DbContext.OnConfiguring' or 'AddDbContext'.
            modelBuilder.Entity<OrganizationBase>()
                .HasMany<OrganizationNotes>(o => o.Notes);
            // .WithOne(n => n.Organization)
            // .HasForeignKey(k => k.OrganizationId);

            modelBuilder.Entity<OrganizationBase>(entity =>
            {
                entity.HasOne(s => s.Parent);
                entity.HasMany(s => s.Children)
                .WithOne()
                .HasForeignKey(s => s.ParentId);
            });

            modelBuilder.Entity<Customer>()
                .HasMany<Recommendation>(c => c.Recommendations)
                .WithOne(r => r.RecommendedBy);
        }

        public override int SaveChanges()
        {
            //var now = DateTime.Now;
            foreach (var changedEntity in ChangeTracker.Entries())
            {
                if (changedEntity.Entity is ConcurrencyStampBase entity)
                {
                    // Simulate DB Trigger.
                    // With postgresql we are using xmin column which gets updated automatically
                    entity.ConcurrencyToken += 1;
                }
            }
            try
            {
                int result = base.SaveChanges();
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var entries = ex.Entries;
                throw;
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException;
                throw;
            }
        }
    }
}
