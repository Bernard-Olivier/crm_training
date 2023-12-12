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
        private const string TEMPLATE_ENTITY = "template";
        private const string EMAIL_ENTITY = "email";
        private const string PRE_IMAGE_NAME = "preImage";
        private string[] templateVariables = {
            Contact.INVESTMENT_PERIOD, Contact.INITIAL_INVESTMENT, Contact.INVESTMENT_RATE,
            Contact.NAME, Contact.SURNAME, Contact.CORPORATE_CLIENT_NAME
        };


        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService =
            (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Obtain the IOrganizationService instance which you will need for  
            // web service calls.  
            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity entity = (Entity)context.InputParameters["Target"];

                // Check if the form entity is correct
                if (entity.LogicalName != "contact")
                {
                    tracingService.Trace("Contact PostUpdate: The required entity was not found");
                    return;
                }

                try
                {
                    // Check if Initial Investment, Intrest Rate or Investment Period have changed
                    Entity preImage = context.PreEntityImages[PRE_IMAGE_NAME];
                    bool hasInvestmentRateChanged = preImage.Contains(Contact.INVESTMENT_RATE)
                        || preImage[Contact.INVESTMENT_RATE] != entity[Contact.INVESTMENT_RATE];
                    bool hasInvestmentPeriodChanged = preImage.Contains(Contact.INVESTMENT_PERIOD)
                         || preImage[Contact.INVESTMENT_PERIOD] != entity[Contact.INVESTMENT_PERIOD];
                    bool hasInitialInvestmentChanged = preImage.Contains(Contact.INITIAL_INVESTMENT)
                         || preImage[Contact.INITIAL_INVESTMENT] != entity[Contact.INITIAL_INVESTMENT];
                    bool sendEmail = hasInvestmentRateChanged || hasInvestmentPeriodChanged || hasInitialInvestmentChanged;
                    if (!sendEmail)
                    {
                        return;
                    }

                    // Check if template id exists
                    Guid? emailTemplateId = GetTemplateId(EMAIL_TEMPLATE_NAME, service);
                    if (emailTemplateId == null)
                    {
                        tracingService.Trace($"Contact PostUpdate: {EMAIL_TEMPLATE_NAME} tempalate does not exist");
                    }

                    // Replace template variables
                    ColumnSet columns = new ColumnSet("body");
                    Entity emailTemplate = service.Retrieve(TEMPLATE_ENTITY, (Guid)emailTemplateId, columns);
                    string templateBody = emailTemplate.GetAttributeValue<string>("body");
                    foreach (string key in templateVariables)
                    {
                        tracingService.Trace("var: {0}", key);
                        string variable = String.Concat("{{", key, "}}");
                        if (entity.Contains(key))
                        { 
                            string value;
                            if (entity[key] is Money)
                            {
                                Money moneyValue = entity.GetAttributeValue<Money>(key);
                                value = moneyValue.Value.ToString();
                            }
                            else
                            {
                                value = entity[key].ToString();
                            }
                            // tracingService.Trace($"{(templateBody.Contains(variable) ? "Replacing value" : "Not replacing")}");
                            templateBody = templateBody.Replace(variable, value);
                            tracingService.Trace("var and value: {0} & {1}", variable, value);
                        }
                    }
                    // replace preimage variables
                    foreach (string key in templateVariables)
                    {
                        tracingService.Trace("pre var: {0}", key);
                        string variable = String.Concat("{{pre", key, "}}");
                        if (preImage.Contains(key))
                        {
                            string value;
                            if (preImage[key] is Money)
                            {
                                Money moneyValue = preImage.GetAttributeValue<Money>(key);
                                value = moneyValue.Value.ToString();
                            }
                            else
                            {
                                value = preImage[key].ToString();
                            }
                            templateBody = templateBody.Replace(variable, value);
                            tracingService.Trace("pre var and value: {0} & {1}", variable, value);
                        }
                    }
                    // Get user and client entities
                    Entity fromparty = new Entity("activityparty");
                    Entity toparty = new Entity("activityparty");
                    fromparty["partyid"] = new EntityReference("systemuser", context.UserId);
                    toparty["partyid"] = new EntityReference("contact", entity.Id);
                    // Create email entity
                    Entity email = new Entity(EMAIL_ENTITY);
                    email["from"] = new Entity[] { fromparty };
                    email["to"] = new Entity[] { toparty };
                    email["subject"] = "Your contact record has been updated";
                    email["description"] = templateBody;
                    email["directioncode"] = true;
                    Guid emailid = service.Create(email);
                    // Create Email Request
                    SendEmailRequest sendEmailRequest = new SendEmailRequest
                    {
                        EmailId = emailid,
                        TrackingToken = "",
                        IssueSend = true
                    };
                    // Send email request
                    service.Execute(sendEmailRequest);
                    tracingService.Trace("Contact PostUpdate plugin: Successfully");
                }
                catch (Exception ex)
                {
                    tracingService.Trace("Contact PostUpdate plugin: {0}", ex.ToString());
                    throw new InvalidPluginExecutionException("An error occurred in Contact PostCreate plugin", ex);
                }
            }
        }


        private Guid? GetTemplateId(string templateName, IOrganizationService service)
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
