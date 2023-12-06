// A namespace defined for the sample code
// As a best practice, you should always define
// a unique namespace for your libraries
const Example = window.Example || {};
(function () {
  // Define some global variables
  const myUniqueId = "_myUniqueId"; // Define an ID for the notification
  const currentUserName = Xrm.Utility.getGlobalContext().userSettings.userName; // get current user name
  const message = currentUserName + ": Your JavaScript code in action!";
  const typesOfClients = {
    CORPORATE: "1",
    INDIVIDUAL: "2",
  };
  const contactMethods = {
    EMAIL: "2",
    MOBILE_PHONE: "3"
  }
  const logicalNames = {
    PREFERRED_CONTACT_METHOD: "preferredcontactmethodcode",
    EMAIL: "emailaddress1",
    MOBILE_PHONE: "mobilephone",
    TYPE_OF_CLIENT: "ss_typeofclient",
    LAST_NAME: "fullname_compositionLinkControl_lastname",
    FIRST_NAME: "fullname_compositionLinkControl_firstname",
    CORPORATE_CLIENT_NAME: "ss_corporateclientname"
  }
  // Code to run in the form OnLoad event
  this.formOnLoad = function (executionContext) {
    const formContext = executionContext.getFormContext();

    // Display the form level notification as an INFO
    formContext.ui.setFormNotification(message, "INFO", myUniqueId);

    // Wait for 5 seconds before clearing the notification
    window.setTimeout(function () {
      formContext.ui.clearFormNotification(myUniqueId);
    }, 5000);

    // Changes accorrding to Type of Client
    this.setVisibilityOfClientName(formContext);
    this.setDisabledOfClientName(formContext);
  };

  // Code to run in the column OnChange event
  this.attributeOnChange = function (executionContext) {
    const formContext = executionContext.getFormContext();
    // Business Rules
    // Changes accorrding to Preferred Method of Contact
    const preferredContactMethod = formContext.getAttribute(logicalNames.PREFERRED_CONTACT_METHOD);
    if (preferredContactMethod.getValue() == contactMethods.EMAIL) {
      formContext.getAttribute(logicalNames.EMAIL).setRequiredLevel("required");
      formContext.getAttribute(logicalNames.MOBILE_PHONE).setRequiredLevel("none");
    } else if (preferredContactMethod.getValue() == contactMethods.MOBILE_PHONE) {
      formContext.getAttribute(logicalNames.MOBILE_PHONE).setRequiredLevel("required");
      formContext.getAttribute(logicalNames.EMAIL).setRequiredLevel("none");
    }

    // Changes accorrding to Type of Client
    this.setVisibilityOfClientName(formContext);
  };

  // Code to run in the form OnSave event
  this.formOnSave = function (executionContext) {
    const formContext = executionContext.getFormContext();
    // Disable fields on save
    this.setDisabledOfClientName(formContext);

    // Display an alert dialog
    Xrm.Navigation.openAlertDialog({ text: "Record saved." });
  };

  /**
   *  Hides either the Individual or Corporate Client Name depending on the Type of Client
   *
   * @param {any} formContext - formContext
   * @returns {void} void
   */
  this.setVisibilityOfClientName = (formContext) => {
    const typeOfClientAttribute = formContext.getAttribute(logicalNames.TYPE_OF_CLIENT);
    if (typeOfClientAttribute.getValue() == typesOfClients.CORPORATE) {
      const lastNameControl = formContext.getControl(logicalNames.LAST_NAME);
      lastNameControl.setVisible(false);
      const firstNameControl = formContext.getControl(logicalNames.FIRST_NAME);
      firstNameControl.setVisible(false);
      const corporateClientNameControl = formContext.getControl(logicalNames.CORPORATE_CLIENT_NAME);
      corporateClientNameControl.setVisible(true);
    } else if (typeOfClientAttribute.getValue() == typesOfClients.INDIVIDUAL) {
      const corporateClientNameControl = formContext.getControl(logicalNames.CORPORATE_CLIENT_NAME);
      corporateClientNameControl.setVisible(false);
      const lastNameControl = formContext.getControl(logicalNames.LAST_NAME);
      lastNameControl.setVisible(true);
      const firstNameControl = formContext.getControl(logicalNames.FIRST_NAME);
      firstNameControl.setVisible(true);
    } else {
      const corporateClientNameControl = formContext.getControl(logicalNames.CORPORATE_CLIENT_NAME);
      corporateClientNameControl.setVisible(true);
      const lastNameControl = formContext.getControl(logicalNames.LAST_NAME);
      lastNameControl.setVisible(true);
      const firstNameControl = formContext.getControl(logicalNames.FIRST_NAME);
      firstNameControl.setVisible(true);
    }
  };

  /**
   *  Disables either the Individual or Corporate Client Name depending on the Type of Client
   *
   * @param {any} formContext - formContext
   * @returns {void} void
   */
  this.setDisabledOfClientName = (formContext) => {
    const typeOfClientAttribute = formContext.getAttribute(logicalNames.TYPE_OF_CLIENT);
    const lastNameControl = formContext.getControl(logicalNames.LAST_NAME);
    const firstNameControl = formContext.getControl(logicalNames.FIRST_NAME);
    const CorporateClientNameControl = formContext.getControl(logicalNames.CORPORATE_CLIENT_NAME);

    if (typeOfClientAttribute.getValue() != null) {
      const enableCorporateClientName =
        CorporateClientNameControl.getVisible() &&
        typeOfClientAttribute.getValue() == typesOfClients.CORPORATE;
      if (enableCorporateClientName) {
        CorporateClientNameControl.setDisabled(false);
      } else {
        CorporateClientNameControl.setDisabled(true);
      }

      const enableFirstAndLastNames =
        firstNameControl.getVisible() &&
        lastNameControl.getVisible() &&
        typeOfClientAttribute.getValue() == typesOfClients.INDIVIDUAL;
      if (enableFirstAndLastNames) {
        firstNameControl.setDisabled(false);
        lastNameControl.setDisabled(false);
      } else {
        firstNameControl.setDisabled(true);
        lastNameControl.setDisabled(true);
      }
    }
  };
}).call(Example);
