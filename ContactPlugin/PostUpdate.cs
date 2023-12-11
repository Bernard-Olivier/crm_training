using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.CodeDom;
using System.Linq;
using System.Runtime.Remoting.Services;

namespace ContactPlugin
{
    public class PostUpdate : IPlugin
    {
        private const string EMAIL_TEMPLATE_NAME = "updatedContactEmail";
        private const string CONTACT_ENTITY = "contact";
        private const string TEMPLATE_ENTITY = "template";
        private const string EMAIL_ENTITY = "email";

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
                // Obtain the IOrganizationService instance which you will need for  
                // web service calls.  
                IOrganizationServiceFactory serviceFactory =
                    (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                // Obtain the target entity from the input parameters.  
                Entity entity = (Entity)context.InputParameters["Target"];

                // Check if the form entity is correct
                if (entity.LogicalName != "contact")
                {
                    tracingService.Trace("Contact PostUpdate: The required entity was not found");
                    return;
                }

                // Check if Initial Investment, Intrest Rate or Investment Period have changed
                Entity preImage = context.PreEntityImages["preImage"];
                bool hasInvestmentRateChanged = preImage.Contains(Contact.INVESTMENT_RATE)
                    || preImage[Contact.INVESTMENT_RATE] != entity[Contact.INVESTMENT_RATE];
                bool hasInvestmentPeriodChanged = preImage.Contains(Contact.INVESTMENT_PERIOD)
                     || preImage[Contact.INVESTMENT_PERIOD] != entity[Contact.INVESTMENT_PERIOD];
                bool hasInitialInvestmentChanged = preImage.Contains(Contact.INITIAL_INVESTMENT)
                     || preImage[Contact.INITIAL_INVESTMENT] != entity[Contact.INITIAL_INVESTMENT];
                bool sendEmail = hasInvestmentRateChanged || hasInvestmentPeriodChanged || hasInitialInvestmentChanged;
                if (!sendEmail)
                {
                    tracingService.Trace("Contact PostUpdate: No important changes occurred in this update");
                    return;
                }

                
                // Check if template id exists
                Guid? emailTemplateId = GetTemplate(EMAIL_TEMPLATE_NAME, service);
                if (emailTemplateId == null)
                {
                    tracingService.Trace("Contact PostUpdate: {0} tempalate does not exist", EMAIL_TEMPLATE_NAME);
                    return;
                }

                try
                {
                    // Get user and client entities

                    Entity fromparty = new Entity("activityparty");
                    Entity toparty = new Entity("activityparty");
                    fromparty["partyid"] = new EntityReference("systemuser", context.UserId);
                    EntityReference clientEntityReference = new EntityReference("contact", entity.Id);
                    toparty["partyid"] = clientEntityReference;

                    Entity email = new Entity(EMAIL_ENTITY);
                    email["from"] = new Entity[] { fromparty };
                    email["to"] = new Entity[] { toparty };
                    email["subject"] = "Your contact records has been updated";
                    email["description"] = "Body of the email";
                    email["templateid"] = new EntityReference(TEMPLATE_ENTITY, (Guid)emailTemplateId);
                    email["directioncode"] = true;
                    // Refer to the contact in the task activity
                    email["regardingobjectid"] = clientEntityReference;

                    tracingService.Trace("Flag 1");
                    SendEmailRequest sendEmailRequest = new SendEmailRequest
                    {
                        EmailId = service.Create(email),
                        TrackingToken = "",
                        IssueSend = true
                    };
                    // Create the task in Microsoft Dynamics CRM
                    tracingService.Trace("Flag 2");
                    SendEmailResponse emailResponse = (SendEmailResponse)service.Execute(sendEmailRequest);
                    tracingService.Trace("Contact PostUpdate plugin: Successfully {0}", emailResponse.ResponseName);
                }

                catch (InvalidPluginExecutionException ex)
                {
                    tracingService.Trace("Contact PostUpdate plugin: {0}", ex.ToString());
                    throw new InvalidPluginExecutionException("An error occurred in Contact PostCreate plugin", ex);
                }
            }


        }

        private Guid? GetTemplate(string templateName, IOrganizationService service)
        {
            // Create a query to check if the template with the specified ID exists.
            QueryExpression query = new QueryExpression("template")
            {
                ColumnSet = new ColumnSet(false), // Set to false to retrieve all columns
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("title", ConditionOperator.Equal, templateName)
                    }
                }
            };

            // Execute the query.
            EntityCollection results = service.RetrieveMultiple(query);

            // Check if any templates were found
            if (results.Entities.Count < 1)
                return null;
            return results.Entities.First().Id;
        }

    }

}
