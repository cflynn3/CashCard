using System;
using System.Collections.Generic;
using System.Text;

namespace CashCardTransactions
{
    public interface IPinService
    {
        bool VerifyPin(long accountNumber, int enteredPin);
    }
}
