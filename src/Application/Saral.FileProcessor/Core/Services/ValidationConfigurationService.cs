namespace Saral.FileProcessor.Core.Services;

public sealed class ValidationConfigurationService : IValidationConfigurationService
{
    public IDataValidator ConfigureValidation(ReadOnlySpan<string> columnNames) =>
        ConfigureColumnValidations(new DataValidator(), columnNames);
    
    private static DataValidator ConfigureColumnValidations(DataValidator dataValidator, ReadOnlySpan<string> columnNames)
    {
        // First column validation: Customer Number - not empty
        if (columnNames.Length > 0)
        {
            dataValidator.AddValidator(new NotEmptyValidator(columnNames[0]));
        }
        
        // Second column validation: Facility number - not empty and unique
        if (columnNames.Length > 1)
        {
            dataValidator.AddValidator(new NotEmptyValidator(columnNames[1]));
            dataValidator.AddValidator(new UniqueValidator(columnNames[1]));
        }
        
        // Third column validation: Branch - not empty
        if (columnNames.Length > 2)
        {
            dataValidator.AddValidator(new NotEmptyValidator(columnNames[2]));
        }
        
        // Configure remaining columns using pattern matching and switch expressions
        ConfigureProductAndBusinessColumns(dataValidator, columnNames);
        ConfigureFinancialColumns(dataValidator, columnNames);
        ConfigureCollateralAndStatusColumns(dataValidator, columnNames);
        
        return dataValidator;
    }
    
    private static void ConfigureProductAndBusinessColumns(DataValidator dataValidator, ReadOnlySpan<string> columnNames)
    {
        // Fourth column validation: Product category - Required, Validation is required
        if (columnNames.Length > 3)
        {
            dataValidator.AddValidator(new NotEmptyValidator(columnNames[3]));
        }
        
        // Fifth column validation: Segment - Required, Validation is required
        if (columnNames.Length > 4)
        {
            dataValidator.AddValidator(new NotEmptyValidator(columnNames[4]));
        }
        
        // Sixth column validation: Industry - Optional, Display warning message
        if (columnNames.Length > 5)
        {
            dataValidator.AddValidator(new WarningValidator(columnNames[5], "Industry field is optional but recommended for better analysis"));
        }
        
        // Seventh column validation: Earning Type - Optional, Validation is not required
        // No validation added for optional fields without validation requirement
        
        // Eighth column validation: Nature - Required, Validation is required
        if (columnNames.Length > 7)
        {
            dataValidator.AddValidator(new NotEmptyValidator(columnNames[7]));
        }
        
        // Ninth column validation: Grant date - Optional, Validation is not required
        // No validation added
        
        // Tenth column validation: Maturity date/ Expiry Date - Required, Validation is required => if nature = 'Revolving' allow empty value
        if (columnNames.Length > 9)
        {
            dataValidator.AddValidator(new ConditionalNotEmptyValidator(columnNames[9], columnNames[7], "Revolving", true));
        }
    }
    
    private static void ConfigureFinancialColumns(DataValidator dataValidator, ReadOnlySpan<string> columnNames)
    {
        // Eleventh column validation: Interest Rate - Required, Validation is required, Number
        if (columnNames.Length > 10)
        {
            dataValidator.AddValidators(
                new NotEmptyValidator(columnNames[10]),
                new NumericRangeValidator(columnNames[10], 0, double.MaxValue));
        }
        
        // Twelfth column validation: Installment Type - Required, Validation is required
        if (columnNames.Length > 11)
        {
            dataValidator.AddValidator(new NotEmptyValidator(columnNames[11]));
        }
        
        // Thirteenth column validation: Days Past Due - Required, Validation is required, Number
        if (columnNames.Length > 12)
        {
            dataValidator.AddValidators(
                new NotEmptyValidator(columnNames[12]),
                new NumericRangeValidator(columnNames[12], 0, double.MaxValue));
        }
        
        // Fourteenth column validation: Limit - Optional, Validation is required
        if (columnNames.Length > 13)
        {
            dataValidator.AddValidator(new NumericRangeValidator(columnNames[13], 0, double.MaxValue));
        }
        
        // Fifteenth column validation: Total OS - Dependent on column H (Nature), Required
        if (columnNames.Length > 14)
        {
            dataValidator.AddValidator(new DependentValidator(columnNames[14], columnNames[7], "Required"));
        }
        
        // Sixteenth column validation: Undisbursed Amount - Validation is required
        if (columnNames.Length > 15)
        {
            dataValidator.AddValidator(new NumericRangeValidator(columnNames[15], 0, double.MaxValue));
        }
        
        // Seventeenth column validation: Interest in Suspense - Optional, Validation is not required
        // No validation added
    }
    
    private static void ConfigureCollateralAndStatusColumns(DataValidator dataValidator, ReadOnlySpan<string> columnNames)
    {
        // Eighteenth column validation: Collateral Type - Optional, Validation is not required
        // No validation added
        
        // Nineteenth column validation: Collateral Value - Optional, Validation is required, Depend on R (Cash Collateral)
        if (columnNames.Length > 18)
        {
            dataValidator.AddValidator(new NumericRangeValidator(columnNames[18], 0, double.MaxValue));
        }

        // Twentieth column validation: Rescheduled (Yes/No) - Optional, Validation is not required
        // No validation added

        // Twenty-first column validation: Restructured (Yes/No) - Optional, Validation is not required
        // No validation added

        // Configure remaining status columns using modern patterns
        (int Index, IColumnValidator Validator)[] statusColumnConfigs =
        [
            (Index: 21, Validator: (IColumnValidator)new NumericRangeValidator(columnNames.Length > 21 ? columnNames[21] : "", 0, double.MaxValue)),
            (Index: 22, Validator: (IColumnValidator)new BooleanValidator(columnNames.Length > 22 ? columnNames[22] : "")),
            (Index: 23, Validator: (IColumnValidator)new BooleanValidator(columnNames.Length > 23 ? columnNames[23] : "")),
            (Index: 24, Validator: (IColumnValidator)new NumericRangeValidator(columnNames.Length > 24 ? columnNames[24] : "", 0, double.MaxValue))
        ];
        
        foreach ((int index, IColumnValidator? validator) in statusColumnConfigs)
        {
            if (columnNames.Length > index)
            {
                dataValidator.AddValidator(validator);
            }
        }
    }
}
