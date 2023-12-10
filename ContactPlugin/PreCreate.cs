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

                // Obtain the IOrganizationService instance which you will need for  
                // web service calls.  
                IOrganizationServiceFactory serviceFactory =
                    (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
                if (entity.LogicalName != "contact" || !entity.Attributes.Contains("birthdate"))
                {
                    tracingService.Trace("Contact Pre-create: The required entity or fields where not found");
                    return;
                }

                try
                {
                    // Plug-in business logic goes 
                    if (entity.LogicalName == "contact")
                    {
                        // Calculate the age of the client
                        DateTime birthdate = entity.GetAttributeValue<DateTime>("birthdate");
                        DateTime today = new DateTime();
                        today = DateTime.Now;
                        int age = today.Year - birthdate.Year;
                        entity["ss_age"] = age;

                        // Calculate Maturity Date based on the Investment Period
                        DateTime joinDate = entity.GetAttributeValue<DateTime>("ss_joiningdate");
                        int investmentPeriod = entity.GetAttributeValue<int>("ss_investmentperiodmonths");
                        DateTime maturityDate = joinDate.AddMonths(investmentPeriod);
                        tracingService.Trace("FollowupPlugin: Successfully {0}", maturityDate.Date.ToString());
                        entity["ss_maturitydate"] = maturityDate.Date;

                        // Calculate the Estimated Return
                        double initialInvestment = (double)entity.GetAttributeValue<Money>("ss_initialinvestment").Value;
                        double investmentRate = (double)entity.GetAttributeValue<decimal>("ss_interestrate");
                        decimal estimatedReturn = CalculateEstimatedReturn(initialInvestment, investmentRate, investmentPeriod);
                        entity["ss_estimatedreturn"] = new Money(estimatedReturn);

                        // Auto set Status Reason to “In - Force”
                        entity["statuscode"] = 1;
                        tracingService.Trace("FollowupPlugin: Successfully");
                    }
                }

                catch (InvalidPluginExecutionException ex)
                {
                    tracingService.Trace("Contact Pre-reate plugin: {0}", ex.ToString());
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
