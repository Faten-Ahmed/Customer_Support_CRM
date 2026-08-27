using CRM.Domain.Common;
using CRM.Application.Customers.Queries;
using CRM.Domain.Customers;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Customers;

public class ListCustomersQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly ListCustomersQueryHandler _handler;

    public ListCustomersQueryHandlerTests()
    {
        _handler = new ListCustomersQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_NoFilter_ReturnsPagedResult()
    {
        var customers = new List<CustomerSummaryProjection>
        {
            new(Guid.NewGuid(), "Ali Hassan", "ali@example.com", null, null, false, true, 3, DateTime.UtcNow),
        };

        _repo.Setup(r => r.ListAsync(It.IsAny<CustomerFilter>(), default))
             .ReturnsAsync(new PagedResult<CustomerSummaryProjection>(customers, 1, 1, 20));

        var result = await _handler.Handle(
            new ListCustomersQuery(null, null, null, 1, 20, null, false), default);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Ali Hassan", result.Items[0].FullName);
    }

    [Fact]
    public async Task Handle_WithFilter_PassesFilterToRepository()
    {
        _repo.Setup(r => r.ListAsync(
            It.Is<CustomerFilter>(f => f.Search == "Ali" && f.IsVip == true),
            default))
             .ReturnsAsync(new PagedResult<CustomerSummaryProjection>(
                 new List<CustomerSummaryProjection>(), 0, 1, 20));

        await _handler.Handle(
            new ListCustomersQuery("Ali", true, null, 1, 20, null, false), default);

        _repo.Verify(r => r.ListAsync(
            It.Is<CustomerFilter>(f => f.Search == "Ali" && f.IsVip == true), default),
            Times.Once);
    }
}
