using EquipTrack.Application.DTOs;
using EquipTrack.Application.Services;
using EquipTrack.Domain.Entities;
using EquipTrack.Domain.Interfaces;
using Moq;
using Xunit;

namespace EquipTrack.Tests.Services;

public class AssetServiceTests
{
    private readonly Mock<IAssetRepository> _assetRepo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<IAllocationRepository> _allocationRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly AssetService _sut;

    public AssetServiceTests()
    {
        _uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _sut = new AssetService(_assetRepo.Object, _employeeRepo.Object, _allocationRepo.Object, _uow.Object);
    }

    [Fact]
    public async Task GetAllAssetsAsync_ReturnsAllAssets()
    {
        _assetRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Asset>
        {
            new() { Id = 1, Name = "Laptop", SerialNumber = "SN001", Status = AssetStatus.Available }
        });

        var result = (await _sut.GetAllAssetsAsync()).ToList();

        Assert.Single(result);
        Assert.Equal("Laptop", result[0].Name);
        Assert.Equal("Available", result[0].Status);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsDto()
    {
        _assetRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Asset { Id = 1, Name = "Laptop", SerialNumber = "SN001", Status = AssetStatus.Available });

        var result = await _sut.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Laptop", result.Name);
        Assert.Equal("Available", result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _assetRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Asset?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAssetAsync_ReturnsNewId()
    {
        _assetRepo.Setup(r => r.AddAsync(It.IsAny<Asset>()))
            .Callback<Asset>(a => a.Id = 5)
            .Returns(Task.CompletedTask);

        var id = await _sut.CreateAssetAsync(new CreateAssetDto { Name = "Monitor", SerialNumber = "MON001" });

        Assert.Equal(5, id);
        _uow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AssignAssetAsync_WhenAssetAndEmployeeExist_ReturnsTrue()
    {
        var asset = new Asset { Id = 1, Status = AssetStatus.Available };
        _assetRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(asset);
        _employeeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Employee { Id = 2 });
        _allocationRepo.Setup(r => r.AddAsync(It.IsAny<Allocation>())).Returns(Task.CompletedTask);

        var result = await _sut.AssignAssetAsync(1, 2);

        Assert.True(result);
        Assert.Equal(AssetStatus.InUse, asset.Status);
        _uow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AssignAssetAsync_WhenAssetNotFound_ReturnsFalse()
    {
        _assetRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Asset?)null);

        var result = await _sut.AssignAssetAsync(99, 1);

        Assert.False(result);
        _uow.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AssignAssetAsync_WhenAssetAlreadyInUse_ThrowsInvalidOperationException()
    {
        var asset = new Asset { Id = 1, Status = AssetStatus.InUse };
        _assetRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(asset);
        _employeeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Employee { Id = 2 });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AssignAssetAsync(1, 2));
        _uow.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ReturnAssetAsync_WhenAssetInUse_ReturnsTrue()
    {
        var asset = new Asset { Id = 1, Status = AssetStatus.InUse };
        var allocation = new Allocation { Id = 10, AssetId = 1, EmployeeId = 2, AssignedDate = DateTime.UtcNow };
        _assetRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(asset);
        _allocationRepo.Setup(r => r.GetActiveByAssetIdAsync(1)).ReturnsAsync(allocation);

        var result = await _sut.ReturnAssetAsync(1);

        Assert.True(result);
        Assert.Equal(AssetStatus.Available, asset.Status);
        Assert.NotNull(allocation.ReturnDate);
        _uow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReturnAssetAsync_WhenAssetNotFound_ReturnsFalse()
    {
        _assetRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Asset?)null);

        var result = await _sut.ReturnAssetAsync(99);

        Assert.False(result);
        _uow.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ReturnAssetAsync_WhenNoActiveAllocation_ReturnsFalse()
    {
        var asset = new Asset { Id = 1, Status = AssetStatus.Available };
        _assetRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(asset);
        _allocationRepo.Setup(r => r.GetActiveByAssetIdAsync(1)).ReturnsAsync((Allocation?)null);

        var result = await _sut.ReturnAssetAsync(1);

        Assert.False(result);
        _uow.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
