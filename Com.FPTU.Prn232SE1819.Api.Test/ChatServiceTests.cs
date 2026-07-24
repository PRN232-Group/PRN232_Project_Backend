using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Services;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class ChatServiceTests
{
    [Fact]
    public async Task GetCustomerMessages_empty_when_no_thread()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ChatService(db);

            Assert.Empty(await svc.GetCustomerMessagesAsync(1));
        }
    }

    [Fact]
    public async Task GetCustomerMessages_ordered_with_IsFromCustomer()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ChatService(db);

            await svc.SendMessageAsync(1, "Customer", new SendMessageDto { Content = "Hi" });
            await Task.Delay(15);
            await svc.SendMessageAsync(2, "Sales", new SendMessageDto
            {
                Content = "Hello",
                CustomerId = 1,
            });

            var msgs = await svc.GetCustomerMessagesAsync(1);
            Assert.Equal(2, msgs.Count);
            Assert.True(msgs[0].SentAt <= msgs[1].SentAt);
            Assert.True(msgs[0].IsFromCustomer);
            Assert.False(msgs[1].IsFromCustomer);
            Assert.Equal("Hi", msgs[0].Content);
            Assert.Equal("Hello", msgs[1].Content);
        }
    }

    [Fact]
    public async Task GetChatCustomers_ordered_and_skips_null_customer()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ChatService(db);

            await svc.SendMessageAsync(1, "Customer", new SendMessageDto { Content = "first" });
            db.Users.Add(new User
            {
                Id = 5,
                Email = "kh2@test.com",
                PasswordHash = "x",
                FullName = "Khach 2",
                RoleId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
            await Task.Delay(15);
            await svc.SendMessageAsync(5, "Customer", new SendMessageDto { Content = "second" });

            db.ChatThreads.Add(new ChatThread
            {
                CustomerId = 999,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow.AddHours(1),
            });
            await db.SaveChangesAsync();

            var customers = await svc.GetChatCustomersAsync();
            Assert.Equal(2, customers.Count);
            Assert.DoesNotContain(customers, c => c.CustomerId == 999);
            Assert.Equal(5, customers[0].CustomerId);
            Assert.Equal(1, customers[1].CustomerId);
        }
    }

    [Fact]
    public async Task SendMessage_Customer_creates_thread()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ChatService(db);

            var msg = await svc.SendMessageAsync(1, "Customer", new SendMessageDto
            {
                Content = "Xin chao",
            });

            Assert.Equal(1, msg.CustomerId);
            Assert.True(msg.IsFromCustomer);
            Assert.Single(db.ChatThreads.Where(t => t.CustomerId == 1));
        }
    }

    [Fact]
    public async Task SendMessage_Customer_reuses_existing_thread()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ChatService(db);

            await svc.SendMessageAsync(1, "Customer", new SendMessageDto { Content = "1" });
            await svc.SendMessageAsync(1, "Customer", new SendMessageDto { Content = "2" });

            Assert.Equal(1, db.ChatThreads.Count(t => t.CustomerId == 1));
            Assert.Equal(2, db.ChatMessages.Count());
        }
    }

    [Fact]
    public async Task SendMessage_Sales_with_CustomerId()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ChatService(db);

            var msg = await svc.SendMessageAsync(2, "Sales", new SendMessageDto
            {
                Content = "Support",
                CustomerId = 1,
            });

            Assert.Equal(1, msg.CustomerId);
            Assert.False(msg.IsFromCustomer);
            Assert.Equal(2, msg.SenderId);
        }
    }

    [Fact]
    public async Task SendMessage_staff_without_CustomerId_throws()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ChatService(db);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                svc.SendMessageAsync(2, "Sales", new SendMessageDto { Content = "oops" }));
        }
    }
}
