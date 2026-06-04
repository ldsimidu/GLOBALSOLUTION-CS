using GlobalSolution.SenseSpot.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GlobalSolution.SenseSpot.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceConfiguration> DeviceConfigurations => Set<DeviceConfiguration>();
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<RiskAssessment> RiskAssessments => Set<RiskAssessment>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasIndex(x => x.SerialNumber).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.SerialNumber).HasMaxLength(80).IsRequired();
            entity.Property(x => x.EnvironmentContext).HasMaxLength(80).IsRequired();
            entity.Property(x => x.IsActive).HasConversion<int>().HasColumnType("NUMBER(1)");
            entity.HasOne(x => x.Configuration)
                .WithOne(x => x.Device)
                .HasForeignKey<DeviceConfiguration>(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Sensor>(entity =>
        {
            entity.HasDiscriminator(x => x.SensorType)
                .HasValue<TemperatureSensor>(SensorType.Temperature)
                .HasValue<HumiditySensor>(SensorType.Humidity)
                .HasValue<LuminositySensor>(SensorType.Luminosity)
                .HasValue<AirQualitySensor>(SensorType.AirQuality)
                .HasValue<VibrationSensor>(SensorType.Vibration);

            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Unit).HasMaxLength(30).IsRequired();
            entity.Property(x => x.IsActive).HasConversion<int>().HasColumnType("NUMBER(1)");
            entity.HasOne(x => x.Device)
                .WithMany(x => x.Sensors)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SensorReading>(entity =>
        {
            entity.Property(x => x.Value).HasPrecision(10, 2);
            entity.Property(x => x.IsSynced).HasConversion<int>().HasColumnType("NUMBER(1)");
            entity.HasOne(x => x.Device)
                .WithMany(x => x.SensorReadings)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Sensor)
                .WithMany(x => x.Readings)
                .HasForeignKey(x => x.SensorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.Property(x => x.Message).HasMaxLength(240).IsRequired();
            entity.Property(x => x.IsAcknowledged).HasConversion<int>().HasColumnType("NUMBER(1)");
            entity.HasOne(x => x.Device)
                .WithMany(x => x.Alerts)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RiskAssessment>(entity =>
        {
            entity.Property(x => x.Summary).HasMaxLength(600).IsRequired();
            entity.Property(x => x.RecommendedAction).HasMaxLength(240).IsRequired();
            entity.Property(x => x.PrimaryRiskFactors).HasMaxLength(240).IsRequired();
            entity.HasOne(x => x.Device)
                .WithMany(x => x.RiskAssessments)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SyncLog>(entity =>
        {
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(240).IsRequired();
            entity.HasOne(x => x.Device)
                .WithMany(x => x.SyncLogs)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeviceConfiguration>(entity =>
        {
            entity.Property(x => x.TemperatureAlertThreshold).HasPrecision(10, 2);
            entity.Property(x => x.HumidityAlertThreshold).HasPrecision(10, 2);
            entity.Property(x => x.LuminosityAlertThreshold).HasPrecision(10, 2);
            entity.Property(x => x.AirQualityAlertThreshold).HasPrecision(10, 2);
            entity.Property(x => x.VibrationAlertThreshold).HasPrecision(10, 2);
        });
    }
}
