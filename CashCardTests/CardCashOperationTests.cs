using Moq;
using System;
using Xunit;
using CashCardTransactions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace CashCardTests
{
    public class TestsFixture : IDisposable
    {
        public Mock<IPinService> MockPinService { get; private set; }

        public TestsFixture()
        {
            // Do "global" initialization here; Only called once.
            MockPinService = new Mock<IPinService>();
            MockPinService.Setup(x => x.VerifyPin(It.IsAny<long>(), 1234)).Returns(true);
            MockPinService.Setup(x => x.VerifyPin(It.IsAny<long>(), It.IsNotIn(1234))).Returns(false);
        }

        public void Dispose()
        {
            // Do "global" teardown here; Only called once.
        }
    }
    
    public class CardCashOperationTests: IClassFixture<TestsFixture>
    {
        private readonly Mock<IPinService> _mockPinservice;
        private readonly TestsFixture _testsFixture;

        public CardCashOperationTests(TestsFixture fixture)
        {
            _testsFixture = fixture;
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 1)]
        [InlineData(1, 100000)]
        [InlineData(999999, 999999)]
        public void IncorrectPinWithdrawRejectedBalanceNotRevealed(decimal withdrawAmount, decimal balance)
        {
            // Arrange
            var cashCard = new CashCard(_testsFixture.MockPinService.Object, 1, balance);

            // Act
            var result = cashCard.WithdrawCash(1111, withdrawAmount);

            // Assert
            Assert.Equal(RejectionReasonEnum.IncorrectPin, result.RejectionReason);
            Assert.Equal(0, result.RemainingBalance);
        }

        [Theory]
        [InlineData(1.01, 1)]
        [InlineData(2, 1)]
        [InlineData(100000, 1)]
        [InlineData(999999, 999998.99)]
        public void CorrectPinAmountTooLargeWithdrawRejected(decimal withdrawAmount, decimal balance)
        {
            // Arrange
            var cashCard = new CashCard(_testsFixture.MockPinService.Object, 1, balance);

            // Act
            var result = cashCard.WithdrawCash(1234, withdrawAmount);

            // Assert
            Assert.Equal(RejectionReasonEnum.InsufficientBalance, result.RejectionReason);
            Assert.Equal(balance, result.RemainingBalance);
        }

        [Theory]
        [InlineData(1.01, 1)]
        [InlineData(2, 1)]
        [InlineData(100000, 1)]
        [InlineData(999999, 999998.99)]
        public void CorrectPinCorrectAmountWithdrawSucceeds(decimal balance, decimal withdrawAmount)
        {
            // Arrange
            var cashCard = new CashCard(_testsFixture.MockPinService.Object, 1, balance);

            // Act
            var result = cashCard.WithdrawCash(1234, withdrawAmount);

            // Assert
            Assert.Null(result.RejectionReason);
            Assert.Equal(balance - withdrawAmount, result.RemainingBalance);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 1)]
        [InlineData(1, 100000)]
        [InlineData(999999, 999999)]
        public void IncorrectPinTopupRejectedBalanceNotRevealed(decimal topupAmount, decimal balance)
        {
            // Arrange
            var cashCard = new CashCard(_testsFixture.MockPinService.Object, 1, balance);

            // Act
            var result = cashCard.TopupCard(1111, topupAmount);

            // Assert
            Assert.Equal(RejectionReasonEnum.IncorrectPin, result.RejectionReason);
            Assert.Equal(0, result.RemainingBalance);
        }

        [Theory]
        [InlineData(1.01, 1)]
        [InlineData(2, 1)]
        [InlineData(100000, 1)]
        [InlineData(999999, 999998.99)]
        public void CorrectPinTopupSucceeds(decimal balance, decimal topupAmount)
        {
            // Arrange
            var cashCard = new CashCard(_testsFixture.MockPinService.Object, 1, balance);

            // Act
            var result = cashCard.TopupCard(1234, topupAmount);

            // Assert
            Assert.Null(result.RejectionReason);
            Assert.Equal(balance + topupAmount, result.RemainingBalance);
        }

        // Test to confirm that calling WithdrawCash() on multiple threads is safe and will not allow withdrawing past zero
        [Fact]
        public async void MultipleSimultaneousWithdrawTransactions()
        {
            // Arrange
            var cashCard = new CashCard(_testsFixture.MockPinService.Object, 1, 5m);

            // Act
            var withdrawTask1 = Task.Run(() => { return cashCard.WithdrawCash(1234, 5m); });
            var withdrawTask2 = Task.Run(() => { return cashCard.WithdrawCash(1234, 5m); });
            var withdrawTask3 = Task.Run(() => { return cashCard.WithdrawCash(1234, 5m); });

            await Task.WhenAll(withdrawTask1, withdrawTask2, withdrawTask3);

            // Assert
            // Check that we received exactly 2 rejections for insufficient balance from the 3 operations
            var rejectionReasons = new List<RejectionReasonEnum?>() { withdrawTask1.Result.RejectionReason, withdrawTask2.Result.RejectionReason, withdrawTask3.Result.RejectionReason };

            Assert.Single(rejectionReasons.Where(x => x == null));
            Assert.Equal(2, rejectionReasons.Where(x => x == RejectionReasonEnum.InsufficientBalance).Count());

            Assert.Equal(0m, withdrawTask1.Result.RemainingBalance);
            Assert.Equal(0m, withdrawTask2.Result.RemainingBalance);
            Assert.Equal(0m, withdrawTask3.Result.RemainingBalance);
        }
    }
}
