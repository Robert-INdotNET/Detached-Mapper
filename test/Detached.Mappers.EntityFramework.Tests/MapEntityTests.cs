﻿using Detached.Mappers.EntityFramework.Tests.Model;
using Detached.Mappers.EntityFramework.Tests.Model.DTOs;
using Detached.Mappers.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Detached.Mappers.EntityFramework.Tests
{
    public class MapEntityTests
    {
        [Fact]
        public async Task map_entity()
        {
            DefaultTestDbContext dbContext = await DefaultTestDbContext.CreateAsync();

            dbContext.Roles.Add(new Role { Id = 1, Name = "admin" });
            dbContext.Roles.Add(new Role { Id = 2, Name = "user" });
            dbContext.UserTypes.Add(new UserType { Id = 1, Name = "system" });
            await dbContext.SaveChangesAsync();

            await dbContext.MapAsync<User>(new UserDTO
            {
                Id = 1,
                Name = "cr",
                Profile = new UserProfileDTO
                {
                    FirstName = "chris",
                    LastName = "redfield"
                },
                Addresses = new List<AddressDTO>
                {
                    new AddressDTO { Street = "rc", Number = "123" }
                },
                Roles = new List<RoleDTO>
                {
                    new RoleDTO { Id = 1 },
                    new RoleDTO { Id = 2 }
                },
                UserType = new UserTypeDTO { Id = 1 }
            });

            await dbContext.SaveChangesAsync();

            User user = await dbContext.Users.Where(u => u.Id == 1)
                    .Include(u => u.Roles)
                    .Include(u => u.Addresses)
                    .Include(u => u.Profile)
                    .Include(u => u.UserType)
                    .FirstOrDefaultAsync();

            Assert.Equal(1, user.Id);
            Assert.Equal("cr", user.Name);
            Assert.NotNull(user.Profile);
            Assert.Equal("chris", user.Profile.FirstName);
            Assert.Equal("redfield", user.Profile.LastName);
            Assert.NotNull(user.Addresses);
            Assert.Equal("rc", user.Addresses[0].Street);
            Assert.Equal("123", user.Addresses[0].Number);
            Assert.NotNull(user.Roles);
            Assert.Equal(2, user.Roles.Count);
            Assert.Contains(user.Roles, r => r.Id == 1);
            Assert.Contains(user.Roles, r => r.Id == 2);
            Assert.NotNull(user.UserType);
            Assert.Equal(1, user.UserType.Id);
        }

        [Fact]
        public async Task map_entity_not_found()
        {
            DefaultTestDbContext db = await DefaultTestDbContext.CreateAsync();

            db.Roles.Add(new Role { Id = 1, Name = "admin" });
            db.Roles.Add(new Role { Id = 2, Name = "user" });
            db.UserTypes.Add(new UserType { Id = 1, Name = "system" });
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<MapperException>(() =>
                db.MapAsync<User>(new UserDTO
                {
                    Id = 1,
                    Name = "cr",
                },
                new MapParameters
                {
                    Upsert = false
                })
            );
        }
    }
}