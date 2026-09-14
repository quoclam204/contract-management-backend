using ContractManagement.Application.Contracts.Events;
using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Application.Workflow.Interfaces;
using ContractManagement.Application.Workflow.Services;
using ContractManagement.Application.Workflow.Services.SignatureProviders;
using ContractManagement.Domain.Workflow.Enums;
using ContractManagement.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContractManagement.UnitTests.Workflow;

public class SignatureServiceTests
{
    private static ContractManagementDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ContractManagementDbContext(options);
    }

    private class FakePublisher : IPublisher
    {
        public List<INotification> PublishedEvents { get; } = new();

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            if (notification is INotification n)
                PublishedEvents.Add(n);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            PublishedEvents.Add(notification);
            return Task.CompletedTask;
        }
    }

    private static (SignatureService service, FakePublisher publisher, ContractManagementDbContext context) CreateSut(ContractManagementDbContext? context = null)
    {
        var db = context ?? CreateInMemoryDbContext();
        var publisher = new FakePublisher();
        var providers = new ISignatureProvider[]
        {
            new MockSignatureProvider(),
            new OtpSignatureProvider()
        };

        var service = new SignatureService(
            db,
            providers,
            publisher,
            NullLogger<SignatureService>.Instance);

        return (service, publisher, db);
    }

    #region 1. Successful Signature Tests

    [Fact]
    public async Task SignContract_WithValidInternalMock_SavesSignatureSuccessfully()
    {
        // Arrange
        var (service, publisher, context) = CreateSut();
        var contractId = Guid.NewGuid();
        var internalUserId = Guid.NewGuid();

        var request = new CreateSignatureRequest
        {
            ContractId = contractId,
            SignerType = SignerType.InternalUser,
            InternalSignerId = internalUserId,
            SignerName = "Nguyen Van A - Giam doc",
            SignatureMethod = SignatureMethod.Mock
        };

        // Act
        var result = await service.SignContractAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(contractId, result.ContractId);
        Assert.Equal(SignerType.InternalUser, result.SignerType);
        Assert.Equal(internalUserId, result.InternalSignerId);
        Assert.Null(result.PartnerSignerId);
        Assert.Equal("Nguyen Van A - Giam doc", result.SignerNameSnapshot);
        Assert.Equal(SignatureMethod.Mock, result.SignatureMethod);
        Assert.StartsWith("mock_sig_", result.SignatureHash);

        var savedSig = await context.Signatures.FirstOrDefaultAsync(s => s.Id == result.Id);
        Assert.NotNull(savedSig);
        Assert.Equal(result.SignatureHash, savedSig.SignatureHash);
    }

    [Fact]
    public async Task SignContract_WithValidPartnerOtp_SavesSignatureSuccessfully()
    {
        // Arrange
        var (service, publisher, context) = CreateSut();
        var contractId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();

        var request = new CreateSignatureRequest
        {
            ContractId = contractId,
            SignerType = SignerType.PartnerRepresentative,
            PartnerSignerId = partnerId,
            SignerName = "Tran Thi B - Dai dien Doi tac",
            SignatureMethod = SignatureMethod.Otp,
            OtpCode = "123456"
        };

        // Act
        var result = await service.SignContractAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(contractId, result.ContractId);
        Assert.Equal(SignerType.PartnerRepresentative, result.SignerType);
        Assert.Equal(partnerId, result.PartnerSignerId);
        Assert.Null(result.InternalSignerId);
        Assert.Equal(SignatureMethod.Otp, result.SignatureMethod);
        Assert.StartsWith("otp_sig_", result.SignatureHash);
    }

    #endregion

    #region 2. Failed / Invalid Signature Tests

    [Fact]
    public async Task SignContract_WithEmptyContractId_ThrowsArgumentException()
    {
        // Arrange
        var (service, _, _) = CreateSut();
        var request = new CreateSignatureRequest
        {
            ContractId = Guid.Empty,
            SignerType = SignerType.InternalUser,
            InternalSignerId = Guid.NewGuid(),
            SignerName = "Signer"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.SignContractAsync(request));
    }

    [Fact]
    public async Task SignContract_InternalUserWithMissingInternalSignerId_ThrowsArgumentException()
    {
        // Arrange
        var (service, _, _) = CreateSut();
        var request = new CreateSignatureRequest
        {
            ContractId = Guid.NewGuid(),
            SignerType = SignerType.InternalUser,
            InternalSignerId = null,
            SignerName = "Signer"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.SignContractAsync(request));
        Assert.Contains("InternalSignerId", ex.Message);
    }

    [Fact]
    public async Task SignContract_InternalUserWithPartnerSignerIdPresent_ThrowsArgumentException()
    {
        // Arrange
        var (service, _, _) = CreateSut();
        var request = new CreateSignatureRequest
        {
            ContractId = Guid.NewGuid(),
            SignerType = SignerType.InternalUser,
            InternalSignerId = Guid.NewGuid(),
            PartnerSignerId = Guid.NewGuid(), // Invalid! Exclusive constraint
            SignerName = "Signer"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.SignContractAsync(request));
        Assert.Contains("PartnerSignerId phải là null", ex.Message);
    }

    [Fact]
    public async Task SignContract_WithInvalidOtp_ThrowsInvalidOperationException()
    {
        // Arrange
        var (service, _, _) = CreateSut();
        var request = new CreateSignatureRequest
        {
            ContractId = Guid.NewGuid(),
            SignerType = SignerType.InternalUser,
            InternalSignerId = Guid.NewGuid(),
            SignerName = "Signer",
            SignatureMethod = SignatureMethod.Otp,
            OtpCode = "000000" // Expired / simulated invalid
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SignContractAsync(request));
        Assert.Contains("Mã OTP không chính xác", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("abcdef")]
    [InlineData("1234567")]
    public async Task SignContract_WithMalformedOtp_ThrowsInvalidOperationException(string badOtp)
    {
        // Arrange
        var (service, _, _) = CreateSut();
        var request = new CreateSignatureRequest
        {
            ContractId = Guid.NewGuid(),
            SignerType = SignerType.InternalUser,
            InternalSignerId = Guid.NewGuid(),
            SignerName = "Signer",
            SignatureMethod = SignatureMethod.Otp,
            OtpCode = badOtp
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SignContractAsync(request));
    }

    [Fact]
    public async Task SignContract_WhenSamePartySignsTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var (service, _, _) = CreateSut();
        var contractId = Guid.NewGuid();

        var request1 = new CreateSignatureRequest
        {
            ContractId = contractId,
            SignerType = SignerType.InternalUser,
            InternalSignerId = Guid.NewGuid(),
            SignerName = "Dai dien Noi bo",
            SignatureMethod = SignatureMethod.Mock
        };

        await service.SignContractAsync(request1);

        var request2 = new CreateSignatureRequest
        {
            ContractId = contractId,
            SignerType = SignerType.InternalUser, // Same party signs again
            InternalSignerId = Guid.NewGuid(),
            SignerName = "Dai dien Noi bo 2",
            SignatureMethod = SignatureMethod.Mock
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SignContractAsync(request2));
        Assert.Contains("đã hoàn tất chữ ký cho hợp đồng này", ex.Message);
    }

    #endregion

    #region 3. Partial Signatures & All Parties Signed Tests

    [Fact]
    public async Task PartialSignature_InternalSignsOnly_DoesNotPublishContractSignedEvent()
    {
        // Arrange
        var (service, publisher, _) = CreateSut();
        var contractId = Guid.NewGuid();

        var internalRequest = new CreateSignatureRequest
        {
            ContractId = contractId,
            SignerType = SignerType.InternalUser,
            InternalSignerId = Guid.NewGuid(),
            SignerName = "Internal Signer",
            SignatureMethod = SignatureMethod.Mock
        };

        // Act
        await service.SignContractAsync(internalRequest);
        var status = await service.GetSignatureStatusAsync(contractId);

        // Assert
        Assert.True(status.HasInternalSignature);
        Assert.False(status.HasPartnerSignature);
        Assert.False(status.IsFullySigned);
        Assert.Equal(1, status.SignedPartiesCount);
        Assert.Equal(2, status.TotalRequiredParties);

        // Event should NOT be published yet
        Assert.Empty(publisher.PublishedEvents.OfType<ContractSignedEvent>());
    }

    [Fact]
    public async Task AllPartiesSigned_BothInternalAndPartnerSign_PublishesContractSignedEvent()
    {
        // Arrange
        var (service, publisher, _) = CreateSut();
        var contractId = Guid.NewGuid();

        var internalRequest = new CreateSignatureRequest
        {
            ContractId = contractId,
            SignerType = SignerType.InternalUser,
            InternalSignerId = Guid.NewGuid(),
            SignerName = "Giam doc FPT",
            SignatureMethod = SignatureMethod.Mock
        };

        var partnerRequest = new CreateSignatureRequest
        {
            ContractId = contractId,
            SignerType = SignerType.PartnerRepresentative,
            PartnerSignerId = Guid.NewGuid(),
            SignerName = "Tong Giam doc Doi tac",
            SignatureMethod = SignatureMethod.Otp,
            OtpCode = "654321"
        };

        // Act - 1. Internal signs
        await service.SignContractAsync(internalRequest);
        Assert.Empty(publisher.PublishedEvents.OfType<ContractSignedEvent>());

        // Act - 2. Partner signs (Completes all required parties)
        await service.SignContractAsync(partnerRequest);

        // Assert
        var status = await service.GetSignatureStatusAsync(contractId);
        Assert.True(status.HasInternalSignature);
        Assert.True(status.HasPartnerSignature);
        Assert.True(status.IsFullySigned);
        Assert.Equal(2, status.SignedPartiesCount);

        // ContractSignedEvent MUST be published now
        var signedEvent = Assert.Single(publisher.PublishedEvents.OfType<ContractSignedEvent>());
        Assert.Equal(contractId, signedEvent.ContractId);
    }

    [Fact]
    public async Task GetSignaturesByContractId_ReturnsOrderedSignatures()
    {
        // Arrange
        var (service, _, _) = CreateSut();
        var contractId = Guid.NewGuid();

        await service.SignContractAsync(new CreateSignatureRequest
        {
            ContractId = contractId,
            SignerType = SignerType.InternalUser,
            InternalSignerId = Guid.NewGuid(),
            SignerName = "Signer A",
            SignatureMethod = SignatureMethod.Mock
        });

        await service.SignContractAsync(new CreateSignatureRequest
        {
            ContractId = contractId,
            SignerType = SignerType.PartnerRepresentative,
            PartnerSignerId = Guid.NewGuid(),
            SignerName = "Signer B",
            SignatureMethod = SignatureMethod.Mock
        });

        // Act
        var signatures = await service.GetSignaturesByContractIdAsync(contractId);

        // Assert
        Assert.Equal(2, signatures.Count);
        Assert.Equal("Signer A", signatures[0].SignerNameSnapshot);
        Assert.Equal("Signer B", signatures[1].SignerNameSnapshot);
    }

    #endregion
}
