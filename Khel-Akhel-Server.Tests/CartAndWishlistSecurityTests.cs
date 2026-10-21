using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Khel_Akhel_Server.BL.Services;
using Khel_Akhel_Server.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Khel_Akhel_Server.Tests
{
    public class CartAndWishlistSecurityTests
    {
        private readonly IUrlEncryptionService _encryptionService;

        public CartAndWishlistSecurityTests()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Security:UrlEncryptionKey", "TestUrlEncryptionKey2026_Secure32BytesLength!" },
                { "Jwt:Key", "TestJwtKeyForUnitTesting2026Secure32BytesKey!" }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _encryptionService = new UrlEncryptionService(configuration);
        }

        #region Guest Wishlist Token Security Tests
        [Fact]
        public void GenerateGuestToken_ProducesUniqueHighEntropyUrlSafeTokens()
        {
            // Act
            string token1 = WishlistService.GenerateGuestToken();
            string token2 = WishlistService.GenerateGuestToken();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(token1));
            Assert.False(string.IsNullOrWhiteSpace(token2));
            Assert.NotEqual(token1, token2);
            // Must be URL-safe (no +, no /, no =)
            Assert.DoesNotContain("+", token1);
            Assert.DoesNotContain("/", token1);
            Assert.DoesNotContain("=", token1);
            Assert.True(token1.Length >= 40, "Token length should provide at least 256 bits of entropy");
        }

        [Fact]
        public void HashGuestToken_ProducesDeterministicSha256Hash()
        {
            // Arrange
            string token = "test-guest-token-12345";

            // Act
            string hash1 = WishlistService.HashGuestToken(token);
            string hash2 = WishlistService.HashGuestToken(token);
            string hashDifferent = WishlistService.HashGuestToken("different-token");

            // Assert
            Assert.Equal(hash1, hash2);
            Assert.NotEqual(hash1, hashDifferent);
            Assert.False(string.IsNullOrWhiteSpace(hash1));
        }
        #endregion

        #region URL Encryption Tests
        [Theory]
        [InlineData(1L)]
        [InlineData(42L)]
        [InlineData(999999L)]
        [InlineData(long.MaxValue)]
        public void UrlEncryption_EncryptsAndDecryptsSuccessfully(long originalId)
        {
            // Act
            string encrypted = _encryptionService.Encrypt(originalId);
            bool success = _encryptionService.TryDecrypt(encrypted, out long decryptedId);

            // Assert
            Assert.True(success);
            Assert.Equal(originalId, decryptedId);
            Assert.False(string.IsNullOrWhiteSpace(encrypted));
            // URL safe
            Assert.DoesNotContain("+", encrypted);
            Assert.DoesNotContain("/", encrypted);
            Assert.DoesNotContain("=", encrypted);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("InvalidBase64String!!")]
        [InlineData("Tampered_Encrypted_Data_12345")]
        public void UrlEncryption_RejectsTamperedOrInvalidPayloads(string invalidPayload)
        {
            // Act
            bool success = _encryptionService.TryDecrypt(invalidPayload, out long decryptedId);

            // Assert
            Assert.False(success);
            Assert.Equal(0, decryptedId);
        }
        #endregion

        #region Validation Attribute Tests
        private static IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-50)]
        [InlineData(101)]
        public void AddCartItemRequest_RejectsInvalidQuantities(int quantity)
        {
            // Arrange
            var request = new AddCartItemRequest
            {
                EncryptedProductId = "valid-encrypted-id",
                Quantity = quantity
            };

            // Act
            var errors = ValidateModel(request);

            // Assert
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(AddCartItemRequest.Quantity)));
        }

        [Fact]
        public void AddCartItemRequest_RejectsMissingProductId()
        {
            // Arrange
            var request = new AddCartItemRequest
            {
                EncryptedProductId = "",
                Quantity = 1
            };

            // Act
            var errors = ValidateModel(request);

            // Assert
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(AddCartItemRequest.EncryptedProductId)));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        public void AddCartItemRequest_AcceptsValidQuantities(int quantity)
        {
            // Arrange
            var request = new AddCartItemRequest
            {
                EncryptedProductId = "valid-encrypted-id",
                Quantity = quantity
            };

            // Act
            var errors = ValidateModel(request);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        [InlineData(101)]
        public void UpdateCartItemQuantityRequest_RejectsInvalidQuantity(int quantity)
        {
            // Arrange
            var request = new UpdateCartItemQuantityRequest
            {
                Quantity = quantity
            };

            // Act
            var errors = ValidateModel(request);

            // Assert
            Assert.NotEmpty(errors);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(101)]
        public void MoveToCartRequest_RejectsInvalidQuantity(int quantity)
        {
            // Arrange
            var request = new MoveToCartRequest
            {
                Quantity = quantity
            };

            // Act
            var errors = ValidateModel(request);

            // Assert
            Assert.NotEmpty(errors);
        }
        #endregion
    }
}
