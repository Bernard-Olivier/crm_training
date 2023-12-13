using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.CodeDom;
using System.IdentityModel.Metadata;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Services;
using System.Web.UI.WebControls;

namespace ContactPlugin
{
    public class ContactPostUpdate : IPlugin
    {
        private const string EMAIL_TEMPLATE_NAME = "updatedContactEmail";
        private const string TEMPLATE_ENTITY = "template";
        private const string EMAIL_ENTITY = "email";
        private const string PRE_IMAGE_NAME = "preImage";
        private const string POST_IMAGE_NAME = "postImage";

        private string[] templateVariables = {
            ContactFields.INVESTMENT_PERIOD, ContactFields.INITIAL_INVESTMENT, ContactFields.INVESTMENT_RATE,
            ContactFields.NAME, ContactFields.SURNAME, ContactFields.CORPORATE_CLIENT_NAME, ContactFields.FULLNAME
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
                if (entity.LogicalName != "contact")
                {
                    tracingService.Trace("Contact PostUpdate: The required entity was not found");
                    return;
                }

                try
                {
                    // Check if Investment period has changed
                    //Entity postImage = context.PostEntityImages[POST_IMAGE_NAME];
                    //if (entity.Contains(ContactFields.INVESTMENT_PERIOD))
                    //{
                    //    Entity contact = service.Retrieve("contact", entity.Id, new ColumnSet(true));
                    //    DateTime joinDate = contact.GetAttributeValue<DateTime>(ContactFields.JOINING_DATE);
                    //    int investmentPeriod = entity.GetAttributeValue<int>(ContactFields.INVESTMENT_PERIOD);
                    //    DateTime maturityDate = joinDate.AddMonths(investmentPeriod);
                    //    contact[ContactFields.MATURITY_DATE] = maturityDate.Date;
                    //    tracingService.Trace("Fired");
                    //    service.Update(contact);
                    //}

                    // Check if Initial Investment, Intrest Rate or Investment Period have changed
                    bool sendEmail = entity.Contains(ContactFields.INVESTMENT_RATE)
                        || entity.Contains(ContactFields.INVESTMENT_PERIOD)
                        || entity.Contains(ContactFields.INITIAL_INVESTMENT);
                    if (!sendEmail)
                    {
                        tracingService.Trace("Contact PostUpdate: There were no important changes");
                        return;
                    }
                    // Get template and replace variables
                    Guid? emailTemplateId = GetTemplateId(EMAIL_TEMPLATE_NAME, service);
                    if (emailTemplateId == null)
                    {
                        tracingService.Trace($"Contact PostUpdate: {EMAIL_TEMPLATE_NAME} tempalate does not exist");
                    }
                    ColumnSet columns = new ColumnSet("body");
                    Entity emailTemplate = service.Retrieve(TEMPLATE_ENTITY, (Guid)emailTemplateId, columns);
                    string templateBody = this.ReplaceTemplateVariables(emailTemplate, context, service);
                    Guid emailId = CreateEmailEntity(context, service, entity, templateBody);
                    SendEmailRequest sendEmailRequest = new SendEmailRequest
                    {
                        EmailId = emailId,
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

        private string ReplaceTemplateVariables(Entity emailTemplate, IPluginExecutionContext context, IOrganizationService service)
        {
            Entity preImage = context.PreEntityImages[PRE_IMAGE_NAME];
            Entity postImage = context.PostEntityImages[POST_IMAGE_NAME];
            // Replace post image template variables
            string templateBody = emailTemplate.GetAttributeValue<string>("body");
            foreach (string key in templateVariables)
            {
                string variable = String.Concat("{{", key, "}}");
                if (postImage.Contains(key))
                {
                    string value;
                    if (postImage[key] is Money)
                    {
                        EntityReference currencyRef = postImage.GetAttributeValue<EntityReference>("transactioncurrencyid");
                        Entity currency = service.Retrieve("transactioncurrency", currencyRef.Id, new ColumnSet(true));
                        string currencySymbol = currency.GetAttributeValue<string>("currencysymbol");
                        Money moneyValue = postImage.GetAttributeValue<Money>(key);
                        value = currencySymbol + " " + Math.Round(moneyValue.Value, 2).ToString();
                    }
                    else if (postImage[key] is decimal)
                    {
                        decimal decimalValue = postImage.GetAttributeValue<decimal>(key);
                        value = Math.Round(decimalValue, 2).ToString();
                    }
                    else
                    {
                        value = postImage[key].ToString();
                    }
                    templateBody = templateBody.Replace(variable, value);
                }
                else
                {
                    templateBody = templateBody.Replace(variable, "");
                }
            }
            // replace pre image template variables
            foreach (string key in templateVariables)
            {
                string variable = String.Concat("{{pre", key, "}}");
                if (preImage.Contains(key))
                {
                    string value;
                    if (preImage[key] is Money)
                    {
                        EntityReference currencyRef = preImage.GetAttributeValue<EntityReference>("transactioncurrencyid");
                        Entity currency = service.Retrieve("transactioncurrency", currencyRef.Id, new ColumnSet(true));
                        string currencySymbol = currency.GetAttributeValue<string>("currencysymbol");
                        Money moneyValue = preImage.GetAttributeValue<Money>(key);
                        value = currencySymbol + " " + Math.Round(moneyValue.Value, 2).ToString();
                    }
                    else if (preImage[key] is decimal)
                    {
                        decimal decimalValue = preImage.GetAttributeValue<decimal>(key);
                        value = Math.Round(decimalValue, 2).ToString();
                    }
                    else
                    {
                        value = preImage[key].ToString();
                    }
                    templateBody = templateBody.Replace(variable, value);
                }
                else
                {
                    templateBody = templateBody.Replace(variable, "");
                }
            }
            return templateBody;
        }

        private Guid CreateEmailEntity(IPluginExecutionContext context, IOrganizationService service, Entity entity, string body)
        {
            Entity fromparty = new Entity("activityparty");
            Entity toparty = new Entity("activityparty");
            fromparty["partyid"] = new EntityReference("systemuser", context.UserId);
            toparty["partyid"] = new EntityReference("contact", entity.Id);
            Entity email = new Entity(EMAIL_ENTITY);
            email["from"] = new Entity[] { fromparty };
            email["to"] = new Entity[] { toparty };
            email["subject"] = "Your contact record has been updated";
            email["description"] = body;
            email["directioncode"] = true;
            Guid emailid = service.Create(email);
            return emailid;
        }
    }

}
