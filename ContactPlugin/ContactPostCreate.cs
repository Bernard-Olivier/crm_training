using System;
using Microsoft.Xrm.Sdk;

namespace ContactPlugin
{
    public class ContactPostCreate : IPlugin
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

                if (entity.LogicalName != "contact" || !context.OutputParameters.Contains("id"))
                {
                    tracingService.Trace("Contact PostCreate: The required entity was not found");
                    return;
                }

                try
                {
                    Entity followup = new Entity("task");
                    Guid regardingobjectid = new Guid(context.OutputParameters["id"].ToString());
                    string regardingobjectidType = "contact";
                    followup["subject"] = $"Setting up a follow-up meeting with the new client";
                    followup["description"] =
                        "Setting up a follow-up meeting with the new client";
                    followup["scheduledstart"] = DateTime.Now.AddDays(7);
                    followup["scheduledend"] = DateTime.Now.AddDays(7);
                    followup["category"] = context.PrimaryEntityName;
                    followup["regardingobjectid"] =
                        new EntityReference(regardingobjectidType, regardingobjectid);
                    service.Create(followup);
                    tracingService.Trace("Contact PostCreate plugin: Successfully");
                }

                catch (InvalidPluginExecutionException ex)
                {
                    tracingService.Trace("Contact PostCreate plugin: {0}", ex.ToString());
                    throw new InvalidPluginExecutionException("An error occurred in Contact PostCreate plugin", ex);
                }
            }
        }
    }
}
