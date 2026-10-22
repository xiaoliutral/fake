using Fake.Messaging.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Fake.Messaging.EntityFrameworkCore.Migrations;

[DbContext(typeof(MessagingDbContext))]
partial class MessagingDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.0");
#pragma warning restore 612, 618
    }
}
