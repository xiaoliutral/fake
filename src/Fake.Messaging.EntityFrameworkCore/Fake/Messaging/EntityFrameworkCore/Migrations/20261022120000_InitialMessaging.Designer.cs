using System;
using Fake.Messaging.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fake.Messaging.EntityFrameworkCore.Migrations;

[DbContext(typeof(MessagingDbContext))]
[Migration("20261022120000_InitialMessaging")]
partial class InitialMessaging
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.0");
#pragma warning restore 612, 618
    }
}
