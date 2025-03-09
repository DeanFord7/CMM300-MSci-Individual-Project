using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPLAssistant.Models
{
    public class RecommendedTransfer
    {
        public PlayerData PlayerOut { get; set; }
        public PlayerData PlayerIn { get; set; }
        public double? BudgetImpact { get; set; }
    }
}
