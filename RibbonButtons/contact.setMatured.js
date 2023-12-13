var Contact2 = window.Contact2 || {};
(function () {

  const notification1 = this.generateUniqueNumber;
  const notification2 = this.generateUniqueNumber;
  const notification3 = this.generateUniqueNumber;
  const statusCodes = {
    ACTIVE: 0,
    INACTIVE: 1,
  };

  const statusReason = {
    MATURED: 2,
  };

  const logicalNames = {
    STATUS: "statecode",
    STATUS_REASON: "statuscode",
    JOIN_DATE: "ss_joiningdate",
    MATURED_DATE: "ss_maturitydate",
  };

  this.setMaturedButton = (formContext) => {
    // get values
    const today = new Date();
    const statusCodeAttribute = formContext.getAttribute(logicalNames.STATUS);
    const statusCodeValue = statusCodeAttribute.getValue();
    const statusReasonAttribute = formContext.getAttribute(logicalNames.STATUS_REASON);
    const maturityDateAttribute = formContext.getAttribute(logicalNames.MATURED_DATE);
    const maturityDateValue = new Date(maturityDateAttribute.getValue());

    // check if investment has matured and if status is active
    if (today >= maturityDateValue) {
      if (statusCodeValue === statusCodes.ACTIVE) {
        // set status and status reason
        statusCodeAttribute.setValue(statusCodes.INACTIVE);
        statusReasonAttribute.setValue(statusReason.MATURED);
        formContext.data.refresh(true);
      } else {
        formContext.ui.setFormNotification("This investment is inactive", "WARNING", notification1);
        // Wait for 5 seconds before clearing the notification
        window.setTimeout(function () {
          formContext.ui.clearFormNotification(notification1);
        }, 10 * 1000);
      }
    } else {
      formContext.ui.setFormNotification("This investment has not matured yet", "INFO", notification2);
      // Wait for 5 seconds before clearing the notification
      window.setTimeout(function () {
        formContext.ui.clearFormNotification(notification2);
      }, 10 * 1000);
    }
  };

  this.enabledRule = (formContext) => {
    const today = new Date();
    const maturityDateAttribute = formContext.getAttribute(logicalNames.MATURED_DATE);
    const maturityDateValue = new Date(maturityDateAttribute.getValue());
    const statusCodeAttribute = formContext.getAttribute(logicalNames.STATUS);
    const statusCodeValue = statusCodeAttribute.getValue();
    const setVisible = maturityDateAttribute.getValue() && today >= maturityDateValue && statusCodeValue === statusCodes.ACTIVE;
    if (setVisible) {
      formContext.ui.setFormNotification("This investment can be matured", "INFO", notification3);
      window.setTimeout(function () {
        formContext.ui.clearFormNotification(notification3);
      }, 10 * 1000);
    }
    return setVisible;
  };

  this.generateUniqueNumber = () => {
    var timestamp = new Date().getTime();
    var randomNumber = Math.floor(Math.random() * 1000) + 1;
    var uniqueNumber = timestamp + randomNumber;
    return uniqueNumber;
}
}).call(Contact2);
