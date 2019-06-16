using System;
using System.Collections.Generic;
using System.Text;

namespace CashCardTransactions
{
    public enum RejectionReasonEnum
    {
        IncorrectPin,
        InsufficientBalance
    }

    public class TransactionResult
    {
        public RejectionReasonEnum? RejectionReason { get; set; }
        public decimal RemainingBalance { get; set; }
    }
}
