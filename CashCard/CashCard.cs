using System;

namespace CashCardTransactions
{
    public class CashCard
    {
        private readonly IPinService _pinService;
        private readonly long _accountNumber;
        private decimal _remainingAmount;
        private object _transactionLock = new object();

        public CashCard(IPinService pinService, long accountNumber, decimal remainingAmount)
        {
            _pinService = pinService;
            _accountNumber = accountNumber;
            _remainingAmount = remainingAmount;
        }

        public TransactionResult WithdrawCash(int suppliedPin, decimal withdrawAmount)
        {
            if(!_pinService.VerifyPin(_accountNumber, suppliedPin))
            {
                return new TransactionResult() { RejectionReason = RejectionReasonEnum.IncorrectPin };
            }

            lock (_transactionLock)
            {
                if (withdrawAmount > _remainingAmount)
                {
                    return new TransactionResult() { RejectionReason = RejectionReasonEnum.InsufficientBalance, RemainingBalance = _remainingAmount };
                }

                _remainingAmount -= withdrawAmount;
            }

            return new TransactionResult() { RejectionReason = null, RemainingBalance = _remainingAmount }; ;
        }

        public TransactionResult TopupCard(int suppliedPin, decimal topupAmount)
        {
            if (!_pinService.VerifyPin(_accountNumber, suppliedPin))
            {
                return new TransactionResult() { RejectionReason = RejectionReasonEnum.IncorrectPin };
            }

            lock (_transactionLock)
            {
                _remainingAmount += topupAmount;
            }

            return new TransactionResult() { RejectionReason = null, RemainingBalance = _remainingAmount }; ;
        }
    }
}
