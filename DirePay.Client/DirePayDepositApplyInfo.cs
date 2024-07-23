using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tgs.Payment.DirePay.Client;
public record DirePayDepositApplyInfo(
    DirePayDepositSettings Settings,
    DirePayDepositResponse Response);
