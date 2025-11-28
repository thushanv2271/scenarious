using SharedKernel;

namespace Domain.RiskEvaluations;

/// 
/// Master data for risk indicators (SICR and OEIL)
/// 
public enum RiskIndicatorCategory
{
    SICR = 1,  // Significant Increase in Credit Risk
    OEIL = 2   // Objective Evidence of Incurred Loss
}
