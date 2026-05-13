using EquipTrack.Application.DTOs;
using EquipTrack.Application.Services;
using EquipTrack.Domain.Entities;
using EquipTrack.Domain.Interfaces;
using Moq;
using Xunit;

namespace EquipTrack.Tests.Services;

public class EmployeeServiceTests
{
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly EmployeeService _sut;

    public EmployeeServiceTests()
    {
        _uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _sut = new EmployeeService(_employeeRepo.Object, _uow.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEmployees()
    {
        _employeeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Employee>
        {
            new() { Id = 1, Name = "Alice", Department = "IT", Email = "alice@example.com" }
        });

        var result = (await _sut.GetAllAsync()).ToList();

        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal("alice@example.com", result[0].Email);
    }

    [Fact]
    public async Task CreateEmployeeAsync_ReturnsNewEmployeeDto()
    {
        _employeeRepo.Setup(r => r.AddAsync(It.IsAny<Employee>()))
            .Callback<Employee>(e => e.Id = 7)
            .Returns(Task.CompletedTask);

        var id = await _sut.CreateAsync(new CreateEmployeeDto
        {
            Name = "Bob",
            Department = "Finance",
            Email = "bob@example.com"
        });

        Assert.Equal(7, id);
        _uow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsDto()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(
            new Employee { Id = 3, Name = "Carol", Department = "HR", Email = "carol@example.com" });

        var result = await _sut.GetByIdAsync(3);

        Assert.NotNull(result);
        Assert.Equal("Carol", result.Name);
        Assert.Equal("carol@example.com", result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Employee?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_MapsAllFieldsToEntity()
    {
        Employee? captured = null;
        _employeeRepo.Setup(r => r.AddAsync(It.IsAny<Employee>()))
            .Callback<Employee>(e => { captured = e; e.Id = 1; })
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateEmployeeDto
        {
            Name = "Dan",
            Department = "Legal",
            Email = "dan@example.com"
        });

        Assert.NotNull(captured);
        Assert.Equal("Dan", captured!.Name);
        Assert.Equal("Legal", captured.Department);
        Assert.Equal("dan@example.com", captured.Email);
    }
}
