﻿// <auto-generated />
using HSESport_web_app_trial2.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace HSESport_web_app_trial2.Migrations
{
    [DbContext(typeof(HSESport_web_app_trial2Context))]
    partial class HSESport_web_app_trial2ContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("HSESport_web_app_trial2.Models.Students", b =>
                {
                    b.Property<int>("StudentIdentificator")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("StudentIdentificator"));

                    b.Property<int>("StudentAttendanceOnSportActivities")
                        .HasColumnType("int");

                    b.Property<string>("StudentEmail")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("StudentName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("StudentSurname")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("studentSecondName")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("StudentIdentificator");

                    b.ToTable("Students");
                });
#pragma warning restore 612, 618
        }
    }
}
