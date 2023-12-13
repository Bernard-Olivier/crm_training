using Microsoft.Xrm.Sdk;
using System;

namespace ContactPlugin
{
    public class PreCreate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService =
            (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity entity = (Entity)context.InputParameters["Target"];

                if (entity.LogicalName != "contact")
                {
                    tracingService.Trace("Contact PreCreate: The required entity was not found");
                    return;
                }

                try
                {
                    // Calculate the age of the client
                    DateTime birthdate = entity.GetAttributeValue<DateTime>(ContactFields.BIRTH_DATE);
                    DateTime today = new DateTime();
                    today = DateTime.Now;
                    int age = today.Year - birthdate.Year;
                    entity[ContactFields.AGE] = age;

                    // Calculate Maturity Date based on the Investment Period
                    DateTime joinDate = entity.GetAttributeValue<DateTime>(ContactFields.JOINING_DATE);
                    int investmentPeriod = entity.GetAttributeValue<int>(ContactFields.INVESTMENT_PERIOD);
                    DateTime maturityDate = joinDate.AddMonths(investmentPeriod);
                    entity[ContactFields.MATURITY_DATE] = maturityDate.Date;

                    // Calculate the Estimated Return
                    double initialInvestment = (double)entity.GetAttributeValue<Money>(ContactFields.INITIAL_INVESTMENT).Value;
                    double investmentRate = (double)entity.GetAttributeValue<decimal>(ContactFields.INVESTMENT_RATE);
                    decimal estimatedReturn = CalculateEstimatedReturn(initialInvestment, investmentRate, investmentPeriod);
                    entity[ContactFields.ESTIMATED_RETURN] = new Money(estimatedReturn);

                    // Auto set Status Reason to “In - Force”
                    entity[ContactFields.STATUS_REASON] = 1;
                    tracingService.Trace("Contact PreCreate plugin: Successfully");
                }

                catch (InvalidPluginExecutionException ex)
                {
                    tracingService.Trace("Contact PreCreate plugin: {0}", ex.ToString());
                    throw new InvalidPluginExecutionException("An error occurred in Contact Pre-reate plugin", ex);
                }
            }


        }
        private decimal CalculateEstimatedReturn(double initialInvestment, double investmentRate, int investmentPeriodInMonths)
        {
            // Convert the investment period to years
            double investmentPeriodInYears = investmentPeriodInMonths / 12.0;

            // Formula for compound interest: A = P * (1 + r/n)^(nt)
            // Where:
            // A = the future value of the investment/loan, including interest
            // P = the principal investment amount (initial investment)
            // r = the annual interest rate (as a decimal)
            // n = the number of times that interest is compounded per unit year
            // t = the number of units the money is invested or borrowed for (investment period in years)

            // Assuming interest is compounded annually (n = 1)
            double compoundInterest = initialInvestment * Math.Pow(1 + (investmentRate / 100), investmentPeriodInYears);

            return (decimal)compoundInterest;
        }
    }
}
