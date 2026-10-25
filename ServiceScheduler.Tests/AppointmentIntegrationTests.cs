using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceScheduler.Api.Application;
using ServiceScheduler.Api.Controllers;
using ServiceScheduler.Api.Domain;
using ServiceScheduler.Api.Infrastructure;

namespace ServiceScheduler.Tests;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class AppointmentIntegrationTests(
    PostgreSqlTestFixture fixture) : IAsyncLifetime
{
    private static readonly Guid DealershipId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid OilChangeId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid BrakeServiceId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid EvDiagnosticsId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly Guid VehicleOneId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid VehicleTwoId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid VehicleThreeId = Guid.Parse("20000000-0000-0000-0000-000000000003");
    private static readonly Guid CustomerOneId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid BayOneId = Guid.Parse("60000000-0000-0000-0000-000000000001");
    private static readonly Guid TechnicianOneId = Guid.Parse("70000000-0000-0000-0000-000000000001");

    public async Task InitializeAsync()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        await dbContext.Appointments.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Booking_HappyPath_Returns201PersistsAndCanBeRetrieved()
    {
        var request = CreateRequest(VehicleOneId, OilChangeId, NextOpenTime());

        using var response = await fixture.Client.PostAsJsonAsync("/api/appointments", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        Assert.NotNull(created);
        Assert.Equal(request.VehicleId, created.VehicleId);
        Assert.Equal("Confirmed", created.Status);

        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var persisted = await dbContext.Appointments.SingleAsync(item => item.Id == created.Id);
        Assert.Equal(created.Id, persisted.Id);

        using var getResponse = await fixture.Client.GetAsync($"/api/appointments/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<AppointmentResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
    }

    [Fact]
    public async Task SecondBookingForAnotherVehicle_UsesDifferentBayAndTechnician()
    {
        var start = NextOpenTime();

        using var firstResponse = await fixture.Client.PostAsJsonAsync(
            "/api/appointments", CreateRequest(VehicleOneId, OilChangeId, start));
        using var secondResponse = await fixture.Client.PostAsJsonAsync(
            "/api/appointments", CreateRequest(VehicleTwoId, OilChangeId, start));

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<AppointmentResponse>();
        var second = await secondResponse.Content.ReadFromJsonAsync<AppointmentResponse>();
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first.ServiceBayId, second.ServiceBayId);
        Assert.NotEqual(first.TechnicianId, second.TechnicianId);
    }

    [Fact]
    public async Task ThirdBookingWhenBothBaysAreOccupied_Returns409NoBayAvailable()
    {
        var start = NextOpenTime();
        using var first = await fixture.Client.PostAsJsonAsync(
            "/api/appointments", CreateRequest(VehicleOneId, OilChangeId, start));
        using var second = await fixture.Client.PostAsJsonAsync(
            "/api/appointments", CreateRequest(VehicleTwoId, OilChangeId, start));

        using var third = await fixture.Client.PostAsJsonAsync(
            "/api/appointments", CreateRequest(VehicleThreeId, OilChangeId, start));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, third.StatusCode);
        Assert.Equal("NO_BAY_AVAILABLE", await ReadProblemCode(third));
    }

    [Fact]
    public async Task SecondEvDiagnosticsBookingWhenOnlyTechnicianIsOccupied_Returns409NoTechnicianAvailable()
    {
        var start = NextOpenTime();
        using var first = await fixture.Client.PostAsJsonAsync(
            "/api/appointments", CreateRequest(VehicleThreeId, EvDiagnosticsId, start));

        using var second = await fixture.Client.PostAsJsonAsync(
            "/api/appointments", CreateRequest(VehicleTwoId, EvDiagnosticsId, start));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("NO_TECHNICIAN_AVAILABLE", await ReadProblemCode(second));
    }

    [Fact]
    public async Task BackToBackBookingsForSameVehicle_Succeed()
    {
        var start = NextOpenTime();
        using var first = await fixture.Client.PostAsJsonAsync(
            "/api/appointments", CreateRequest(VehicleOneId, OilChangeId, start));
        using var second = await fixture.Client.PostAsJsonAsync(
            "/api/appointments", CreateRequest(VehicleOneId, OilChangeId, start.AddHours(1)));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task CancelledAppointment_DoesNotBlockItsSlot()
    {
        var request = CreateRequest(VehicleOneId, OilChangeId, NextOpenTime());
        var start = DateTimeOffset.Parse(request.StartTime!);
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
            dbContext.Appointments.Add(new Appointment
            {
                Id = Guid.NewGuid(),
                CustomerId = CustomerOneId,
                VehicleId = VehicleOneId,
                DealershipId = DealershipId,
                ServiceTypeId = OilChangeId,
                ServiceBayId = BayOneId,
                TechnicianId = TechnicianOneId,
                StartUtc = start,
                EndUtc = start.AddHours(1),
                Status = AppointmentStatus.Cancelled,
                CreatedUtc = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        using var response = await fixture.Client.PostAsJsonAsync("/api/appointments", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UnknownVehicle_Returns404VehicleNotFound()
    {
        var request = CreateRequest(Guid.NewGuid(), OilChangeId, NextOpenTime());

        using var response = await fixture.Client.PostAsJsonAsync("/api/appointments", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("VEHICLE_NOT_FOUND", await ReadProblemCode(response));
    }

    [Fact]
    public async Task BookingOutsideWorkingHours_Returns400()
    {
        var start = new DateTimeOffset(
            DateTime.UtcNow.Date.AddDays(2).AddHours(16).AddMinutes(30), TimeSpan.Zero);

        using var response = await fixture.Client.PostAsJsonAsync(
            "/api/appointments", CreateRequest(VehicleOneId, BrakeServiceId, start));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("OUTSIDE_WORKING_HOURS", await ReadProblemCode(response));
    }

    [Fact]
    public async Task TenConcurrentBookingsWithTwoBays_ConfirmExactlyTwo()
    {
        var start = NextOpenTime();
        var vehicleIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToArray();
        await AddVehicles(vehicleIds);

        var responses = await Task.WhenAll(vehicleIds.Select(vehicleId =>
            fixture.Client.PostAsJsonAsync(
                "/api/appointments", CreateRequest(vehicleId, OilChangeId, start))));
        try
        {
            Assert.Equal(2, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
            Assert.Equal(8, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));

            await using var scope = fixture.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
            var confirmedCount = await dbContext.Appointments.CountAsync(appointment =>
                appointment.Status == AppointmentStatus.Confirmed &&
                appointment.StartUtc == start &&
                appointment.EndUtc == start.AddHours(1));
            Assert.Equal(2, confirmedCount);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }

            await using var cleanupScope = fixture.Services.CreateAsyncScope();
            var cleanupContext = cleanupScope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
            await cleanupContext.Appointments
                .Where(appointment => vehicleIds.Contains(appointment.VehicleId))
                .ExecuteDeleteAsync();
            await cleanupContext.Vehicles
                .Where(vehicle => vehicleIds.Contains(vehicle.Id))
                .ExecuteDeleteAsync();
        }
    }

    private async Task AddVehicles(IEnumerable<Guid> vehicleIds)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        foreach (var vehicleId in vehicleIds)
        {
            dbContext.Vehicles.Add(new Vehicle
            {
                Id = vehicleId,
                CustomerId = CustomerOneId,
                Vin = $"TEST-{vehicleId:N}",
                Make = "Test",
                Model = "Vehicle",
                Year = 2026
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static BookingRequest CreateRequest(
        Guid vehicleId,
        Guid serviceTypeId,
        DateTimeOffset start) =>
        new(vehicleId, DealershipId, serviceTypeId, start.ToString("O"));

    private static DateTimeOffset NextOpenTime() =>
        new(DateTime.UtcNow.Date.AddDays(2).AddHours(9), TimeSpan.Zero);

    private static async Task<string?> ReadProblemCode(HttpResponseMessage response)
    {
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
