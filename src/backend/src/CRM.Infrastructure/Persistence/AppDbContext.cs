using CRM.Domain.Agents;
using CRM.Domain.Auth;
using CRM.Domain.Branches;
using CRM.Domain.Notifications;
using CRM.Domain.Categories;
using CRM.Domain.Customers;
using CRM.Domain.Departments;
using CRM.Domain.KnowledgeBase;
using CRM.Domain.Sla;
using CRM.Domain.Templates;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerContact> CustomerContacts => Set<CustomerContact>();
    public DbSet<CustomerCredential> CustomerCredentials => Set<CustomerCredential>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<TicketHistory> TicketHistory => Set<TicketHistory>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<TicketCategory> TicketCategories => Set<TicketCategory>();
    public DbSet<TicketFieldDefinition> TicketFieldDefinitions => Set<TicketFieldDefinition>();
    public DbSet<QuickReplyTemplate> QuickReplyTemplates => Set<QuickReplyTemplate>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<BusinessHours> BusinessHours => Set<BusinessHours>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<TicketSla> TicketSlas => Set<TicketSla>();
    public DbSet<KbCategory> KbCategories => Set<KbCategory>();
    public DbSet<KbArticle> KbArticles => Set<KbArticle>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AgentTask> AgentTasks => Set<AgentTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
